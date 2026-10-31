using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class ValueObjectTests {

    [Fact]
    public void Downloading_media_takes_its_size_over_the_throughput() {
        // Arrange
        Bitrate bitrate = new(5_000);

        // Act
        double seconds = bitrate.DownloadSeconds(mediaSeconds: 4, throughputKbps: 2_000);

        // Assert
        Assert.Equal(10, seconds, precision: 9);
    }

    [Fact]
    public void A_segment_weighs_its_duration_times_the_rung_bitrate() {
        // Arrange
        Segment         segment = Any.Segment().WithDurationSeconds(4).Generate();
        EncodingVariant variant = Any.EncodingVariant().WithBitrate(new Bitrate(2_500)).Generate();

        // Act
        double kilobits = segment.SizeKilobits(variant);

        // Assert
        Assert.Equal(10_000, kilobits, precision: 9);
    }

    [Fact]
    public void The_frame_rate_is_read_from_the_variant_id() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(720, 7_680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        EncodingVariant variant = new("720p50", anyWidth, 720, anyBitrate);

        // Assert
        Assert.Equal(50, variant.FramesPerSecond);
    }

    [Fact]
    public void A_variant_id_must_declare_the_real_height() {
        // Arrange
        int     anyWidth   = Any.Int32().Between(1_080, 7_680).MultipleOf(2).Generate();
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new EncodingVariant("720p30", anyWidth, 1_080, anyBitrate));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("declares height 720", argument.Message);
    }

    [Fact]
    public void A_new_sample_is_blended_into_the_estimate_by_the_smoothing_factor() {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithThroughputKbps(1_000).WithSmoothingFactor(0.25).Generate();

        // Act: 12 000 kbit in 4 s is 3000 kbps observed.
        BandwidthEstimate updated = estimate.WithSample(kilobits: 12_000, seconds: 4);

        // Assert: 0.25 x 3000 + 0.75 x 1000.
        Assert.Equal(1_500, updated.ThroughputKbps, precision: 9);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void An_estimate_is_trusted_from_its_third_sample(int sampleCount, bool warmingUp) {
        // Arrange
        BandwidthEstimate estimate = Any.BandwidthEstimate().WithSampleCount(sampleCount).Generate();

        // Act
        bool isWarmingUp = estimate.IsWarmingUp;

        // Assert
        Assert.Equal(warmingUp, isWarmingUp);
    }

    [Fact]
    public void A_segment_that_does_not_fit_in_the_buffer_is_refused() {
        // Arrange: 18 s buffered out of 20.
        PlaybackBuffer buffer     = new(levelSeconds: 18, capacitySeconds: 20);
        Segment        segment    = Any.Segment().WithDurationSeconds(4).Generate();

        // Act
        Exception? refusal = Record.Exception(() => buffer.Append(segment));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(18, buffer.LevelSeconds);
    }

    [Fact]
    public void Holding_a_rung_while_claiming_a_switch_up_is_refused() {
        // Arrange
        EncodingVariant anyVariant  = Any.EncodingVariant().Generate();
        Segment         anySegment  = Any.Segment().Generate();
        double          anyPredicted = Any.Double().Positive().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new AdaptationDecision(anySegment, anyVariant, anyVariant,
                                                                           AdaptationReason.SwitchUp, anyPredicted));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_panic_may_keep_the_current_rung_but_never_climb() {
        // Arrange
        BitrateLadder anyLadder    = Any.BitrateLadder().WithRungCount(2).Generate();
        Segment       anySegment   = Any.Segment().Generate();
        double        anyPredicted = Any.Double().Positive().Generate();

        // Act
        Exception? holding  = Record.Exception(() => new AdaptationDecision(anySegment, anyLadder.Lowest, anyLadder.Lowest,
                                                                            AdaptationReason.Panic, anyPredicted));
        Exception? climbing = Record.Exception(() => new AdaptationDecision(anySegment, anyLadder.Lowest, anyLadder.Highest,
                                                                            AdaptationReason.Panic, anyPredicted));

        // Assert
        Assert.Null(holding);
        Assert.IsType<ArgumentException>(climbing);
    }

}
