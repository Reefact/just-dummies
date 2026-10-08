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
    public async Task Does_not_report_a_draw_through_a_consumer_extension() {
        // A consumer's own extension could draw from anywhere, so the walk stops there: the conservative side.
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

}
