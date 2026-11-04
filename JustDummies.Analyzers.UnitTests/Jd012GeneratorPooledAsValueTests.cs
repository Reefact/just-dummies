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
                    IAny<AnyInt32> pool = Any.OneOf(Any.Int32().Positive(), Any.Int32().Negative());
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
        // OneOf and ElementOf are mirrored on AnyContext; a rule keyed on Any alone would miss half the surface.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    AnyContext context = Any.WithSeed(1234);
                    IAny<AnyInt32> pool = context.OneOf(context.Int32(), context.Int32());
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
                    IAny<int> pool = Any.OneOf(1, 2, 3);
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
                    IAny<int> pool = Any.OneOf(Any.Int32().Positive().Generate(), Any.Int32().Negative().Generate());
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
                    AnyInt32 chosen = Other.OneOf(Any.Int32(), Any.Int32());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    // Then draws one recipe from the pool and then a value from it, so a pool flattened that way yields values, split
    // across the recipes and replayed from one seed: the remedy the message names, not the mistake (issue #435).
    [Theory]
    [InlineData("IAny<int> anyLeaf = Any.ElementOf(leaves).Then(leaf => leaf);")]
    [InlineData("IAny<int> anyLeaf = Any.OneOf(leaves[0], leaves[1]).Then(leaf => leaf);")]
    [InlineData("IAny<int> anyLeaf = Any.WithSeed(1234).ElementOf(leaves).Then(leaf => leaf);")]
    public async Task Does_not_report_a_pool_that_Then_flattens_into_values(string statement) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    AnyInt32[] leaves = { Any.Int32().Positive(), Any.Int32().Negative() };
                    {{statement}}
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics).IsEmpty();
    }

    [Fact]
    public async Task Still_reports_a_pool_that_Then_keeps_as_recipes() {
        // A Then whose result is itself a generator still hands out recipes, so the pool remains the mistake.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    IAny<AnyInt32> anyRecipe = Any.OneOf(Any.Int32().Positive(), Any.Int32().Negative()).Then(recipe => Any.Fixed(recipe));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new GeneratorPooledAsValueAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD012");
    }

}
