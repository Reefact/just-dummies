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

    /// <summary>The parentheses a writer is free to add do not make the subject something else.</summary>
    [Theory(DisplayName = "Parentheses around the subject leave the guard readable.")]
    [InlineData("if ((value).Length < 8) { throw new ArgumentException(nameof(value)); }")]
    [InlineData("if ((value.Length) < 8) { throw new ArgumentException(nameof(value)); }")]
    public void ParenthesesAroundTheSubjectLeaveTheGuardReadable(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty().WithMinLength(8)");
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
