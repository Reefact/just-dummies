using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class BucketGapTests {

    [Theory]
    [InlineData("1000", "400", "600")]
    [InlineData("400", "1000", "-600")]
    [InlineData("250.50", "250.50", "0")]
    [InlineData("0", "0", "0")]
    public void A_gap_is_inflows_minus_outflows(string inflows, string outflows, string expected) {
        // Arrange
        BucketGap gap = Any.BucketGap().WithInflows(Parse(inflows)).WithOutflows(Parse(outflows)).Generate();

        // Assert
        Assert.Equal(Parse(expected), gap.Gap);
    }

    [Fact]
    public void The_cumulative_gap_is_kept_as_given_whatever_the_gap_of_the_bucket() {
        // Arrange: a single bucket does not know the buckets before it
        BucketGap gap = Any.BucketGap().WithInflows(10m).WithOutflows(4m).WithCumulativeGap(-123.45m).Generate();

        // Assert
        Assert.Equal(-123.45m, gap.CumulativeGap);
        Assert.Equal(6m, gap.Gap);
    }

    [Fact]
    public void Negative_inflows_are_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BucketGap().WithInflows(-0.01m).Generate());

        // Assert
        Assert.Equal("inflows", refusal.ParamName);
    }

    [Fact]
    public void Negative_outflows_are_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BucketGap().WithOutflows(-0.01m).Generate());

        // Assert
        Assert.Equal("outflows", refusal.ParamName);
    }

    [Fact]
    public void A_missing_bucket_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.BucketGap().WithBucket((TimeBucket)null!).Generate());

        // Assert
        Assert.Equal("bucket", refusal.ParamName);
    }

    private static decimal Parse(string value) {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

}
