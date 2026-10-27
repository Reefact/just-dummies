using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

/// <summary>Never run: each line is a mistake an analyzer could catch at build time. Kept to record which ones fire.</summary>
internal static class AnalyzerProbes {

    public static void ContradictoryConstants() {
        int     reversedInts     = Any.Int32().Between(10, 1).Generate();
        int     crossedInts      = Any.Int32().GreaterThan(36).LessThan(36).Generate();
        decimal crossedDecimals  = Any.Decimal().GreaterThan(36m).LessThan(36m).Generate();
        decimal offScaleBounds   = Any.Decimal().Between(1.2m, 1.8m).WithScale(0).Generate();
        DateOnly pastTheEnd      = Any.DateOnly().After(DateOnly.MaxValue).Generate();
        DateOnly reversedDates   = Any.DateOnly().Between(new DateOnly(2025, 1, 2), new DateOnly(2025, 1, 1)).Generate();
        TimeOnly offGrid         = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Between(new TimeOnly(10, 10), new TimeOnly(10, 20)).Generate();
        TimeOnly wrappingWindow  = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Generate();
        HashSet<ReadingSource> tooManySources = Any.SetOf(Any.Enum<ReadingSource>()).WithCount(4).Generate();
        ReadingSource undeclared = Any.Enum<ReadingSource>().OneOf((ReadingSource)7).Generate();
        decimal neverDrawn       = Any.Decimal().WithScale(3).OneOf(2.0001m, 2.5m).Generate();
        decimal neverDrawnPoolFirst = Any.Decimal().OneOf(2.0001m, 2.5m).WithScale(3).Generate();
        decimal neverDrawnByBound   = Any.Decimal().OneOf(5m, 50m).LessThan(36m).Generate();
        int     neverDrawnInt       = Any.Int32().OneOf(5, 50).LessThan(36).Generate();
    }

    public static void ArbitraryValueInTheAssertion() {
        decimal anyKwh = Any.Decimal().Between(0m, MeterIndex.MaxKwh).WithScale(3).Generate();

        MeterIndex index = new MeterIndex(anyKwh);

        Assert.Equal(anyKwh, index.Kwh);
    }

    public static void UnusedCombineOperand() {
        IAny<MeterIndex> anyIndex = Any.Combine(Any.Decimal().Between(0m, 10m).WithScale(3), Any.Enum<ReadingSource>(), (kwh, source) => new MeterIndex(kwh));
    }

}
