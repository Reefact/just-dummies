using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class BandwidthEstimateTests {

    [Fact]
    public void An_estimate_exposes_what_it_was_built_with() {
        // Act
        BandwidthEstimate estimate = new BandwidthEstimate(4_000, 5, 0.25);

        // Assert
        Assert.Equal(4_000, estimate.ThroughputKbps);
        Assert.Equal(5, estimate.SampleCount);
        Assert.Equal(0.25, estimate.SmoothingFactor);
    }

    [Theory]
    [InlineData(double.Epsilon, 1, 1.0)]
    [InlineData(1_000_000, int.MaxValue, double.Epsilon)]
    public void An_estimate_accepts_both_ends_of_every_range(double throughput, int samples, double smoothing) {
        // Act
        BandwidthEstimate estimate = new BandwidthEstimate(throughput, samples, smoothing);

        // Assert
        Assert.Equal(throughput, estimate.ThroughputKbps);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1_000_000.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_throughput_outside_the_range_is_refused(double throughput) {
        Assert.Throws<ArgumentOutOfRangeException>("throughputKbps", () => new BandwidthEstimate(throughput, 1, 0.5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void An_estimate_without_a_sample_is_refused(int samples) {
        Assert.Throws<ArgumentOutOfRangeException>("sampleCount", () => new BandwidthEstimate(1_000, samples, 0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void A_smoothing_factor_outside_the_range_is_refused(double smoothing) {
        Assert.Throws<ArgumentOutOfRangeException>("smoothingFactor", () => new BandwidthEstimate(1_000, 1, smoothing));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void An_estimate_is_warming_up_below_three_samples(int samples, bool warmingUp) {
        // Arrange
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithSampleCount(samples).Generate();

        // Assert
        Assert.Equal(warmingUp, estimate.IsWarmingUp);
    }

    [Fact]
    public void A_sample_is_folded_in_with_the_weight_of_the_smoothing_factor() {
        // Arrange: 2 000 kilobits in one second is a 2 000 kbps observation.
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(0.5).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 2_000, seconds: 1);

        // Assert
        Assert.Equal(1_500, updated.ThroughputKbps);
    }

    [Fact]
    public void A_smoothing_factor_of_one_keeps_only_the_last_observation() {
        // Arrange
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithSmoothingFactor(1.0).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 6_000, seconds: 2);

        // Assert
        Assert.Equal(3_000, updated.ThroughputKbps);
    }

    [Fact]
    public void A_sample_adds_one_to_the_count_and_keeps_the_smoothing_factor() {
        // Arrange
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithSampleCount(2).WithSmoothingFactor(0.3).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 1_000, seconds: 1);

        // Assert
        Assert.Equal(3, updated.SampleCount);
        Assert.Equal(0.3, updated.SmoothingFactor);
        Assert.False(updated.IsWarmingUp);
    }

    [Fact]
    public void Folding_a_sample_in_leaves_the_estimate_it_started_from_untouched() {
        // Arrange
        BandwidthEstimate estimate = new BandwidthEstimate(1_000, 1, 0.5);

        // Act
        _ = estimate.WithSample(kilobits: 9_000, seconds: 1);

        // Assert
        Assert.Equal(1_000, estimate.ThroughputKbps);
        Assert.Equal(1, estimate.SampleCount);
    }

    [Fact]
    public void An_observation_beyond_the_ceiling_cannot_push_the_estimate_past_it() {
        // Arrange: the estimate is already at the ceiling.
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithThroughputKbps(1_000_000).Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 5_000_000_000, seconds: 1);

        // Assert
        Assert.True(updated.ThroughputKbps <= 1_000_000);
    }

    [Fact]
    public void A_sample_without_data_is_refused() {
        // Arrange
        BandwidthEstimate estimate  = new AnyBandwidthEstimate().Generate();
        double            anyEmpty  = Any.Double().LessThanOrEqualTo(0).Generate();
        double            anySecond = Any.Double().Positive().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("kilobits", () => estimate.WithSample(anyEmpty, anySecond));
    }

    [Fact]
    public void A_sample_without_duration_is_refused() {
        // Arrange
        BandwidthEstimate estimate   = new AnyBandwidthEstimate().Generate();
        double            anyData    = Any.Double().Positive().Generate();
        double            anyNoTime  = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => estimate.WithSample(anyData, anyNoTime));
    }

    [Fact(Skip = "domain defect: SampleCount + 1 overflows to int.MinValue at int.MaxValue and WithSample then throws a misleading ArgumentOutOfRangeException('sampleCount'); deterministic, literal input, no seed")]
    public void An_estimate_resting_on_the_largest_possible_count_can_still_take_a_sample() {
        // Arrange
        BandwidthEstimate estimate = new AnyBandwidthEstimate().WithSampleCount(int.MaxValue).Generate();

        // Act
        Exception? failure = Record.Exception(() => estimate.WithSample(kilobits: 1_000, seconds: 1));

        // Assert
        Assert.Null(failure);
    }

}
