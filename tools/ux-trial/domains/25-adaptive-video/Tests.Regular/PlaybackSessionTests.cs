using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

/// <summary>
///     The feedback loop, on the ladder 240p 400 / 480p 1 200 / 720p 3 000 / 1080p 6 000 kbps, four-second segments and the
///     policy "spend 80 % of the estimate, panic below 5 s, climb from 15 s". Those numbers are what the cases are about, so they
///     are literals; the latency, the frame of mind of the network and everything not named in a case stay dummies.
/// </summary>
public sealed class PlaybackSessionTests {

    private static Latency AnyLatency => new AnyLatency().Generate();

    private static AnyPlaybackSession Standard() {
        // A buffer with room for the next segment: what a session about to decide is about, unless a case says otherwise.
        return new AnyPlaybackSession().WithLadder(Fixtures.FourRungLadder())
                                       .WithBuffer(b => b.WithCapacitySeconds(100).WithLevelSeconds(l => l.LessThanOrEqualTo(50)))
                                       .WithPolicy(new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 5, comfortBufferSeconds: 15))
                                       .WithSegmentDurationSeconds(4);
    }

    /// <summary>
    ///     A session that has downloaded its first segment (at 480p, 4 800 kilobits) in <paramref name="elapsed" /> seconds, whose estimate
    ///     now is therefore 4 800 / <paramref name="elapsed" /> kbps (smoothing 1), and whose buffer is set at <paramref name="level" />.
    /// </summary>
    private static PlaybackSession AfterStartup(double elapsed, double level, int samples = 5) {
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(100, 300))
                                            .WithBandwidth(new BandwidthEstimate(5_000, samples, 1.0))
                                            .Generate();

        session.DecideNext(AnyLatency);
        session.CompleteDownload(elapsed);
        session.Buffer.Drain(session.Buffer.LevelSeconds - level);

        return session;
    }

    /// <summary>
    ///     The same, one segment later: the second one (720p, 12 000 kilobits) took <paramref name="elapsed" /> seconds, and the buffer is set again.
    /// </summary>
    private static PlaybackSession At720(double elapsed, double level) {
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 60);

        AdaptationDecision climb = session.DecideNext(AnyLatency);
        Assert.Equal("720p30", climb.Chosen.Id);   // the set-up itself: the estimate was 9 600 and the buffer comfortable
        session.CompleteDownload(elapsed);
        session.Buffer.Drain(session.Buffer.LevelSeconds - level);

        return session;
    }

    // ---- the first decision ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_first_decision_is_a_startup_one_rung_above_the_lowest_even_when_the_estimate_would_afford_more() {
        // Arrange: a budget of 4 000 kbps would afford 720p.
        PlaybackSession session = Standard().WithBandwidth(new BandwidthEstimate(5_000, 5, 0.5)).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(100));

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal("240p30", decision.Previous.Id);
        Assert.Equal("480p30", decision.Chosen.Id);
        Assert.Equal(0, decision.Segment.Index);
    }

    [Fact]
    public void The_first_decision_never_climbs_more_than_one_rung_however_fast_the_network() {
        // Arrange
        PlaybackSession session = Standard().WithBandwidth(new BandwidthEstimate(1_000_000, 5, 0.5)).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal("480p30", decision.Chosen.Id);
    }

    [Fact]
    public void The_first_decision_stays_on_the_lowest_variant_when_nothing_better_is_affordable() {
        // Arrange: a budget of 160 kbps affords nothing, the lowest is the answer anyway.
        PlaybackSession session = Standard().WithBandwidth(new BandwidthEstimate(200, 5, 0.5)).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Equal("240p30", decision.Chosen.Id);
        Assert.False(decision.IsSwitch);
    }

    [Theory]
    [InlineData(0, 0.96)]
    [InlineData(1_000, 1.96)]
    [InlineData(5_000, 5.96)]
    public void The_predicted_download_time_is_the_latency_plus_the_time_the_chosen_segment_takes(int latencyMilliseconds, double expected) {
        // Arrange: 480p is 1 200 kbps, a segment is 4 s, the estimate is 5 000 kbps: 4 800 kilobits take 0.96 s.
        PlaybackSession session = Standard().WithBandwidth(new BandwidthEstimate(5_000, 5, 0.5)).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(latencyMilliseconds));

        // Assert
        Assert.Equal(expected, decision.PredictedDownloadSeconds, precision: 9);
    }

    [Fact]
    public void A_decision_is_pending_and_recorded_as_soon_as_it_is_made() {
        // Arrange
        PlaybackSession session = Standard().Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Same(decision, session.PendingDecision);
        Assert.Same(decision, Assert.Single(session.History));
    }

    [Fact]
    public void A_decision_needs_a_latency() {
        // Arrange
        PlaybackSession session = Standard().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("latency", () => session.DecideNext(null!));
    }

    [Fact]
    public void Deciding_again_while_a_download_is_pending_is_refused() {
        // Arrange
        PlaybackSession session = Standard().Generate();
        session.DecideNext(AnyLatency);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => session.DecideNext(AnyLatency));

        // Assert
        Assert.Contains("Segment 0", refusal.Message);
        Assert.Single(session.History);
    }

    [Fact]
    public void Deciding_with_a_buffer_that_cannot_take_another_segment_is_refused() {
        // Arrange: 3 s free, a segment is 4 s.
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(17, 20)).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => session.DecideNext(AnyLatency));

        // Assert
        Assert.Contains("full", refusal.Message);
        Assert.Null(session.PendingDecision);
        Assert.Empty(session.History);
    }

    [Fact]
    public void Deciding_with_exactly_enough_room_for_the_segment_is_accepted() {
        // Arrange: 4 s free, a segment is 4 s.
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(16, 20)).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.NotNull(decision);
    }

    // ---- the decisions that follow ---------------------------------------------------------------------------------------

    [Fact]
    public void With_the_buffer_at_the_comfort_threshold_and_a_better_variant_affordable_it_climbs_one_rung() {
        // Arrange: the estimate is 4 800 kbps, a budget of 3 840 affords 720p; the buffer is exactly at comfort.
        PlaybackSession session = AfterStartup(elapsed: 1, level: 15);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal("480p30", decision.Previous.Id);
        Assert.Equal("720p30", decision.Chosen.Id);
    }

    [Fact]
    public void Below_the_comfort_threshold_it_holds_even_though_a_better_variant_is_affordable() {
        // Arrange
        PlaybackSession session = AfterStartup(elapsed: 1, level: 14.5);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal("480p30", decision.Chosen.Id);
    }

    [Fact]
    public void It_climbs_a_single_rung_although_the_top_of_the_ladder_is_affordable() {
        // Arrange: the estimate is 9 600 kbps, a budget of 7 680 affords 1080p; the buffer is comfortable.
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 40);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal("720p30", decision.Chosen.Id);
    }

    [Fact]
    public void It_holds_when_the_current_variant_is_the_one_the_estimate_affords_whatever_the_buffer() {
        // Arrange: the estimate is 2 400 kbps, a budget of 1 920 affords exactly 480p; the buffer is nearly full.
        PlaybackSession session = AfterStartup(elapsed: 2, level: 100);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal("480p30", decision.Chosen.Id);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void It_drops_at_once_to_the_variant_the_estimate_affords_even_with_a_full_buffer() {
        // Arrange: the estimate is 1 200 kbps, a budget of 960 affords only 240p.
        PlaybackSession session = AfterStartup(elapsed: 4, level: 100);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal("240p30", decision.Chosen.Id);
    }

    [Fact]
    public void It_drops_several_rungs_in_one_decision() {
        // Arrange: on 720p, the last segment (12 000 kilobits) took 30 s: the estimate is 400 kbps, a budget of 320.
        PlaybackSession session = At720(elapsed: 30, level: 30);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal("720p30", decision.Previous.Id);
        Assert.Equal("240p30", decision.Chosen.Id);
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
    }

    [Fact]
    public void Under_the_panic_threshold_it_drops_to_the_lowest_variant_whatever_the_estimate_says() {
        // Arrange: the estimate is 9 600 kbps, 1080p is affordable; the buffer holds 4.5 s.
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 4.5);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Equal("240p30", decision.Chosen.Id);
    }

    [Fact]
    public void Panic_skips_the_sustainable_variant_and_goes_straight_to_the_lowest() {
        // Arrange: on 720p, with an estimate of 3 000 kbps (a budget of 2 400 affords 480p) but only 3 s of buffer.
        PlaybackSession session = At720(elapsed: 4, level: 3);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Equal("240p30", decision.Chosen.Id);
    }

    [Fact]
    public void A_buffer_exactly_at_the_panic_threshold_is_not_in_panic() {
        // Arrange
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 5);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Equal("480p30", decision.Chosen.Id);
    }

    [Fact]
    public void Panic_on_the_lowest_variant_is_still_a_panic_that_does_not_move() {
        // Arrange: a first segment so slow that the estimate fell to 100 kbps, which sent the second decision down to 240p.
        PlaybackSession session = AfterStartup(elapsed: 48, level: 40);
        AdaptationDecision down = session.DecideNext(AnyLatency);
        Assert.Equal("240p30", down.Chosen.Id);   // the set-up itself
        session.CompleteDownload(1);
        session.Buffer.Drain(session.Buffer.LevelSeconds - 3);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Equal("240p30", decision.Previous.Id);
        Assert.Equal("240p30", decision.Chosen.Id);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void While_the_estimate_is_warming_up_it_neither_panics_nor_waits_for_the_comfort_threshold() {
        // Arrange: one sample before the first segment, so two after it: still warming up. The estimate is 9 600 kbps; the buffer holds 3 s.
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 3, samples: 1);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal("720p30", decision.Chosen.Id);
    }

    [Fact]
    public void Once_the_estimate_has_three_samples_the_panic_threshold_applies() {
        // Arrange: two samples before the first segment, so three after it.
        PlaybackSession session = AfterStartup(elapsed: 0.5, level: 3, samples: 2);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
    }

    [Fact]
    public void While_the_estimate_is_warming_up_a_slow_network_still_drops_the_variant() {
        // Arrange: an estimate of 1 200 kbps, a budget of 960.
        PlaybackSession session = AfterStartup(elapsed: 4, level: 40, samples: 1);

        // Act
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal("240p30", decision.Chosen.Id);
    }

    // ---- completing a download -------------------------------------------------------------------------------------------

    [Fact]
    public void Completing_a_download_plays_the_buffer_down_appends_the_segment_and_moves_on() {
        // Arrange: 20 s buffered in a 60 s buffer, an estimate of 5 000 kbps, half weight to a new observation.
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 60))
                                            .WithBandwidth(new BandwidthEstimate(5_000, 5, 0.5))
                                            .Generate();
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Act: the 4 800 kilobits of 480p took 1 s.
        double stall = session.CompleteDownload(1);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(23, session.Buffer.LevelSeconds);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Equal(4, session.NextSegment.DurationSeconds);
        Assert.Same(decision.Chosen, session.CurrentVariant);
        Assert.Null(session.PendingDecision);
        Assert.Single(session.History);
    }

    [Fact]
    public void The_estimate_learns_the_throughput_observed_while_downloading() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 60))
                                            .WithBandwidth(new BandwidthEstimate(5_000, 5, 0.5))
                                            .Generate();
        session.DecideNext(AnyLatency);

        // Act: 4 800 kilobits in 1 s is 4 800 kbps, folded in at half weight into 5 000.
        session.CompleteDownload(1);

        // Assert
        Assert.Equal(4_900, session.Bandwidth.ThroughputKbps, precision: 9);
        Assert.Equal(6, session.Bandwidth.SampleCount);
    }

    [Fact]
    public void A_download_longer_than_the_buffer_stalls_for_the_difference() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(2, 60)).Generate();
        session.DecideNext(AnyLatency);

        // Act
        double stall = session.CompleteDownload(5);

        // Assert
        Assert.Equal(3, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(3, session.TotalStallSeconds);
        Assert.Equal(4, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void Stalls_accumulate_over_the_session() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(2, 60)).Generate();
        session.DecideNext(AnyLatency);
        session.CompleteDownload(5);   // stalls 3 s; the buffer is then 4 s
        session.DecideNext(AnyLatency);

        // Act
        double second = session.CompleteDownload(6);   // stalls 2 s

        // Assert
        Assert.Equal(2, second);
        Assert.Equal(2, session.StallCount);
        Assert.Equal(5, session.TotalStallSeconds);
    }

    [Fact]
    public void A_download_that_the_buffer_outlasts_is_not_a_stall() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(5, 60)).Generate();
        session.DecideNext(AnyLatency);

        // Act
        session.CompleteDownload(5);

        // Assert
        Assert.Equal(0, session.StallCount);
        Assert.Equal(0, session.TotalStallSeconds);
    }

    [Fact]
    public void Completing_with_nothing_pending_is_refused() {
        // Arrange
        PlaybackSession session = Standard().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => session.CompleteDownload(1));
    }

    [Fact]
    public void A_download_can_be_completed_only_once() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 60)).Generate();
        session.DecideNext(AnyLatency);
        session.CompleteDownload(1);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => session.CompleteDownload(1));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(600.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_elapsed_time_outside_zero_to_six_hundred_seconds_is_refused_and_the_download_stays_pending(double elapsed) {
        // Arrange
        PlaybackSession    session  = Standard().WithBuffer(new PlaybackBuffer(20, 60)).Generate();
        AdaptationDecision decision = session.DecideNext(AnyLatency);

        // Act
        Assert.Throws<ArgumentOutOfRangeException>("elapsedSeconds", () => session.CompleteDownload(elapsed));

        // Assert
        Assert.Same(decision, session.PendingDecision);
        Assert.Equal(20, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void A_download_of_exactly_six_hundred_seconds_is_accepted() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 60)).Generate();
        session.DecideNext(AnyLatency);

        // Act
        double stall = session.CompleteDownload(600);

        // Assert
        Assert.Equal(580, stall);
    }

    [Fact]
    public void The_history_keeps_the_decisions_in_order_with_their_segments() {
        // Arrange
        PlaybackSession session = AfterStartup(elapsed: 1, level: 15);

        // Act
        AdaptationDecision second = session.DecideNext(AnyLatency);
        session.CompleteDownload(1);

        // Assert
        Assert.Equal([0, 1], session.History.Select(d => d.Segment.Index));
        Assert.Equal([AdaptationReason.Startup, AdaptationReason.SwitchUp], session.History.Select(d => d.Reason));
        Assert.Same(second, session.History[1]);
    }

    [Fact]
    public void A_session_downloading_to_the_end_of_a_comfortable_run_climbs_the_ladder_and_does_not_stall() {
        // Arrange: a fast, steady network and a generous buffer, with the estimate watching a 10 000 kbps link.
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(30, 120))
                                            .WithBandwidth(new BandwidthEstimate(10_000, 5, 0.5))
                                            .Generate();

        // Act: each segment arrives at 10 000 kbps.
        for (int i = 0; i < 6; i++) {
            AdaptationDecision decision = session.DecideNext(AnyLatency);
            session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / 10_000);
        }

        // Assert
        Assert.Equal("1080p30", session.CurrentVariant.Id);
        Assert.Equal(0, session.StallCount);
    }

    // ---- idling ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void Idling_plays_the_buffer_down() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(10, 60)).Generate();

        // Act
        double stall = session.Idle(4);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(6, session.Buffer.LevelSeconds);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void Idling_beyond_the_buffer_is_a_stall() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(10, 60)).Generate();

        // Act
        double stall = session.Idle(15);

        // Assert
        Assert.Equal(5, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(5, session.TotalStallSeconds);
    }

    [Fact]
    public void Idling_makes_room_for_the_next_segment_when_the_buffer_was_full() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 20)).Generate();
        Assert.Throws<InvalidOperationException>(() => session.DecideNext(AnyLatency));

        // Act
        session.Idle(4);

        // Assert
        Assert.NotNull(session.DecideNext(AnyLatency));
    }

    [Fact]
    public void Idling_while_a_download_is_pending_is_refused() {
        // Arrange
        PlaybackSession session = Standard().WithBuffer(new PlaybackBuffer(20, 60)).Generate();
        session.DecideNext(AnyLatency);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => session.Idle(1));
        Assert.Equal(20, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void Idling_for_a_negative_time_is_refused() {
        // Arrange
        PlaybackSession session = Standard().Generate();
        double          anyNegative = Any.Double().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => session.Idle(anyNegative));
    }

    // ---- sessions nobody described -----------------------------------------------------------------------------------------

    [Fact]
    public void A_download_completed_on_a_session_nobody_described_advances_to_the_next_segment() {
        // Arrange: only the room for a segment is stated; the ladder, the policy and the estimate are whatever they are.
        PlaybackSession session = new AnyPlaybackSession().WithBuffer(b => b.WithCapacitySeconds(100).WithLevelSeconds(l => l.LessThanOrEqualTo(50))).Generate();
        session.DecideNext(AnyLatency);

        // Act
        session.CompleteDownload(Any.Double().Between(0.1, 20).Generate());

        // Assert
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Null(session.PendingDecision);
    }

}
