using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The one rule between two parameters the engine draws rather than marks: a direct comparison of two parameters
///     of one ordered type, decided at the draw — through <c>Any.Pair(…).Having.&lt;relation&gt;()</c> when neither
///     is pinned, a bound on the other side when one is (§5.3, #405, #428). Every other rule between parameters stays
///     <c>to verify</c>.
/// </summary>
public sealed class OrderedPairTests {

    [Theory(DisplayName = "A direct comparison of two parameters is read as the order the constructor demands.")]
    [InlineData("if (start > end)", "start", "end", false)]
    [InlineData("if (start >= end)", "start", "end", true)]
    [InlineData("if (end < start)", "start", "end", false)]
    [InlineData("if (end <= start)", "start", "end", true)]
    [InlineData("if ((start) > end!)", "start", "end", false)]
    public void ADirectComparisonIsReadAsAnOrder(string condition, string first, string second, bool strict) {
        ScaffoldOutcome outcome = Scaffolded("int start, int end", $"{condition} {{ throw new ArgumentException(nameof(start)); }}");

        OrderedPairing pairing = outcome.Plan!.Pairings.Single();

        Check.That(pairing.First).IsEqualTo(first);
        Check.That(pairing.Second).IsEqualTo(second);
        Check.That(pairing.Strict).IsEqualTo(strict);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.RequiresVerification)).ContainsOnlyElementsThatMatch(doubt => !doubt);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.Provenance.HasFlag(Provenance.Guard))).ContainsOnlyElementsThatMatch(guard => guard);
    }

    [Theory(DisplayName = "Every other relation between parameters is still marked on every one it spans.")]
    [InlineData("int start, int end", "if (start + 1 > end) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("int? start, int? end", "if (start > end) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("char start, char end", "if (start > end) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("int start, long end", "if (start > end) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("int start, int end", "Kept = start; if (start > end) { throw new ArgumentException(nameof(start)); }")]
    public void EveryOtherRelationIsStillMarked(string parameters, string body) {
        ScaffoldOutcome outcome = Scaffolded(parameters, body);

        Check.That(outcome.Plan!.Pairings).IsEmpty();
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.RequiresVerification)).ContainsExactly(true, true);
    }

    // The order is drawn by the pair and the gap is not, so a developer sent to either recipe would narrow one side
    // alone: the note names the pair's method, the one place both are drawn together (#459).
    [Fact(DisplayName = "A rule the engine cannot read between two paired parameters points to the pair's method.")]
    public void AnUnreadRuleBetweenPairedParametersPointsToThePair() {
        ScaffoldOutcome outcome = Scaffolded("int start, int end", """
                                                                   if (start >= end) { throw new ArgumentException(nameof(end)); }
                                                                   if (end - start > 31) { throw new ArgumentException(nameof(end)); }
                                                                   """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note))
             .ContainsExactly("ties end to start; StartAndEnd() draws their order, not this guard: write it there",
                              "ties end to start; StartAndEnd() draws their order, not this guard: write it there");
        Check.That(outcome.File!.SourceText).Contains("— ties end to start; StartAndEnd() draws their order, not this guard: write it there — then delete these two lines.");
    }

    // Reached outside TieOrMark: a throwing branch that does something else first is marked as a rejection, not read,
    // and a guard below the scan is read on a reading of its own. Both are still rules between the two paired ones.
    [Theory(DisplayName = "A rule between two paired parameters points to the pair whatever path marks it.")]
    [InlineData("""
                if (start >= end) { throw new ArgumentException(nameof(end)); }
                if (end - start > 31) { Console.WriteLine(); throw new ArgumentException(nameof(end)); }
                """)]
    [InlineData("""
                if (start >= end) { throw new ArgumentException(nameof(end)); }
                Kept = start;
                if (end - start > 31) { throw new ArgumentException(nameof(end)); }
                """)]
    public void AnUnreadRuleBetweenPairedParametersPointsToThePairOnEveryPath(string body) {
        ScaffoldOutcome outcome = Scaffolded("int start, int end", body);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note))
             .ContainsExactly("ties end to start; StartAndEnd() draws their order, not this guard: write it there",
                              "ties end to start; StartAndEnd() draws their order, not this guard: write it there");
    }

    // A same-type void helper is not folded, only marked at its call: still a rule between the two paired parameters.
    [Fact(DisplayName = "A throwing helper handed both paired parameters points to the pair.")]
    public void AThrowingHelperHandedBothPairedParametersPointsToThePair() {
        ScaffoldOutcome outcome = Scaffolded("""
                                             using System;

                                             namespace Shop.Domain;

                                             public sealed class Subject {

                                                 public Subject(int start, int end) {
                                                     if (start >= end) { throw new ArgumentException(nameof(end)); }
                                                     ValidateGap(start, end);
                                                 }

                                                 private static void ValidateGap(int start, int end) {
                                                     if (end - start > 31) { throw new ArgumentException(nameof(end)); }
                                                 }

                                             }
                                             """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note))
             .ContainsExactly("ties start to end; StartAndEnd() draws their order, not this guard: write it there",
                              "ties start to end; StartAndEnd() draws their order, not this guard: write it there");
    }

    // Two arguments do not make a rule between them: a helper throwing on one side alone is a guard that side's recipe
    // can honour, so both stay marked with no word of the pair.
    [Fact(DisplayName = "A throwing helper handed both paired parameters but testing one keeps the plain mark.")]
    public void AThrowingHelperTestingOneSideKeepsThePlainMark() {
        ScaffoldOutcome outcome = Scaffolded("""
                                             using System;

                                             namespace Shop.Domain;

                                             public sealed class Subject {

                                                 public Subject(int start, int end) {
                                                     if (start >= end) { throw new ArgumentException(nameof(end)); }
                                                     ValidateStart(start, end);
                                                 }

                                                 private static void ValidateStart(int start, int end) {
                                                     if (start < 0) { throw new ArgumentException(nameof(start)); }
                                                 }

                                             }
                                             """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.RequiresVerification)).ContainsExactly(true, true);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note)).ContainsExactly(null, null);
    }

    // A condition naming both through separate tests is two bounds, each honoured in its own recipe: the marks stay, and
    // no note sends the developer to the pair's method for them.
    [Theory(DisplayName = "Separate tests on each side of a pair are not called a rule between them.")]
    [InlineData("""
                public Subject(int start, int end) {
                    if (start >= end) { throw new ArgumentException(nameof(end)); }
                    Validate(start, end);
                }

                private static void Validate(int start, int end) {
                    if (start < 0 || end > 31) { throw new ArgumentException(nameof(start)); }
                }
                """)]
    [InlineData("""
                public Subject(int start, int end) {
                    if (start >= end) { throw new ArgumentException(nameof(end)); }
                    if (start < 0 || end > 31) { Console.WriteLine(); throw new ArgumentException(nameof(start)); }
                }
                """)]
    [InlineData("""
                public Subject(int start, int end) {
                    if (start >= end) { throw new ArgumentException(nameof(end)); }
                    if (!(start >= 0 && end <= 31)) { throw new ArgumentException(nameof(start)); }
                }
                """)]
    public void SeparateTestsOnEachSideAreNotARuleBetweenThem(string members) {
        ScaffoldOutcome outcome = Scaffolded($$"""
                                               using System;

                                               namespace Shop.Domain;

                                               public sealed class Subject {

                                                   {{members}}

                                               }
                                               """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.RequiresVerification)).ContainsExactly(true, true);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note ?? string.Empty))
             .ContainsOnlyElementsThatMatch(note => !note.Contains("StartAndEnd()"));
    }

    // A throw on a conjunction rejects only where both hold: narrowing either side alone excludes values the
    // constructor accepts (ADR-0128), so it is a rule between the two, and the pair's method is where it belongs.
    [Theory(DisplayName = "A conjunction across both sides of a pair is a rule between them, however it is spelled.")]
    [InlineData("if (start < 0 && end > 31) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("if (!(start >= 0 || end <= 31)) { throw new ArgumentException(nameof(start)); }")]
    [InlineData("if (start < 0 && end > 31) { Console.WriteLine(); throw new ArgumentException(nameof(start)); }")]
    [InlineData("Validate(start, end);")]
    public void AConjunctionAcrossBothSidesIsARuleBetweenThem(string guard) {
        const string pointed = "ties start to end; StartAndEnd() draws their order, not this guard: write it there";

        ScaffoldOutcome outcome = Scaffolded($$"""
                                               using System;

                                               namespace Shop.Domain;

                                               public sealed class Subject {

                                                   public Subject(int start, int end) {
                                                       if (start >= end) { throw new ArgumentException(nameof(end)); }
                                                       {{guard}}
                                                   }

                                                   private static void Validate(int start, int end) {
                                                       if (start < 0 && end > 31) { throw new ArgumentException(nameof(start)); }
                                                   }

                                               }
                                               """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note)).ContainsExactly(pointed, pointed);
    }

    // The rule travels with the order through the hand-off, swapped where the hand-off swaps the values, and is named
    // after the parameters this generator draws, never after the delegated constructor's own.
    [Theory(DisplayName = "A rule between two parameters a constructor hands on points to the pair drawn for them.")]
    [InlineData("public Subject(int low, int high) : this(low, high, true) { }", "ties high to low; LowAndHigh()")]
    [InlineData("public Subject(int low, int high) : this(high, low, true) { }", "ties low to high; HighAndLow()")]
    public void AnUnreadRuleTravelsThroughAHandOff(string handOff, string pointer) {
        ScaffoldOutcome outcome = Scaffolded($$"""
                                               using System;

                                               namespace Shop.Domain;

                                               public sealed class Subject {

                                                   private Subject(int min, int max, bool checkedOnce) {
                                                       if (min > max) { throw new ArgumentException(nameof(min)); }
                                                       if (max - min > 31) { throw new ArgumentException(nameof(max)); }
                                                   }

                                                   {{handOff}}

                                               }
                                               """);

        Check.That(outcome.Plan!.Pairings).HasSize(1);
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note))
             .ContainsExactly(pointer + " draws their order, not this guard: write it there",
                              pointer + " draws their order, not this guard: write it there");
    }

    [Fact(DisplayName = "A rule between two parameters that are not paired keeps its plain note.")]
    public void AnUnreadRuleBetweenUnpairedParametersKeepsItsNote() {
        ScaffoldOutcome outcome = Scaffolded("int start, int end", "if (end - start > 31) { throw new ArgumentException(nameof(end)); }");

        Check.That(outcome.Plan!.Pairings).IsEmpty();
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.UnreadGuard!.Note)).ContainsExactly("ties end to start", "ties end to start");
    }

    // Three parameters in a chain are a relation among three: pairing two of them would leave the third order to luck,
    // and solving it is what the engine does not try (ADR-0046).
    [Fact(DisplayName = "A chain of orders is marked, not paired.")]
    public void AChainOfOrdersIsMarked() {
        ScaffoldOutcome outcome = Scaffolded("int low, int middle, int high", """
                                                                              if (low > middle) { throw new ArgumentException(nameof(low)); }
                                                                              if (middle > high) { throw new ArgumentException(nameof(high)); }
                                                                              """);

        Check.That(outcome.Plan!.Pairings).IsEmpty();
        Check.That(outcome.Plan.Parameters.Select(parameter => parameter.RequiresVerification)).ContainsExactly(true, true, true);
    }

    // A public constructor handing both values to the private one that guards them is the ordinary shape of a value
    // object with a convenience overload, and the order travels with the values — swapped where the hand-off swaps them.
    [Theory(DisplayName = "An order the delegated constructor reads is one between the parameters handed to it.")]
    [InlineData("this(low, high, true)", "low", "high")]
    [InlineData("this(high, low, true)", "high", "low")]
    public void AnOrderTravelsThroughAHandOff(string initializer, string first, string second) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                    using System;

                                                    namespace Shop.Domain;

                                                    public sealed class Subject {

                                                        private Subject(int min, int max, bool checkedOnce) {
                                                            if (min > max) { throw new ArgumentException(nameof(min)); }
                                                        }

                                                        public Subject(int low, int high) : {{initializer}} { }

                                                    }
                                                    """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        OrderedPairing pairing = outcome.Plan!.Pairings.Single();

        Check.That(pairing.First).IsEqualTo(first);
        Check.That(pairing.Second).IsEqualTo(second);
    }

    [Fact(DisplayName = "The pair is drawn in order, pins keep their meaning whatever order they are written in, and the draw decides.")]
    public void ThePairIsDrawnInOrderAndPinsKeepTheirMeaning() {
        const string domain = """
                              using System;

                              namespace Shop.Domain;

                              public sealed class Window {

                                  public Window(int start, int end) {
                                      if (start >= end) { throw new ArgumentException("A window ends after it starts.", nameof(end)); }

                                      Start = start;
                                      End   = end;
                                  }

                                  public int Start { get; }

                                  public int End { get; }

                              }
                              """;
        const string probe = """
                             using System;

                             using Shop.Domain;

                             namespace Shop.Tests;

                             public static class Probe {

                                 public static bool AlwaysInOrder(int draws) {
                                     AnyWindow windows = new AnyWindow();

                                     for (int draw = 0; draw < draws; draw++) {
                                         Window window = windows.Generate();

                                         if (window.Start >= window.End) { return false; }
                                     }

                                     return true;
                                 }

                                 public static bool EndPinnedBoundsStart() {
                                     AnyWindow windows = new AnyWindow().WithEnd(10).WithStart(s => s.Positive());

                                     for (int draw = 0; draw < 200; draw++) {
                                         Window window = windows.Generate();

                                         if (window.End != 10 || window.Start <= 0 || window.Start >= 10) { return false; }
                                     }

                                     return true;
                                 }

                                 public static string PinsOutsideTheRecipesInEitherOrder() {
                                     AnyWindow bounded    = new AnyWindow().WithStart(s => s.Between(0, 100)).WithEnd(e => e.Between(0, 100));
                                     Window    startFirst = bounded.WithStart(500).WithEnd(600).Generate();
                                     Window    endFirst   = bounded.WithEnd(600).WithStart(500).Generate();

                                     return $"{startFirst.Start}-{startFirst.End} {endFirst.Start}-{endFirst.End}";
                                 }

                                 public static string? LonePinNoRecipeCanFollow() {
                                     try {
                                         new AnyWindow().WithEnd(e => e.Between(0, 100)).WithStart(500).Generate();

                                         return null;
                                     } catch (JustDummies.ConflictingAnyConstraintException conflict) {
                                         return conflict.Message;
                                     }
                                 }

                                 public static string? PinsOutOfOrder() {
                                     AnyWindow pinned = new AnyWindow().WithStart(10).WithEnd(10);

                                     try {
                                         pinned.Generate();

                                         return null;
                                     } catch (ArgumentException refusal) {
                                         return refusal.Message;
                                     }
                                 }

                             }
                             """;

        string            emitted     = Subject.ScaffoldByName("Window", domain).File!.SourceText;
        CSharpCompilation compilation = EmittedCodeCompiler.CompileAllWith([emitted, probe], domain);

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();

        Type probes = Loaded(compilation).GetType("Shop.Tests.Probe")!;

        using IDisposable seeded = Any.UseSeed(20261102);

        Check.That((bool)probes.GetMethod("AlwaysInOrder")!.Invoke(null, [2000])!).IsTrue();
        Check.That((bool)probes.GetMethod("EndPinnedBoundsStart")!.Invoke(null, [])!).IsTrue();
        Check.That((string)probes.GetMethod("PinsOutsideTheRecipesInEitherOrder")!.Invoke(null, [])!).IsEqualTo("500-600 500-600");
        Check.That((string?)probes.GetMethod("LonePinNoRecipeCanFollow")!.Invoke(null, [])).Contains("GreaterThan(500)", "Between(0, 100)");
        Check.That((string?)probes.GetMethod("PinsOutOfOrder")!.Invoke(null, [])).StartsWith("A window ends after it starts.");
    }

    // A pair adds three identifiers to the file, derived from the two parameter names, and a parameter can already
    // declare one of them — or two pairs can synthesise one. Such a scaffold declared a member twice (CS0102), so the
    // order is left to verify instead, as any order the engine does not draw.
    [Theory(DisplayName = "An order whose pair would redeclare an identifier the file has is marked, and the file compiles.")]
    [InlineData("int start, int end, int startPin", "if (start > end) { throw new ArgumentException(nameof(start)); }", 2)]
    [InlineData("int a, int bAndC, int aAndB, int c", "if (a > bAndC) { throw new ArgumentException(nameof(a)); } if (aAndB > c) { throw new ArgumentException(nameof(c)); }", 4)]
    public void AnOrderWhosePairWouldRedeclareAnIdentifierIsMarked(string parameters, string body, int marked) {
        string          source  = SubjectWith(parameters, body);
        ScaffoldOutcome outcome = Scaffolded(source);

        Check.That(outcome.Plan!.Pairings).IsEmpty();
        Check.That(outcome.Plan.Parameters.Count(parameter => parameter.RequiresVerification)).IsEqualTo(marked);

        CSharpCompilation compiled = EmittedCodeCompiler.CompileWith(VerifySentinel.StrippedFrom(outcome.File!.SourceText), source);

        Check.That(EmittedCodeCompiler.ErrorsIn(compiled)).IsEmpty();
    }

    /// <summary>One <c>Subject</c> whose constructor takes <paramref name="parameters" /> and runs <paramref name="body" />.</summary>
    private static ScaffoldOutcome Scaffolded(string parameters, string body) {
        return Scaffolded(SubjectWith(parameters, body));
    }

    private static ScaffoldOutcome Scaffolded(string source) {
        ScaffoldOutcome outcome = Subject.Scaffold(source);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome;
    }

    private static string SubjectWith(string parameters, string body) {
        return $$"""
                 using System;

                 namespace Shop.Domain;

                 public sealed class Subject {

                     public Subject({{parameters}}) {
                         {{body}}
                     }

                     public object? Kept { get; private set; }

                 }
                 """;
    }

    private static Assembly Loaded(CSharpCompilation compilation) {
        using MemoryStream assembly = new();

        EmitResult emitted = compilation.Emit(assembly);

        Check.That(emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();

        return Assembly.Load(assembly.ToArray());
    }

}
