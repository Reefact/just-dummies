using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

/// <summary>The decisions of the feedback loop: startup, panic, switching down, climbing, holding.</summary>
public sealed class AdaptationLoopTests {

    [Fact]
    public void A_session_starts_on_the_lowest_rung_at_segment_zero() {
        // Arrange
        PlaybackSession session = Player.NewSession.Generate();

        // Act
        EncodingVariant current = session.CurrentVariant;

        // Assert
        Assert.Equal(400, current.Bitrate.Kbps);
        Assert.Equal(0, session.NextSegment.Index);
    }

    [Fact]
    public void The_first_decision_climbs_at_most_one_rung_however_fast_the_network() {
        // Arrange
        PlaybackSession session = Player.NewSession.WithBandwidth(b => b.WithThroughputKbps(100_000)).Generate();
        Latency         anyLatency = Any.Latency().Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal(1_000, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void The_first_decision_on_a_thin_pipe_stays_on_the_lowest_rung() {
        // Arrange
        PlaybackSession session    = Player.NewSession.WithBandwidth(b => b.WithThroughputKbps(300)).Generate();
        Latency         anyLatency = Any.Latency().Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(anyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal(400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void The_predicted_download_time_adds_the_round_trip_to_the_transfer() {
        // Arrange
        PlaybackSession session = Player.NewSession
                                        .WithBandwidth(b => b.WithThroughputKbps(8_000))
                                        .WithSegmentDurationSeconds(4)
                                        .Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(250));

        // Assert: 0.25 s round trip + 1000 kbps x 4 s / 8000 kbps.
        Assert.Equal(0.75, decision.PredictedDownloadSeconds, precision: 9);
    }

    [Fact]
    public void While_the_estimate_warms_up_the_player_climbs_one_rung_per_segment() {
        // Arrange: one sample only, so the second decision still rests on an untrusted estimate.
        PlaybackSession session = Player.NewSession
                                        .WithBandwidth(b => b.WithSampleCount(1).WithSmoothingFactor(1).WithThroughputKbps(100_000))
                                        .Generate();
        Player.Download(session, observedKbps: 100_000);

        // Act
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(2_500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void A_buffer_below_the_panic_threshold_drops_straight_to_the_lowest_rung() {
        // Arrange
        PlaybackSession session = Player.PlayingSecondRung(estimateKbps: 10_000, bufferSeconds: 2);

        // Act
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Equal(400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void A_comfortable_buffer_lets_the_player_climb_one_rung_only() {
        // Arrange: the budget would afford the top rung.
        PlaybackSession session = Player.PlayingSecondRung(estimateKbps: 10_000, bufferSeconds: 20);

        // Act
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(2_500, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void Below_comfort_the_player_holds_its_rung_despite_spare_bandwidth() {
        // Arrange
        PlaybackSession session = Player.PlayingSecondRung(estimateKbps: 10_000, bufferSeconds: 10);

        // Act
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal(1_000, decision.Chosen.Bitrate.Kbps);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void A_bandwidth_drop_switches_down_at_once_to_the_sustainable_rung_skipping_rungs() {
        // Arrange: climb to 2500 kbps, then the network delivers only 900 kbps. Whatever the segment
        // duration, 20 s of buffer keeps the player above the panic threshold through that download.
        PlaybackSession session = Player.PlayingSecondRung(estimateKbps: 10_000, bufferSeconds: 20);
        Player.Download(session, observedKbps: 900);

        // Act
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal(2_500, decision.Previous.Bitrate.Kbps);
        Assert.Equal(400, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void Climbing_just_before_the_network_collapses_stalls_then_panics() {
        // Arrange: a comfortable buffer invites a climb to 2500 kbps on 4 s segments...
        PlaybackSession session = Player.NewSession
                                        .WithSegmentDurationSeconds(4)
                                        .WithBuffer(b => b.WithLevelSeconds(30))
                                        .WithBandwidth(_ => Player.TrustedEstimate.WithThroughputKbps(10_000))
                                        .Generate();
        Player.Download(session, observedKbps: 10_000);
        Player.DrainTo(session, Player.ComfortSeconds);

        // Act: ...then that segment arrives at 500 kbps: 20 s of download against 15 s of buffer.
        AdaptationDecision climb    = Player.Download(session, observedKbps: 500);
        AdaptationDecision recovery = session.DecideNext(Any.Latency().Generate());

        // Assert
        Assert.Equal(2_500, climb.Chosen.Bitrate.Kbps);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(5, session.TotalStallSeconds, precision: 9);
        Assert.Equal(AdaptationReason.Panic, recovery.Reason);
        Assert.Equal(400, recovery.Chosen.Bitrate.Kbps);
    }

}
