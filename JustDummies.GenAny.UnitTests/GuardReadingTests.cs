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
    [InlineData("if (value.Length < 3) { throw new ArgumentException(nameof(value)); }", "Any.String().WithMinLength(3)")]
    [InlineData("if (value.Length != 12) { throw new ArgumentException(nameof(value)); }", "Any.String().WithLength(12)")]
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
    ///     The arithmetic <c>ArgumentOutOfRangeException</c> throw helpers read into the same numeric rows as
    ///     the equivalent <c>if</c> condition would.
    /// </summary>
    /// <remarks>
    ///     <c>ThrowIfNegative</c> throws on <c>value &lt; 0</c>, so zero is admissible —
    ///     <c>GreaterThanOrEqualTo(0)</c>, not <c>Positive()</c>. <c>ThrowIfNegativeOrZero</c> throws on
    ///     <c>value &lt;= 0</c>, which is <c>Positive()</c>. Getting the two the wrong way round is exactly the
    ///     failure mode this reading exists to remove: a generator whose draws the constructor rejects.
    /// </remarks>
    [Theory(DisplayName = "An arithmetic throw helper is read into the numeric family.")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfNegative(value);", "Any.Int32().GreaterThanOrEqualTo(0)")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);", "Any.Int32().Positive()")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfZero(value);", "Any.Int32().NonZero()")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfLessThan(value, 18);", "Any.Int32().GreaterThanOrEqualTo(18)")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);", "Any.Int32().LessThanOrEqualTo(100)")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);", "Any.Int32().GreaterThan(0)")]
    [InlineData("int", "ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 100);", "Any.Int32().LessThan(100)")]
    [InlineData("decimal", "ArgumentOutOfRangeException.ThrowIfLessThan(value, 9.99m);", "Any.Decimal().GreaterThanOrEqualTo(9.99m)")]
    public void AnArithmeticThrowHelperIsReadIntoTheNumericFamily(string parameterType, string guard, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    // The subject-identity discipline the comparison rows keep, on the arithmetic throw helpers too.
    [Fact(DisplayName = "An arithmetic throw helper naming something other than the parameter is not read as its guard.")]
    public void AnArithmeticThrowHelperNamingSomethingElseIsNotReadAsItsGuard() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", "ArgumentOutOfRangeException.ThrowIfLessThan(18, value);");

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // The second argument has to be a compile-time constant, the same discipline the comparison rows keep.
    [Fact(DisplayName = "An arithmetic throw helper compared against a non-constant is unread.")]
    public void AnArithmeticThrowHelperComparedAgainstANonConstantIsUnread() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int value, int other) {
                                                           ArgumentOutOfRangeException.ThrowIfLessThan(value, other);
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A guard carrying an <c>else</c> is read the same as one with none — the <c>else</c> only says what
    ///     happens when the guard's own condition is false, which can never weaken what it rejects.
    /// </summary>
    [Fact(DisplayName = "A guard followed by an else is read the same as one with no else at all.")]
    public void AGuardFollowedByAnElseIsReadTheSameAsOneWithNoElseAtAll() {
        string guard = "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); } else { }";

        ScaffoldedParameter parameter = Subject.GuardedBy("int", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().Positive()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     An <c>else if</c> chain reads every branch, as long as every branch before it throws
    ///     unconditionally too — reaching a later branch then presupposes only that the earlier ones already
    ///     rejected the value, never a fact about a parameter of its own.
    /// </summary>
    [Fact(DisplayName = "An else-if chain that throws throughout reads every branch's own guard.")]
    public void AnElseIfChainThatThrowsThroughoutReadsEveryBranchsOwnGuard() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int value) {
                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                           else if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32().Between(0, 100)");
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
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

    /// <summary>
    ///     An enum exclusion guard reads as <c>AnyEnum&lt;T&gt;.DifferentFrom</c> — the commonest enum guard
    ///     there is, and the one the numeric family's own rows could never say, since Roslyn reports a
    ///     zero-valued enum member as a plain integer constant.
    /// </summary>
    [Fact(DisplayName = "An enum exclusion guard is read as DifferentFrom.")]
    public void AnEnumExclusionGuardIsReadAsDifferentFrom() {
        string guard = "if (value == OrderStatus.Draft) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("OrderStatus", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Enum<OrderStatus>().DifferentFrom(OrderStatus.Draft)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>The universe check unwraps a nullable enum the same way the numeric rows unwrap a nullable number.</summary>
    [Fact(DisplayName = "An enum exclusion guard is read the same on a nullable enum parameter.")]
    public void AnEnumExclusionGuardIsReadTheSameOnANullableEnumParameter() {
        string guard = "if (value == OrderStatus.Draft) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("OrderStatus?", guard);

        Check.That(parameter.Expression)
             .IsEqualTo("Any.Enum<OrderStatus>().DifferentFrom(OrderStatus.Draft).As(value => (OrderStatus?)value)");
    }

    /// <summary>
    ///     The negation is a different invariant, not this guard's inverse: <c>value != E.Member</c> throws
    ///     unless the value <b>is</b> that member — a pin, not an exclusion — and is out of scope for this
    ///     reading, so it is reported as unread rather than misread as <c>DifferentFrom</c>.
    /// </summary>
    [Fact(DisplayName = "An enum equality guard's negation is not read as an exclusion.")]
    public void AnEnumEqualityGuardsNegationIsNotReadAsAnExclusion() {
        string guard = "if (value != OrderStatus.Draft) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("OrderStatus", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Enum<OrderStatus>()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
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
                "Any.Int32().Between(0, 100)")]
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
    ///     Two guards bounding the same side are a conjunction, not a collision.
    /// </summary>
    /// <remarks>
    ///     Both <c>if</c>s throw, so a value has to satisfy both, and the conjunction of two ceilings is the
    ///     lower one. Picking it is not guessing which the developer meant — it is the only thing they can both
    ///     mean. Dropping both, as this once did, threw away an invariant the engine had read correctly, and
    ///     writing both would emit a call the library folds away in silence, which <c>JD032</c> reports as dead.
    /// </remarks>
    [Theory(DisplayName = "Two guards bounding the same side fold to the tighter one.")]
    [InlineData("""
                        if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                        if (value > 50) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """,
                "Any.Int32().LessThanOrEqualTo(50)")]
    [InlineData("""
                        if (value < 10) { throw new ArgumentOutOfRangeException(nameof(value)); }
                        if (value < 40) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """,
                "Any.Int32().GreaterThanOrEqualTo(40)")]
    public void TwoGuardsBoundingTheSameSideFoldToTheTighter(string guards, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", guards);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     Bounds that leave no value at all are dropped rather than reconciled, and the parameter says so.
    /// </summary>
    /// <remarks>
    ///     The library refuses such a chain at construction with <c>ConflictingAnyConstraintException</c>, and
    ///     <c>JD023</c> and its siblings report it at compile time — so the engine must not write it in the
    ///     first place. Which guard the developer meant is not the engine's guess to make; the contradiction is
    ///     theirs to see, and the recap points at it.
    ///     <para>
    ///         The three rows are the three shapes that used to escape: a floor above a ceiling was caught, but
    ///         an exact size beside a floor above it was not, and neither was a sign against an opposing bound
    ///         — <c>Bound</c> has six members and the check read two of them.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "Bounds that admit no value are dropped, and the parameter says so.")]
    [InlineData("int", """
                               if (value < 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                               if (value > 10) { throw new ArgumentOutOfRangeException(nameof(value)); }
                       """,
                "Any.Int32()")]
    [InlineData("int", """
                               if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                               if (value > -5) { throw new ArgumentOutOfRangeException(nameof(value)); }
                       """,
                "Any.Int32()")]
    [InlineData("string", """
                                  if (value.Length < 10) { throw new ArgumentException(nameof(value)); }
                                  if (value.Length != 8) { throw new ArgumentException(nameof(value)); }
                          """,
                "Any.String()")]
    public void BoundsThatAdmitNoValueAreDropped(string parameterType, string guards, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guards);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsTrue();
    }

    /// <summary>
    ///     The base table's own refinement yields to a guard, rather than contradicting it.
    /// </summary>
    /// <remarks>
    ///     A constructor demanding a blank string is not contradicting itself; it is contradicting the row that
    ///     assumed a <c>string</c> parameter wants <c>NonEmpty</c>. Dropping both sides would emit a generator
    ///     that violates a perfectly good guard and report a reconciliation the developer never asked for — so
    ///     the opinion yields and the declaration stands.
    ///     <para>
    ///         The same reading absorbs it where they merely overlap: a floor of eight already says non-empty,
    ///         so writing both states one invariant twice.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A base-table refinement yields to the guard it cannot stand beside.")]
    [InlineData("if (value.Length > 0) { throw new ArgumentException(nameof(value)); }", "Any.String().WithMaxLength(0)")]
    [InlineData("if (value.Length != 0) { throw new ArgumentException(nameof(value)); }", "Any.String().WithLength(0)")]
    [InlineData("if (value.Length < 8) { throw new ArgumentException(nameof(value)); }", "Any.String().WithMinLength(8)")]
    public void ABaseTableRefinementYieldsToTheGuard(string guard, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     ADR-0059 reaches the guards too, and a drop it makes is said out loud.
    /// </summary>
    /// <remarks>
    ///     <c>.Positive()</c> is not declared on the unsigned engine, so it is skipped rather than emitted into
    ///     a chain that would not compile — that half was always right. What was not is the column beside it:
    ///     provenance was computed from the constraints <b>read</b>, so the parameter reported <c>guard</c>
    ///     over an invariant nothing honoured, and the run reported every parameter inferred. §6 words that
    ///     column <c>tightened</c>, and a constraint with no member to be written with tightened nothing.
    ///     <para>
    ///         The <c>uint</c> case is benign — no draw violates it — and that is exactly why it is the one to
    ///         pin. The same silence over an enum guard the closed set cannot express costs a third of the
    ///         draws, and the two are one bug.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A constraint the generator does not carry is skipped, and reported as unavailable.")]
    public void AConstraintTheGeneratorDoesNotCarryIsSkipped() {
        string guard = "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("uint", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.UInt32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.ConstraintUnavailable)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsFalse();
    }

    /// <summary>A guard that did reach the chain still reports <c>guard</c>, whatever the fold did to it.</summary>
    /// <remarks>
    ///     The range fold rewrites a floor and a ceiling into one call neither of them is, so a provenance read
    ///     off the finished text would lose them both. It is read before the fold: how a chain is spelled is
    ///     never what it says.
    /// </remarks>
    [Fact(DisplayName = "A folded pair of bounds still reports the guard that produced it.")]
    public void AFoldedPairStillReportsTheGuard() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", """
                                                                         if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                                         if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                                 """);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().Between(0, 100)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.ConstraintUnavailable)).IsFalse();
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
