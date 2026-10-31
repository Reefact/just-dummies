using AdaptiveVideo.Domain;
using AdaptiveVideo.Tests.Regular.Generators;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void Download_time_is_the_media_size_divided_by_the_throughput() {
        // Arrange
        Bitrate bitrate = new(3_000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 6_000);

        // Assert
        Assert.Equal(2.0, seconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_variant_reads_its_frame_rate_from_its_id() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(720, 7680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Arbitrary.Bitrate().Generate();

        // Act
        EncodingVariant variant = new("720p60", anyWidth, 720, anyBitrate);

        // Assert
        Assert.Equal(60, variant.FramesPerSecond);
    }

    [Fact, Reproducible]
    public void A_variant_whose_id_declares_another_height_is_rejected() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(1080, 7680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Arbitrary.Bitrate().Generate();

        // Act
        Exception? error = Record.Exception(() => new EncodingVariant("720p30", anyWidth, 1080, anyBitrate));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_variant_id_that_is_not_height_p_fps_is_a_format_error() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(720, 7680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Arbitrary.Bitrate().Generate();

        // Act
        Exception? error = Record.Exception(() => new EncodingVariant("720i30", anyWidth, 720, anyBitrate));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact, Reproducible]
    public void A_segment_weighs_its_duration_times_the_variant_bitrate() {
        // Arrange
        Segment         segment = Arbitrary.Segment().WithDurationSeconds(4.0).Generate();
        EncodingVariant variant = Arbitrary.EncodingVariant().WithBitrate(new Bitrate(2_500)).Generate();

        // Act
        double kilobits = segment.SizeKilobits(variant);

        // Assert
        Assert.Equal(10_000.0, kilobits, precision: 10);
    }

    [Fact, Reproducible]
    public void The_next_segment_follows_on_the_timeline() {
        // Arrange
        Segment segment = Arbitrary.Segment().WithIndex(7).Generate();

        // Act
        Segment next = segment.Next();

        // Assert
        Assert.Equal(8, next.Index);
    }

    [Fact, Reproducible]
    public void A_sample_moves_the_estimate_by_the_smoothing_factor_towards_the_observed_throughput() {
        // Arrange
        BandwidthEstimate estimate = Arbitrary.BandwidthEstimate()
                                              .WithThroughputKbps(1_000)
                                              .WithSmoothingFactor(0.5)
                                              .Generate();

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 4_000, seconds: 2);

        // Assert
        Assert.Equal(1_500.0, updated.ThroughputKbps, precision: 10);
    }

    [Fact, Reproducible]
    public void An_estimate_stops_warming_up_at_its_third_sample() {
        // Arrange
        BandwidthEstimate estimate = Arbitrary.BandwidthEstimate().WithSampleCount(2).Generate();
        bool              before   = estimate.IsWarmingUp;

        // Act
        BandwidthEstimate updated = estimate.WithSample(kilobits: 4_000, seconds: 2);

        // Assert
        Assert.True(before);
        Assert.False(updated.IsWarmingUp);
    }

    [Fact, Reproducible]
    public void A_policy_whose_comfort_threshold_is_not_above_its_panic_threshold_is_rejected() {
        // Arrange
        double anySafetyFactor = Any.Double().Between(0.1, 1).Generate();

        // Act
        Exception? error = Record.Exception(() => new AdaptationPolicy(anySafetyFactor, panicBufferSeconds: 10, comfortBufferSeconds: 10));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Theory, Reproducible]
    [InlineData(AdaptationReason.Hold)]
    [InlineData(AdaptationReason.SwitchDown)]
    [InlineData(AdaptationReason.Panic)]
    public void A_decision_cannot_give_a_reason_that_contradicts_a_move_up(AdaptationReason reason) {
        // Arrange
        Segment         anySegment    = Arbitrary.Segment().Generate();
        EncodingVariant previous      = Arbitrary.EncodingVariant().WithBitrate(new Bitrate(800)).Generate();
        EncodingVariant higher        = Arbitrary.EncodingVariant().WithBitrate(new Bitrate(1_600)).Generate();
        double          anyPrediction = Any.Double().GreaterThan(0).LessThanOrEqualTo(60).Generate();

        // Act
        Exception? error = Record.Exception(() => new AdaptationDecision(anySegment, previous, higher, reason, anyPrediction));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
