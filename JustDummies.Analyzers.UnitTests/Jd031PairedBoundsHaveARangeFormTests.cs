using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd031PairedBoundsHaveARangeFormTests {

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

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new PairedBoundsHaveARangeFormAnalyzer(), source);
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
    public async Task Reports_a_string_chain_naming_the_range_form() {
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.String().WithMinLength(8).WithMaxLength(20).Generate();");

        Check.That(diagnostic.Id).IsEqualTo("JD031");
        Check.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Info);
        Check.That(diagnostic.GetMessage()).IsEqualTo("These two bounds are the range WithLengthBetween(8, 20)");
    }

    [Theory]
    [InlineData("_ = Any.ListOf(Any.Int32()).WithMinCount(1).WithMaxCount(5).Generate();", "WithCountBetween(1, 5)")]
    [InlineData("_ = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();", "Between(1, 50)")]
    [InlineData("_ = Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero).LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();", "Between(TimeSpan.Zero, TimeSpan.FromHours(1))")]
    [InlineData("_ = Any.DateTime().AfterOrEqualTo(DateTime.UnixEpoch).BeforeOrEqualTo(DateTime.MaxValue).Generate();", "Between(DateTime.UnixEpoch, DateTime.MaxValue)")]
    public async Task Reports_every_vocabulary(string body, string expected) {
        Diagnostic diagnostic = await SingleDiagnosticAsync(body);

        Check.That(diagnostic.GetMessage()).IsEqualTo($"These two bounds are the range {expected}");
    }

    [Fact]
    public async Task Reports_the_range_in_parameter_order_whatever_the_writing_order() {
        // The bound written first carries the diagnostic, but the call the message names is always (minimum,
        // maximum) -- that is the range method's own parameter order, and reversing it would not compile.
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.String().WithMaxLength(20).WithMinLength(8).Generate();");

        Check.That(diagnostic.GetMessage()).IsEqualTo("These two bounds are the range WithLengthBetween(8, 20)");
    }

    [Fact]
    public async Task Reaches_a_floating_point_type_where_no_constant_folding_could() {
        // The rule matches names and quotes the argument's syntax, so it never needs to evaluate a bound. That is
        // what lets it reach the types whose values an integral constraint model cannot represent.
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.Double().GreaterThanOrEqualTo(1.5).LessThanOrEqualTo(9.5).Generate();");

        Check.That(diagnostic.GetMessage()).IsEqualTo("These two bounds are the range Between(1.5, 9.5)");
    }

    [Fact]
    public async Task Reports_a_pair_of_equal_bounds_as_the_range_and_never_as_the_exact_form() {
        // WithLength(8) is NOT this pair: it settles the length without drawing, where a minimum and a maximum of
        // eight still draw across a one-value range and consume a draw doing it. On a seeded run the two spellings
        // diverge from that point on (ADR-0049), so only the range form is exactly equivalent here.
        Diagnostic diagnostic = await SingleDiagnosticAsync("_ = Any.String().WithMinLength(8).WithMaxLength(8).Generate();");

        Check.That(diagnostic.GetMessage()).IsEqualTo("These two bounds are the range WithLengthBetween(8, 8)");
    }

    [Theory]
    [InlineData("_ = Any.String().WithLengthBetween(8, 20).Generate();")]
    [InlineData("_ = Any.ListOf(Any.Int32()).WithCountBetween(1, 5).Generate();")]
    [InlineData("_ = Any.Int32().Between(1, 50).Generate();")]
    public async Task Does_not_report_a_chain_already_written_as_a_range(string body) {
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.Int32().GreaterThan(5).LessThan(10).Generate();")]
    [InlineData("_ = Any.Double().GreaterThan(1.5).LessThan(9.5).Generate();")]
    [InlineData("_ = Any.DateTime().After(DateTime.UnixEpoch).Before(DateTime.MaxValue).Generate();")]
    public async Task Does_not_report_a_strict_pair(string body) {
        // Between is inclusive on both sides. On an integral type GreaterThan(5).LessThan(10) is Between(6, 9),
        // so reporting it would rewrite the numbers the author wrote; on a floating-point type there is no next
        // value, so it has no range form at all. Silent for every type, which is the boundary the rule turns on.
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.Int32().GreaterThan(5).LessThanOrEqualTo(10).Generate();")]
    [InlineData("_ = Any.Int32().GreaterThanOrEqualTo(5).LessThan(10).Generate();")]
    [InlineData("_ = Any.DateTime().After(DateTime.UnixEpoch).BeforeOrEqualTo(DateTime.MaxValue).Generate();")]
    public async Task Does_not_report_a_mixed_strict_and_inclusive_pair(string body) {
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.String().WithMinLength(8).WithMinLength(10).WithMaxLength(20).Generate();")]
    [InlineData("_ = Any.String().WithMinLength(8).WithMaxLength(20).WithMaxLength(15).Generate();")]
    public async Task Does_not_report_when_a_bound_is_declared_twice(string body) {
        // A bound declared twice folds to the tighter one silently, so the first minimum is not the chain's
        // minimum. Pairing it anyway would name a range WIDER than the chain draws -- the one way this rule could
        // be unsound. It refuses the pair instead; that shape is reported by its own rule (ADR-0078).
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.String().WithLength(8).WithMinLength(3).WithMaxLength(20).Generate();")]
    [InlineData("_ = Any.ListOf(Any.Int32()).WithCount(3).WithMinCount(1).WithMaxCount(5).Generate();")]
    public async Task Does_not_report_a_chain_that_also_settles_an_exact_size(string body) {
        await NothingReportedAsync(body);
    }

    [Theory]
    [InlineData("_ = Any.String().NonEmpty().WithMaxLength(20).Generate();")]
    [InlineData("_ = Any.Int32().Positive().LessThanOrEqualTo(50).Generate();")]
    public async Task Does_not_report_a_bound_reached_through_an_alias(string body) {
        // NonEmpty IS a minimum length of one and Positive IS a minimum of one, so these chains do declare both
        // bounds. They stay silent all the same: choosing the alias says something about intent that the explicit
        // bound does not, and which of two correct spellings to prefer is ADR-0077's question, not this rule's.
        await NothingReportedAsync(body);
    }

    [Fact]
    public async Task Does_not_report_bounds_that_are_not_in_the_same_chain() {
        // Declaring the bounds separately is a documented feature: a helper sets a floor, a call site adds a
        // ceiling. Only a single fluent chain is ever paired.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    AnyString atLeastEight = Any.String().WithMinLength(8);
                    _ = atLeastEight.WithMaxLength(20).Generate();
            """);

        Check.That(diagnostics.Length).IsEqualTo(0);
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
                    Check2.ThatCode(() => Any.String().WithMinLength(8).WithMaxLength(20));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerTestHarness.GetDiagnosticsAsync(new PairedBoundsHaveARangeFormAnalyzer(), source);

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Fact]
    public async Task Reports_each_chain_once() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("""
                    _ = Any.String().Alpha().WithMinLength(8).UpperCase().WithMaxLength(20).Generate();
            """);

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("These two bounds are the range WithLengthBetween(8, 20)");
    }

}
