using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class BandwidthEstimateTests {

    [Fact]
    public void A_valid_estimate_exposes_what_it_was_given() {
        BandwidthEstimate estimate = new BandwidthEstimate(2_500.5, 4, 0.25);

        Assert.Equal(2_500.5, estimate.ThroughputKbps);
        Assert.Equal(4, estimate.SampleCount);
        Assert.Equal(0.25, estimate.SmoothingFactor);
    }

    [Theory]
    [InlineData(0.000001)]
    [InlineData(1_000_000)]
    public void A_throughput_at_the_edge_of_the_range_is_accepted(double throughputKbps) {
        int    anySamples   = Any.Int32().Between(1, 100).Generate();
        double anySmoothing = Any.Double().Between(0.01, 1).Generate();

        BandwidthEstimate estimate = new BandwidthEstimate(throughputKbps, anySamples, anySmoothing);

        Assert.Equal(throughputKbps, estimate.ThroughputKbps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_throughput_outside_the_range_is_refused(double throughputKbps) {
        int    anySamples   = Any.Int32().Between(1, 100).Generate();
        double anySmoothing = Any.Double().Between(0.01, 1).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BandwidthEstimate(throughputKbps, anySamples, anySmoothing));

        Assert.Equal("throughputKbps", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_estimate_without_a_sample_is_refused(int sampleCount) {
        double anyThroughput = Any.Double().Between(1, 1_000_000).Generate();
        double anySmoothing  = Any.Double().Between(0.01, 1).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BandwidthEstimate(anyThroughput, sampleCount, anySmoothing));

        Assert.Equal("sampleCount", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_smoothing_factor_outside_the_range_is_refused(double smoothingFactor) {
        double anyThroughput = Any.Double().Between(1, 1_000_000).Generate();
        int    anySamples    = Any.Int32().Between(1, 100).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BandwidthEstimate(anyThroughput, anySamples, smoothingFactor));

        Assert.Equal("smoothingFactor", exception.ParamName);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(1_000, false)]
    public void An_estimate_is_warming_up_below_three_samples(int sampleCount, bool expectedWarmingUp) {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(sampleCount).Generate();

        Assert.Equal(expectedWarmingUp, estimate.IsWarmingUp);
    }

    [Fact]
    public void A_sample_is_folded_with_an_exponentially_weighted_average() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(0.25).WithSampleCount(5).Generate();

        BandwidthEstimate updated = estimate.WithSample(kilobits: 8_000, seconds: 2);   // observed 4 000 kbps

        Assert.Equal(1_750.0, updated.ThroughputKbps);   // 0.25 * 4000 + 0.75 * 1000
    }

    [Fact]
    public void A_sample_counts_for_one_more_and_keeps_the_smoothing_factor() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(2).WithSmoothingFactor(0.3).Generate();
        double            anyKilobits = Any.Double().Between(1, 100_000).Generate();
        double            anySeconds  = Any.Double().Between(0.1, 60).Generate();

        BandwidthEstimate updated = estimate.WithSample(anyKilobits, anySeconds);

        Assert.Equal(3, updated.SampleCount);
        Assert.Equal(0.3, updated.SmoothingFactor);
    }

    [Fact]
    public void The_third_sample_ends_the_warm_up() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(2).Generate();
        double            anyKilobits = Any.Double().Between(1, 100_000).Generate();
        double            anySeconds  = Any.Double().Between(0.1, 60).Generate();

        BandwidthEstimate updated = estimate.WithSample(anyKilobits, anySeconds);

        Assert.True(estimate.IsWarmingUp);
        Assert.False(updated.IsWarmingUp);
    }

    [Fact]
    public void Folding_a_sample_leaves_the_original_estimate_untouched() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSampleCount(4).Generate();
        double            anyKilobits = Any.Double().Between(1, 100_000).Generate();
        double            anySeconds  = Any.Double().Between(0.1, 60).Generate();

        BandwidthEstimate updated = estimate.WithSample(anyKilobits, anySeconds);

        Assert.NotSame(estimate, updated);
        Assert.Equal(1_000, estimate.ThroughputKbps);
        Assert.Equal(4, estimate.SampleCount);
    }

    [Fact]
    public void A_fully_smoothed_estimate_takes_the_observation_as_it_is() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSmoothingFactor(1).Generate();

        BandwidthEstimate updated = estimate.WithSample(kilobits: 6_000, seconds: 3);

        Assert.Equal(2_000.0, updated.ThroughputKbps);
    }

    [Fact]
    public void An_observation_above_the_ceiling_is_clamped_before_smoothing() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSmoothingFactor(1).Generate();

        BandwidthEstimate updated = estimate.WithSample(kilobits: 5_000_000, seconds: 1);

        Assert.Equal(1_000_000.0, updated.ThroughputKbps);
    }

    [Fact]
    public void A_near_zero_observation_halves_an_estimate_smoothed_at_one_half() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(0.5).Generate();

        BandwidthEstimate updated = estimate.WithSample(kilobits: 0.001, seconds: 600);

        Assert.Equal(500.0, updated.ThroughputKbps, precision: 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NegativeInfinity)]
    public void A_sample_without_data_is_refused(double kilobits) {
        BandwidthEstimate estimate = Any.BandwidthEstimate().Generate();
        double            anySeconds = Any.Double().Between(0.1, 60).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => estimate.WithSample(kilobits, anySeconds));

        Assert.Equal("kilobits", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NegativeInfinity)]
    public void A_sample_without_duration_is_refused(double seconds) {
        BandwidthEstimate estimate = Any.BandwidthEstimate().Generate();
        double            anyKilobits = Any.Double().Between(1, 100_000).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => estimate.WithSample(anyKilobits, seconds));

        Assert.Equal("seconds", exception.ParamName);
    }

    [Fact]
    public void A_NaN_sample_is_refused() {
        BandwidthEstimate estimate = Any.BandwidthEstimate().Generate();
        double            anySeconds = Any.Double().Between(0.1, 60).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => estimate.WithSample(double.NaN, anySeconds));
    }

}
