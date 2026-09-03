#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The cross-type convention for re-declaring a constraint that is "declared once per generator".
///     <para>
///         Declaring the <b>same</b> constraint twice is not a contradiction — the second declaration asks for exactly
///         what the first already guarantees — so it is a no-op. Declaring a <b>different</b> value for the same
///         once-only constraint is a contradiction and still fails at declaration time. The rule is one rule across
///         every generator family, which is why it is pinned here rather than scattered through each type's own tests.
///     </para>
///     <para>
///         This is a structural convention over a fixed table, not a property: the input space is "which constraint",
///         and the constraints are heterogeneous — there is nothing to generate.
///     </para>
/// </summary>
public sealed class ConstraintRedeclarationTests {

    #region Statics members declarations

    private static readonly Guid Pinned    = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");
    private static readonly Guid PinnedAlt = Guid.Parse("72b8586b-9d81-4e2a-9d17-8f6a1e6b8c1d");

    /// <summary>Every once-only constraint, declared twice with identical arguments.</summary>
    private static IEnumerable<(string Label, Func<object> Redeclare)> IdenticalRedeclarations() {
        yield return ("String().WithLength(3)", () => Dummy.String().WithLength(3).WithLength(3));
        yield return ("String().StartingWith(\"a\")", () => Dummy.String().StartingWith("a").StartingWith("a"));
        yield return ("String().EndingWith(\"z\")", () => Dummy.String().EndingWith("z").EndingWith("z"));
        yield return ("String().Alpha()", () => Dummy.String().Alpha().Alpha());
        yield return ("String().Numeric()", () => Dummy.String().Numeric().Numeric());
        yield return ("String().AlphaNumeric()", () => Dummy.String().AlphaNumeric().AlphaNumeric());
        yield return ("String().Punctuation()", () => Dummy.String().Punctuation().Punctuation());
        yield return ("String().Printable()", () => Dummy.String().Printable().Printable());
        yield return ("String().WithChars(\"ab\")", () => Dummy.String().WithChars("ab").WithChars("ab"));
        yield return ("String().InLowerCase()", () => Dummy.String().InLowerCase().InLowerCase());
        yield return ("String().InUpperCase()", () => Dummy.String().InUpperCase().InUpperCase());
        yield return ("String().OneOf(\"a\", \"b\")", () => Dummy.String().OneOf("a", "b").OneOf("a", "b"));
        yield return ("Int32().OneOf(1, 2)", () => Dummy.Int32().OneOf(1, 2).OneOf(1, 2));
        yield return ("Int32().MultipleOf(3)", () => Dummy.Int32().MultipleOf(3).MultipleOf(3));
        yield return ("Int64().OneOf(1L)", () => Dummy.Int64().OneOf(1L).OneOf(1L));
        yield return ("Double().OneOf(1.5)", () => Dummy.Double().OneOf(1.5).OneOf(1.5));
        yield return ("Decimal().WithScale(2)", () => Dummy.Decimal().WithScale(2).WithScale(2));
        yield return ("Char().Alpha()", () => Dummy.Char().Alpha().Alpha());
        yield return ("Char().Numeric()", () => Dummy.Char().Numeric().Numeric());
        yield return ("Char().AlphaNumeric()", () => Dummy.Char().AlphaNumeric().AlphaNumeric());
        yield return ("Char().Punctuation()", () => Dummy.Char().Punctuation().Punctuation());
        yield return ("Char().Printable()", () => Dummy.Char().Printable().Printable());
        yield return ("Char().NonPrintable()", () => Dummy.Char().NonPrintable().NonPrintable());
        yield return ("Char().Whitespaces()", () => Dummy.Char().Whitespaces().Whitespaces());
        yield return ("Char().Hexadecimal()", () => Dummy.Char().Hexadecimal().Hexadecimal());
        yield return ("Char().InLowerCase()", () => Dummy.Char().InLowerCase().InLowerCase());
        yield return ("Char().InUpperCase()", () => Dummy.Char().InUpperCase().InUpperCase());
        yield return ("Char().OneOf('a', 'b')", () => Dummy.Char().OneOf('a', 'b').OneOf('a', 'b'));
        // Not a once-only slot like the family constraints above — WithoutAlpha/WithoutNumeric accumulate instead —
        // but re-declaring one is documented as inert rather than contradictory, so it belongs to the same table.
        yield return ("Char().WithoutAlpha()", () => Dummy.Char().WithoutAlpha().WithoutAlpha());
        yield return ("Char().WithoutNumeric()", () => Dummy.Char().WithoutNumeric().WithoutNumeric());
        yield return ("Boolean().True()", () => Dummy.Boolean().True().True());
        yield return ("Boolean().False()", () => Dummy.Boolean().False().False());
        yield return ("Guid().OneOf(pinned)", () => Dummy.Guid().OneOf(Pinned).OneOf(Pinned));
        yield return ("Uri().Web().WithPathSegments(2)", () => Dummy.Uri().Web().WithPathSegments(2).WithPathSegments(2));
        yield return ("Uri().Web().WithHost(\"a.example\")", () => Dummy.Uri().Web().WithHost("a.example").WithHost("a.example"));
        yield return ("Uri().Web().WithPort(8080)", () => Dummy.Uri().Web().WithPort(8080).WithPort(8080));
        yield return ("Uri().Web().WithUserInfo(\"alice\")", () => Dummy.Uri().Web().WithUserInfo("alice").WithUserInfo("alice"));
        yield return ("Uri().Mailto().WithDomain(\"a.example\")", () => Dummy.Uri().Mailto().WithDomain("a.example").WithDomain("a.example"));
        yield return ("ListOf().WithCount(3)", () => Dummy.ListOf(Dummy.Int32()).WithCount(3).WithCount(3));
        yield return ("DateTimeOffset().WithOffset(zero)", () => Dummy.DateTimeOffset().WithOffset(TimeSpan.Zero).WithOffset(TimeSpan.Zero));
    }

