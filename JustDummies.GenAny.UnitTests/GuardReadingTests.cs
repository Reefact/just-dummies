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
    [InlineData("if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }", "Any.String().NotBlank()")]
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

    /// <summary>
    ///     A sign guard on an unsigned parameter is written in the spelling that generator actually carries.
    /// </summary>
    /// <remarks>
    ///     §14.3 gives the unsigned families the signed surface <b>less <c>Positive</c> and <c>Negative</c></b>,
    ///     so emitting <c>Positive()</c> there resolves to nothing and ADR-0059 drops it — leaving an
    ///     unnarrowed draw under a file that still compiles, and a generator that draws the one value the
    ///     constructor exists to refuse. Zero is the floor of an unsigned type, so <i>above zero</i> is exactly
    ///     <i>not zero</i>: <c>NonZero()</c> is the same constraint, not a looser one.
    ///     <para>
    ///         Both spellings are pinned, because the reading has to hold whichever way the guard is written —
    ///         and the helper spelling is the one that turned a build this repository used to block into one
    ///         that compiles.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A sign guard on an unsigned parameter is read as NonZero, which its generator carries.")]
    [InlineData("byte", "Any.Byte()", "ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);")]
    [InlineData("uint", "Any.UInt32()", "ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);")]
    [InlineData("ulong", "Any.UInt64()", "ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);")]
    [InlineData("ushort", "Any.UInt16()", "ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);")]
    [InlineData("byte", "Any.Byte()", "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("uint", "Any.UInt32()", "if (value < 1) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    public void ASignGuardOnAnUnsignedParameterIsReadAsNonZero(string parameterType, string generator, string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.Expression).IsEqualTo(generator + ".NonZero()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.ConstraintUnavailable)).IsFalse();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     The signed reading is unchanged, so the two rows stay told apart — the trap issue #106 named.
    /// </summary>
    /// <remarks>
    ///     Pinned as a <b>pair</b> rather than one row each: the failure this guards against is not either
    ///     mapping being absent, it is the two being swapped, and a test per row passes just as happily
    ///     swapped as not.
    /// </remarks>
    [Fact(DisplayName = "ThrowIfNegative and ThrowIfNegativeOrZero read as different constraints, in the right order.")]
    public void ThrowIfNegativeAndThrowIfNegativeOrZeroReadAsDifferentConstraints() {
        string admittingZero = Subject.GuardedBy("int", "ArgumentOutOfRangeException.ThrowIfNegative(value);").Expression!;
        string refusingZero  = Subject.GuardedBy("int", "ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);").Expression!;

        // ThrowIfNegative throws on `value < 0`, so zero is admissible; ThrowIfNegativeOrZero throws on
        // `value <= 0`, so it is not. Read the wrong way round, the second yields a generator drawing a value
        // the constructor rejects — the failure mode this whole reading exists to remove.
        Check.That(admittingZero).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(refusingZero).IsEqualTo("Any.Int32().Positive()");
        Check.That(admittingZero).IsNotEqualTo(refusingZero);
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

    /// <summary>
    ///     A <c>.Count</c>/<c>.Length</c> read off a parameter whose own type is neither a string nor a
    ///     collection is not a size guard the engine can vouch for — it is an arbitrary member the domain
    ///     type happens to expose, and the engine has no way to know what it means.
    /// </summary>
    /// <remarks>
    ///     Before this, the family was chosen from <c>GeneratorFor.Sizes(parameter.Type)</c>'s <c>ByCount</c>
    ///     flag, false for any non-collection type, which fell to the length family unconditionally — not
    ///     because the parameter's type <b>is</b> a string, but because that family was the only one left.
    ///     On a composed parameter the constraint then landed on the factory's own string argument, which
    ///     happens to carry a same-named member too — a silent misattribution rather than the drop this test
    ///     now pins instead.
    /// </remarks>
    [Fact(DisplayName = "A .Count read off a non-collection, non-string parameter is unread, not misattributed.")]
    public void ACountReadOffANonCollectionNonStringParameterIsUnread() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using System.Collections.Generic;

                                                   namespace Shop.Domain;

                                                   public sealed class Tags {

                                                       private Tags(string csv) { Csv = csv; }

                                                       public static Tags Parse(string csv) {
                                                           if (string.IsNullOrWhiteSpace(csv)) { throw new System.ArgumentException("blank", nameof(csv)); }

                                                           return new Tags(csv);
                                                       }

                                                       public string Csv { get; }

                                                       public int Count { get { return Csv.Split(',').Length; } }

                                                   }

                                                   public sealed class Subject {

                                                       public Subject(Tags tags) {
                                                           if (tags.Count < 3) { throw new System.ArgumentException("three tags", nameof(tags)); }
                                                       }

                                                   }
                                                   """);

        ScaffoldedParameter tags = outcome.Plan!.Parameters.Single();

        Check.That(tags.Expression).IsEqualTo("Any.String().NotBlank().As(Tags.Parse)");
        Check.That(tags.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
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
    // is already NonEmpty, and a constructor guarding on IsNullOrEmpty agrees with it. IsNullOrWhiteSpace is
    // deliberately not the example any more -- it reads as NotBlank, which strengthens the row instead of
    // repeating it, and the case below covers that.
    [Fact(DisplayName = "A guard repeating the row's own constraint collapses into it.")]
    public void AGuardRepeatingTheRowsConstraintCollapses() {
        string guard = "if (string.IsNullOrEmpty(value)) { throw new ArgumentException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     A guard that STRENGTHENS the row rather than repeating it replaces the row's own constraint, and is
    ///     never absorbed by a tighter floor beside it.
    /// </summary>
    /// <remarks>
    ///     NotBlank and NonEmpty both spell an emptiness bound at an edge of one, so the ordinary
    ///     tightest-floor fold would keep one and drop the other -- and a tighter numeric floor would drop both.
    ///     Either outcome loses the half of NotBlank that is not a floor: eight characters every one of which
    ///     may be a space is exactly what this domain rejects (ADR-0088).
    /// </remarks>
    [Fact(DisplayName = "A guard strengthening the row's own constraint replaces it, and survives a tighter floor.")]
    public void AGuardStrengtheningTheRowsConstraintSurvives() {
        string guard = """
                       if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                               if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                       """;

        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NotBlank().WithMinLength(8)");
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
    ///     <c>.NonZero()</c> is not declared on the enum engine — <c>AnyEnum&lt;T&gt;</c> carries only
    ///     <c>OneOf</c>, <c>Except</c>, <c>DifferentFrom</c> and <c>AllowingCombinations</c> — so it is skipped
    ///     rather than emitted into a chain that would not compile. That half was always right. What was not is
    ///     the column beside it: provenance was computed from the constraints <b>read</b>, so the parameter
    ///     reported <c>guard</c> over an invariant nothing honoured, and the run reported every parameter
    ///     inferred. §6 words that column <c>tightened</c>, and a constraint with no member to be written with
    ///     tightened nothing.
    ///     <para>
    ///         Written against the enum comparing to a bare <c>0</c> rather than to a named member: the named
    ///         spelling is now the <c>DifferentFrom</c> row, which resolves, and this case is what is left of
    ///         the drop it replaced. The pairing matters — the same guard costs a third of the draws in the
    ///         spelling that is read and nothing in the spelling that is not, which is why the column has to
    ///         tell them apart.
    ///     </para>
    ///     <para>
    ///         It used to be pinned on <c>.Positive()</c> over a <c>uint</c>. That vehicle is gone on purpose:
    ///         a sign guard on an unsigned parameter is now written as the <c>NonZero</c> its generator does
    ///         carry, so nothing is dropped there any more.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A constraint the generator does not carry is skipped, and reported as unavailable.")]
    public void AConstraintTheGeneratorDoesNotCarryIsSkipped() {
        string guard = "if (value == 0) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("OrderStatus", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Enum<OrderStatus>()");
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

    /// <summary>
    ///     The CommunityToolkit rows of ADR-0086, each pinned at the semantics that was measured — the strict
    ///     comparisons build the exclusive bound, and <c>IsInRange</c> keeps its half-open ceiling.
    /// </summary>
    [Theory(DisplayName = "A CommunityToolkit guard is read into the closed set's own rows.")]
    [InlineData("string", "Guard.IsNotNullOrEmpty(value, nameof(value));", "Any.String().NonEmpty()")]
    [InlineData("int", "Guard.IsGreaterThan(value, 0, nameof(value));", "Any.Int32().GreaterThan(0)")]
    [InlineData("int", "Guard.IsGreaterThanOrEqualTo(value, 18, nameof(value));", "Any.Int32().GreaterThanOrEqualTo(18)")]
    [InlineData("int", "Guard.IsLessThan(value, 100, nameof(value));", "Any.Int32().LessThan(100)")]
    [InlineData("int", "Guard.IsLessThanOrEqualTo(value, 100, nameof(value));", "Any.Int32().LessThanOrEqualTo(100)")]
    [InlineData("int", "Guard.IsInRange(value, 0, 100, nameof(value));", "Any.Int32().GreaterThanOrEqualTo(0).LessThan(100)")]
    public void AToolkitGuardIsReadIntoTheClosedSet(string parameterType, string guard, string expected) {
        ScaffoldedParameter parameter = LibraryGuarded(parameterType, guard, "using CommunityToolkit.Diagnostics;");

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     The Ardalis rows of ADR-0086 in the discarded spelling, measured semantics and all: zero passes
    ///     <c>Negative</c>, both bounds pass <c>OutOfRange</c>, both boundary lengths pass the string pair.
    /// </summary>
    [Theory(DisplayName = "An Ardalis guard is read into the closed set's own rows.")]
    [InlineData("string", "Guard.Against.NullOrEmpty(value);", "Any.String().NonEmpty()")]
    [InlineData("int", "Guard.Against.Negative(value);", "Any.Int32().GreaterThanOrEqualTo(0)")]
    [InlineData("int", "Guard.Against.NegativeOrZero(value);", "Any.Int32().Positive()")]
    [InlineData("int", "Guard.Against.Zero(value);", "Any.Int32().NonZero()")]
    [InlineData("int", "Guard.Against.OutOfRange(value, nameof(value), 0, 100);", "Any.Int32().Between(0, 100)")]
    [InlineData("string", "Guard.Against.StringTooShort(value, 3);", "Any.String().WithMinLength(3)")]
    [InlineData("string", "Guard.Against.StringTooLong(value, 20);", "Any.String().NonEmpty().WithMaxLength(20)")]
    [InlineData("string", "Guard.Against.LengthOutOfRange(value, 8, 20);", "Any.String().WithLengthBetween(8, 20)")]
    [InlineData("System.Guid", "Guard.Against.Default(value);", "Any.Guid().NonEmpty()")]
    [InlineData("int", "Guard.Against.Default(value);", "Any.Int32().NonZero()")]
    public void AnArdalisGuardIsReadIntoTheClosedSet(string parameterType, string guard, string expected) {
        ScaffoldedParameter parameter = LibraryGuarded(parameterType, guard, "using Ardalis.GuardClauses;");

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     Both libraries' whitespace rejection reads as <c>NotBlank()</c>, never as <c>NonEmpty()</c> — a floor
    ///     of one character admits the all-whitespace value the guard exists to refuse.
    /// </summary>
    /// <remarks>
    ///     Each row was recognised-but-unmapped until the member existed, which is what ADR-0086's own rule —
    ///     "measured, or not in the table" — demands of semantics nothing spells exactly. ADR-0088 added the
    ///     member, so the honest answer moved from a mark to a read, and the emitted chain carries no
    ///     <c>NonEmpty()</c> beside it: the stronger constraint subsumes the string row's own.
    /// </remarks>
    [Theory(DisplayName = "A guard-library whitespace rejection reads as NotBlank.")]
    [InlineData("Guard.Against.NullOrWhiteSpace(value);", "using Ardalis.GuardClauses;")]
    [InlineData("Guard.IsNotNullOrWhiteSpace(value, nameof(value));", "using CommunityToolkit.Diagnostics;")]
    public void AGuardLibraryWhitespaceRejectionReadsAsNotBlank(string guard, string usings) {
        ScaffoldedParameter parameter = LibraryGuarded("string", guard, usings);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NotBlank()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     ADR-0086's assigned spelling: the helper returns its validated input, so assigning it to state both
    ///     validates and stores — and no longer ends the leading scan, which is what used to hide every guard
    ///     below the first such line.
    /// </summary>
    [Fact(DisplayName = "A guard assigned to state is read, and the guards below it still are.")]
    public void AGuardAssignedToStateIsReadAndTheScanContinues() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public string Name { get; }

                                                       public Subject(string name, int quantity) {
                                                           Name = Guard.Against.NullOrEmpty(name);

                                                           if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

                                                           kept = quantity;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        IReadOnlyList<ScaffoldedParameter> parameters = outcome.Plan!.Parameters;

        ScaffoldedParameter name     = parameters[0];
        ScaffoldedParameter quantity = parameters[1];

        Check.That(name.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(name.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(quantity.Expression).IsEqualTo("Any.Int32().Positive()");
        Check.That(quantity.Provenance.HasFlag(Provenance.Guard)).IsTrue();
    }

    /// <summary>
    ///     The carve-out reaches only a right side the set recognises: any other assignment to state ends the
    ///     scan exactly as it always did, which is what keeps <c>_name = value.Trim();</c> ordinary
    ///     production rather than doubt (ADR-0083, Follow-up).
    /// </summary>
    [Fact(DisplayName = "An assignment the set does not recognise still ends the scan, in silence.")]
    public void AnUnrecognisedAssignmentStillEndsTheScan() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public string Name { get; }

                                                       public Subject(string name, int quantity) {
                                                           Name = name.Trim();

                                                           if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

                                                           kept = quantity;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter quantity = outcome.Plan!.Parameters[1];

        Check.That(quantity.Expression).IsEqualTo("Any.Int32()");
        Check.That(quantity.Provenance.HasFlag(Provenance.Guard)).IsFalse();
        Check.That(quantity.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     The statement that ends the scan is still asked whether it rejects, before it does — a `throw`
    ///     carried inside the assignment's own right side is not the same silence as an ordinary one like
    ///     <c>_name = value.Trim();</c>. Everything below still stays out of reach, exactly as
    ///     <see cref="AnUnrecognisedAssignmentStillEndsTheScan" /> pins.
    /// </summary>
    [Fact(DisplayName = "A throw inside the ending assignment is marked before the scan ends.")]
    public void AThrowInsideTheEndingAssignmentIsMarkedBeforeTheScanEnds() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public string Code { get; }

                                                       public int Uses { get; }

                                                       public Subject(string code, int uses) {
                                                           Code = code.Length switch {
                                                               < 8  => throw new ArgumentException("Too short.", nameof(code)),
                                                               > 20 => throw new ArgumentException("Too long.", nameof(code)),
                                                               _    => code
                                                           };

                                                           if (uses < 1) { throw new ArgumentOutOfRangeException(nameof(uses)); }

                                                           Uses = uses;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter code = outcome.Plan!.Parameters[0];

        Check.That(code.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     ADR-0086's carve-out reaches <c>field = call;</c> only — a recognised library call whose result is
    ///     RETURNED, rather than assigned to a field or property, is a shape <c>MarkIfValidatedElsewhere</c>
    ///     did not scan for at all. Marked here, not read: reading it would mean trusting a used result the
    ///     rest of the carve-out deliberately keeps narrow.
    /// </summary>
    [Fact(DisplayName = "A recognised library call handed to a return statement is marked, not silent.")]
    public void ARecognisedLibraryCallHandedToAReturnStatementIsMarked() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       private Subject(int stars) { Stars = stars; }

                                                       public int Stars { get; }

                                                       public static Subject Create(int stars) {
                                                           return new Subject(Guard.Against.OutOfRange(stars, nameof(stars), 1, 5));
                                                       }

                                                   }
                                                   """);

        ScaffoldedParameter stars = outcome.Plan!.Parameters.Single();

        Check.That(stars.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     The same gap, a third statement shape: a recognised call as a local declaration's own initializer.
    /// </summary>
    [Fact(DisplayName = "A recognised library call handed to a local declaration is marked, not silent.")]
    public void ARecognisedLibraryCallHandedToALocalDeclarationIsMarked() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       public decimal Total { get; }

                                                       public Subject(decimal total) {
                                                           decimal net = Guard.Against.NegativeOrZero(total);
                                                           Total = net;
                                                       }

                                                   }
                                                   """);

        ScaffoldedParameter total = outcome.Plan!.Parameters.Single();

        Check.That(total.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>One parameter of a <c>Subject</c> guarded through a library's own spelling, using and all.</summary>
    private static ScaffoldedParameter LibraryGuarded(string parameterType, string body, string usings) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                    {{usings}}

                                                    namespace Shop.Domain;

                                                    public sealed class Subject {

                                                        private readonly {{parameterType}} kept;

                                                        public Subject({{parameterType}} value) {
                                                            {{body}}
                                                            kept = value;
                                                        }

                                                    }
                                                    """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters.Single();
    }

}
