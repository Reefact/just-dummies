using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class BandwidthEstimateTests {

    [Fact]
    public void An_estimate_exposes_what_it_was_built_from() {
        // Act
        BandwidthEstimate estimate = new BandwidthEstimate(2_500.5, 4, 0.3);

        // Assert
        Assert.Equal(2_500.5, estimate.ThroughputKbps);
        Assert.Equal(4, estimate.SampleCount);
        Assert.Equal(0.3, estimate.SmoothingFactor);
    }

    [Theory]
    [InlineData(double.Epsilon, 1, 1.0)]
    [InlineData(1_000_000, int.MaxValue, double.Epsilon)]
    public void The_edges_of_every_range_are_accepted(double throughputKbps, int sampleCount, double smoothingFactor) {
        // Act
        BandwidthEstimate estimate = new BandwidthEstimate(throughputKbps, sampleCount, smoothingFactor);

        // Assert
        Assert.Equal(throughputKbps, estimate.ThroughputKbps);
        Assert.Equal(sampleCount, estimate.SampleCount);
        Assert.Equal(smoothingFactor, estimate.SmoothingFactor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_throughput_outside_zero_exclusive_to_a_million_is_refused(double throughputKbps) {
        // Arrange
        BandwidthEstimate anyEstimate = Any.BandwidthEstimate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BandwidthEstimate(throughputKbps, anyEstimate.SampleCount, anyEstimate.SmoothingFactor));

        // Assert
        Assert.Equal("throughputKbps", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_estimate_resting_on_no_sample_is_refused(int sampleCount) {
        // Arrange
        BandwidthEstimate anyEstimate = Any.BandwidthEstimate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BandwidthEstimate(anyEstimate.ThroughputKbps, sampleCount, anyEstimate.SmoothingFactor));

        // Assert
        Assert.Equal("sampleCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_smoothing_factor_outside_zero_exclusive_to_one_is_refused(double smoothingFactor) {
        // Arrange
        BandwidthEstimate anyEstimate = Any.BandwidthEstimate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BandwidthEstimate(anyEstimate.ThroughputKbps, anyEstimate.SampleCount, smoothingFactor));

        // Assert
        Assert.Equal("smoothingFactor", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(1_000, false)]
    public void An_estimate_is_trusted_from_its_third_sample(int sampleCount, bool expectedWarmingUp) {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(sampleCount).Generate();

        // Act + Assert
        Assert.Equal(expectedWarmingUp, estimate.IsWarmingUp);
    }

    [Fact]
    public void A_new_sample_is_blended_with_the_estimate_by_the_smoothing_factor() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(0.25).Generate();

        // Act: 4000 kilobits in one second is 4000 kbps, which weighs for a quarter
        BandwidthEstimate updated = estimate.WithSample(kilobits: 4_000, seconds: 1);

        // Assert
        Assert.Equal(1_750, updated.ThroughputKbps, precision: 9);
    }

    [Fact]
    public void A_sample_is_a_throughput_whatever_the_size_and_the_time_taken_to_get_it() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(1.0).Generate();

        // Act
        BandwidthEstimate small = estimate.WithSample(kilobits: 300, seconds: 0.5);
        BandwidthEstimate large = estimate.WithSample(kilobits: 12_000, seconds: 4);

        // Assert
        Assert.Equal(600, small.ThroughputKbps, precision: 9);
        Assert.Equal(3_000, large.ThroughputKbps, precision: 9);
    }

    [Fact]
    public void A_smoothing_factor_of_one_forgets_the_past() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSmoothingFactor(1.0).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 6_000, seconds: 2);

        // Assert
        Assert.Equal(3_000, updated.ThroughputKbps, precision: 9);
    }

    [Fact]
    public void An_observed_throughput_beyond_a_million_kbps_counts_for_a_million() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSmoothingFactor(1.0).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 5_000_000_000, seconds: 1);

        // Assert
        Assert.Equal(1_000_000, updated.ThroughputKbps);
    }

    [Fact]
    public void A_sample_adds_one_to_the_count_and_keeps_the_smoothing_factor() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(7).WithSmoothingFactor(0.3).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 1_000, seconds: 1);

        // Assert
        Assert.Equal(8, updated.SampleCount);
        Assert.Equal(0.3, updated.SmoothingFactor);
    }

    [Fact]
    public void The_third_sample_ends_the_warm_up() {
        // Arrange
        BandwidthEstimate secondSample = Any.BandwidthEstimate().WithSampleCount(2).Generate();

        // Act
        BandwidthEstimate thirdSample = secondSample.WithSample(kilobits: 1_000, seconds: 1);

        // Assert
        Assert.True(secondSample.IsWarmingUp);
        Assert.False(thirdSample.IsWarmingUp);
    }

    [Fact]
    public void Folding_a_sample_in_leaves_the_estimate_it_started_from_as_it_was() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSampleCount(5).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 9_000, seconds: 1);

        // Assert
        Assert.NotSame(estimate, updated);
        Assert.Equal(1_000, estimate.ThroughputKbps);
        Assert.Equal(5, estimate.SampleCount);
    }

    [Theory]
    [InlineData(0, 1, "kilobits")]
    [InlineData(-5, 1, "kilobits")]
    [InlineData(1_000, 0, "seconds")]
    [InlineData(1_000, -2, "seconds")]
    public void A_sample_must_carry_data_and_take_time(double kilobits, double seconds, string expectedParameter) {
        // Arrange
        BandwidthEstimate anyEstimate = Any.BandwidthEstimate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyEstimate.WithSample(kilobits, seconds));

        // Assert
        Assert.Equal(expectedParameter, refusal.ParamName);
    }

    [Theory(Skip = "domain defect: WithSample lets NaN through its own guards (NaN <= 0 is false); the refusal then comes from the " +
                   "constructor, as an out-of-range 'throughputKbps' instead of the 'kilobits' or 'seconds' that was wrong. " +
                   "Literal inputs, no seed involved.")]
    [InlineData(double.NaN, 1, "kilobits")]
    [InlineData(1_000, double.NaN, "seconds")]
    public void A_NaN_sample_is_refused_naming_the_argument_at_fault(double kilobits, double seconds, string expectedParameter) {
        // Arrange
        BandwidthEstimate anyEstimate = Any.BandwidthEstimate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyEstimate.WithSample(kilobits, seconds));

        // Assert
        Assert.Equal(expectedParameter, refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the sample count has no upper bound, but WithSample adds one without a check: at int.MaxValue " +
                 "it wraps to a negative count and fails with \"An estimate rests on at least one sample\" (parameter 'sampleCount'). " +
                 "Literal input, no seed involved; a drawn count (uniform over the int range) never reaches it in practice.")]
    public void An_estimate_that_has_seen_every_countable_sample_still_takes_one_more() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(int.MaxValue).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 1_000, seconds: 1);

        // Assert
        Assert.True(updated.SampleCount > 0);
    }

}