    /// <summary>The same constraints declared twice with arguments that genuinely contradict.</summary>
    private static IEnumerable<(string Label, Func<object> Contradict)> ContradictoryRedeclarations() {
        yield return ("String().WithLength(3).WithLength(5)", () => Dummy.String().WithLength(3).WithLength(5));
        yield return ("String().StartingWith(\"a\").StartingWith(\"b\")", () => Dummy.String().StartingWith("a").StartingWith("b"));
        yield return ("String().Alpha().Numeric()", () => Dummy.String().Alpha().Numeric());
        yield return ("String().Printable().Punctuation()", () => Dummy.String().Printable().Punctuation());
        yield return ("String().InLowerCase().InUpperCase()", () => Dummy.String().InLowerCase().InUpperCase());
        yield return ("String().OneOf(\"a\", \"b\").OneOf(\"c\", \"d\")", () => Dummy.String().OneOf("a", "b").OneOf("c", "d"));
        yield return ("Int32().OneOf(1, 2).OneOf(3, 4)", () => Dummy.Int32().OneOf(1, 2).OneOf(3, 4));
        yield return ("Int32().MultipleOf(2).MultipleOf(3)", () => Dummy.Int32().MultipleOf(2).MultipleOf(3));
        yield return ("Decimal().WithScale(2).WithScale(4)", () => Dummy.Decimal().WithScale(2).WithScale(4));
        yield return ("Char().Alpha().Numeric()", () => Dummy.Char().Alpha().Numeric());
        yield return ("Char().Punctuation().Printable()", () => Dummy.Char().Punctuation().Printable());
        yield return ("Char().Whitespaces().Hexadecimal()", () => Dummy.Char().Whitespaces().Hexadecimal());
        yield return ("Char().InLowerCase().InUpperCase()", () => Dummy.Char().InLowerCase().InUpperCase());
        yield return ("Char().OneOf('a', 'b').OneOf('c', 'd')", () => Dummy.Char().OneOf('a', 'b').OneOf('c', 'd'));
        yield return ("Boolean().True().False()", () => Dummy.Boolean().True().False());
        yield return ("Guid().OneOf(a).OneOf(b)", () => Dummy.Guid().OneOf(Pinned).OneOf(PinnedAlt));
        yield return ("Uri().Web().WithPathSegments(2).WithPathSegments(3)", () => Dummy.Uri().Web().WithPathSegments(2).WithPathSegments(3));
        yield return ("Uri().Web().WithHost(a).WithHost(b)", () => Dummy.Uri().Web().WithHost("first.example").WithHost("second.example"));
        yield return ("Uri().Web().WithPort(8080).WithPort(9090)", () => Dummy.Uri().Web().WithPort(8080).WithPort(9090));
        yield return ("Uri().Web().WithUserInfo(a).WithUserInfo(b)", () => Dummy.Uri().Web().WithUserInfo("alice").WithUserInfo("bob"));
        yield return ("Uri().Ftp().WithPort().WithPort(21)", () => Dummy.Uri().Ftp().WithPort().WithPort(21));
        yield return ("Uri().Mailto().WithDomain(a).WithDomain(b)", () => Dummy.Uri().Mailto().WithDomain("a.example").WithDomain("b.example"));
        yield return ("ListOf().WithCount(3).WithCount(4)", () => Dummy.ListOf(Dummy.Int32()).WithCount(3).WithCount(4));
        yield return ("DateTimeOffset().WithOffset(0h).WithOffset(1h)", () => Dummy.DateTimeOffset().WithOffset(TimeSpan.Zero).WithOffset(TimeSpan.FromHours(1)));
    }

    #endregion

