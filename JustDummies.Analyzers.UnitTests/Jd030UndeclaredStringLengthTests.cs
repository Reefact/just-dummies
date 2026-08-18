using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd030UndeclaredStringLengthTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredStringLengthAnalyzer(), source);
    }

    [Fact]
    public async Task Reports_a_chain_that_declares_no_length() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD030");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Info);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 0 to 1024 characters");
    }

    [Theory]
    [InlineData("_ = Any.String().WithLength(10).Generate();")]
    [InlineData("_ = Any.String().WithMinLength(3).Generate();")]
    [InlineData("_ = Any.String().WithMaxLength(50).Generate();")]
    [InlineData("_ = Any.String().WithLengthBetween(1, 5).Generate();")]
    [InlineData("_ = Any.String().Alpha().WithMaxLength(8).Generate();")]
    public async Task Does_not_report_a_chain_that_settles_its_length(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_value_set_which_supplies_its_own_lengths() {
        // OneOf replaces the layout: the caller supplied the values, so their lengths are theirs and no spread
        // applies. Nothing here is left unsaid.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().OneOf(\"EUR\", \"USD\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Does_not_report_a_pattern_whose_shape_is_the_whole_specification() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.StringMatching(@\"[A-Z]{3}\").Generate();");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_chain_carrying_only_NonEmpty() {
        // NonEmpty raises the floor to one and leaves the ceiling where it was, so the draw still spans the whole
        // spread. Reporting it is the point: this is the chain the scaffolder emits, and the one most likely to be
        // mistaken for a bounded string. The interval shifts with the floor rather than staying at the constant
        // the unconstrained chain reports -- measured against the library, NonEmpty draws 1 to 1025.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.String().NonEmpty().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 1 to 1025 characters");
    }

    [Fact]
    public async Task Does_not_report_a_chain_written_as_a_negative_test() {
        // The guard the other rules share: a chain that is the sole body of a lambda ARGUMENT is being asserted
        // about, not written as arrange code.
        const string source = """
            using System;
            using JustDummies;

            public static class Expect {
                public static bool Throws<T>(Func<string> code) where T : Exception => true;
            }

            public static class Sample {
                public static void M() {
                    Expect.Throws<ArgumentException>(() => Any.String().Generate());
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredStringLengthAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_the_factory_call_even_when_a_later_statement_narrows_the_reassigned_variable() {
        // The rule is syntactic (its documented Scope): it reasons about the Any.String() expression as written, not
        // about what the variable it is assigned to ends up holding. The factory call is reported the moment it is
        // written with no length, whatever a later statement does to the same variable.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "        AnyString s = Any.String();\n            s = s.WithMaxLength(5);\n            _ = s.Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("This string dummy declares no length: it draws 0 to 1024 characters");
    }

}
