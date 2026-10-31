using AdaptiveVideo.Domain;
using AdaptiveVideo.Tests.Regular.Generators;

using JustDummies.Xunit;

using Xunit;

namespace AdaptiveVideo.Tests.Regular;

public sealed class PlaybackSessionTests {

    // Every scenario below plays on the same terms, and the numbers in the assertions follow from them:
    // rungs at 500 / 1 500 / 3 000 / 6 000 kbps, half the estimate spent (safety 0.5), panic below 5 s of
    // buffer, comfort from 20 s, 4-second segments, and an estimate that trusts its last sample only
    // (smoothing 1) and is past its warm-up (5 samples). What the buffer holds is set by each test.
    private static AnyPlaybackSession OnAFourRungLadder(double bufferLevelSeconds, int sampleCount = 5) {
        return Arbitrary.PlaybackSession()
                        .WithLadder(l => l.WithBitrates(500, 1_500, 3_000, 6_000))
                        .WithPolicy(new AdaptationPolicy(safetyFactor: 0.5, panicBufferSeconds: 5, comfortBufferSeconds: 20))
                        .WithBandwidth(b => b.WithThroughputKbps(10_000).WithSampleCount(sampleCount).WithSmoothingFactor(1))
                        .WithBuffer(b => b.WithLevelSeconds(bufferLevelSeconds).WithCapacitySeconds(c => c.GreaterThanOrEqualTo(40)))
                        .WithSegmentDurationSeconds(4);
    }

    [Fact, Reproducible]
    public void A_session_starts_on_the_lowest_variant_at_the_first_segment() {
        // Arrange
        BitrateLadder anyLadder = Arbitrary.BitrateLadder().Generate();

        // Act
        PlaybackSession session = Arbitrary.PlaybackSession().WithLadder(anyLadder).Generate();

        // Assert
        Assert.Same(anyLadder.Lowest, session.CurrentVariant);
        Assert.Equal(0, session.NextSegment.Index);
    }

    [Fact, Reproducible]
    public void The_first_decision_climbs_at_most_one_rung_however_fast_the_network() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 10).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal(1_500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void The_predicted_download_time_adds_the_round_trip_to_the_transfer_time() {
        // Arrange
        PlaybackSession session = OnAFourRungLadder(bufferLevelSeconds: 10).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(200));

        // Assert
        Assert.Equal(0.2 + (1_500 * 4.0 / 10_000), decision.PredictedDownloadSeconds, precision: 10);
    }

    [Fact, Reproducible]
    public void When_the_throughput_collapses_the_session_drops_at_once_to_the_sustainable_variant() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 30).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);   // startup on 1 500 kbps
        session.CompleteDownload(6);      // 6 000 kbit in 6 s: the estimate falls to 1 000 kbps, a budget of 500

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal(500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void With_a_comfortable_buffer_the_session_climbs_a_single_rung_towards_a_faster_network() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 30).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);   // startup on 1 500 kbps
        session.CompleteDownload(0.5);    // 12 000 kbps observed: 6 000 would be sustainable; the buffer holds 33.5 s

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(3_000, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void Below_the_comfort_threshold_the_session_holds_even_when_the_network_allows_more() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 10).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);   // startup on 1 500 kbps
        session.CompleteDownload(0.5);    // 12 000 kbps observed, but the buffer holds only 13.5 s

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal(1_500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void Below_the_panic_threshold_the_session_drops_to_the_lowest_variant() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 2).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);   // startup on 1 500 kbps
        session.CompleteDownload(1.5);    // the buffer is left with 0.5 + 4 = 4.5 s

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    [Fact, Reproducible]
    public void While_the_estimate_warms_up_a_low_buffer_does_not_trigger_a_panic() {
        // Arrange
        PlaybackSession session    = OnAFourRungLadder(bufferLevelSeconds: 2, sampleCount: 1).Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);   // startup on 1 500 kbps
        session.CompleteDownload(1.5);    // 4.5 s of buffer, 4 000 kbps observed: still only two samples

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal(1_500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact, Reproducible]
    public void A_download_slower_than_the_buffer_counts_as_a_stall() {
        // Arrange
        PlaybackSession session = OnAFourRungLadder(bufferLevelSeconds: 1).Generate();
        session.DecideNext(Arbitrary.Latency().Generate());

        // Act
        double stall = session.CompleteDownload(3);

        // Assert
        Assert.Equal(2.0, stall, precision: 10);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(2.0, session.TotalStallSeconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_completed_download_adopts_the_chosen_variant_and_moves_to_the_next_segment() {
        // Arrange
        PlaybackSession    session  = OnAFourRungLadder(bufferLevelSeconds: 10).Generate();
        AdaptationDecision decision = session.DecideNext(Arbitrary.Latency().Generate());

        // Act
        session.CompleteDownload(1);

        // Assert
        Assert.Same(decision.Chosen, session.CurrentVariant);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Null(session.PendingDecision);
    }

    [Fact, Reproducible]
    public void A_session_refuses_a_second_decision_while_a_download_is_pending() {
        // Arrange
        PlaybackSession session    = Arbitrary.PlaybackSession().Generate();
        Latency         anyLatency = Arbitrary.Latency().Generate();
        session.DecideNext(anyLatency);

        // Act
        Exception? error = Record.Exception(() => session.DecideNext(anyLatency));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void A_session_refuses_to_complete_a_download_it_never_started() {
        // Arrange
        PlaybackSession session = Arbitrary.PlaybackSession().Generate();

        // Act
        Exception? error = Record.Exception(() => session.CompleteDownload(1));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void A_full_buffer_refuses_another_segment() {
        // Arrange
        PlaybackSession session = Arbitrary.PlaybackSession()
                                           .WithBuffer(new PlaybackBuffer(levelSeconds: 60, capacitySeconds: 60))
                                           .Generate();

        // Act
        Exception? error = Record.Exception(() => session.DecideNext(Arbitrary.Latency().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact, Reproducible]
    public void Idling_past_the_end_of_the_buffer_stalls_playback() {
        // Arrange
        PlaybackSession session = Arbitrary.PlaybackSession()
                                           .WithBuffer(b => b.WithLevelSeconds(3))
                                           .Generate();

        // Act
        double stall = session.Idle(5);

        // Assert
        Assert.Equal(2.0, stall, precision: 10);
        Assert.Equal(1, session.StallCount);
    }

}
