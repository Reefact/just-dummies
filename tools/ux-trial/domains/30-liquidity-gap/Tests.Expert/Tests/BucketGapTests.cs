namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class BucketGapTests {

    [Fact]
    public void The_gap_of_a_bucket_is_what_comes_in_minus_what_goes_out() {
        // Arrange
        BucketGap anySurplusBucket = Any.BucketGap().WithInflows(100m).WithOutflows(30m).Generate();

        // Assert
        Assert.Equal(70m, anySurplusBucket.Gap);
    }

    [Fact]
    public void A_bucket_that_pays_out_more_than_it_receives_has_a_negative_gap() {
        // Arrange
        BucketGap anyDeficitBucket = Any.BucketGap().WithInflows(30m).WithOutflows(100m).Generate();

        // Assert
        Assert.Equal(-70m, anyDeficitBucket.Gap);
    }

    [Fact]
    public void A_quiet_bucket_has_no_gap() {
        // Arrange
        BucketGap anyQuietBucket = Any.BucketGap().WithInflows(0m).WithOutflows(0m).Generate();

        // Assert
        Assert.Equal(0m, anyQuietBucket.Gap);
    }

    [Fact]
    public void The_cumulative_gap_is_carried_as_given() {
        // Arrange
        BucketGap anyBucket = Any.BucketGap().WithCumulativeGap(-12.34m).Generate();

        // Assert
        Assert.Equal(-12.34m, anyBucket.CumulativeGap);
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

}
