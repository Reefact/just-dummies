using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The closed set of §5.3, and the rules that decide what happens when two of them meet.
/// </summary>
/// <remarks>
///     Reading guards is what makes the tool worth building rather than templating, and the measurement behind
///     that claim is precise: <c>Any.String().As(OrderReference.Create)</c> — the chain a scaffolder gets
///     without this — threw <c>AnyGenerationException</c> 594 times in 10 000 draws, about one in seventeen.
/// </remarks>
public sealed class GuardReadingTests {

    [Theory(DisplayName = "A guard on a string is read into the string family.")]
    [InlineData("if (string.IsNullOrEmpty(value)) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("if (value.Length == 0) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("if (value.Length < 1) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("if (value.Length > 10) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty().WithMaxLength(10)")]
    [InlineData("if (value.Length < 3) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty().WithMinLength(3)")]
    [InlineData("if (value.Length != 12) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty().WithLength(12)")]
    public void AGuardOnAStringIsReadIntoTheStringFamily(string guard, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
    }

    [Theory(DisplayName = "A guard on a number is read into the numeric family.")]
    [InlineData("int", "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().Positive()")]
    [InlineData("int", "if (value < 1) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().Positive()")]
    [InlineData("int", "if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().GreaterThanOrEqualTo(0)")]
    [InlineData("int", "if (value >= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().Negative()")]
    [InlineData("int", "if (value == 0) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().NonZero()")]
    [InlineData("int", "if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().LessThanOrEqualTo(100)")]
    [InlineData("int", "if (value < 18) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int32().GreaterThanOrEqualTo(18)")]
    [InlineData("long", "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }", "Any.Int64().Positive()")]
    public void AGuardOnANumberIsReadIntoTheNumericFamily(string parameterType, string guard, string expected) {
        Check.That(Subject.GuardedBy(parameterType, guard).Expression).IsEqualTo(expected);
    }

    /// <summary>
    ///     Where two rows both match, the more specific wins.
    /// </summary>
    /// <remarks>
    ///     <c>p &lt; 1</c> is <c>Positive</c> on an integral type and a floor of one on a <c>decimal</c>,
    ///     because <c>Positive</c> would admit the values between zero and one that the guard rejects — a rare
    ///     draw for an otherwise unconstrained decimal, and a common one as soon as the parameter carries
    ///     another bound. Exactly the profile of a defect that survives casual testing.
    /// </remarks>
    [Theory(DisplayName = "The same guard reads differently on an integral and on a decimal.")]
    [InlineData("int", "Any.Int32().Positive()")]
    [InlineData("long", "Any.Int64().Positive()")]
    [InlineData("decimal", "Any.Decimal().GreaterThanOrEqualTo(1m)")]
    [InlineData("double", "Any.Double().GreaterThanOrEqualTo(1d)")]
    [InlineData("float", "Any.Single().GreaterThanOrEqualTo(1f)")]
    public void TheSameGuardReadsDifferentlyByType(string parameterType, string expected) {
        string guard = "if (value < 1) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        Check.That(Subject.GuardedBy(parameterType, guard).Expression).IsEqualTo(expected);
    }

    // A decimal bound written as `9.99` is a double literal, and there is no implicit conversion: the emitted
    // chain would not compile. The suffix is not decoration.
    [Fact(DisplayName = "A decimal bound is written as a decimal literal.")]
    public void ADecimalBoundIsWrittenAsADecimalLiteral() {
        string guard = "if (value > 9.99m) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        Check.That(Subject.GuardedBy("decimal", guard).Expression).IsEqualTo("Any.Decimal().LessThanOrEqualTo(9.99m)");
    }

    /// <summary>
    ///     A size guard on a collection reads against the count family, never the length one.
    /// </summary>
    /// <remarks>
    ///     A collection generator exposes <c>NonEmpty</c>, <c>WithCount</c>, <c>WithMinCount</c> and
    ///     <c>WithMaxCount</c>, and no <c>WithLength</c> at all (§14.3). Read against the string family instead,
    ///     the emitted member would not resolve and ADR-0059 would drop it <b>silently</b> — a real constraint
    ///     lost without a trace, which is the one failure mode this whole section cannot tolerate.
    /// </remarks>
    [Theory(DisplayName = "A size guard on a collection reads into the count family.")]
    [InlineData("int[]", "if (value.Length > 5) { throw new ArgumentException(nameof(value)); }",
                "Any.ArrayOf(Any.Int32()).WithMaxCount(5)")]
    [InlineData("List<string>", "if (value.Count > 5) { throw new ArgumentException(nameof(value)); }",
                "Any.ListOf(Any.String().NonEmpty()).WithMaxCount(5)")]
    [InlineData("List<string>", "if (value.Count != 3) { throw new ArgumentException(nameof(value)); }",
                "Any.ListOf(Any.String().NonEmpty()).WithCount(3)")]
    [InlineData("List<string>", "if (value.Count == 0) { throw new ArgumentException(nameof(value)); }",
                "Any.ListOf(Any.String().NonEmpty()).NonEmpty()")]
    public void ASizeGuardOnACollectionReadsIntoTheCountFamily(string parameterType, string guard, string expected) {
        Check.That(Subject.GuardedBy(parameterType, guard).Expression).IsEqualTo(expected);
    }

    [Theory(DisplayName = "A guard the generator already satisfies adds nothing.")]
    [InlineData("string", "if (value is null) { throw new ArgumentNullException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("string", "if (value == null) { throw new ArgumentNullException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("OrderStatus", "if (!Enum.IsDefined(typeof(OrderStatus), value)) { throw new ArgumentException(nameof(value)); }",
                "Any.Enum<OrderStatus>()")]
    public void AGuardTheGeneratorAlreadySatisfiesAddsNothing(string parameterType, string guard, string expected) {
        Check.That(Subject.GuardedBy(parameterType, guard).Expression).IsEqualTo(expected);
    }

    [Fact(DisplayName = "An empty Guid guard is read as NonEmpty.")]
    public void AnEmptyGuidGuardIsReadAsNonEmpty() {
        string guard = "if (value == Guid.Empty) { throw new ArgumentException(nameof(value)); }";

        Check.That(Subject.GuardedBy("Guid", guard).Expression).IsEqualTo("Any.Guid().NonEmpty()");
    }

    // The row's own refinement and a guard saying the same thing collapse, rather than colliding: a string row
    // is already NonEmpty, and a constructor guarding on IsNullOrWhiteSpace agrees with it.
    [Fact(DisplayName = "A guard repeating the row's own constraint collapses into it.")]
    public void AGuardRepeatingTheRowsConstraintCollapses() {
        string guard = "if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     Two guards bounding different things are the ordinary bounded-range idiom, and both are kept.
    /// </summary>
    /// <remarks>
    ///     Discarding it would make guard reading useless for the case it most often meets: a constructor that
    ///     states a floor and a ceiling on consecutive lines.
    /// </remarks>
    [Theory(DisplayName = "A floor and a ceiling compose.")]
    [InlineData("int",
                """
                        if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                        if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """,
                "Any.Int32().GreaterThanOrEqualTo(0).LessThanOrEqualTo(100)")]
    [InlineData("string",
                """
                        if (string.IsNullOrEmpty(value)) { throw new ArgumentException(nameof(value)); }
                        if (value.Length > 10) { throw new ArgumentException(nameof(value)); }
                """,
                "Any.String().NonEmpty().WithMaxLength(10)")]
    public void AFloorAndACeilingCompose(string parameterType, string guards, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guards);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     Two guards setting the same bound, or a floor above a ceiling, are dropped rather than reconciled.
    /// </summary>
    /// <remarks>
    ///     The library refuses such a chain at construction with <c>ConflictingAnyConstraintException</c>, and
    ///     <c>JD023</c> reports it at compile time — so the engine must not write it in the first place. Which
    ///     one the developer meant is not the engine's guess to make.
    /// </remarks>
    [Theory(DisplayName = "Two guards on the same bound are dropped, and the parameter says so.")]
    [InlineData("""
                        if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                        if (value > 50) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """,
                "Any.Int32()")]
    [InlineData("""
                        if (value < 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                        if (value > 10) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """,
                "Any.Int32()")]
    public void TwoGuardsOnTheSameBoundAreDropped(string guards, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", guards);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsTrue();
    }

    // ADR-0059 reaches the guards too: .Positive() is not declared on the unsigned engine, so it is skipped
    // rather than emitted into a chain that would not compile.
    [Fact(DisplayName = "A constraint the generator does not carry is skipped.")]
    public void AConstraintTheGeneratorDoesNotCarryIsSkipped() {
        string guard = "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        Check.That(Subject.GuardedBy("uint", guard).Expression).IsEqualTo("Any.UInt32()");
    }

    // §5.3: the constraint belongs to the generator for the parameter's own type, BEFORE the conversion. The
    // .As hop always comes last, because it is the step that changes the type.
    [Fact(DisplayName = "A guard on a nullable value type is read before the conversion, not after.")]
    public void AGuardOnANullableIsReadBeforeTheConversion() {
        string guard = "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        Check.That(Subject.GuardedBy("int?", guard).Expression)
             .IsEqualTo("Any.Int32().Positive().As(value => (int?)value)");
    }

}
