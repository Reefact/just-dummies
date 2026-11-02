using JustDummies.Analyzers.CodeFixes;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The first batch of code fixes (ADR-0130): one of each shape — a removal, an argument swap, a rewrite — so the
///     first fix sets the pattern for the others. Each is checked by the source it produces, whole.
/// </summary>
public class CodeFixTests {

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).Except(20)",               "Any.Int32().Between(1, 10)")]
    [InlineData("Any.Int32().Between(1, 10).Except(20).MultipleOf(3)", "Any.Int32().Between(1, 10).MultipleOf(3)")]
    [InlineData("Any.Int32().Positive().GreaterThan(-5)",              "Any.Int32().Positive()")]
    [InlineData("Any.String().Except()",                               "Any.String()")]
    [InlineData("Any.Int32().Between(1, 10).Except(20).Generate()",    "Any.Int32().Between(1, 10).Generate()")]
    public async Task JD024_removes_the_constraint_that_narrows_nothing(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new ScalarChainAdmitsNoValueAnalyzer(), new ConstraintWithNoEffectCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.Int32().Between(10, 5)",                                                     "Any.Int32().Between(5, 10)")]
    [InlineData("Any.String().WithLengthBetween(10, 5)",                                          "Any.String().WithLengthBetween(5, 10)")]
    [InlineData("Any.Int32().Between(maximum: 5, minimum: 10)",                                   "Any.Int32().Between(maximum: 10, minimum: 5)")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1))",     "Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2))")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1))", "Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))")]
    public async Task JD014_swaps_a_transposed_pair(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.String().WithLength(-1)")]
    [InlineData("Any.Int32().MultipleOf(0)")]
    [InlineData("Any.String().StartingWith(\"\")")]
    public async Task JD014_offers_nothing_where_a_rewrite_would_guess(string chain) {
        // A negative size, a step of zero, an empty prefix: what the developer meant is not in the call.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Theory]
    [InlineData("Any.String().WithMinLength(8).WithMaxLength(20)",                     "Any.String().WithLengthBetween(8, 20)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50)",           "Any.Int32().Between(1, 50)")]
    // Whichever bound was written first keeps its place in the chain, and the arguments come minimum first, as the range takes them.
    [InlineData("Any.Int32().LessThanOrEqualTo(50).GreaterThanOrEqualTo(1)",           "Any.Int32().Between(1, 50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).Positive().LessThanOrEqualTo(50)", "Any.Int32().Between(1, 50).Positive()")]
    [InlineData("Any.ListOf(Any.Int32()).WithMinCount(2).WithMaxCount(4).Generate()",  "Any.ListOf(Any.Int32()).WithCountBetween(2, 4).Generate()")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.MinValue).BeforeOrEqualTo(DateTime.MaxValue)", "Any.DateTime().Between(DateTime.MinValue, DateTime.MaxValue)")]
    // A constant, a cast or a minus over one, a limit a System value type names, a System value type built from
    // constants: nothing here runs when it is moved, so the rewrite keeps every observable order.
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Floor).LessThanOrEqualTo(Bounds.Ceiling)",       "Any.Int32().Between(Bounds.Floor, Bounds.Ceiling)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(-(5)).LessThanOrEqualTo((int)50L)",                      "Any.Int32().Between(-(5), (int)50L)")]
    [InlineData("Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero).LessThanOrEqualTo(new TimeSpan(1, 0, 0))", "Any.TimeSpan().Between(TimeSpan.Zero, new TimeSpan(1, 0, 0))")]
    [InlineData("Any.DateOnly().AfterOrEqualTo(DateOnly.MinValue).BeforeOrEqualTo(DateOnly.FromDayNumber(7))", "Any.DateOnly().Between(DateOnly.MinValue, DateOnly.FromDayNumber(7))")]
    public async Task JD031_writes_the_pair_as_the_range(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Minimum()).LessThanOrEqualTo(50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).Positive().LessThanOrEqualTo(Bounds.Maximum())")]
    [InlineData("Any.String().WithMinLength(Bounds.Minimum()).WithMaxLength(Bounds.Maximum())")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Low).LessThanOrEqualTo(50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(bound).LessThanOrEqualTo(50)")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.Now).BeforeOrEqualTo(DateTime.MaxValue)")]
    public async Task JD031_offers_nothing_where_an_argument_may_run_code(string chain) {
        // The rewrite carries the later bound's argument past the links written between the two, so an argument that
        // runs the developer's own code -- a call, a property of their own, a local or a parameter -- could run in
        // another order. The rule still reports the pair; the fix stays away.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Fact]
    public async Task A_fix_is_named_for_what_it_does() {
        IReadOnlyList<string> removal = await CodeFixTestHarness.OffersAsync(new ScalarChainAdmitsNoValueAnalyzer(), new ConstraintWithNoEffectCodeFix(), Source("Any.Int32().Between(1, 10).Except(20)"));
        IReadOnlyList<string> swap    = await CodeFixTestHarness.OffersAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source("Any.Int32().Between(10, 5)"));
        IReadOnlyList<string> range   = await CodeFixTestHarness.OffersAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source("Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50)"));

        Check.That(removal).ContainsExactly("Remove Except(20), which narrows nothing");
        Check.That(swap).ContainsExactly("Swap the two arguments");
        Check.That(range).ContainsExactly("Write the range as Between(1, 50)");
    }

    private static string Source(string expression) {
        return $$"""
            using System;
            using JustDummies;

            public static class Bounds {
                public const int Floor = 1;
                public const int Ceiling = 50;
                public static int Low => 1;
                public static int Minimum() => 1;
                public static int Maximum() => 50;
            }

            public static class Sample {
                public static object M(int bound) {
                    return {{expression}};
                }
            }
            """;
    }

}
