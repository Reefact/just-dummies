using System.Linq;

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

    /// <summary>
    ///     An <c>else if</c> chain is readable only where every branch before the one being read throws
    ///     unconditionally too — the moment one does not, reaching a later branch depends on that earlier
    ///     condition, and §9 already names a cross-parameter rule as out of reach.
    /// </summary>
    /// <remarks>
    ///     <c>if (first &lt; 0) { first = 0; } else if (second &gt; 100) { throw … }</c>: reaching
    ///     <c>second</c>'s test presupposes <c>first &gt;= 0</c>, which is not <c>second</c>'s own invariant to
    ///     state. Both parameters are marked unread — <c>second</c> because its guard is not standalone, and
    ///     <c>first</c> because the branch it sits in was handed to the same rule.
    /// </remarks>
    [Fact(DisplayName = "An else-if branch whose predecessor does not throw unconditionally is unread.")]
    public void AnElseIfBranchWhosePredecessorDoesNotThrowUnconditionallyIsUnread() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int first, int second) {
                                                           if (first < 0) { first = 0; } else if (second > 100) { throw new ArgumentOutOfRangeException(nameof(second)); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.Guard)).IsFalse();
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

    /// <summary>
    ///     A guard below a reassignment of its own parameter is unread, and the ones above it still stand.
    /// </summary>
    /// <remarks>
    ///     The one shape where the tool was confidently wrong rather than blind: it saw the guard, parsed it
    ///     correctly, and attributed it to a value the generator no longer draws. Only an assignment to a field
    ///     or a property ended the leading scan, so writing over the parameter itself ended nothing and the
    ///     test below it read as a bound on the drawn value — <c>GreaterThanOrEqualTo(0)</c> over a real domain
    ///     of 0 to 100, reported as inferred, with a draw of a million throwing inside the constructor.
    ///     <para>
    ///         What the reading keeps matters as much as what it drops. The guard above the reassignment is
    ///         true of the drawn value, so the parameter is narrowed <b>and</b> marked rather than losing both
    ///         to one blunt refusal.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A guard below a reassignment of its parameter is unread, and one above it still reads.")]
    public void AGuardBelowAReassignmentOfItsParameterIsUnread() {
        string body = """
                              if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                              value = 100 - value;
                              if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     Every spelling of a reassignment ends the reading, because every one of them changes the value.
    /// </summary>
    /// <remarks>
    ///     Which is why the engine does not enumerate them. The compound forms are assignments like any
    ///     other, but the increments are not assignments at all in the syntax tree, and the last two rows are
    ///     not written on the parameter's own name in any form: a <c>ref</c> local aliases it, and a
    ///     deconstruction writes through a tuple. A list of spellings reads as complete and is not, so the
    ///     question goes to the compiler's own data-flow analysis, which answers for all of them at once.
    ///     The nesting row is the other axis: a write inside a block is still a write.
    /// </remarks>
    [Theory(DisplayName = "A guard below a reassignment is unread, in every spelling a reassignment takes.")]
    [InlineData("        value = 100 - value;")]
    [InlineData("        value += 10;")]
    [InlineData("        value++;")]
    [InlineData("        --value;")]
    [InlineData("        if (value > 100) { value = 100; }")]
    [InlineData("        ref int alias = ref value;\n        alias = 100 - alias;")]
    public void AGuardBelowAReassignmentIsUnreadInEverySpelling(string reassignment) {
        string body = reassignment + "\n        if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }";

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A reassignment inside an <c>else</c> ends the reading below it, and not the condition above it.
    /// </summary>
    /// <remarks>
    ///     Both halves are the same piece of timing: a condition is evaluated before anything its own
    ///     <c>else</c> body runs, so the first guard is true of the drawn value and stays, while the value that
    ///     <c>else</c> leaves behind is what every statement below is about. Collecting the reassignment before
    ///     reading the statement rather than after it would lose the first constraint for nothing.
    /// </remarks>
    [Fact(DisplayName = "A reassignment inside an else ends the reading below it, not the condition above it.")]
    public void AReassignmentInsideAnElseEndsTheReadingBelowIt() {
        string body = """
                              if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); } else { value = 100 - value; }
                              if (value > 1000) { throw new ArgumentOutOfRangeException(nameof(value)); }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     The call spelling of a guard obeys the reassignment rule too.
    /// </summary>
    /// <remarks>
    ///     A throw helper the closed set knows is read straight into a numeric row, by a path of its own that
    ///     never passes through the condition reader — so a reassignment above it would have produced the same
    ///     false bound the <c>if</c> spelling did, and the rule has to be spelled in both places.
    /// </remarks>
    [Fact(DisplayName = "A throw helper below a reassignment of its parameter is unread, not read.")]
    public void AThrowHelperBelowAReassignmentIsUnread() {
        string body = """
                              value = 100 - value;
                              ArgumentOutOfRangeException.ThrowIfNegative(value);
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     The mark follows the parameter that was written over, and no other.
    /// </summary>
    /// <remarks>
    ///     Ending the whole scan at a reassignment would be one line shorter and would drop the guards of every
    ///     other parameter the constructor declares — one constraint the engine must not read, traded for
    ///     several it must.
    /// </remarks>
    [Fact(DisplayName = "A reassignment ends the reading for its own parameter only.")]
    public void AReassignmentEndsTheReadingForItsOwnParameterOnly() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int percent;
                                                       private readonly string label;

                                                       public Subject(int percent, string label) {
                                                           percent = 100 - percent;

                                                           if (percent < 0) { throw new ArgumentOutOfRangeException(nameof(percent)); }
                                                           if (label.Length < 8) { throw new ArgumentException(nameof(label)); }

                                                           this.percent = percent;
                                                           this.label = label;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(outcome.Plan.Parameters[1].Expression).IsEqualTo("Any.String().WithMinLength(8)");
        Check.That(outcome.Plan.Parameters[1].RequiresVerification).IsFalse();
    }

    // A reassignment says nothing on its own about which values are admissible, so a constructor that
    // normalises and guards nothing more reads exactly as it did before. This narrowing costs only the guards
    // that were being read wrong.
    [Theory(DisplayName = "A reassignment with no guard below it changes nothing.")]
    [InlineData("        value = value.Trim();")]
    [InlineData("""
                        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                        value = value.Trim();
                """)]
    public void AReassignmentWithNoGuardBelowItChangesNothing(string body) {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", body);

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     A guard below a write it <b>shares a statement with</b> is unread, and the condition above that
    ///     write still reads.
    /// </summary>
    /// <remarks>
    ///     The reading used to place a write by the statement it sat in rather than by where it sat, so a
    ///     write and a guard inside one <c>else</c> were read as though the guard came first. This shape was
    ///     measured emitting <c>Between(0, 50)</c> — a domain whose real answer is 50 and above, so every draw
    ///     but one is rejected by the constructor, under a recap reporting the parameter fully inferred. Worse
    ///     than the shape #112 was opened for, where the second guard happened to restate the first.
    /// </remarks>
    [Fact(DisplayName = "A guard below a write inside the same statement is unread, and the condition above it still reads.")]
    public void AGuardBelowAWriteInsideTheSameStatementIsUnread() {
        string body = """
                              if (value < 0) {
                                  throw new ArgumentOutOfRangeException(nameof(value));
                              } else {
                                  value = 100 - value;
                                  ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 50);
                              }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Expression).Not.Contains("50");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A write through a deconstruction ends the reading, though nothing about it is an assignment to the
    ///     parameter's own name.
    /// </summary>
    /// <remarks>
    ///     The left side of <c>(value, other) = …</c> is a tuple, and asking the compiler what it resolves to
    ///     yields no parameter at all — so an enumeration of the assignment spellings passed it over and the
    ///     guard below it was read as a bound on the drawn value. Which is why which writes exist is asked of
    ///     data-flow analysis rather than of the syntax: it answers for the spellings nobody listed.
    /// </remarks>
    [Fact(DisplayName = "A write through a deconstruction ends the reading of that parameter.")]
    public void AWriteThroughADeconstructionEndsTheReading() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(int value, int other) {
                                                           (value, other) = (100 - value, other);

                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                           kept = value + other;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32()");
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A write through an <c>out</c> argument ends the reading, though there is no assignment node at all.
    /// </summary>
    /// <remarks>
    ///     <c>out</c> on the <b>constructor's own</b> parameters needs no handling — §5.1 declines such a
    ///     constructor outright — but a parameter handed to somebody else's <c>out</c> is written just the
    ///     same, and the syntax carries nothing an assignment walk would recognise.
    /// </remarks>
    [Fact(DisplayName = "A write through an out argument ends the reading of that parameter.")]
    public void AWriteThroughAnOutArgumentEndsTheReading() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(string text, int value) {
                                                           bool parsed = int.TryParse(text, out value);

                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                           kept = parsed ? value : 0;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[1].Expression).IsEqualTo("Any.Int32()");
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A guard sharing a loop with a write to its parameter is unread, whichever of the two the source
    ///     puts first.
    /// </summary>
    /// <remarks>
    ///     Source order is not execution order inside a loop: the write below the guard runs above it on the
    ///     next turn. This shape accepts nothing between 51 and 99 and rejects 40, which reading the guard
    ///     against source order alone calls <c>LessThanOrEqualTo(50)</c> — a generator drawing 40 under a
    ///     constructor that refuses it.
    /// </remarks>
    [Fact(DisplayName = "A guard sharing a loop with a write to its parameter is unread.")]
    public void AGuardSharingALoopWithAWriteIsUnread() {
        string body = """
                              while (value < 100) {
                                  ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 50);
                                  value += 30;
                              }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A <c>goto</c> ends the reading of every parameter the body writes, wherever the guard sits.
    /// </summary>
    /// <remarks>
    ///     A backward jump puts a write above a guard the source puts below it, and there is no position the
    ///     engine could read that says so. Rare enough to refuse wholesale rather than model — and refusing is
    ///     what a shape the engine cannot place deserves.
    /// </remarks>
    [Fact(DisplayName = "A goto ends the reading of a parameter the body writes.")]
    public void AGotoEndsTheReadingOfAParameterTheBodyWrites() {
        string body = """
                              start:
                              ArgumentOutOfRangeException.ThrowIfNegative(value);
                              value--;

                              if (value > 0) { goto start; }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // A loop is no reason in itself to refuse a guard for the question above: what a loop changes is where a
    // WRITE can have run, so on that question a parameter the loop never writes reads as it would outside
    // one. It answers the other question differently, and this pairs with the case above to say so: a `for`
    // may run its body no times, so a guard inside one states nothing about the value the generator draws
    // whether the loop writes the parameter or not.
    [Fact(DisplayName = "A guard inside a loop is unread, a loop being free to run its body no times.")]
    public void AGuardInsideALoopIsUnread() {
        string body = """
                              for (int index = 0; index < 3; index++) {
                                  ArgumentOutOfRangeException.ThrowIfNegative(value);
                              }
                      """;

        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A write inside something the body <b>calls</b> ends the reading wherever that thing is written.
    /// </summary>
    /// <remarks>
    ///     A local function or a lambda runs when it is called, not where it is declared, so its position
    ///     says nothing about whether its write ran before a guard: <c>Bump();</c> above the guard and
    ///     <c>void Bump() { value++; }</c> below it writes first and reads last. §9 already names a guard
    ///     reached only through indirection the tool does not follow; a <b>write</b> reached that way is the
    ///     same gap seen from the other side, and this is the answer it gets.
    /// </remarks>
    [Theory(DisplayName = "A write inside a local function or a lambda ends the reading, wherever it is declared.")]
    [InlineData("""
                        Action bump = () => value++;
                        bump();
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    [InlineData("""
                        Bump();
                        ArgumentOutOfRangeException.ThrowIfNegative(value);

                        void Bump() { value++; }
                """)]
    public void AWriteInsideSomethingCalledEndsTheReading(string body) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A construct whose order the walk does not claim to read is unread <b>only</b> where it writes the
    ///     parameter — never for being that construct.
    /// </summary>
    /// <remarks>
    ///     Placement used to work from a list of parents it knew how to walk, and to yield nothing for the
    ///     rest — so a <c>finally</c> after a <c>try</c> that wrote, a <c>catch</c> over the same, and a
    ///     <c>switch</c> whose governing expression wrote all read as though nothing had run. Silence was the
    ///     unsafe default: a construct nobody had listed waved the guard through.
    ///     <para>
    ///         The rows come in pairs on purpose. One writes and one does not, and only the write decides —
    ///         which is what separates a rule that refuses what it cannot place from one that refuses whatever
    ///         it does not recognise.
    ///     </para>
    ///     <para>
    ///         The <c>catch</c> and <c>switch</c> pairs that used to sit here moved to the conditioning
    ///         theory above, and their move is the point rather than a tidying: whether the guard runs at all
    ///         is asked before whether a write preceded it, and neither construct runs its body on every
    ///         path. What is left is the pair where both questions are answered — a <c>finally</c> runs
    ///         whatever happens, so only the write decides.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A construct the walk does not read is unread only where it writes the parameter.")]
    [InlineData("Any.Int32()", true, """
                        try {
                            value = 100 - value;
                        } finally {
                            ArgumentOutOfRangeException.ThrowIfNegative(value);
                        }
                """)]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(0)", false, """
                        try {
                        } finally {
                            ArgumentOutOfRangeException.ThrowIfNegative(value);
                        }
                """)]
    public void AConstructIsUnreadOnlyWhereItWritesTheParameter(string expected, bool unread, string body) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsEqualTo(unread);
    }

    /// <summary>
    ///     A <c>using</c> resource is evaluated before the body it scopes, so a write in it ends the reading
    ///     of the guards inside.
    /// </summary>
    /// <remarks>
    ///     The pair is the point again: the same <c>using</c>, the same guard, and the only difference is
    ///     whether the resource expression writes the parameter.
    /// </remarks>
    [Fact(DisplayName = "A using resource that writes the parameter ends the reading, and one that does not leaves it read.")]
    public void AUsingResourceThatWritesEndsTheReading() {
        ScaffoldOutcome writing = Using("Acquire(out value)");
        ScaffoldOutcome quiet   = Using("Acquire(out int ignored)");

        Check.That(writing.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32()");
        Check.That(writing.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();

        Check.That(quiet.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(quiet.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    private static ScaffoldOutcome Using(string resource) {
        return Subject.Scaffold($$"""
                                 public sealed class Subject {

                                     private readonly int kept;

                                     public Subject(int value) {
                                         using ({{resource}}) {
                                             ArgumentOutOfRangeException.ThrowIfNegative(value);
                                         }

                                         kept = value;
                                     }

                                     private static System.IO.MemoryStream Acquire(out int candidate) { candidate = 42; return new System.IO.MemoryStream(); }

                                 }
                                 """);
    }

    /// <summary>
    ///     A constructor initializer that writes the parameter ends the reading of the body's guards; one
    ///     that does not leaves them read.
    /// </summary>
    /// <remarks>
    ///     <c>: this(…)</c> and <c>: base(…)</c> run <b>entire</b> before the first statement of the body, so
    ///     a write in one has already replaced the drawn value by the time any guard below it is evaluated.
    ///     The placement walk started at the body, where an initializer is not a preceding statement but a
    ///     sibling of the block itself — so it saw nothing, and
    ///     <c>: this(Normalise(ref value))</c> above <c>if (value &lt; 0)</c> read as a bound on what the
    ///     generator draws. An ordinary delegation to a wider overload, and the guard was about the value the
    ///     delegation had computed.
    ///     <para>
    ///         The quiet row is the half that keeps the fix honest: having an initializer at all is not a
    ///         reason to refuse, only writing the parameter in one is. It also pins that the compiler will
    ///         analyse an initializer argument — a region reported as unanalysable would refuse here, and
    ///         this row would fail rather than pass quietly.
    ///     </para>
    ///     <para>
    ///         The direct rows are a second question, not a second spelling of the first. Where the modifier
    ///         sits <b>inside</b> the argument's expression the analysis covers it; where it belongs to the
    ///         <b>argument</b>, the region analysed is the bare identifier under it and the compiler reports
    ///         it read rather than written. <c>: this(ref value, true)</c> read
    ///         <c>GreaterThanOrEqualTo(0)</c> over a delegation that had already replaced the drawn value,
    ///         which is why the reading asks the invoked constructor what it receives by reference.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A constructor initializer is unread only where it writes the parameter.")]
    [InlineData("Any.Int32()", true, "this(Normalize(ref value), true)")]
    [InlineData("Any.Int32()", true, "this(Normalize(out value), true)")]
    [InlineData("Any.Int32()", true, "this(ref value, true)")]
    [InlineData("Any.Int32()", true, "this(out value, \"\")")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(0)", false, "this(value, true)")]
    public void AnInitializerIsUnreadOnlyWhereItWritesTheParameter(string expected, bool unread, string initializer) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(int value) : {{initializer}} {
                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                           kept = value;
                                                       }

                                                       private Subject(int seed, bool _) { kept = seed; }

                                                       private Subject(ref int seed, bool _) { seed = 100 - seed; kept = seed; }

                                                       private Subject(out int seed, string _) { seed = 42; kept = seed; }

                                                       private static int Normalize(ref int value) { value = 100 - value; return value; }

                                                       private static int Normalize(out int candidate) { candidate = 42; return candidate; }

                                                   }
                                                   """);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsEqualTo(unread);
    }

    // `: base(…)` is the same rule and a different syntax node, so it is pinned rather than assumed to
    // follow: a base initializer may write a parameter through `ref` or `out` exactly as a `this` one may.
    // Both spellings, because they reach the answer by different routes — the nested one through data flow
    // over the argument's expression, the direct one through the base constructor's own symbol.
    [Theory(DisplayName = "A base initializer that writes the parameter ends the reading too.")]
    [InlineData("base(Normalize(out value))")]
    [InlineData("base(ref value)")]
    public void ABaseInitializerThatWritesEndsTheReading(string initializer) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   public abstract class Root {
                                                       protected Root(int seed) { }

                                                       protected Root(ref int seed) { seed = 100 - seed; }
                                                   }

                                                   public sealed class Subject : Root {

                                                       private readonly int kept;

                                                       public Subject(int value) : {{initializer}} {
                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                           kept = value;
                                                       }

                                                       private static int Normalize(out int candidate) { candidate = 42; return candidate; }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Expression).IsEqualTo("Any.Int32()");
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // The initializer obeys the same scoping as every other write: it ends the reading of the parameter it
    // writes and of no other, so the constructor's remaining guards are untouched.
    [Theory(DisplayName = "An initializer write ends the reading of its own parameter only.")]
    [InlineData("this(Normalize(ref percent), label, true)")]
    [InlineData("this(ref percent, label, true)")]
    public void AnInitializerWriteEndsTheReadingOfItsOwnParameterOnly(string initializer) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   public sealed class Subject {

                                                       private readonly int percent;
                                                       private readonly string label;

                                                       public Subject(int percent, string label) : {{initializer}} {
                                                           if (percent < 0) { throw new ArgumentOutOfRangeException(nameof(percent)); }
                                                           if (label.Length < 8) { throw new ArgumentException(nameof(label)); }

                                                           this.percent = percent;
                                                           this.label = label;
                                                       }

                                                       private Subject(int seed, string text, bool _) { percent = seed; label = text; }

                                                       private Subject(ref int seed, string text, bool _) { seed = 100 - seed; percent = seed; label = text; }

                                                       private static int Normalize(ref int value) { value = 100 - value; return value; }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(outcome.Plan.Parameters[1].Expression).IsEqualTo("Any.String().WithMinLength(8)");
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A constructor with no body of its own reads no guards at all, so the placement question never
    ///     arises for it.
    /// </summary>
    /// <remarks>
    ///     The modern constructor forms §5.1 already accepts — a positional record, a primary constructor —
    ///     declare no <c>BaseMethodDeclarationSyntax</c> to read, so §6 reports them as having no source
    ///     rather than as having no guards. That is not an accident worth relying on silently: it is the
    ///     reason an initializer on one of those forms cannot produce a bound the generator would draw
    ///     against, and it is pinned here so the day one of them grows a body is the day this test says so.
    /// </remarks>
    [Theory(DisplayName = "A constructor form with no body of its own reads no guards, and says so.")]
    [InlineData("public sealed record Subject(int Value);")]
    [InlineData("public sealed class Subject(int value) { public int Kept => value; }")]
    public void AConstructorFormWithNoBodyReadsNoGuards(string declaration) {
        ScaffoldOutcome outcome = Subject.Scaffold(declaration);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Provenance.HasFlag(Provenance.NoSource)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsFalse();
    }

    // A statement that rejects nothing constrains nothing: defaulting a value is not refusing it, so there is
    // no invariant here for a draw to violate and nothing to send the developer looking at.
    [Theory(DisplayName = "A statement that rejects no value is not a guard, and says nothing.")]
    [InlineData("if (value <= 0) { value = 1; }")]
    public void AStatementThatRejectsNoValueIsNotAGuard(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A statement that throws is a guard whatever its shape, so one the recognised set cannot parse is
    ///     reported as unread rather than passed over.
    /// </summary>
    /// <remarks>
    ///     The one thing a <c>throw</c> before the first assignment to state cannot be is ordinary logic: it
    ///     refuses to build the object. Each shape below fell past the recognised-guard branch and, carrying no
    ///     call that names the parameter, past the call rule as well — so a throwing guard in plain sight read
    ///     exactly like a parameter nobody had constrained.
    /// </remarks>
    [Theory(DisplayName = "A throwing guard the set cannot parse is unread, not silent.")]
    [InlineData("""
                        if (value < 0) {
                            Console.WriteLine("out of range");
                            throw new ArgumentOutOfRangeException(nameof(value));
                        }
                """)]
    [InlineData("        if (value < 0 || value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }")]
    public void AThrowingGuardTheSetCannotParseIsUnread(string guard) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", guard);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // The throw names its rejected parameter with `nameof`, so counting that would make the message the
    // evidence instead of the test — and would mark a parameter a guard about something else merely mentions.
    [Fact(DisplayName = "A throw naming a parameter only in its message does not mark that parameter.")]
    public void AThrowNamingAParameterOnlyInItsMessageDoesNotMarkIt() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int value, string other) {
                                                           if (value < 0) {
                                                               Console.WriteLine(value);
                                                               throw new ArgumentException(nameof(other));
                                                           }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A guard whose <c>else</c> is empty reads exactly like one with no <c>else</c> at all — the shape
    ///     that used to stop guard reading before this widening, now read the same either way.
    /// </summary>
    [Fact(DisplayName = "A throw naming a parameter only in its message does not mark it, even beside an else.")]
    public void AThrowNamingAParameterOnlyInItsMessageDoesNotMarkItBesideAnElse() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       public Subject(int value, string other) {
                                                           if (value < 0) { throw new ArgumentException(nameof(other)); } else { }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(outcome.Plan.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A plain <c>else</c> is walked, not skipped: what it contains is read by the same two rules every
    ///     other unrecognised leading statement goes through.
    /// </summary>
    /// <remarks>
    ///     The chain walker reads a branch's condition and then has to say something about the branch that
    ///     carries no condition of its own. Doing nothing there would be silent, and silence is the one answer
    ///     §9 refuses: validation delegated to a helper inside an <c>else</c> is exactly the shape the
    ///     <c>helper-delegated-length</c> corpus shape exists for, with an <c>else</c> in front of it.
    ///     <para>
    ///         Both cases below pass just as well with the terminal-<c>else</c> arm deleted <b>if nothing
    ///         asserts them</b> — which is what makes them worth writing: the empty <c>else</c> the other
    ///         cases use reaches that arm and finds nothing to do, so it proves the arm runs and not that it
    ///         does anything.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A guard delegated to a helper inside a plain else is unread, not silent.")]
    public void AGuardDelegatedToAHelperInsideAPlainElseIsUnread() {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", """
                                                                            if (value is null) {
                                                                                throw new ArgumentNullException(nameof(value));
                                                                            } else {
                                                                                Validate(value);
                                                                            }

                                                                            static void Validate(string candidate) {
                                                                                if (candidate.Length < 8) { throw new ArgumentException(nameof(candidate)); }
                                                                            }
                                                                    """);

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     A conditional throw nested inside a plain <c>else</c> is a guard whose shape the closed set cannot
    ///     parse from there, so the bound it states is reported as unread rather than lost in silence.
    /// </summary>
    /// <remarks>
    ///     The floor from the outer branch is still read and still correct — an <c>else</c> cannot weaken what
    ///     the branch before it rejects — so the parameter carries a real constraint <b>and</b> the mark. That
    ///     pairing is the point: without the mark the emitted file compiles, claims the parameter inferred, and
    ///     draws past a ceiling the developer plainly wrote.
    /// </remarks>
    [Fact(DisplayName = "A conditional throw nested inside a plain else keeps the outer bound and is marked unread.")]
    public void AConditionalThrowNestedInsideAPlainElseKeepsTheOuterBoundAndIsMarkedUnread() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", """
                                                                        if (value < 0) {
                                                                            throw new ArgumentOutOfRangeException(nameof(value));
                                                                        } else {
                                                                            if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                                        }
                                                                """);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // Not itself a guard — the block throws conditionally on the surrounding `if`, but only once every other
    // statement in it has run — and it still calls something involving the parameter the closed set of §5.3
    // does not parse. That call could be a guard the tool cannot read as easily as it could be a log line, and
    // it cannot tell the two apart, so it reports the same doubt it would over a helper it cannot see into.
    [Fact(DisplayName = "A call involving the parameter, beside a throw the recognised set does not parse alone, is unread.")]
    public void ACallInvolvingTheParameterBesideAnUnrecognisedThrowIsUnread() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int",
                                                          "if (value <= 0) { Console.WriteLine(value); throw new ArgumentOutOfRangeException(nameof(value)); }");

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // The shape the bug report behind this whole reading path was about: validation delegated entirely to a
    // helper, with no `if` in the constructor for §5.3 to parse at all. Before this case, the parameter read
    // exactly like one with no guard on it — `None`, indistinguishable from truly unconstrained — and the
    // neutral generator it kept could draw a value the helper would have rejected on every real construction.
    [Fact(DisplayName = "A guard delegated entirely to a helper, with no `if` to read, is unread rather than silent.")]
    public void AGuardDelegatedToAHelperIsUnreadRatherThanSilent() {
        string guard = """
                               Validate(value);

                               static void Validate(string candidate) {
                                   if (string.IsNullOrWhiteSpace(candidate)) { throw new ArgumentException(nameof(candidate)); }
                               }
                       """;

        ScaffoldedParameter parameter = Subject.GuardedBy("string", guard);

        // The base table's own NonEmpty() (§5.2) — close to the helper's real invariant, and not it: a
        // generator this neutral still draws the empty-adjacent strings the helper rejects on every real
        // construction, which is the whole cost of a guard the tool cannot read.
        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A guard the closed set already knows reads the same written as a throw helper as written as an
    ///     <c>if</c> — same expression, same provenance, and neither blocks the developer's build.
    /// </summary>
    /// <remarks>
    ///     These are one invariant in two spellings. Reading only the older one sent the modern one to the
    ///     call rule and blocked compilation over a generator that was already exactly right, which is the
    ///     worst of both outcomes: nothing tightened, and nothing compiling either.
    /// </remarks>
    [Theory(DisplayName = "A guard the set knows reads the same as a throw helper as it does as an `if`.")]
    [InlineData("        if (value is null) { throw new ArgumentNullException(nameof(value)); }",
                "        ArgumentNullException.ThrowIfNull(value);")]
    [InlineData("        if (string.IsNullOrEmpty(value)) { throw new ArgumentException(nameof(value)); }",
                "        ArgumentException.ThrowIfNullOrEmpty(value);")]
    [InlineData("        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }",
                "        ArgumentException.ThrowIfNullOrWhiteSpace(value);")]
    public void AGuardTheSetKnowsReadsTheSameEitherWay(string written, string called) {
        ScaffoldedParameter asIf   = Subject.GuardedBy("string", written);
        ScaffoldedParameter asCall = Subject.GuardedBy("string", called);

        Check.That(asCall.Expression).IsEqualTo(asIf.Expression);
        Check.That(asCall.Provenance).IsEqualTo(asIf.Provenance);
        Check.That(asCall.RequiresVerification).IsFalse();
    }

    // The subject-identity discipline the comparison rows keep, on the call form too: the helper has to be
    // about this parameter, not about something reached from it.
    [Fact(DisplayName = "A throw helper naming something other than the parameter is not read as its guard.")]
    public void AThrowHelperNamingSomethingElseIsNotReadAsItsGuard() {
        ScaffoldedParameter parameter = Subject.GuardedBy("string", "        ArgumentNullException.ThrowIfNull(value.Length);");

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    // A call whose value is USED is producing something, and normalising a value or copying a collection says
    // nothing about which values are admissible. Flagging those blocked the compilation of constructors
    // carrying no guard at all — which is most of them — so the discarded result is what separates a call made
    // to reject from a call made to produce.
    [Theory(DisplayName = "A call whose result is used is production, not a guard, and does not block.")]
    [InlineData("string", "        this.kept = value.Trim();")]
    [InlineData("string", "        this.kept = value.ToUpperInvariant();")]
    [InlineData("IReadOnlyList<string>", "        this.kept = value.ToList();")]
    public void ACallWhoseResultIsUsedIsNotAGuard(string parameterType, string body) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, body);

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     A helper the closed set knows says nothing about the drawn value where a condition decides whether
    ///     it runs at all.
    /// </summary>
    /// <remarks>
    ///     <c>if (strict) { ThrowIfNegative(value); }</c> was measured reading
    ///     <c>Any.Int32().GreaterThanOrEqualTo(0)</c>, reported as inferred with nothing worth looking at,
    ///     over a constructor whose <c>strict: false</c> callers construct happily with a negative. The loss
    ///     is silent rather than loud — every draw still compiles and still constructs, so nothing sends the
    ///     developer looking, which is what makes it worse than a crash.
    ///     <para>
    ///         The braceless row is not a duplicate: a branch without a block reaches the call rule with one
    ///         less node between the call and the <c>if</c>, which is exactly where an off-by-one walk would
    ///         let it through.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A throw helper the set knows is unread where a condition decides whether it runs.")]
    [InlineData("        if (strict) { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    [InlineData("        if (strict) ArgumentOutOfRangeException.ThrowIfNegative(value);")]
    public void AConditionedThrowHelperIsUnread(string guard) {
        ScaffoldedParameter parameter = ConditionedSubject(guard).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     Every construct that decides whether the statement below it runs conditions the helper inside it,
    ///     whatever that construct is.
    /// </summary>
    /// <remarks>
    ///     The <c>if</c> is only the spelling the report arrived in. A loop may run its body no times, a
    ///     <c>switch</c> picks one section among several, a <c>catch</c> runs only when something threw, and
    ///     a lambda body runs where it is called rather than where it is written — and a <c>try</c> beside a
    ///     <c>catch</c> is worse than conditioned: the guard runs, and the rejection it exists to make is
    ///     swallowed, leaving the constructor accepting exactly what the guard refuses.
    ///     <para>
    ///         The last two rows moved here from the write-placement theory below, where they were pinned
    ///         reading <c>GreaterThanOrEqualTo(0)</c>. They were this defect all along:
    ///         <c>switch (value) { case 0: ThrowIfNegative(value); }</c> runs the helper only where it cannot
    ///         throw, and a <c>catch</c> over an empty <c>try</c> never runs at all, yet both narrowed the
    ///         draw as though the guard had held.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A throw helper is unread inside every construct that decides whether it runs.")]
    [InlineData("        while (strict) { ArgumentOutOfRangeException.ThrowIfNegative(value); break; }")]
    [InlineData("        foreach (bool flag in new[] { strict }) { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    [InlineData("        Action check = () => { ArgumentOutOfRangeException.ThrowIfNegative(value); }; check();")]
    [InlineData("        try { ArgumentOutOfRangeException.ThrowIfNegative(value); } catch (OverflowException) { }")]
    [InlineData("        switch (value) { case 0: ArgumentOutOfRangeException.ThrowIfNegative(value); break; }")]
    [InlineData("        try { } catch (Exception) { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    public void AThrowHelperInsideAConditioningConstructIsUnread(string guard) {
        ScaffoldedParameter parameter = ConditionedSubject(guard).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A construct that only scopes the body it wraps decides nothing, and the helper inside it reads as
    ///     it would outside one.
    /// </summary>
    /// <remarks>
    ///     The pair to the theory above, and the reason the rule is about conditioning rather than about
    ///     nesting. A <c>using</c> acquires its resource and runs the body, a <c>lock</c> takes its lock and
    ///     runs the body, a <c>checked</c> block runs the body under different arithmetic — none of the three
    ///     can skip it. A refusal here would cost the developer a confirmation over a guard that plainly
    ///     holds.
    /// </remarks>
    [Theory(DisplayName = "A throw helper inside a construct that only scopes its body still reads.")]
    [InlineData("        using (new System.IO.MemoryStream()) { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    [InlineData("        lock (this) { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    [InlineData("        checked { ArgumentOutOfRangeException.ThrowIfNegative(value); }")]
    public void AThrowHelperInsideAScopingConstructStillReads(string guard) {
        ScaffoldedParameter parameter = ConditionedSubject(guard).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A condition gating one parameter's helper says nothing about another parameter's unconditional
    ///     one, and the mark is scoped accordingly.
    /// </summary>
    /// <remarks>
    ///     The same scoping a reassignment already keeps: refusing outright would drop the guards of every
    ///     other parameter the constructor declares, trading one constraint that must not be read for several
    ///     that must. The <c>bool</c> doing the gating is not itself marked — it is tested, not guarded.
    /// </remarks>
    [Fact(DisplayName = "A condition gating one parameter's helper leaves another parameter's helper read.")]
    public void AConditionGatingOneHelperLeavesAnotherRead() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int kept;

                                                       public Subject(bool strict, int first, int second) {
                                                           if (strict) { ArgumentOutOfRangeException.ThrowIfNegative(first); }
                                                           ArgumentOutOfRangeException.ThrowIfNegative(second);

                                                           kept = first + second;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters[0].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();

        Check.That(outcome.Plan.Parameters[1].Expression).IsEqualTo("Any.Int32()");
        Check.That(outcome.Plan.Parameters[1].Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();

        Check.That(outcome.Plan.Parameters[2].Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(outcome.Plan.Parameters[2].Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     An unconditional helper above a conditioned one keeps its constraint, and the parameter carries
    ///     the mark as well.
    /// </summary>
    /// <remarks>
    ///     The pairing §5.6 asks for, on the call spelling: what the engine could read is true of the drawn
    ///     value and is kept, and what it could not is said out loud rather than dropped in silence. Without
    ///     the mark the emitted file compiles, claims the parameter inferred, and draws past a ceiling the
    ///     developer plainly wrote.
    /// </remarks>
    [Fact(DisplayName = "An unconditional throw helper above a conditioned one keeps its bound and is marked.")]
    public void AnUnconditionalThrowHelperAboveAConditionedOneKeepsItsBound() {
        ScaffoldedParameter parameter =
            ConditionedSubject("""
                                       ArgumentOutOfRangeException.ThrowIfNegative(value);
                                       if (strict) { ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100); }
                               """).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     A helper inside an <c>else</c> whose branch throws unconditionally still reads, because every
    ///     construction that survives went through that <c>else</c>.
    /// </summary>
    /// <remarks>
    ///     The one shape a conditioning rule must not take away, and the reason the walk stops where the
    ///     reading handed it the statement rather than climbing to the body: reaching the <c>else</c> means
    ///     the condition was false, and the branch above refuses outright, so the helper runs on every path
    ///     that constructs at all. It is the same reasoning that lets an <c>else if</c> chain be read one
    ///     branch at a time.
    /// </remarks>
    [Fact(DisplayName = "A throw helper inside an else after a branch that throws still reads.")]
    public void AThrowHelperInsideATerminalElseStillReads() {
        ScaffoldedParameter parameter =
            ConditionedSubject("""
                                       if (strict) {
                                           throw new ArgumentException(nameof(strict));
                                       } else {
                                           ArgumentOutOfRangeException.ThrowIfNegative(value);
                                       }
                               """).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>A <c>Subject</c> whose guard may lean on a <c>bool</c> beside the parameter it is about.</summary>
    private static ScaffoldOutcome ConditionedSubject(string guard) {
        return Subject.Scaffold($$"""
                                 public sealed class Subject {

                                     private readonly int kept;

                                     public Subject(bool strict, int value) {
                                 {{guard}}

                                         kept = value;
                                     }

                                 }
                                 """);
    }

    /// <summary>
    ///     Two parameters normalised on consecutive lines are read the same way, whichever comes first.
    /// </summary>
    /// <remarks>
    ///     The scan stops at the first assignment to state, so while a used result still counted as doubt the
    ///     verdict fell on whichever parameter happened to be assigned first and spared the other — the same
    ///     two calls, the same two parameters, opposite outcomes decided by statement order. Nobody chose that,
    ///     and a mark that moves with the line order is not a judgement about the code.
    /// </remarks>
    [Fact(DisplayName = "Two normalised parameters read the same, whichever is assigned first.")]
    public void TwoNormalisedParametersReadTheSameWhicheverIsAssignedFirst() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly string name;
                                                       private readonly string city;

                                                       public Subject(string name, string city) {
                                                           this.name = name.Trim();
                                                           this.city = city.Trim();
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Plan!.Parameters.Select(parameter => parameter.RequiresVerification))
             .IsEquivalentTo(false, false);
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

    /// <summary>
    ///     A guard a statement above it can jump past states nothing about the drawn value, whichever
    ///     spelling it is written in.
    /// </summary>
    /// <remarks>
    ///     The other half of the question, and the half no ancestor of the guard can answer:
    ///     <c>if (strict) { kept = value; return; }</c> encloses nothing at all, and yet
    ///     <c>new Subject(strict: true, value: -5)</c> constructs happily while the reading emitted
    ///     <c>GreaterThanOrEqualTo(0)</c> — inferred, with nothing to look at, over half a domain the
    ///     constructor admits.
    ///     <para>
    ///         The third row is the one that decides where the fix belongs: the same <c>return</c> above an
    ///         <c>if</c> guard reads exactly as wrongly, so the question is asked of the leading scan rather
    ///         than of the call rule. The last is the shape a jump takes when it is not a <c>return</c> —
    ///         <c>-1</c> is the one value that reaches the assignment without meeting the guard.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A guard a jump above it can skip is unread, in either spelling.")]
    [InlineData("""
                        if (strict) { kept = value; return; }
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    [InlineData("""
                        if (strict) return;
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    [InlineData("""
                        if (strict) { kept = value; return; }
                        if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                """)]
    [InlineData("""
                        switch (strict) { case true: kept = value; return; default: break; }
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    [InlineData("""
                        if (value == -1) { goto assign; }
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                        assign:
                """)]
    public void AGuardAJumpAboveItCanSkipIsUnread(string body) {
        ScaffoldedParameter parameter = ConditionedSubject(body).Plan!.Parameters[1];

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    // The rule is positional, not a refusal of any body that returns: a jump BELOW a guard cannot skip it,
    // and refusing there would cost a constraint for nothing. This is the pair that keeps the rule honest —
    // the same `return`, the same guard, and only their order differs.
    [Fact(DisplayName = "A jump below a guard leaves it read.")]
    public void AJumpBelowAGuardLeavesItRead() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", """
                                                                        ArgumentOutOfRangeException.ThrowIfNegative(value);

                                                                        if (value == 7) { return; }
                                                                """);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A <c>return</c> inside a body the constructor merely declares leaves that constructor, not this
    ///     one, so the guards below it still read.
    /// </summary>
    /// <remarks>
    ///     The row that decided the mechanism. An ordinary helper declared among the leading statements
    ///     nearly always carries a <c>return</c>, so a rule matching the spelling in the tree would have
    ///     refused the guards under every one of them — a mark on constructors that carry no jump at all.
    ///     The compiler's own control-flow analysis excludes both bodies from the region's exit points, and
    ///     is asked instead.
    /// </remarks>
    [Theory(DisplayName = "A return inside a lambda or a local function is not the constructor's.")]
    [InlineData("""
                        static int Doubled(int candidate) { return candidate * 2; }
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    [InlineData("""
                        Func<int, int> doubled = candidate => { return candidate * 2; };
                        ArgumentOutOfRangeException.ThrowIfNegative(value);
                """)]
    public void AReturnInsideADeferredBodyIsNotTheConstructors(string body) {
        ScaffoldedParameter parameter = Subject.GuardedBy("int", body);

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().GreaterThanOrEqualTo(0)");
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A recognised library's method the table carries no measured row for is declared validation the
    ///     engine cannot vouch for — the mark, not silence, and never an approximated bound (ADR-0086).
    /// </summary>
    /// <remarks>
    ///     <c>IsBetween</c> is deliberately in this list: its boundary semantics was not measured, and a row
    ///     written from what the name suggests is exactly how a range guard goes confidently wrong — the two
    ///     libraries already disagree about a ceiling. A bound that is no compile-time constant earns the same
    ///     answer, the discipline every comparison row keeps.
    /// </remarks>
    [Theory(DisplayName = "A library method without a measured row is marked, never approximated.")]
    [InlineData("int", "CommunityToolkit.Diagnostics.Guard.IsBetween(value, 0, 100, nameof(value));", "Any.Int32()")]
    [InlineData("int", "CommunityToolkit.Diagnostics.Guard.IsGreaterThan(value, int.Parse(\"1\"), nameof(value));", "Any.Int32()")]
    public void ALibraryMethodWithoutAMeasuredRowIsMarked(string parameterType, string guard, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     The assigned spelling answers the same placement questions as the discarded one: conditioned, it
    ///     states an invariant of the paths that reach it; below a write to its own subject, one of a value
    ///     the constructor computed. Both earn the mark the discarded spelling would.
    /// </summary>
    [Fact(DisplayName = "A guard-assignment under a condition is marked, not read.")]
    public void AGuardAssignmentUnderAConditionIsMarked() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       public int Points { get; }

                                                       public Subject(bool strict, int value) {
                                                           if (strict) { Points = Guard.Against.Negative(value); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter value = outcome.Plan!.Parameters[1];

        Check.That(value.Expression).IsEqualTo("Any.Int32()");
        Check.That(value.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    [Fact(DisplayName = "A guard-assignment below a write to its subject is marked, not read.")]
    public void AGuardAssignmentBelowAWriteToItsSubjectIsMarked() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       public string Name { get; }

                                                       public Subject(string name) {
                                                           name = name.Trim();
                                                           Name = Guard.Against.NullOrWhiteSpace(name);
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter name = outcome.Plan!.Parameters.Single();

        Check.That(name.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(name.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
    }

    /// <summary>
    ///     Assigned back to the parameter itself, the helper is a write the placement rules refuse to read
    ///     past — same statement, same value, no vouching. The mark converts what used to be the silent
    ///     returning-helper residue of §9 into a confirmation.
    /// </summary>
    [Fact(DisplayName = "A guard assigned back to its own parameter is marked, not silent.")]
    public void AGuardAssignedBackToItsOwnParameterIsMarked() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using Ardalis.GuardClauses;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       private readonly string kept;

                                                       public Subject(string value) {
                                                           value = Guard.Against.NullOrWhiteSpace(value);
                                                           kept  = value;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter value = outcome.Plan!.Parameters.Single();

        Check.That(value.Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(value.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(value.RequiresVerification).IsTrue();
    }

}
