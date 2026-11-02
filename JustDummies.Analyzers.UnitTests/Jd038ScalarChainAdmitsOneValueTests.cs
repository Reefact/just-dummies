using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

public class Jd038ScalarChainAdmitsOneValueTests {

    [Theory]
    [InlineData("Any.Int32().Between(5, 5)",                                                    "5")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(7).LessThanOrEqualTo(7)",                     "7")]
    [InlineData("Any.Int32().GreaterThan(6).LessThan(8)",                                       "7")]
    [InlineData("Any.Int32().OneOf(5)",                                                         "5")]
    [InlineData("Any.Int32().OneOf(1, 2).Except(2)",                                            "1")]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(7)",                                     "7")]
    [InlineData("Any.Int32().Odd().Between(1, 3).Except(1)",                                    "3")]
    [InlineData("Any.Int32().Between(4, 4).Even()",                                             "4")]
    [InlineData("Any.Int64().LessThanOrEqualTo(long.MinValue)",                                 "-9223372036854775808")]
    [InlineData("Any.Byte().GreaterThanOrEqualTo(255)",                                         "255")]
    [InlineData("Any.Byte().OneOf(0, 255).Except(255)",                                        "0")]
    [InlineData("Any.Byte().OneOf(0, 255).Except(0)",                                          "255")]
    [InlineData("Any.Decimal().Between(1.50m, 1.50m)",                                          "1.50")]
    [InlineData("Any.Decimal().Between(1m, 1.4m).WithScale(0)",                                 "1")]
    [InlineData("Any.Decimal().Between(1m, 2m).MultipleOf(1.5m)",                               "1.5")]
    [InlineData("Any.Decimal().OneOf(1m, 2m).Except(2m)",                                       "1")]
    // The same step or scale again changes nothing at run time, so the walk goes on past it (a different one is
    // JD034's, and ends the walk). Found by the property suite on a seed the named cases had not drawn.
    [InlineData("Any.Decimal().LessThanOrEqualTo(1.5m).GreaterThanOrEqualTo(1.5m).MultipleOf(0.5m).MultipleOf(0.5m)", "1.5")]
    [InlineData("Any.Decimal().Between(1m, 1.4m).WithScale(0).WithScale(0)",                    "1")]
    [InlineData("Any.Decimal().MultipleOf(0.5m).MultipleOf(0.50m).Between(1m, 1.4m)",           "1.0")]
    [InlineData("Any.Int32().MultipleOf(7).MultipleOf(7).Between(1, 10)",                       "7")]
    [InlineData("Any.Decimal().GreaterThanOrEqualTo(decimal.MaxValue)",                         "79228162514264337593543950335")]
    [InlineData("Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1))",   "2026-01-01")]
    [InlineData("Any.DateTime().After(new DateTime(2026, 1, 1)).Before(new DateTime(639028224000000002L))", "2026-01-01T00:00:00.0000001Z")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 1, 1, 0, 0), new DateTime(2026, 1, 2, 23, 0, 0)).WithGranularity(TimeSpan.FromDays(1))", "2026-01-02T00:00:00.0000000Z")]
    [InlineData("Any.TimeSpan().Between(TimeSpan.FromHours(2), TimeSpan.FromHours(2))",         "02:00:00")]
    public async Task Reports_a_chain_that_admits_exactly_one_value(string expression, string value) {
        // What is significant stays a literal: a constant dressed as a draw is the hand-picked literal the library
        // exists to replace (ADR-0129).
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD038");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo($"Write the literal {value}, or Any.Fixed({value}) where a generator is required: the constraints declared on this chain admit exactly one value");
    }

    [Fact]
    public async Task Names_the_whole_day_on_a_time_of_day() {
        // Between(t, t) stays the single value t on a time of day too (ADR-0125), and a test that wrote it for the
        // whole day is told what names the day.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.TimeOnly().Between(new TimeOnly(9, 0), new TimeOnly(9, 0))");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).ContainsExactly("JD038");
        Check.That(diagnostics[0].GetMessage()).IsEqualTo("Write the literal 09:00:00.0000000, or Any.Fixed(09:00:00.0000000) where a generator is required: the constraints declared on this chain admit exactly one value, or Any.TimeOnly() if the whole day was meant");
    }

    [Fact]
    public async Task Points_at_the_call_that_pinned_the_value() {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync("Any.Int32().Positive().Between(5, 5).Except(9)");

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal)).ContainsExactly("JD024", "JD038");
        Check.That(diagnostics.Single(diagnostic => diagnostic.Id == "JD038").Location.SourceSpan.Length).IsEqualTo("Any.Int32().Positive().Between(5, 5)".Length);
    }

    [Theory]
    [InlineData("Any.Int32().Zero()")]
    [InlineData("Any.Decimal().Zero().WithScale(2)")]
    [InlineData("Any.TimeSpan().Zero()")]
    [InlineData("Any.Int32().Between(5, 6)")]
    [InlineData("Any.Int32().OneOf(5, 6)")]
    [InlineData("Any.Int32().Between(1, 10).MultipleOf(5)")]
    [InlineData("Any.Decimal().Between(1m, 1.4m)")]
    [InlineData("Any.Decimal().Between(1m, 1.4m).WithScale(1)")]
    [InlineData("Any.UInt64().GreaterThanOrEqualTo(ulong.MaxValue)")]
    [InlineData("Any.Int32().Between(bound, bound)")]
    [InlineData("Any.Double().Between(1.0, 1.0)")]
    [InlineData("Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0))")]
    [InlineData("Any.Int32().Between(5, 5).Except(5)")]
    public async Task Does_not_report_a_chain_that_admits_more_or_pins_by_name(string expression) {
        // Zero() names its value, and reads as the constant it is; a ulong at the model's top is left unjudged; a
        // non-constant bound does not fold; a binary floating-point chain is not read; an emptied chain is JD023's.
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(expression);

        Check.That(diagnostics.Select(diagnostic => diagnostic.Id)).Not.Contains("JD038");
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string expression) {
        string source = $$"""
            using System;
            using JustDummies;

            public static class Sample {
                public static void M(int bound) {
                    _ = {{expression}};
                }
            }
            """;

        return await AnalyzerTestHarness.GetDiagnosticsAsync(new ScalarChainAdmitsNoValueAnalyzer(), source, "JD023", "JD024");
    }

}
