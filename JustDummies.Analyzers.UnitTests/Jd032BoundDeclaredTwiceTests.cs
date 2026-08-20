using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd032BoundDeclaredTwiceTests {

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using System;

            using JustDummies;

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new BoundDeclaredTwiceAnalyzer(), source);
    }

    private static async Task<Diagnostic> SingleDiagnosticAsync(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(1);

        return diagnostics[0];
    }

    private static async Task NothingReportedAsync(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_minimum_declared_twice() {
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.String().WithMinLength(8).WithMinLength(10).Generate();");

        Check.That(diagnostic.Id).IsEqualTo("JD032");
        Check.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Warning);
        Check.That(diagnostic.GetMessage()).IsEqualTo("WithMinLength is declared twice on this chain — 8 then 10 — and only the tighter applies");
    }

    [Fact]
    public async Task Reports_both_writing_orders_alike() {
        // The dead call is the LOOSER bound either way -- erased by the second in one order, inert in the other.
        // One phenomenon, one message, so the author does not meet a different rule for a difference they did not
        // make on purpose.
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.String().WithMinLength(10).WithMinLength(8).Generate();");

        Check.That(diagnostic.GetMessage()).IsEqualTo("WithMinLength is declared twice on this chain — 10 then 8 — and only the tighter applies");
    }

    [Theory]
    [InlineData("_ = Any.String().WithMaxLength(50).WithMaxLength(20).Generate();", "WithMaxLength — 50 then 20")]
    [InlineData("_ = Any.ListOf(Any.Int32()).WithMinCount(1).WithMinCount(3).Generate();", "WithMinCount — 1 then 3")]
    [InlineData("_ = Any.ListOf(Any.Int32()).WithMaxCount(9).WithMaxCount(5).Generate();", "WithMaxCount — 9 then 5")]
    [InlineData("_ = Any.Int32().GreaterThanOrEqualTo(1).GreaterThanOrEqualTo(5).Generate();", "GreaterThanOrEqualTo — 1 then 5")]
    [InlineData("_ = Any.Int32().LessThanOrEqualTo(90).LessThanOrEqualTo(50).Generate();", "LessThanOrEqualTo — 90 then 50")]
    [InlineData("_ = Any.Double().GreaterThanOrEqualTo(1.5).GreaterThanOrEqualTo(2.5).Generate();", "GreaterThanOrEqualTo — 1.5 then 2.5")]
    [InlineData("_ = Any.TimeSpan().LessThanOrEqualTo(TimeSpan.FromHours(2)).LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();", "LessThanOrEqualTo — TimeSpan.FromHours(2) then TimeSpan.FromHours(1)")]
    [InlineData("_ = Any.DateTime().AfterOrEqualTo(DateTime.UnixEpoch).AfterOrEqualTo(DateTime.Today).Generate();", "AfterOrEqualTo — DateTime.UnixEpoch then DateTime.Today")]
    public async Task Reports_every_vocabulary(string body, string expected) {
        Diagnostic diagnostic = await SingleDiagnosticAsync(body);

        Check.That(diagnostic.GetMessage()).IsEqualTo($"{expected.Replace(" — ", " is declared twice on this chain — ")} — and only the tighter applies");
    }

    [Theory]
    [InlineData("_ = Any.Int32().GreaterThan(1).GreaterThan(5).Generate();")]
    [InlineData("_ = Any.DateTime().Before(DateTime.MaxValue).Before(DateTime.Today).Generate();")]
    public async Task Reports_a_strict_bound_declared_twice(string body) {
        // Strict bounds belong here where JD031 refuses them: nothing is rewritten, so pairing two calls to the
        // same name is sound whatever the type.
        Diagnostic diagnostic = await SingleDiagnosticAsync(body);

        Check.That(diagnostic.Id).IsEqualTo("JD032");
    }

    [Fact]
    public async Task Does_not_report_bounds_held_under_a_name() {
        // A generator is an immutable recipe: `atLeastEight` still exists and still draws a minimum of eight, so
        // WithMinLength(8) is not dead and reporting it would be a false positive. What makes the looser call dead
        // inside one chain is that the intermediate generator is unnamed and unreachable.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    AnyString atLeastEight = Any.String().WithMinLength(8);
                    AnyString atLeastTen   = atLeastEight.WithMinLength(10);
                    _ = atLeastEight.Generate();
                    _ = atLeastTen.Generate();
            """);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("_ = Any.String().NonEmpty().WithMinLength(8).Generate();")]
    [InlineData("_ = Any.Int32().Positive().GreaterThanOrEqualTo(5).Generate();")]
    public async Task Does_not_report_a_bound_reached_through_an_alias(string body) {
        // NonEmpty IS a minimum length of one and Positive IS a minimum of one, so these chains do reach the same
        // bound twice. Matching on the NAME leaves them alone: choosing the alias says something about intent the
        // explicit bound does not, and which of two correct spellings to prefer is ADR-0077's question.
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.String().WithMinLength(8).WithMaxLength(20).Generate();")]
    [InlineData("_ = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();")]
    public async Task Does_not_report_two_different_bounds(string body) {
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.String().WithLengthBetween(1, 50).WithMaxLength(20).Generate();")]
    [InlineData("_ = Any.Int32().Between(1, 50).Between(2, 20).Generate();")]
    public async Task Does_not_report_a_range_form(string body) {
        // A range carries two bounds in one call, so no whole call dies: out of scope on purpose, and silent.
        await NothingReportedAsync(body);
    }

    [Fact]
    public async Task Does_not_report_a_chain_asserted_to_throw() {
        const string source = """
            using System;

            using JustDummies;

            public static class Check2 {
                public static void ThatCode(Func<object> code) { }
            }

            public static class Sample {
                public static void M() {
                    Check2.ThatCode(() => Any.String().WithMinLength(8).WithMinLength(10));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerTestHarness.GetDiagnosticsAsync(new BoundDeclaredTwiceAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_a_chain_once_even_when_two_bounds_are_doubled() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    _ = Any.String().WithMinLength(1).WithMinLength(8).WithMaxLength(90).WithMaxLength(20).Generate();
            """);

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

}
