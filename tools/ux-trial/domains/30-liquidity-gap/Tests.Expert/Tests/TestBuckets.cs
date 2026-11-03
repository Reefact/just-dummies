
namespace LiquidityGap.Domain.Tests.Expert.Tests;

/// <summary>Bucket ladders the tests spell out: the day bounds are the point, the labels are not.</summary>
internal static class TestBuckets {

    public static TimeBucket Bucket(int startDay, int? endDay) {
        return Any.TimeBucket().WithStartDay(startDay).WithEndDay(endDay).Generate();
    }

    /// <summary>[0, 30) [30, 90) [90, open): wide enough that a flow 10, 45 or 120 days out lands in the first, second or third.</summary>
    public static IReadOnlyList<TimeBucket> ThreeBucketLadder() {
        return [Bucket(0, 30), Bucket(30, 90), Bucket(90, null)];
    }

    /// <summary>[0, 15) [15, 45) [45, open): a calendar month is 28 to 31 days, so +1 month falls in the middle bucket and +2 months in the last, whatever the as-of date.</summary>
    public static IReadOnlyList<TimeBucket> MonthlyLadder() {
        return [Bucket(0, 15), Bucket(15, 45), Bucket(45, null)];
    }

}
