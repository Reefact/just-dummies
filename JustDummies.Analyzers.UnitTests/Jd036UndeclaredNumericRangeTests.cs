using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd036UndeclaredNumericRangeTests {

    private const string Message = "This floating-point dummy declares no range: one draw in ten spans the whole type";

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = $$"""
            using JustDummies;

            public static class Sample {
                public static void M() {
            {{body}}
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new UndeclaredNumericRangeAnalyzer(), source);
    }

    [Theory]
    [InlineData("_ = Any.Double().Generate();")]
    [InlineData("_ = Any.Single().Generate();")]
    [InlineData("_ = Any.Decimal().Generate();")]
    public async Task Reports_an_unconstrained_floating_point_chain(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Id).IsEqualTo("JD036");
        Check.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Info);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo(Message);
    }

    [Theory]
    [InlineData("_ = Any.Double().Positive().Generate();")]
    [InlineData("_ = Any.Double().Negative().Generate();")]
    [InlineData("_ = Any.Double().GreaterThan(0).Generate();")]
    [InlineData("_ = Any.Single().LessThanOrEqualTo(100f).Generate();")]
    [InlineData("_ = Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2).Generate();")]
    [InlineData("_ = Any.Double().NonZero().Except(1d).Generate();")]
    public async Task Reports_a_chain_that_leaves_one_side_open(string body) {
        // A one-sided bound is a permission, not a range: the window keeps nine draws in ten ordinary and the tenth
        // spans the open side to the type's edge, which is exactly what the rule names.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].GetMessage()).IsEqualTo(Message);
    }

    [Theory]
    [InlineData("_ = Any.Double().Between(0d, 100d).Generate();")]
    [InlineData("_ = Any.Double().GreaterThan(0).LessThanOrEqualTo(100e6).Generate();")]
    [InlineData("_ = Any.Double().Positive().LessThan(1d).Generate();")]
    [InlineData("_ = Any.Single().Negative().GreaterThanOrEqualTo(-5f).Generate();")]
    [InlineData("_ = Any.Decimal().Between(0m, 1000m).WithScale(2).Generate();")]
    [InlineData("_ = Any.Double().Zero().Generate();")]
    [InlineData("_ = Any.Decimal().OneOf(1m, 2m).Generate();")]
    [InlineData("_ = Any.Double().Between(5d, 5d).Generate();")]
    // A floor on the type's maximum, or a ceiling on its minimum, leaves one value: the run time returns it as a pin
    // and tosses no edge coin. The bound is on the edge, but it closes the range rather than opening it.
    [InlineData("_ = Any.Double().Between(double.MaxValue, double.MaxValue).Generate();")]
    [InlineData("_ = Any.Double().Between(double.MinValue, double.MinValue).Generate();")]
    [InlineData("_ = Any.Single().Between(float.MaxValue, float.MaxValue).Generate();")]
    [InlineData("_ = Any.Decimal().GreaterThanOrEqualTo(decimal.MaxValue).Generate();")]
    [InlineData("_ = Any.Decimal().LessThanOrEqualTo(decimal.MinValue).Generate();")]
    [InlineData("_ = Any.Double().GreaterThanOrEqualTo(double.MaxValue).Generate();")]
    // An integer draw spans its whole domain already and tosses no edge coin: nothing is left unsaid there.
    [InlineData("_ = Any.Int32().Generate();")]
    [InlineData("_ = Any.Int64().Positive().Generate();")]
    public async Task Does_not_report_a_chain_that_declares_its_range(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(0);
    }

    [Theory]
    [InlineData("_ = Any.Double().Between(0d, double.MaxValue).Generate();")]
    [InlineData("_ = Any.Double().Between(double.MinValue, 0d).Generate();")]
    [InlineData("_ = Any.Single().Between(0f, float.MaxValue).Generate();")]
    [InlineData("_ = Any.Decimal().Between(0m, decimal.MaxValue).Generate();")]
    [InlineData("_ = Any.Double().GreaterThanOrEqualTo(double.MinValue).LessThan(5d).Generate();")]
    public async Task Reports_a_bound_spelled_against_the_type_s_own_edge(string body) {
        // Between(0, double.MaxValue) and Positive-or-zero denote the same set, and the run time draws them alike:
        // a bound on the type's edge is the domain showing through, never a side the caller closed.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync($"        {body}");

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

    [Fact]
    public async Task Reports_a_seeded_chain_on_its_factory_call() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.WithSeed(42).Double().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
        Check.That(diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length))
             .IsEqualTo("Any.WithSeed(42).Double()");
    }

    [Fact]
    public async Task Reports_a_chain_wrapped_by_OrNull_once() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("        _ = Any.Double().OrNull().Generate();");

        Check.That(diagnostics.Length).IsEqualTo(1);
    }

}
