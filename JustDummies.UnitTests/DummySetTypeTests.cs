#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnySetTypeTests {

    private const int SampleCount = 200;

    private enum OrderStatus {

        Draft,
        Validated,
        Cancelled

    }

    [Fact(DisplayName = "Boolean: unconstrained draws hit both values; pins pin; contradictory pins conflict.")]
    public void BooleanBehaves() {
        HashSet<bool> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Dummy.Boolean().Generate()); }
        Check.That(seen.Count).IsEqualTo(2);

        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Boolean().True().Generate()).IsTrue();
            Check.That(Dummy.Boolean().False().Generate()).IsFalse();
            Check.That(Dummy.Boolean().DifferentFrom(true).Generate()).IsFalse();
        }

        Check.ThatCode(() => Dummy.Boolean().True().False())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("False()", "True()");

        bool value = Dummy.Boolean().True().Generate();
        Check.That(value).IsTrue();
    }

    [Fact(DisplayName = "Guid: unconstrained draws are non-empty, varied, and reproducible under a context seed.")]
    public void GuidBehaves() {
        HashSet<Guid> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            Guid value = Dummy.Guid().Generate();
            seen.Add(value);
            Check.That(value).IsNotEqualTo(Guid.Empty);
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Dummy.WithSeed(42).Guid().Generate()).IsEqualTo(Dummy.WithSeed(42).Guid().Generate());
    }

    [Fact(DisplayName = "Guid: Empty pins, NonEmpty excludes, and the pair conflicts in both orders.")]
    public void GuidEmptyFamily() {
        Check.That(Dummy.Guid().Empty().Generate()).IsEqualTo(Guid.Empty);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Guid().NonEmpty().Generate()).IsNotEqualTo(Guid.Empty);
        }

        Check.ThatCode(() => Dummy.Guid().Empty().NonEmpty()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Guid().NonEmpty().Empty()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Guid: OneOf stays within, exhausting it conflicts, DifferentFrom never yields the value.")]
    public void GuidSets() {
        Guid first  = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        for (int i = 0; i < SampleCount; i++) {
            Guid value = Dummy.Guid().OneOf(first, second).Generate();
            Check.That(value == first || value == second).IsTrue();
            Check.That(Dummy.Guid().OneOf(first, second).DifferentFrom(first).Generate()).IsEqualTo(second);
        }

        Check.ThatCode(() => Dummy.Guid().OneOf(first).Except(first)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Guid: excluding all 256 last-byte variants of the drawn prefix escapes by carry, never hangs, and stays reproducible.")]
    public async Task GuidExclusionByteWraparoundTerminates() {
        const int seed = 20260718;

        // The first unconstrained draw under this seed fixes the 15-byte prefix the escape starts from; a
        // second context with the same seed replays that same first draw, since Except() consumes no randomness.
        Guid   drawn  = Dummy.WithSeed(seed).Guid().Generate();
        byte[] prefix = drawn.ToByteArray();

        // Every identifier sharing that prefix and differing only in the last byte — the exact block the former
        // last-byte-only walk cycled inside forever.
        Guid[] block = new Guid[256];
        for (int last = 0; last < 256; last++) {
            byte[] variant = (byte[])prefix.Clone();
            variant[15] = (byte)last;
            block[last]  = new Guid(variant);
        }

        // Generate off-thread and race a deadline: a regression that reintroduces the unbounded loop loses the
        // race and fails the test instead of hanging the whole suite.
        Task<Guid> run   = Task.Run(() => Dummy.WithSeed(seed).Guid().Except(block).Generate());
        Task       first = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Check.That(first == run).IsTrue();

        Guid escaped = await run;
        Check.That(block.Contains(escaped)).IsFalse();
        Check.That(escaped).IsNotEqualTo(drawn);

        // Same seed and same exclusions yield the same escaped identifier.
        Guid again = Dummy.WithSeed(seed).Guid().Except(block).Generate();
        Check.That(again).IsEqualTo(escaped);
    }

    [Fact(DisplayName = "Enum: unconstrained draws yield only declared members and reach all of them.")]
    [SuppressMessage(NetAnalyzersRule.CA2263.Category, NetAnalyzersRule.CA2263.Id, Justification = SuppressionJustification.CA2263.NoGenericIsDefinedDownlevel)]
    public void EnumDrawsDeclaredMembers() {
        HashSet<OrderStatus> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            OrderStatus value = Dummy.Enum<OrderStatus>().Generate();
            seen.Add(value);
            // The non-generic overload on purpose: this suite also runs on the .NET Framework 4.7.2 support
            // floor (ADR-0007, build/Net472TestFloor.props), where Enum.IsDefined<TEnum>(TEnum) does not exist.
            // CA2263 suggests the generic one and is right on net10.0 only, so it is answered here rather than
            // taken — the same downlevel trap as string.Contains(char) elsewhere in this repository.
            Check.That(System.Enum.IsDefined(typeof(OrderStatus), value)).IsTrue();
        }
        Check.That(seen.Count).IsEqualTo(3);
    }

    [Fact(DisplayName = "Enum: OneOf restricts, Except removes, exhausting the pool conflicts.")]
    public void EnumSets() {
        for (int i = 0; i < SampleCount; i++) {
            OrderStatus restricted = Dummy.Enum<OrderStatus>().OneOf(OrderStatus.Draft, OrderStatus.Validated).Generate();
            Check.That(restricted == OrderStatus.Draft || restricted == OrderStatus.Validated).IsTrue();
            Check.That(Dummy.Enum<OrderStatus>().Except(OrderStatus.Cancelled).Generate()).IsNotEqualTo(OrderStatus.Cancelled);
            Check.That(Dummy.Enum<OrderStatus>().OneOf(OrderStatus.Draft, OrderStatus.Validated).DifferentFrom(OrderStatus.Draft).Generate()).IsEqualTo(OrderStatus.Validated);
        }

        Check.ThatCode(() => Dummy.Enum<OrderStatus>().Except(OrderStatus.Draft, OrderStatus.Validated, OrderStatus.Cancelled).Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Except(");
    }

    [Fact(DisplayName = "Enum: OneOf rejects undeclared numeric values — the declared-members-only contract holds.")]
    public void EnumOneOfRejectsUndeclaredValues() {
        ArgumentException rejected = Assert.Throws<ArgumentException>(
            () => Dummy.Enum<OrderStatus>().OneOf((OrderStatus)42));
        Check.That(rejected.Message).Contains("42");
        Check.That(rejected.Message).Contains("OrderStatus");

        Check.ThatCode(() => Dummy.Enum<OrderStatus>().OneOf(OrderStatus.Draft, (OrderStatus)42)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Char: the default pool is the whole of ASCII; families narrow it.")]
    public void CharPools() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(CharacterPools.IsAscii(Dummy.Char().Generate())).IsTrue();
            Check.That(Dummy.Char().Numeric().Generate() is >= '0' and <= '9').IsTrue();
            Check.That(Dummy.Char().Alpha().Generate() is >= 'A' and <= 'Z' or >= 'a' and <= 'z').IsTrue();
            Check.That(Dummy.Char().InLowerCase().Generate() is >= 'A' and <= 'Z').IsFalse();
            Check.That(Dummy.Char().Alpha().InUpperCase().Generate() is >= 'A' and <= 'Z').IsTrue();
        }
    }

    [Fact(DisplayName = "Char: OneOf restricts, exclusions apply, and contradictions conflict.")]
    public void CharSets() {
        for (int i = 0; i < SampleCount; i++) {
            char value = Dummy.Char().OneOf('a', 'b').Generate();
            Check.That(value == 'a' || value == 'b').IsTrue();
            Check.That(Dummy.Char().OneOf('a', 'b').DifferentFrom('a').Generate()).IsEqualTo('b');
        }

        Check.ThatCode(() => Dummy.Char().Numeric().Alpha()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Char().OneOf('a').Except('a')).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Char().OneOf('a').Numeric()).Throws<ConflictingDummyConstraintException>();
    }

    // Pinned through the pool inspection rather than by sampling: declared beside a value set the family narrows
    // it, so the survivors are exactly the characters the family admits — the whole membership, in one
    // deterministic assertion, with no draw and no coupon-collecting.
    [Fact(DisplayName = "Char: Punctuation admits the 32 printable non-alphanumerics, the space excluded.")]
    public void CharPunctuationAdmitsThePrintableNonAlphaNumerics() {
        IPoolInspection<char> inspection = Dummy.Char().OneOf(PrintableAscii()).Punctuation();

        Check.That(inspection.GetSurvivors()).ContainsExactly("!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".ToCharArray());
    }

    [Fact(DisplayName = "Char: Printable admits every character from the space to '~', the space included.")]
    public void CharPrintableAdmitsEveryPrintableAsciiCharacter() {
        IPoolInspection<char> inspection = Dummy.Char().OneOf(PrintableAscii()).Printable();

        Check.That(inspection.GetSurvivors()).ContainsExactly(PrintableAscii());
    }

    [Fact(DisplayName = "Char: the wider families draw within themselves and still answer to casing.")]
    public void CharWiderFamiliesDrawWithinThemselves() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(CharacterPools.IsAsciiPunctuation(Dummy.Char().Punctuation().Generate())).IsTrue();
            Check.That(CharacterPools.IsAsciiPrintable(Dummy.Char().Printable().Generate())).IsTrue();
            Check.That(Dummy.Char().Printable().InLowerCase().Generate() is >= 'A' and <= 'Z').IsFalse();
        }

        // A wider family is still a family: it occupies the one charset slot, so a second one contradicts it, and
        // a value set it admits nothing of is emptied at declaration.
        Check.ThatCode(() => Dummy.Char().Punctuation().Alpha()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Char().Printable().Numeric()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Char().Punctuation().OneOf('a')).Throws<ConflictingDummyConstraintException>();
    }

    /// <summary>Every printable ASCII character, in code-point order — the widest pool a family can admit.</summary>
    private static char[] PrintableAscii() {
        char[] characters = new char['~' - ' ' + 1];
        for (int index = 0; index < characters.Length; index++) { characters[index] = (char)(' ' + index); }

        return characters;
    }

}
