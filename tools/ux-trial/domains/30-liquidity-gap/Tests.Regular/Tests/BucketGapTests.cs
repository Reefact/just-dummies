using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class BucketGapTests {

    [Fact]
    public void The_gap_of_a_bucket_is_its_inflows_minus_its_outflows() {
        // Arrange
        BucketGap gap = Any.BucketGap().WithInflows(500m).WithOutflows(180.50m).Generate();

        // Act
        decimal netPosition = gap.Gap;

        // Assert
        Assert.Equal(319.50m, netPosition);
    }

    [Fact]
    public void A_bucket_with_more_outflows_than_inflows_has_a_negative_gap() {
        // Arrange
        BucketGap gap = Any.BucketGap().WithInflows(10m).WithOutflows(25m).Generate();

        // Act & Assert
        Assert.Equal(-15m, gap.Gap);
    }

    [Fact]
    public void An_empty_bucket_has_a_zero_gap() {
        // Arrange
        BucketGap gap = Any.BucketGap().WithInflows(0m).WithOutflows(0m).Generate();

        // Act & Assert
        Assert.Equal(0m, gap.Gap);
    }

    [Fact]
    public void The_cumulative_gap_is_kept_as_given_and_is_not_recomputed() {
        // Arrange
        BucketGap gap = Any.BucketGap().WithInflows(1m).WithOutflows(1m).WithCumulativeGap(-42.42m).Generate();

        // Act & Assert
        Assert.Equal(-42.42m, gap.CumulativeGap);
    }

    [Fact]
    public void A_gap_keeps_its_bucket() {
        // Arrange
        TimeBucket anyBucket = Any.TimeBucket().Generate();

        // Act
        BucketGap gap = Any.BucketGap().WithBucket(anyBucket).Generate();

        // Assert
        Assert.Same(anyBucket, gap.Bucket);
    }

    [Fact]
    public void Negative_inflows_are_refused() {
        // Arrange
        TimeBucket anyBucket = Any.TimeBucket().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BucketGap(anyBucket, -0.01m, 0m, 0m));

        // Assert
        Assert.Equal("inflows", refusal.ParamName);
    }

    [Fact]
    public void Negative_outflows_are_refused() {
        // Arrange
        TimeBucket anyBucket = Any.TimeBucket().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BucketGap(anyBucket, 0m, -0.01m, 0m));

        // Assert
        Assert.Equal("outflows", refusal.ParamName);
    }

    [Fact]
    public void A_missing_bucket_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new BucketGap(null!, 0m, 0m, 0m));

        // Assert
        Assert.Equal("bucket", refusal.ParamName);
    }

}
