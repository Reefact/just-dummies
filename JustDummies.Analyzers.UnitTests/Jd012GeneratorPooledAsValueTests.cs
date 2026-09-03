using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd012GeneratorPooledAsValueTests {

    [Fact]
    public async Task Reports_a_pool_of_generators() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IDummy<DummyInt32> pool = Dummy.OneOf(Dummy.Int32().Positive(), Dummy.Int32().Negative());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD012");
        Check.That(diagnostics[0].GetMessage()).Contains("Generate()");
    }

    [Fact]
    public async Task Reports_a_pool_of_generators_on_a_seeded_context() {
        // OneOf and ElementOf are mirrored on DummyContext; a rule keyed on Dummy alone would miss half the surface.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    DummyContext context = Dummy.WithSeed(1234);
                    IDummy<DummyInt32> pool = context.OneOf(context.Int32(), context.Int32());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD012");
    }

    [Fact]
    public async Task Does_not_report_a_pool_of_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IDummy<int> pool = Dummy.OneOf(1, 2, 3);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_pool_of_generated_values() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IDummy<int> pool = Dummy.OneOf(Dummy.Int32().Positive().Generate(), Dummy.Int32().Negative().Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_another_types_OneOf() {
        const string source = """
            using JustDummies;

            public static class Other {
                public static T OneOf<T>(params T[] values) => values[0];
            }

            public static class Sample {
                public static void M() {
                    DummyInt32 chosen = Other.OneOf(Dummy.Int32(), Dummy.Int32());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
