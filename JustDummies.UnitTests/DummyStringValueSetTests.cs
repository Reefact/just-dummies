#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The value-set half of <see cref="DummyString" />: <c>OneOf(...)</c> and how it composes with every other string
///     constraint. Split from <see cref="AnyStringTests" />, which owns the constructive shape.
/// </summary>
[TestSubject(typeof(DummyString))]
public sealed class AnyStringValueSetTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<string> Samples(IDummy<string> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    /// <summary>
    ///     Asserts that <paramref name="generator" /> has been narrowed to exactly one value — every draw, not one.
    ///     A constraint that narrows a two-value set is only proven by the draws it makes impossible: checking a
    ///     single draw would still pass half the time with the narrowing gone.
    /// </summary>
    private static void NarrowsTo(string expected, IDummy<string> generator) {
        foreach (string value in Samples(generator)) {
            Check.That(value).IsEqualTo(expected);
        }
    }

    #endregion

    [Fact(DisplayName = "OneOf draws only the supplied values.")]
    public void DrawsOnlyTheSuppliedValues() {
        string[] allowed = ["Apple", "Microsoft", "Google"];
        foreach (string value in Samples(Dummy.String().OneOf(allowed))) {
            Check.That(allowed.Contains(value)).IsTrue();
        }
    }

    [Fact(DisplayName = "OneOf eventually reaches every supplied value.")]
    public void ReachesEverySuppliedValue() {
        HashSet<string> seen = [.. Samples(Dummy.String().OneOf("EUR", "USD", "GBP"))];

        Check.That(seen).Contains("EUR", "USD", "GBP");
    }

    [Fact(DisplayName = "A single value pins the generated string.")]
    public void SingleValueIsPinned() {
        foreach (string value in Samples(Dummy.String().OneOf("SOLE"))) {
            Check.That(value).IsEqualTo("SOLE");
        }
    }

    [Fact(DisplayName = "OneOf varies from draw to draw when the set holds more than one value.")]
    public void VariesAcrossDraws() {
        HashSet<string> seen = [.. Samples(Dummy.String().OneOf("a", "b", "c", "d"))];

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "Duplicate values are collapsed: both distinct values are still drawn, nothing else.")]
    [SuppressMessage(JustDummiesRule.JD025.Category, JustDummiesRule.JD025.Id, Justification = SuppressionJustification.JD025.DuplicateIsTheSubject)]
    public void DuplicatesAreCollapsed() {
        HashSet<string> seen = [.. Samples(Dummy.String().OneOf("a", "a", "b"))];

        Check.That(seen).IsOnlyMadeOf("a", "b");
        Check.That(seen).Contains("a", "b");
    }

    [Fact(DisplayName = "An empty string is a legitimate member of the set.")]
    public void EmptyStringIsAllowed() {
        Check.That(Dummy.String().OneOf("").Generate()).IsEqualTo(string.Empty);
    }

    [Fact(DisplayName = "OneOf is reproducible under a seed.")]
    public void ReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Dummy.WithSeed(7).String().OneOf("a", "b", "c", "d").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Dummy.WithSeed(7).String().OneOf("a", "b", "c", "d").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "OneOf composes into a value object through As.")]
    public void ComposesThroughAs() {
        IDummy<OrderReference> generator = Dummy.String().OneOf("ORD-12345678", "ORD-87654321").As(OrderReference.Create);

        for (int i = 0; i < SampleCount; i++) {
            OrderReference reference = generator.Generate();
            Check.That(reference.Value).StartsWith("ORD-");
            Check.That(reference.Value.Length).IsEqualTo(12);
        }
    }

    [Fact(DisplayName = "OrNull makes the value set generator null about half the time, otherwise a member of the set.")]
    public void OrNullIsSometimesNull() {
        IDummy<string?> generator = Dummy.WithSeed(20260721).String().OneOf("a", "b").OrNull();

        List<string?> values = [];
        for (int i = 0; i < SampleCount; i++) {
            values.Add(generator.Generate());
        }

        Check.That(values.Any(value => value is null)).IsTrue();
        Check.That(values.Where(value => value is not null)).IsOnlyMadeOf("a", "b");
    }

    [Fact(DisplayName = "A distinct set over OneOf is gated by the set's cardinality, both ways.")]
    public void CardinalityGatesDistinctCollections() {
        // Two distinct values cannot fill a set of three: caught eagerly, like any cardinality conflict.
        Check.ThatCode(() => Dummy.SetOf(Dummy.String().OneOf("a", "b")).WithCount(3).Generate()).Throws<ConflictingDummyConstraintException>();

        // Within the domain it fills the set with the requested distinct values.
        HashSet<string> set = Dummy.SetOf(Dummy.String().OneOf("a", "b", "c")).WithCount(3).Generate();
        Check.That(set.Count).IsEqualTo(3);
        Check.That(set).IsOnlyMadeOf("a", "b", "c");
    }

    [Fact(DisplayName = "The advertised cardinality is the surviving pool, not the declared one.")]
    public void CardinalityCountsTheSurvivingPool() {
        // Only "abc" and "xyz" are three characters long, so the domain a distinct set may draw from holds two
        // values — the shape narrowed the pool before the collection ever gated on it.
        Check.ThatCode(() => Dummy.SetOf(Dummy.String().OneOf("abc", "de", "xyz").WithLength(3)).WithCount(3).Generate())
             .Throws<ConflictingDummyConstraintException>();

        HashSet<string> set = Dummy.SetOf(Dummy.String().OneOf("abc", "de", "xyz").WithLength(3)).WithCount(2).Generate();
        Check.That(set).IsOnlyMadeOf("abc", "xyz");
    }

    [Fact(DisplayName = "A pinned value is counted as extending the set only when the set could not have drawn it.")]
    public void APinnedValueExtendsTheDomainOnlyWhenItIsOutside() {
        // "a" is one of the two values the generator can draw, so pinning it fills a slot the generator would have
        // filled anyway: the domain is still two, and a set of three cannot be filled.
        Check.ThatCode(() => Dummy.SetOf(Dummy.String().OneOf("a", "b")).Containing("a").WithCount(3).Generate())
             .Throws<ConflictingDummyConstraintException>();

        // "z" is a value the generator could never draw, so it occupies its own slot and the set of three fits.
        HashSet<string> set = Dummy.SetOf(Dummy.String().OneOf("a", "b")).Containing("z").WithCount(3).Generate();
        Check.That(set).IsOnlyMadeOf("a", "b", "z");
    }

    [Fact(DisplayName = "A distinct collection pinning a null is answered, not thrown at.")]
    public void PinningANullIsAnsweredNotThrownAt() {
        // Containing(null) is legal, if unlikely; asking a value set whether it could produce that null is a
        // question with the answer "no" — a value set rejects a null element — not a boundary violation. The pool
        // generator answers it that way, and so must this one.
        Check.ThatCode(() => Dummy.SetOf(Dummy.String().OneOf("a", "b")).Containing(null!).WithCount(2)).DoesNotThrow();
    }

    [Fact(DisplayName = "A shape constraint narrows the value set instead of conflicting with it.")]
    public void AShapeConstraintNarrowsTheSet() {
        // The example the composable form exists for: "abc" satisfies both, so both hold at once.
        foreach (string value in Samples(Dummy.String().OneOf("abc", "de").WithLength(3))) {
            Check.That(value).IsEqualTo("abc");
        }
    }

    [Fact(DisplayName = "Every string constraint composes with a value set, narrowing it to the values that satisfy it.")]
    public void EveryConstraintNarrowsTheSet() {
        // Each case pins the whole surviving domain, not one draw: a single draw from a two-value pool would still
        // land on the expected value about half the time with the constraint's filter removed, which is a test that
        // exercises the filter without asserting it.
        NarrowsTo("ORD-1", Dummy.String().OneOf("ORD-1", "INV-1").StartingWith("ORD-"));
        NarrowsTo("a-FR", Dummy.String().OneOf("a-FR", "a-BE").EndingWith("-FR"));
        NarrowsTo("xxKEYxx", Dummy.String().OneOf("xxKEYxx", "nope").Containing("KEY"));
        NarrowsTo("123", Dummy.String().OneOf("abc", "123").Numeric());
        NarrowsTo("abc", Dummy.String().OneOf("abc", "123").Alpha());
        NarrowsTo("abc", Dummy.String().OneOf("abc", "AB-1").AlphaNumeric());
        NarrowsTo("-:-", Dummy.String().OneOf("-:-", "abc").Punctuation());
        NarrowsTo("-:-", Dummy.String().OneOf("-:-", "abc").WithoutAlpha());
        NarrowsTo("AB-1", Dummy.String().OneOf("AB-1", "café").Printable());
        NarrowsTo("abc", Dummy.String().OneOf("abc", "ABC").InLowerCase());
        NarrowsTo("ABC", Dummy.String().OneOf("abc", "ABC").InUpperCase());
        NarrowsTo("aab", Dummy.String().OneOf("aab", "xyz").WithChars("ab"));
        NarrowsTo("abc", Dummy.String().OneOf("", "abc").NonEmpty());
        NarrowsTo("ab", Dummy.String().OneOf("ab", "abcdef").WithMaxLength(3));
        NarrowsTo("abcdef", Dummy.String().OneOf("ab", "abcdef").WithMinLength(4));
        NarrowsTo("abcdef", Dummy.String().OneOf("ab", "abcdef").WithLengthBetween(4, 8));
        NarrowsTo("keep", Dummy.String().OneOf("keep", "drop").DifferentFrom("drop"));
        NarrowsTo("keep", Dummy.String().OneOf("keep", "drop", "gone").Except("drop", "gone"));
    }

    [Fact(DisplayName = "A pooled value satisfies Containing on its own terms, not through the constructive layout.")]
    public void ContainedValuesAreCheckedNotLaidOut() {
        // The constructive path lays fragments side by side, so it could never build "aba" from "ab" and "ba" —
        // it would need four characters. Nothing is laid out here: the value was supplied and simply contains both.
        Check.That(Dummy.String().OneOf("aba").Containing("ab").Containing("ba").Generate()).IsEqualTo("aba");
    }

    [Fact(DisplayName = "A value set and a shape reach the same verdict whichever order they are declared in.")]
    public void OrderOfDeclarationDoesNotChangeTheVerdict() {
        Check.That(Dummy.String().WithLength(3).OneOf("abc", "de").Generate()).IsEqualTo("abc");
        Check.That(Dummy.String().OneOf("abc", "de").WithLength(3).Generate()).IsEqualTo("abc");

        Check.ThatCode(() => Dummy.String().WithLength(9).OneOf("abc", "de")).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.String().OneOf("abc", "de").WithLength(9)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "A constraint no supplied value satisfies names the value set and itself, and nothing else.")]
    public void AnEmptyingConstraintNamesBothSides() {
        Check.ThatCode(() => Dummy.String().OneOf("abc", "de").WithLength(9))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply WithLength(9) because no value OneOf(\"abc\", \"de\") allows satisfies it.");
    }

    [Fact(DisplayName = "A value set no declared constraint admits names that constraint and itself, and nothing else.")]
    public void AnEmptyValueSetNamesTheConstraintThatRefusedIt() {
        Check.ThatCode(() => Dummy.String().WithLength(9).OneOf("abc", "de"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"abc\", \"de\") because WithLength(9) allows none of its values.");
    }

    [Fact(DisplayName = "Several constraints that each refuse every value are all named.")]
    public void EveryConstraintRefusingEveryValueIsNamed() {
        Check.ThatCode(() => Dummy.String().WithLength(9).Numeric().OneOf("abc"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"abc\") because WithLength(9), Numeric() allow none of its values.");
    }

    [Fact(DisplayName = "When only the combination empties the set, no single constraint is blamed for it.")]
    public void ACombinationBlamesNoSingleConstraint() {
        // WithLength(3) admits "abc" and StartingWith("z") admits "zz": neither refuses every value, so naming
        // either one would blame a constraint the caller could loosen without changing the verdict.
        Check.ThatCode(() => Dummy.String().WithLength(3).StartingWith("z").OneOf("abc", "zz"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"abc\", \"zz\") because no value it offers satisfies the constraints already declared.");
    }

    [Fact(DisplayName = "A constraint that would have accepted a value the others removed qualifies its claim.")]
    public void AConstraintTheOthersOutranQualifiesItsClaim() {
        // Numeric() does accept "12" — WithLength(3) is what took it away. Claiming that no value the set offers
        // satisfies Numeric() would be false, so the message says only that nothing the other constraints left does.
        Check.ThatCode(() => Dummy.String().OneOf("abc", "12").WithLength(3).Numeric())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply Numeric() because no value OneOf(\"abc\", \"12\") allows that the other constraints leave satisfies it.");
    }

    [Fact(DisplayName = "A constraint that refuses every supplied value is not qualified away, whatever narrowed the set first.")]
    public void AConstraintRefusingEveryValueIsNotQualifiedAway() {
        // Numeric() refuses "abc" and "de" alike, so loosening WithLength(3) could not help and the message must
        // not suggest it: the claim stays the plain one, identical to the verdict without any prior narrowing.
        ConflictingDummyConstraintException narrowedFirst = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.String().OneOf("abc", "de").WithLength(3).Numeric());
        ConflictingDummyConstraintException onItsOwn = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.String().OneOf("abc", "de").Numeric());

        Check.That(narrowedFirst.Message).IsEqualTo("Cannot apply Numeric() because no value OneOf(\"abc\", \"de\") allows satisfies it.");
        Check.That(narrowedFirst.Message).IsEqualTo(onItsOwn.Message);
    }

    [Fact(DisplayName = "A constraint declared in one call is blamed as one call, not as the bounds it sets.")]
    public void ARangeIsBlamedAsTheCallTheCallerWrote() {
        // WithLengthBetween sets two internal bounds under one name. Judged apart, each admits one of the two
        // values and neither looks guilty; judged as the call the caller wrote — the only thing they can loosen —
        // it is the sole culprit, and it is what the message names, in either declaration order.
        ConflictingDummyConstraintException setLast = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.String().WithLengthBetween(2, 3).OneOf("a", "bbbb"));
        ConflictingDummyConstraintException setFirst = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.String().OneOf("a", "bbbb").WithLengthBetween(2, 3));

        Check.That(setLast.Message).IsEqualTo("Cannot apply OneOf(\"a\", \"bbbb\") because WithLengthBetween(2, 3) allows none of its values.");
        // ... and, applied the other way round, it is not blamed on constraints that do not exist.
        Check.That(setFirst.Message).IsEqualTo("Cannot apply WithLengthBetween(2, 3) because no value OneOf(\"a\", \"bbbb\") allows satisfies it.");
    }

    [Fact(DisplayName = "A casing constraint judges a pooled value on its actual case, accents included.")]
    public void CasingJudgesNonAsciiLettersToo() {
        // The constructive filler is ASCII, but a supplied value is the caller's own text: 'É' is an uppercase
        // letter, so InLowerCase() must refuse it rather than wave it through and emit a value violating itself.
        Check.ThatCode(() => Dummy.String().OneOf("É").InLowerCase()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.String().OneOf("é").InUpperCase()).Throws<ConflictingDummyConstraintException>();

        Check.That(Dummy.String().OneOf("é", "É").InLowerCase().Generate()).IsEqualTo("é");
        Check.That(Dummy.String().OneOf("é", "É").InUpperCase().Generate()).IsEqualTo("É");
    }

    [Fact(DisplayName = "Constraints that contradict each other on their own terms are still refused before a value set is declared.")]
    public void AContradictionIsRefusedBeforeTheValuesAreSeen() {
        // Declared first, the set is the specification and the two fragments are merely checked against "aba".
        Check.That(Dummy.String().OneOf("aba").WithMaxLength(3).Containing("ab").Containing("ba").Generate()).IsEqualTo("aba");

        // Declared last, it arrives too late: laid out side by side those fragments need four characters, and that
        // conflict is reported the moment it is declared — the generator cannot know a value set is coming, and
        // deferring the refusal would cost every shaped string its eager conflict.
        Check.ThatCode(() => Dummy.String().WithMaxLength(3).Containing("ab").Containing("ba").OneOf("aba"))
             .Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "An exclusion that empties the value set conflicts at declaration, naming both sides.")]
    public void AnExclusionEmptyingTheSetConflicts() {
        Check.ThatCode(() => Dummy.String().OneOf("a", "b").Except("a", "b"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply Except(\"a\", \"b\") because no value OneOf(\"a\", \"b\") allows satisfies it.");
    }

    [Fact(DisplayName = "A value set every exclusion covers conflicts, naming the exclusion that refused it.")]
    public void AValueSetTheExclusionsRefuseNamesThem() {
        Check.ThatCode(() => Dummy.String().DifferentFrom("x").OneOf("x"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"x\") because DifferentFrom(\"x\") allows none of its values.");
    }

    [Fact(DisplayName = "An exclusion on a value set never defers to a redraw: the surviving pool is drawn directly.")]
    public void AnExclusionOnAValueSetIsResolvedEagerly() {
        foreach (string value in Samples(Dummy.String().OneOf("keep", "drop").Except("drop"))) {
            Check.That(value).IsEqualTo("keep");
        }
    }

    [Fact(DisplayName = "Re-declaring the same value set is a no-op; a different one conflicts.")]
    public void RedeclaringTheValueSet() {
        Check.ThatCode(() => Dummy.String().OneOf("a", "b").OneOf("a", "b")).DoesNotThrow();

        Check.ThatCode(() => Dummy.String().OneOf("a", "b").OneOf("b", "c"))
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"b\", \"c\") because OneOf(\"a\", \"b\") is already defined.");
    }

    [Fact(DisplayName = "OneOf rejects null, empty, or null-containing value lists as arguments.")]
    [SuppressMessage(SonarRule.S3220.Category, SonarRule.S3220.Id, Justification = SuppressionJustification.S3220.AmbiguityIsTheInputUnderTest)]
    public void RejectsInvalidValueLists() {
        Check.ThatCode(() => Dummy.String().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.String().OneOf(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().OneOf("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "OneOf accepts a sequence, drawing only from its values.")]
    public void AcceptsASequence() {
        IEnumerable<string> vendors = ["Apple", "Microsoft", "Google"];

        HashSet<string> seen = [.. Samples(Dummy.String().OneOf(vendors))];

        Check.That(seen).IsOnlyMadeOf("Apple", "Microsoft", "Google");
        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "The sequence overload validates null, empty and null elements like the params one.")]
    public void SequenceOverloadValidates() {
        Check.ThatCode(() => Dummy.String().OneOf((IEnumerable<string>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().OneOf(Enumerable.Empty<string>())).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.String().OneOf(new List<string> { "a", null! })).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The sequence overload composes with the other constraints too.")]
    public void SequenceOverloadComposes() {
        Check.That(Dummy.String().NonEmpty().OneOf(new List<string> { "", "a" }).Generate()).IsEqualTo("a");
    }

}
