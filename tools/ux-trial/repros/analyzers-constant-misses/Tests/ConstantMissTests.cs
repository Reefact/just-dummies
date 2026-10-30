using JustDummies;
using Xunit;

namespace Acm.Tests;

// Each probe uses a block-bodied lambda, so NegativeTestGuard (sole expression body) does not suppress anything:
// a diagnostic that is absent here is absent from ordinary arrange code too.
public sealed class ConstantMissTests {

    private static Exception? Probe(Action body) {
        return Record.Exception(body);
    }

    // Controls: the rules fire on their documented shapes.
    [Fact] public void Control_Jd016_three_distinct_booleans() { Assert.NotNull(Probe(() => { Any.SetOf(Any.Boolean()).WithCount(3).Generate(); })); }
    [Fact] public void Control_Jd014_transposed_int_between() { Assert.NotNull(Probe(() => { Any.Int32().Between(10, 5).Generate(); })); }
    [Fact] public void Control_Jd014_transposed_double_between() { Assert.NotNull(Probe(() => { Any.Double().Between(10.0, 5.0).Generate(); })); }
    [Fact] public void Control_Jd023_crossed_int_bounds() { Assert.NotNull(Probe(() => { Any.Int32().GreaterThan(100).LessThan(10).Generate(); })); }

    // V1 JD016: eleven distinct numeric characters.
    [Fact] public void V1_distinct_numeric_chars() { Assert.NotNull(Probe(() => { Any.ListOf(Any.Char().Numeric()).WithCount(11).Distinct().Generate(); })); }

    // V2 JD014: NaN bound on a one-sided double constraint.
    [Fact] public void V2_double_less_than_nan() { Assert.NotNull(Probe(() => { Any.Double().LessThan(double.NaN).Generate(); })); }

    // V3 JD014: transposed temporal Between.
    [Fact] public void V3a_transposed_timeonly() { Assert.NotNull(Probe(() => { Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0)).Generate(); })); }
    [Fact] public void V3b_transposed_datetimeoffset() { Assert.NotNull(Probe(() => { Any.DateTimeOffset().Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate(); })); }
    [Fact] public void V3c_transposed_dateonly() { Assert.NotNull(Probe(() => { Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 1)).Generate(); })); }
    [Fact] public void V3d_transposed_timespan() { Assert.NotNull(Probe(() => { Any.TimeSpan().Between(TimeSpan.FromHours(2), TimeSpan.FromHours(1)).Generate(); })); }

    // V4 JD032: Between declared twice (folds; does it throw?).
    [Fact] public void V4_between_twice_overlapping() { Assert.Null(Probe(() => { Any.Double().Between(0.0, 100.0).Between(50.0, 200.0).Generate(); })); }

    // V5: Guid Empty then NonEmpty.
    [Fact] public void V5_guid_empty_nonempty() { Assert.NotNull(Probe(() => { Any.Guid().Empty().NonEmpty().Generate(); })); }

    // V6: constant unsupported regex construct.
    [Fact] public void V6_regex_lookahead() { Assert.NotNull(Probe(() => { Any.StringMatching("(?=a)a").Generate(); })); }

    // V7: crossed decimal / double / DateOnly / TimeSpan constant bounds (JD023 documented integer-only).
    [Fact] public void V7a_decimal_crossed() { Assert.NotNull(Probe(() => { Any.Decimal().GreaterThan(100m).LessThan(10m).Generate(); })); }
    [Fact] public void V7b_double_crossed() { Assert.NotNull(Probe(() => { Any.Double().GreaterThan(100.0).LessThan(10.0).Generate(); })); }
    [Fact] public void V7c_dateonly_crossed() { Assert.NotNull(Probe(() => { Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1)).Generate(); })); }
    [Fact] public void V7d_timespan_crossed() { Assert.NotNull(Probe(() => { Any.TimeSpan().GreaterThan(TimeSpan.FromHours(2)).LessThan(TimeSpan.FromHours(1)).Generate(); })); }

    // V8: negative-test shapes (expression-bodied lambda), with and without .Generate().
    [Fact] public void V8a_jd023_sole_body() { Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 10).MultipleOf(50)); }
    [Fact] public void V8b_jd023_with_generate() { Assert.ThrowsAny<Exception>(() => Any.Int32().Between(1, 10).MultipleOf(50).Generate()); }
    [Fact] public void V8c_jd014_with_generate() { Assert.ThrowsAny<Exception>(() => Any.Decimal().WithScale(29).Generate()); }
    [Fact] public void V8d_jd014_sole_body() { Assert.ThrowsAny<Exception>(() => Any.Decimal().WithScale(29)); }
}
