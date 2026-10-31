using AdaptiveVideo.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class ValueObjectTests {

    [Fact]
    public void Downloading_four_seconds_of_3000_kbps_media_over_6000_kbps_takes_two_seconds() {
        // Arrange
        Bitrate bitrate = new(3000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 6000);

        // Assert
        Assert.Equal(2.0, seconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_variant_reads_its_frame_rate_from_its_id() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(720, 7680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        EncodingVariant variant = new("720p50", anyWidth, 720, anyBitrate);

        // Assert
        Assert.Equal(50, variant.FramesPerSecond);
    }

    [Fact, Reproducible]
    public void A_variant_whose_id_declares_another_height_is_refused() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(720, 7680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        Exception? error = Record.Exception(() => new EncodingVariant("720p30", anyWidth, 480, anyBitrate));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_portrait_variant_is_refused() {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        Exception? error = Record.Exception(() => new EncodingVariant("1080p30", 608, 1080, anyBitrate));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_variant_id_that_is_not_height_p_fps_is_refused() {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        Exception? error = Record.Exception(() => new EncodingVariant("HD-720", 1280, 720, anyBitrate));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact, Reproducible]
    public void A_four_second_segment_at_2500_kbps_weighs_10000_kilobits() {
        // Arrange
        Segment         anyFourSecondSegment = Any.Segment().WithDurationSeconds(4.0).Generate();
        EncodingVariant anyVariantAt2500     = Any.EncodingVariant().WithBitrate(new Bitrate(2500)).Generate();

        // Act
        double size = anyFourSecondSegment.SizeKilobits(anyVariantAt2500);

        // Assert
        Assert.Equal(10_000, size, precision: 10);
    }

    [Fact]
    public void The_next_segment_follows_with_the_same_duration() {
        // Arrange
        Segment segment = new(7, 4.0);

        // Act
        Segment next = segment.Next();

        // Assert
        Assert.Equal(8, next.Index);
        Assert.Equal(4.0, next.DurationSeconds);
    }

    [Fact]
    public void A_250_ms_round_trip_costs_a_quarter_of_a_second() {
        // Arrange
        Latency latency = new(250);

        // Act
        double seconds = latency.Seconds;

        // Assert
        Assert.Equal(0.25, seconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_sample_moves_the_estimate_by_the_smoothing_factor() {
        // Arrange
        BandwidthEstimate anyHalfSmoothedEstimate = Any.BandwidthEstimate()
                                        .WithThroughputKbps(1000)
                                        .WithSmoothingFactor(0.5)
                                        .Generate();

        // Act
        BandwidthEstimate updated = anyHalfSmoothedEstimate.WithSample(kilobits: 4000, seconds: 1);

        // Assert
        Assert.Equal(2500, updated.ThroughputKbps, precision: 10);
    }

    [Fact, Reproducible]
    public void A_sample_counts_towards_the_estimate() {
        // Arrange
        BandwidthEstimate anyTwoSampleEstimate = Any.BandwidthEstimate().WithSampleCount(2).Generate();
        double            anyKilobits          = Any.Double().Between(100, 50_000).Generate();
        double            anySeconds           = Any.Double().Between(0.1, 10).Generate();

        // Act
        BandwidthEstimate updated = anyTwoSampleEstimate.WithSample(anyKilobits, anySeconds);

        // Assert
        Assert.Equal(3, updated.SampleCount);
    }

    [Fact, Reproducible]
    public void An_estimate_resting_on_two_samples_is_still_warming_up() {
        // Arrange
        BandwidthEstimate anyTwoSampleEstimate = Any.BandwidthEstimate().WithSampleCount(2).Generate();

        // Act
        bool warmingUp = anyTwoSampleEstimate.IsWarmingUp;

        // Assert
        Assert.True(warmingUp);
    }

    [Fact, Reproducible]
    public void An_estimate_resting_on_three_samples_is_trusted() {
        // Arrange
        BandwidthEstimate anyThreeSampleEstimate = Any.BandwidthEstimate().WithSampleCount(3).Generate();

        // Act
        bool warmingUp = anyThreeSampleEstimate.IsWarmingUp;

        // Assert
        Assert.False(warmingUp);
    }

    [Fact, Reproducible]
    public void A_comfort_threshold_equal_to_the_panic_threshold_is_refused() {
        // Arrange
        double anySafetyFactor = Any.Double().Between(0.1, 1).Generate();

        // Act
        Exception? error = Record.Exception(() => new AdaptationPolicy(anySafetyFactor, 10, 10));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact, Reproducible]
    public void A_buffer_drained_past_its_level_reports_the_stall_and_runs_dry() {
        // Arrange
        PlaybackBuffer anyBufferHoldingThreeSeconds = Any.PlaybackBuffer().WithLevelSeconds(3.0).Generate();

        // Act
        double stall = anyBufferHoldingThreeSeconds.Drain(5.0);

        // Assert
        Assert.Equal(2.0, stall, precision: 10);
        Assert.True(anyBufferHoldingThreeSeconds.IsEmpty);
    }

    [Fact, Reproducible]
    public void A_segment_longer_than_the_free_space_is_not_appended() {
        // Arrange
        PlaybackBuffer buffer     = new(levelSeconds: 27, capacitySeconds: 30);
        Segment        anySegment = Any.Segment().WithDurationSeconds(4.0).Generate();

        // Act
        Exception? error = Record.Exception(() => buffer.Append(anySegment));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(27, buffer.LevelSeconds);
    }

}