    [Fact(DisplayName = "Re-declaring a once-only constraint with identical arguments is a no-op, in every generator family.")]
    public void IdenticalRedeclarationIsANoOp() {
        // A constraint declared twice with the same argument is not a contradiction: the domain the second declaration
        // asks for is exactly the one the first already produced. Refusing it made the fluent reject a specification it
        // can satisfy — the one thing the eager check exists to avoid.
        List<string> refused = [];
        foreach ((string label, Func<object> redeclare) in IdenticalRedeclarations()) {
            try {
                redeclare();
            } catch (ConflictingDummyConstraintException) {
                refused.Add(label);
            }
        }

        Check.WithCustomMessage($"identical re-declarations still refused: {string.Join(", ", refused)}")
             .That(refused).IsEmpty();
    }

    [Fact(DisplayName = "Re-declaring a once-only constraint with a different argument is still a conflict.")]
    public void ContradictoryRedeclarationStillConflicts() {
        // The other half, and the reason the fix compares the rendered declaration rather than simply dropping the
        // guard: tolerating an identical re-declaration must not tolerate a contradictory one.
        List<string> accepted = [];
        foreach ((string label, Func<object> contradict) in ContradictoryRedeclarations()) {
            try {
                contradict();
                accepted.Add(label);
            } catch (ConflictingDummyConstraintException) {
                // expected
            }
        }

        Check.WithCustomMessage($"contradictions silently accepted: {string.Join(", ", accepted)}")
             .That(accepted).IsEmpty();
    }

    [Fact(DisplayName = "A second URI component pin names both sides instead of silently replacing the first.")]
    public void ASecondComponentPinNamesBothSides() {
        // Regression: WithHost, WithPort and WithUserInfo were plain setters — a second, different value replaced the
        // first and the first vanished without a word, while WithPathSegments in the very same generator raised a
        // conflict. A URI has one host, one port and one user-info, so a second declaration can never be honoured
        // alongside the first; dropping it silently discards a constraint the caller wrote.
        Check.ThatCode(() => Dummy.Uri().Web().WithHost("first.example").WithHost("second.example"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(host => host.Message).Contains("WithHost(\"second.example\")", "WithHost(\"first.example\")");

        // The message names the PUBLIC call, so a mailto's WithDomain reads as WithDomain and not as the host setter
        // it shares with the web families.
        Check.ThatCode(() => Dummy.Uri().Mailto().WithDomain("a.example").WithDomain("b.example"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(domain => domain.Message)
             .Contains("WithDomain(\"b.example\")")
             .And.Not.Contains("WithHost");
    }

    [Fact(DisplayName = "A second, different Distinct comparer conflicts; the same one again is a no-op.")]
    public void ASecondDistinctComparerConflicts() {
        // One collection is distinct under one equality. Two different comparers cannot both be honoured, and the
        // second was silently winning.
        Check.ThatCode(() => Dummy.ListOf(Dummy.Int32()).Distinct(new ModuloComparer(10)).Distinct(new ModuloComparer(100)))
             .Throws<ConflictingDummyConstraintException>();

        IEqualityComparer<int> comparer = new ModuloComparer(10);

        Check.ThatCode(() => Dummy.ListOf(Dummy.Int32()).Distinct(comparer).Distinct(comparer)).DoesNotThrow();
        // Re-declaring distinctness without naming a comparer asks for the equality already in force.
        Check.ThatCode(() => Dummy.ListOf(Dummy.Int32()).Distinct(comparer).Distinct()).DoesNotThrow();
    }

    [Fact(DisplayName = "A no-op re-declaration leaves the generator's domain untouched.")]
    public void ANoOpRedeclarationDoesNotWidenTheDomain() {
        // The no-op must be a no-op: returning `this` rather than rebuilding means the second declaration cannot
        // loosen anything. Asserted on the observable domain, not on object identity.
        for (int i = 0; i < 200; i++) {
            Check.That(Dummy.String().WithLength(4).WithLength(4).Generate().Length).IsEqualTo(4);
            Check.That(new[] { 7, 9 }).Contains(Dummy.Int32().OneOf(7, 9).OneOf(7, 9).Generate());
            Check.That(new[] { "a", "b" }).Contains(Dummy.String().OneOf("a", "b").OneOf("a", "b").Generate());
            Check.That(Dummy.ListOf(Dummy.Int32()).WithCount(3).WithCount(3).Generate().Count).IsEqualTo(3);
        }
    }

    #region Nested types

    private sealed class ModuloComparer : IEqualityComparer<int> {

        private readonly int _modulus;

        public ModuloComparer(int modulus) {
            _modulus = modulus;
        }

        public bool Equals(int x, int y) {
            return x % _modulus == y % _modulus;
        }

        public int GetHashCode(int obj) {
            return obj % _modulus;
        }

    }

    #endregion

}
