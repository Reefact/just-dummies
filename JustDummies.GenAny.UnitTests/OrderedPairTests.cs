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
///     of one ordered type, drawn through <c>Any.OrderedPair</c> (§5.3, #405). Every other rule between parameters
///     stays <c>to verify</c>.
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

    [Fact(DisplayName = "The pair is drawn in order, pins keep their meaning, and two pins out of order are refused at the second.")]
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

                                 public static string? PinsOutOfOrder() {
                                     try {
                                         new AnyWindow().WithStart(10).WithEnd(10);

                                         return null;
                                     } catch (JustDummies.ConflictingAnyConstraintException conflict) {
                                         return conflict.Message;
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
        Check.That((string?)probes.GetMethod("PinsOutOfOrder")!.Invoke(null, [])).IsNotNull();
    }

    /// <summary>One <c>Subject</c> whose constructor takes <paramref name="parameters" /> and runs <paramref name="body" />.</summary>
    private static ScaffoldOutcome Scaffolded(string parameters, string body) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                    using System;

                                                    namespace Shop.Domain;

                                                    public sealed class Subject {

                                                        public Subject({{parameters}}) {
                                                            {{body}}
                                                        }

                                                        public object? Kept { get; private set; }

                                                    }
                                                    """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome;
    }

    private static Assembly Loaded(CSharpCompilation compilation) {
        using MemoryStream assembly = new();

        EmitResult emitted = compilation.Emit(assembly);

        Check.That(emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();

        return Assembly.Load(assembly.ToArray());
    }

}
