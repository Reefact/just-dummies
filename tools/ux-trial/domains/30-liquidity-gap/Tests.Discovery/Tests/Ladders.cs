using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

/// <summary>Literal bucket ladders shared by the tests that are about where a day lands.</summary>
internal static class Ladders {

    public static TimeBucket Bucket(int startDay, int? endDay) {
        return Any.TimeBucket().WithStartDay(startDay).WithEndDay(endDay).Generate();
    }

    /// <summary>[0, 30), [30, 90), [90, open).</summary>
    public static IReadOnlyList<TimeBucket> ThreeBuckets() {
        return new List<TimeBucket> { Bucket(0, 30), Bucket(30, 90), Bucket(90, null) };
    }

    public static Currency Eur { get; } = new Currency("EUR");

    public static Currency Usd { get; } = new Currency("USD");

}
