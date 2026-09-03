using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd014RejectedConstantArgumentTests {

    [Theory]
    [InlineData("Dummy.String().WithLengthBetween(10, 5)",        "transposed")]
    [InlineData("Dummy.Int32().Between(10, 5)",                   "transposed")]
    [InlineData("Dummy.String().WithLength(-1)",                  "negative")]
    [InlineData("Dummy.String().WithMaxLength(-1)",               "negative")]
    [InlineData("Dummy.String().WithLength(2000000)",             "1,000,000")]
    // A ceiling steers the draw as a floor does, so ADR-0076 caps it too — one rule for every size argument.
    // This row said otherwise until the scaffolder emitted WithMaxLength(1048576) for an ordinary 1 MiB limit,
    // compiled clean, drew no diagnostic, and threw inside the emitted parameterless constructor.
    [InlineData("Dummy.String().WithMaxLength(2000000)",          "1,000,000")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithMaxCount(2000000)", "1,000,000")]
    [InlineData("Dummy.Int32().MultipleOf(0)",                    "strictly positive")]
    [InlineData("Dummy.Decimal().WithScale(29)",                  "[0, 28]")]
    [InlineData("Dummy.String().StartingWith(\"\")",              "must not be empty")]
    [InlineData("Dummy.String().WithChars(\"\")",                 "must not be empty")]
    public async Task Reports_an_argument_the_guard_rejects(string expression, string expectedFragment) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD014");
        Check.That(diagnostics[0].GetMessage()).Contains(expectedFragment);
    }

    [Fact]
    public async Task Reports_a_collection_count_range_that_is_transposed() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Int32()).WithCountBetween(10, 2);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD014");
    }

    [Fact]
    public async Task Reports_an_empty_choice_pool() {
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.String().OneOf();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("at least one value");
    }

    [Fact]
    public async Task Reports_a_non_positive_granularity() {
        const string source = """
            using System;
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.DateTime().WithGranularity(TimeSpan.Zero);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).Contains("strictly positive");
    }

    [Theory]
    [InlineData("Dummy.String().WithLengthBetween(5, 10)")]
    [InlineData("Dummy.Int32().Between(5, 10)")]
    [InlineData("Dummy.Int32().Between(5, 5)")]
    [InlineData("Dummy.String().WithLength(0)")]
    [InlineData("Dummy.String().WithMaxLength(0)")]
    // Verified rather than assumed: UriSpec.RequireSegmentCount refuses a negative count and nothing else, so
    // the one member of this family that really is uncapped keeps its silence.
    [InlineData("Dummy.Uri().Relative().WithPathSegments(2000000)")]
    [InlineData("Dummy.Int32().MultipleOf(7)")]
    [InlineData("Dummy.Decimal().WithScale(28)")]
    [InlineData("Dummy.String().StartingWith(\"ORD-\")")]
    [InlineData("Dummy.String().OneOf(\"EUR\")")]
    [InlineData("Dummy.ListOf(Dummy.Int32()).WithCountBetween(2, 10)")]
    public async Task Does_not_report_a_legal_argument(string expression) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = {{expression}};
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_non_constant_argument() {
        // Only a constant is decidable; a variable is the run-time guard's business and must stay unreported.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M(int minimum, int maximum) {
                    _ = Dummy.Int32().Between(minimum, maximum);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_guard_asserting_negative_test() {
        // The shape this repository writes hundreds of times; reporting it would fight the suite that documents
        // the guard's behaviour.
        const string source = """
            using System;
            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Dummy.Int32().Between(10, 5));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_same_named_method_on_another_type() {
        const string source = """
            public sealed class Other {
                public Other Between(int minimum, int maximum) => this;
            }

            public static class Sample {
                public static void M() {
                    _ = new Other().Between(10, 5);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_Containing_on_a_collection() {
        // Containing(TItem) is not the string overload; the non-empty-text check must key on the parameter type.
        const string source = """
            using JustDummies;

            public static class Sample {
                public static void M() {
                    _ = Dummy.ListOf(Dummy.Int32()).Containing(0);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new RejectedConstantArgumentAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

}
