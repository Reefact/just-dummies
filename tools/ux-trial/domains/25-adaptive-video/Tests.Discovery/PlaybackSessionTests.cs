using AdaptiveVideo.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class PlaybackSessionTests {

    private static EncodingVariant AnyRung(int height, int kbps) {
        return Any.EncodingVariant().WithHeight(height).WithBitrate(new Bitrate(kbps)).Generate();
    }

    // 400 / 1200 / 2400 / 4800 kbps: the rungs the adaptation tests reason about.
    private static BitrateLadder FourRungLadder() {
        return new BitrateLadder([AnyRung(240, 400), AnyRung(480, 1200), AnyRung(720, 2400), AnyRung(1080, 4800)]);
    }

    // Safety 1 (the whole estimate is spendable), panic below 5 s, comfort from 10 s.
    private static readonly AdaptationPolicy Policy = new(safetyFactor: 1, panicBufferSeconds: 5, comfortBufferSeconds: 10);

    // A trusted estimate (5 samples) that follows the last observation exactly (smoothing 1).
    private static BandwidthEstimate TrustedEstimate(double throughputKbps) {
        return new BandwidthEstimate(throughputKbps, sampleCount: 5, smoothingFactor: 1);
    }

    [Fact, Reproducible]
    public void A_session_starts_on_the_lowest_variant_at_the_first_segment() {
        // Arrange
        BitrateLadder ladder = FourRungLadder();

        // Act
        PlaybackSession anySession = Any.PlaybackSession().WithLadder(ladder).Generate();

        // Assert
        Assert.Equal(400, anySession.CurrentVariant.Bitrate.Kbps);
        Assert.Equal(0, anySession.NextSegment.Index);
    }

    [Fact, Reproducible]
    public void The_first_decision_climbs_one_rung_at_most_even_on_a_fast_network() {
        // Arrange
        PlaybackSession anySessionOnFastNetwork = Any.PlaybackSession()
                                                     .WithLadder(FourRungLadder())
                                                     .WithBandwidth(b => b.WithThroughputKbps(50_000))
                                                     .Generate();
        Latency anyLatency = Any.Latency().Generate();

        // Act
        AdaptationDecision decision = anySessionOnFastNetwork.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal(1200, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void The_prediction_adds_the_round_trip_to_the_download_time() {
        // Arrange
        BitrateLadder   ladder  = new([AnyRung(240, 500), AnyRung(480, 1000)]);
        PlaybackSession session = new(ladder, new PlaybackBuffer(0, 30), Policy, TrustedEstimate(2000), segmentDurationSeconds: 4);

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(200));

        // Assert: 1000 kbps x 4 s / 2000 kbps = 2 s, plus 0.2 s of round trip.
        Assert.Equal(2.2, decision.PredictedDownloadSeconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_download_that_outlasts_the_buffer_counts_one_stall() {
        // Arrange
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(1, 30), Policy, TrustedEstimate(2000), 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);

        // Act
        double stall = session.CompleteDownload(elapsedSeconds: 2.5);

        // Assert
        Assert.Equal(1.5, stall, precision: 10);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(1.5, session.TotalStallSeconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_completed_download_adopts_the_chosen_variant_and_moves_to_the_next_segment() {
        // Arrange
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(20, 60), Policy, TrustedEstimate(2000), 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);

        // Act
        session.CompleteDownload(elapsedSeconds: 1);

        // Assert
        Assert.Equal(1200, session.CurrentVariant.Bitrate.Kbps);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Equal(21, session.Buffer.LevelSeconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_buffer_below_the_panic_threshold_drops_straight_to_the_lowest_variant() {
        // Arrange
        // The first segment arrives late: the buffer holds 2 s afterwards, below the 5 s panic threshold.
        BandwidthEstimate anyTrustedEstimate = Any.BandwidthEstimate()
                                                  .WithThroughputKbps(10_000)
                                                  .WithSampleCount(s => s.GreaterThanOrEqualTo(3))
                                                  .Generate();
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(1, 30), Policy, anyTrustedEstimate, 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);
        session.CompleteDownload(elapsedSeconds: 2.5);

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Equal(400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_collapsing_throughput_drops_at_once_to_the_sustainable_variant() {
        // Arrange
        // Startup reaches 1200 kbps; the segment then takes 8 s, so the estimate falls to 300 kbps.
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(20, 60), Policy, TrustedEstimate(2000), 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);
        session.CompleteDownload(elapsedSeconds: 8);

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal(400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_comfortable_buffer_and_a_fast_network_climb_a_single_rung() {
        // Arrange
        // Startup reaches 1200 kbps; the segment then takes 0.5 s, so the estimate rises to 4800 kbps.
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(20, 60), Policy, TrustedEstimate(2000), 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);
        session.CompleteDownload(elapsedSeconds: 0.5);

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(2400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_fast_network_does_not_climb_while_the_buffer_is_below_comfort() {
        // Arrange
        // Same network as above, but the buffer holds 9.5 s after the first segment: under the 10 s comfort threshold.
        PlaybackSession session    = new(FourRungLadder(), new PlaybackBuffer(8, 60), Policy, TrustedEstimate(2000), 2);
        Latency         anyLatency = Any.Latency().Generate();
        session.DecideNext(anyLatency);
        session.CompleteDownload(elapsedSeconds: 0.5);

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal(1200, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_session_refuses_a_second_decision_while_a_download_is_pending() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().Generate();
        Latency         anyLatency = Any.Latency().Generate();
        anySession.DecideNext(anyLatency);

        // Act
        Exception? error = Record.Exception(() => anySession.DecideNext(anyLatency));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void A_session_refuses_to_complete_a_download_it_never_requested() {
        // Arrange
        PlaybackSession anySession       = Any.PlaybackSession().Generate();
        double          anyElapsedSeconds = Any.Double().Between(0.1, 600).Generate();

        // Act
        Exception? error = Record.Exception(() => anySession.CompleteDownload(anyElapsedSeconds));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void A_session_refuses_to_request_a_segment_into_a_full_buffer() {
        // Arrange
        PlaybackSession anySessionWithFullBuffer = Any.PlaybackSession()
                                                      .WithBuffer(new PlaybackBuffer(30, 30))
                                                      .Generate();
        Latency anyLatency = Any.Latency().Generate();

        // Act
        Exception? error = Record.Exception(() => anySessionWithFullBuffer.DecideNext(anyLatency));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void Idling_past_the_buffer_level_counts_a_stall() {
        // Arrange
        PlaybackSession anySessionWithThreeSeconds = Any.PlaybackSession()
                                                        .WithBuffer(b => b.WithLevelSeconds(3).WithCapacitySeconds(30))
                                                        .Generate();

        // Act
        double stall = anySessionWithThreeSeconds.Idle(5);

        // Assert
        Assert.Equal(2.0, stall, precision: 10);
        Assert.Equal(1, anySessionWithThreeSeconds.StallCount);
    }

    [Fact, Reproducible]
    public void Each_decision_is_recorded_in_the_history() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().WithBuffer(b => b.WithLevelSeconds(0).WithCapacitySeconds(60)).Generate();
        Latency         anyLatency = Any.Latency().Generate();
        anySession.DecideNext(anyLatency);
        anySession.CompleteDownload(elapsedSeconds: 0.5);

        // Act
        anySession.DecideNext(anyLatency);

        // Assert
        Assert.Equal([0, 1], anySession.History.Select(d => d.Segment.Index));
    }

}
