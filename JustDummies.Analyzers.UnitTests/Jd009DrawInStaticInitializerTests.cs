using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd009DrawInStaticInitializerTests {

    [Fact]
    public async Task Reports_a_draw_in_a_static_field_initializer() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly string Reference = Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
        Check.That(diagnostics[0].GetMessage()).Contains("once for the whole suite");
    }

    [Fact]
    public async Task Reports_a_draw_in_a_static_constructor() {
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly string Reference;

                static Sample() {
                    Reference = Any.String().NonEmpty().Generate();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
    }

    [Fact]
    public async Task Does_not_report_a_static_field_holding_the_generator() {
        // The compliant shape: the recipe is shared, the draw happens per read. RandomSource resolves the source at
        // Generate() time, so a shared generator is safe — only a shared value is not.
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly IAny<string> Reference = Any.String().NonEmpty();

                public static string Next() => Reference.Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_an_instance_initializer() {
        const string source = """
            using JustDummies;

            public class Sample {
                private readonly string _reference = Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_an_ordinary_method() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static string Next() => Any.String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("Any.String().NonEmpty().As(value => new Reference(value)).Generate()")]
    [InlineData("Any.String().NonEmpty().OrNull().Generate()")]
    [InlineData("Any.Int32().Positive().AsNullable().Generate()")]
    public async Task Reports_a_draw_derived_through_a_library_extension(string draw) {
        // Issue #219: the walk stopped at the extension call, whose receiver is its first argument, not its instance,
        // so the derived twin of a reported draw went unreported although it draws once for the whole suite too.
        string source = $$"""
            using JustDummies;

            public sealed class Reference {
                public Reference(string value) { }
            }

            public static class Sample {
                private static readonly object Value = {{draw}};
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
    }

    [Fact]
    public async Task Reports_a_draw_through_a_consumer_extension() {
        // A consumer's extension draws from the generator it is given, and that one is written from Any. The walk
        // once stopped at any extension the library did not declare, which left this draw unreported (issue #365).
        const string source = """
            using JustDummies;

            public static class Extensions {
                public static IAny<string> Trimmed(this IAny<string> generator) => generator;
            }

            public static class Sample {
                private static readonly string Value = Any.String().NonEmpty().Trimmed().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
    }

    [Theory]
    [InlineData("Any.Leg().Generate()")]
    [InlineData("Any.Leg().WithDistanceKm(240).Generate()")]
    [InlineData("new AnyLeg().Generate()")]
    [InlineData("Arbitrary.Leg().Generate()")]
    [InlineData("Shared.Generate()")]
    [InlineData("new HandWrittenLeg().Generate()")]
    [InlineData("Any.ListOf(new AnyLeg()).Generate()")]
    public async Task Reports_a_draw_on_any_generator_whatever_started_the_chain(string draw) {
        // Issue #365: a Generate() on any IAny<T> is a draw, by the library's own contract. A scaffolded entry point,
        // a `new AnyX()`, a static root of the consumer's own, a generator held in a field and a hand-written
        // generator all went unseen while the rule recognised only a chain written from Any itself.
        string source = $$"""
            using JustDummies;

            public sealed class Leg {
                public Leg(int distanceKm) { DistanceKm = distanceKm; }
                public int DistanceKm { get; }
            }

            public sealed class AnyLeg : IAny<Leg> {
                private readonly IAny<int> _distanceKm = Any.Int32().Positive();
                public AnyLeg WithDistanceKm(int distanceKm) => this;
                public Leg Generate() => new Leg(_distanceKm.Generate());
            }

            public sealed class HandWrittenLeg : IAny<Leg> {
                public Leg Generate() => new Leg(1);
            }

            public static class AnyLegEntry {
                extension(Any) {
                    public static AnyLeg Leg() => new AnyLeg();
                }
            }

            public static class Arbitrary {
                public static AnyLeg Leg() => new AnyLeg();
            }

            public static class Sample {
                private static readonly IAny<Leg> Shared = new AnyLeg();
                private static readonly object Value = {{draw}};
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD009");
    }

    [Theory]
    [InlineData("Context.Int32().Generate()")]
    [InlineData("Context.Int32().Positive().OrNull().Generate()")]
    [InlineData("Context.String().NonEmpty().Trimmed().Generate()")]
    [InlineData("Any.WithSeed(1234).String().NonEmpty().Trimmed().Generate()")]
    public async Task Does_not_report_a_draw_from_an_isolated_context_however_it_is_reached(string draw) {
        // The one draw a lifecycle rule must leave alone: an isolated context is unaffected by the ambient scope, held
        // under a name or written inline, and whatever extension the chain goes through on its way to Generate().
        string source = $$"""
            using JustDummies;

            public static class Extensions {
                public static IAny<string> Trimmed(this IAny<string> generator) => generator;
            }

            public static class Sample {
                private static readonly AnyContext Context = Any.WithSeed(1234);
                private static readonly object Value = {{draw}};
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_an_inline_isolated_context() {
        // Issue #219: Any.WithSeed(...) is a static member of Any but hands back an isolated context, not the ambient
        // generator, so a chain written inline from it is no more ambient than one held in a local.
        const string source = """
            using JustDummies;

            public static class Sample {
                private static readonly string Value = Any.WithSeed(1234).String().NonEmpty().Generate();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_draw_in_a_body_handed_on_as_a_value() {
        // Issue #220: a static delegate draws per call, not once for the suite. The second field nests an eager
        // Select inside a stored lambda: the outer lambda is what decides when anything runs.
        const string source = """
            using System;
            using System.Linq;
            using JustDummies;

            public static class Sample {
                private static readonly Func<string> NewTenant = () => Any.String().NonEmpty().Generate();
                private static readonly Func<int[]> NewBatch = () => Enumerable.Range(0, 3).Select(_ => Any.Int32().Generate()).ToArray();
                private static readonly Lazy<int> Later = new Lazy<int>(() => Any.Int32().Generate());
                private static readonly Func<int>[] Factories = new Func<int>[] { () => Any.Int32().Generate() };
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_draw_in_a_lambda_the_initializer_runs_itself() {
        // A lambda handed to an ordinary method, to a constructor or a generic callee that calls it, invoked on the spot,
        // or stored in a container or a field another initializer then reads runs during the initializer: these values
        // are drawn once for the suite.
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using JustDummies;

            public sealed class Runner {
                public Runner(Func<int> next) { Value = next(); }
                public int Value { get; }
            }

            public static class Sample {
                private static readonly int[] Values = Enumerable.Range(0, 3).Select(_ => Any.Int32().Generate()).ToArray();
                private static readonly Runner Eager = new Runner(() => Any.Int32().Generate());
                private static readonly int Value = ((Func<int>)(() => Any.Int32().Generate()))();
                private static readonly object Invoked = Run<Func<int>>(() => Any.Int32().Generate());
                private static readonly object Cast = Unconstrained<Func<int>>(() => Any.Int32().Generate());
                private static readonly int FromArray = new Func<int>[] { () => Any.Int32().Generate() }[0]();
                private static readonly int FromList = new List<Func<int>> { () => Any.Int32().Generate() }[0]();
                private static readonly int FromLazy = new Lazy<int>(() => Any.Int32().Generate()).Value;
                private static readonly Func<int> ReadBack = () => Any.Int32().Generate();
                private static readonly int FromReadBack = ReadBack();

                private static object Run<T>(T body) where T : Delegate => body.DynamicInvoke();
                private static object Unconstrained<T>(T value) => ((Delegate)(object)value!).DynamicInvoke()!;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(9);
    }

    // The rule reads the initializer's own code and does not follow the methods it calls: a helper whose body draws is
    // the documented blind spot, and returning the generator from the helper is the remedy (issue #439).
    [Fact]
    public async Task Does_not_follow_a_static_initializer_into_a_helper_that_draws() {
        const string source = """
            using JustDummies;

            public static class Fixtures {
                public static int Rate(int floor) => Any.Int32().GreaterThanOrEqualTo(floor).Generate();
            }

            public static class Sample {
                private static readonly int ViaHelper = Fixtures.Rate(1);
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new DrawInStaticInitializerAnalyzer(), source);

        Check.That(diagnostics).IsEmpty();
    }
}
