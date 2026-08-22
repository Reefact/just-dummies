using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Where guard reading stops. The rule is deliberately conservative, mirroring how the library's own
///     analyzers under-report rather than misfire.
/// </summary>
/// <remarks>
///     Each case below is a statement that <i>looks</i> like a guard and is not one, or one the closed set does
///     not cover. The parameter keeps the neutral generator either way; what differs is whether the developer
///     is told to go and look (§9).
/// </remarks>
public sealed class GuardBoundaryTests {

    /// <summary>
    ///     Regex guards are deliberately not read, and this is the negative case §12 asks for by name.
    /// </summary>
    /// <remarks>
    ///     The library builds values from the <b>regular</b> subset of the pattern language only, and an
    ///     unsupported pattern throws at <b>construction</b> — the emitted parameterless constructor runs the
    ///     whole recipe, so the generated type would be unusable rather than merely imprecise, and no
    ///     <c>.WithReference(…)</c> the developer could write would rescue it. Four of five realistic validation
    ///     patterns tried against it were rejected. ADR-0063 also stops the engine from asking the library
    ///     whether a pattern is supported, so it cannot tell in advance.
    ///     <para>
    ///         Which yields the rule this case protects: the engine never emits an expression whose validity
    ///         depends on a value it cannot check.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A regex guard produces no pattern constraint, and says it was not read.")]
    public void ARegexGuardProducesNoPatternConstraint() {
        string guard = """
                               if (!System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Z]{3}$")) {
                                   throw new ArgumentException(nameof(value));
                               }
                       """;

        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Expression).Not.Contains("Matching");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    [Theory(DisplayName = "A condition the closed set does not cover is reported as unread.")]
    [InlineData("if (value.Length > 3 && value.Length < 10) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("if (value.StartsWith(\"ORD-\") == false) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("if (value.Trim().Length == 4) { throw new ArgumentException(nameof(value)); }")]
    public void AConditionTheClosedSetDoesNotCoverIsReportedAsUnread(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     Every row of the table is written about the parameter itself, so the subject has to <b>be</b> it.
    /// </summary>
    /// <remarks>
    ///     Mentioning the parameter is not saying something about it, and reading the two as one is not a near
    ///     miss: <c>Math.Abs(value) &gt; 90</c> read as <c>LessThanOrEqualTo(90)</c> yields a generator every
    ///     draw of which the guard rejects, and <c>value.TotalMinutes &lt; 5</c> read as a bound on a
    ///     <c>TimeSpan</c> yields a chain that does not compile, both reported as <c>guard</c> — a false claim
    ///     rather than a missing one. §9 names the arithmetic condition as out of reach; the only derived form
    ///     the table has rows for is the parameter's own length or count, and the receiver of that has to be
    ///     the parameter too, or an element's length reads as the collection's count.
    ///     <para>
    ///         The cast is deliberately in this list. <c>(long)value &gt; 100</c> happens to mean what it seems
    ///         to on an <c>int</c>, and <c>(byte)value &gt; 100</c> does not; telling the two apart is
    ///         conversion reasoning the engine does not do, so both are left unread.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A bound whose subject is not the parameter itself is reported as unread.")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Split(',').Length < 2) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Trim().Length < 8) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Substring(2).Length > 10) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("int", "Any.Int32()", "if (Math.Abs(value) > 90) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("int", "Any.Int32()", "if (value * 2 > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("int", "Any.Int32()", "if (-value > 0) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("int", "Any.Int32()", "if ((long)value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("TimeSpan", "Any.TimeSpan()", "if (value.TotalMinutes < 5) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    public void ABoundWhoseSubjectIsNotTheParameterIsReportedAsUnread(string parameterType, string neutral, string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.Expression).IsEqualTo(neutral);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsFalse();
    }

    /// <summary>
    ///     A constant no bound can be read from leaves the parameter neutral, and the run alive.
    /// </summary>
    /// <remarks>
    ///     The floating-point rows are the ones that used to end the whole run rather than the reading:
    ///     <c>IsNumber</c> admits <c>double</c>, whose range runs past <c>decimal</c>'s, and the conversion
    ///     threw out of the public <c>Scaffolder.Scaffold</c> — types before the offending one written, the
    ///     rest absent, the shell reporting a command line it had understood (§10.3).
    ///     <para>
    ///         The size rows are the other half of the same pipeline. Every size member takes an <c>int</c>,
    ///         and a bound that does not render as one was emitted verbatim: <c>WithMaxLength(140.5)</c>,
    ///         <c>CS1503</c> in the developer's own build, from a scaffold that reported success.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A constant outside what a bound can carry leaves the parameter neutral.")]
    [InlineData("double", "Any.Double()", "if (value > 1e30) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("double", "Any.Double()", "if (value > double.MaxValue) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("float", "Any.Single()", "if (value > 3.4e30f) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Length > 140.5) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Length > 3000000000) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("string", "Any.String().NonEmpty()", "if (value.Length > -1) { throw new ArgumentException(nameof(value)); }")]
    public void AConstantOutsideWhatABoundCanCarryLeavesTheParameterNeutral(string parameterType, string neutral, string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.Expression).IsEqualTo(neutral);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     An enum universe check says something only about a parameter of that enum's type.
    /// </summary>
    /// <remarks>
    ///     The row's justification is that <c>Any.Enum&lt;E&gt;()</c> already draws declared members only —
    ///     which presupposes the parameter is <c>E</c>. On an int-backed status column nothing narrowed the
    ///     draw and nothing was reported either: two admissible values out of four billion, under an empty
    ///     provenance column indistinguishable from having read no guard at all.
    /// </remarks>
    [Theory(DisplayName = "An enum universe check over another type is reported as unread.")]
    [InlineData("int", "Any.Int32()")]
    [InlineData("string", "Any.String().NonEmpty()")]
    public void AnEnumUniverseCheckOverAnotherTypeIsReportedAsUnread(string parameterType, string neutral) {
        ScaffoldedParameter parameter = Subject.GuardedBy(
            parameterType,
            "if (!Enum.IsDefined(typeof(OrderStatus), value)) { throw new ArgumentOutOfRangeException(nameof(value)); }");

        Check.That(parameter.Expression).IsEqualTo(neutral);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A rule spanning two parameters is out of reach, and §9 says out of reach is said out loud.
    /// </summary>
    /// <remarks>
    ///     It was the one rejection path in the reading that marked nothing, while the <c>&amp;&amp;</c> case
    ///     excluded by the same §5.3 bullet marked correctly. Measured on this very shape: 5008 throws in
    ///     10 000 draws, under a recap reading <c>2 of 2 parameters inferred</c> with an empty provenance
    ///     column on both — three times the rate ADR-0060 was written to remove.
    /// </remarks>
    [Fact(DisplayName = "A cross-parameter guard marks every parameter it spans as unread.")]
    public void ACrossParameterGuardMarksEveryParameterItSpans() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int min, int max) {
                                                           if (min > max) { throw new ArgumentException(nameof(min)); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters.Select(parameter => parameter.Provenance.HasFlag(Provenance.UnreadGuards)))
             .ContainsExactly(true, true);
    }

    /// <summary>A condition about no parameter at all sends nobody looking, and must stay silent.</summary>
    [Fact(DisplayName = "A guard mentioning no parameter marks nothing.")]
    public void AGuardMentioningNoParameterMarksNothing() {
        ScaffoldedParameter parameter = Subject.GuardedBy(
            "int",
            "if (DateTime.Now.Hour > 5) { throw new InvalidOperationException(); }");

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>The parentheses a writer is free to add do not make the subject something else.</summary>
    [Theory(DisplayName = "Parentheses around the subject leave the guard readable.")]
    [InlineData("if ((value).Length < 8) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("if ((value.Length) < 8) { throw new ArgumentException(nameof(value)); }")]
    public void ParenthesesAroundTheSubjectLeaveTheGuardReadable(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().WithMinLength(8)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
    }

    // "Leading" is what makes a guard a guard. Past the first assignment to state, an `if` that throws is
    // ordinary logic and says nothing about what the parameter may be.
    [Fact(DisplayName = "A throwing check after the first assignment is not a guard.")]
    public void AThrowingCheckAfterTheFirstAssignmentIsNotAGuard() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(int value) {
                                                           kept = value;

                                                           if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32()");
    }

    [Theory(DisplayName = "A statement that is not an unconditional throw is not a guard.")]
    [InlineData("if (value <= 0) { value = 1; }")]
    [InlineData("if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); } else { }")]
    [InlineData("if (value <= 0) { Console.WriteLine(value); throw new ArgumentOutOfRangeException(nameof(value)); }")]
    public void AStatementThatIsNotAnUnconditionalThrowIsNotAGuard(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    // A cross-parameter rule is precisely the case §9 names as out of reach: the engine cannot say whose
    // invariant it is, so it says nothing about either.
    [Fact(DisplayName = "A guard mentioning two parameters constrains neither.")]
    public void AGuardMentioningTwoParametersConstrainsNeither() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(int first, int second) {
                                                           if (first > second) { throw new ArgumentException(nameof(first)); }

                                                           kept = first;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32()");
        Check.That(outcome.Plan.Parameters[1].Expression).IsEqualTo("Any.Int32()");
    }

    /// <summary>
    ///     A type from a package has no body to read, which is a different fact from having no guards — and §6
    ///     reports it differently, so the developer knows the silence was not an answer.
    /// </summary>
    [Fact(DisplayName = "A constructor with no source available says so.")]
    public void AConstructorWithNoSourceAvailableSaysSo() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Unused { public Unused() { } }",
                                                   metadataName: "System.Version");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.Plan!.Parameters).Not.IsEmpty();
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.NoSource)).IsTrue();
    }

}
