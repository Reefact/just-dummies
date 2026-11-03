using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class PlaybackSessionTests {

    // Scenario arithmetic, for the reader. FiveRungLadder: 400, 1000, 2000, 4000, 8000 kbps (rungs 0 to 4); segments last
    // 4 s, so they weigh 1600, 4000, 8000, 16000, 32000 kilobits. Fixtures.Climbed downloads at 16 000 kbps with a safety
    // factor of 0.7 (budget 11 200 kbps): one rung per round, rung 1 after the first (the start-up is capped at one rung).

    // ---- construction ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_session_starts_on_the_lowest_variant_at_segment_zero() {
        BitrateLadder ladder = Fixtures.FiveRungLadder();

        PlaybackSession session = Any.PlaybackSession().WithLadder(ladder).Generate();

        Assert.Same(ladder.Lowest, session.CurrentVariant);
        Assert.Equal(0, session.NextSegment.Index);
    }

    [Fact]
    public void A_new_session_has_no_download_pending_no_stall_and_no_history() {
        PlaybackSession session = Any.PlaybackSession().WithLadder(Fixtures.FiveRungLadder()).Generate();

        Assert.Null(session.PendingDecision);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(0, session.TotalStallSeconds);
        Assert.Empty(session.History);
    }

    [Fact]
    public void A_new_session_keeps_the_parts_it_was_given() {
        BitrateLadder     ladder    = Fixtures.FiveRungLadder();
        PlaybackBuffer    buffer    = new PlaybackBuffer(10, 60);
        AdaptationPolicy  policy    = new AdaptationPolicy(0.8, 5, 20);
        BandwidthEstimate bandwidth = new BandwidthEstimate(3_000, 4, 0.5);

        PlaybackSession session = new PlaybackSession(ladder, buffer, policy, bandwidth, 6);

        Assert.Same(ladder, session.Ladder);
        Assert.Same(buffer, session.Buffer);
        Assert.Same(policy, session.Policy);
        Assert.Same(bandwidth, session.Bandwidth);
        Assert.Equal(6, session.NextSegment.DurationSeconds);
    }

    [Fact]
    public void A_session_refuses_a_missing_part() {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        PlaybackBuffer    anyBuffer    = new PlaybackBuffer(0, 100);
        AdaptationPolicy  anyPolicy    = new AdaptationPolicy(0.8, 5, 20);
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        Assert.Throws<ArgumentNullException>(() => new PlaybackSession(null!, anyBuffer, anyPolicy, anyBandwidth, 4));
        Assert.Throws<ArgumentNullException>(() => new PlaybackSession(anyLadder, null!, anyPolicy, anyBandwidth, 4));
        Assert.Throws<ArgumentNullException>(() => new PlaybackSession(anyLadder, anyBuffer, null!, anyBandwidth, 4));
        Assert.Throws<ArgumentNullException>(() => new PlaybackSession(anyLadder, anyBuffer, anyPolicy, null!, 4));
    }

    [Fact]
    public void A_comfort_threshold_above_the_buffer_capacity_is_refused() {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PlaybackSession(anyLadder, new PlaybackBuffer(0, 60), new AdaptationPolicy(0.8, 5, 60.001), anyBandwidth, 4));

        Assert.Equal("policy", exception.ParamName);
    }

    [Fact]
    public void A_comfort_threshold_equal_to_the_buffer_capacity_is_accepted() {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        PlaybackSession session = new PlaybackSession(anyLadder, new PlaybackBuffer(0, 60), new AdaptationPolicy(0.8, 5, 60), anyBandwidth, 4);

        Assert.Equal(60, session.Policy.ComfortBufferSeconds);
    }

    [Fact]
    public void A_segment_longer_than_the_buffer_capacity_is_refused() {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PlaybackSession(anyLadder, new PlaybackBuffer(0, 4), new AdaptationPolicy(0.8, 1, 2), anyBandwidth, 5));

        Assert.Equal("segmentDurationSeconds", exception.ParamName);
    }

    [Fact]
    public void A_segment_as_long_as_the_buffer_capacity_is_accepted() {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        PlaybackSession session = new PlaybackSession(anyLadder, new PlaybackBuffer(0, 4), new AdaptationPolicy(0.8, 1, 2), anyBandwidth, 4);

        Assert.Equal(4, session.NextSegment.DurationSeconds);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(10.01)]
    [InlineData(double.NaN)]
    public void A_segment_duration_outside_the_segment_range_is_refused(double segmentDurationSeconds) {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PlaybackSession(anyLadder, new PlaybackBuffer(0, 100), new AdaptationPolicy(0.8, 5, 20), anyBandwidth, segmentDurationSeconds));
    }

    // ---- DecideNext: the first decision -------------------------------------------------------------------------------

    [Fact]
    public void The_first_decision_is_a_startup_capped_at_one_rung_above_the_lowest() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8);   // budget 8 000: rung 4 fits

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Previous);
        Assert.Same(session.Ladder.Variants[1], decision.Chosen);
    }

    [Fact]
    public void The_first_decision_takes_the_rung_the_budget_just_reaches() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 2_500, safetyFactor: 0.4);   // budget 1 000 = rung 1

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Same(session.Ladder.Variants[1], decision.Chosen);
    }

    [Fact]
    public void The_first_decision_stays_on_the_lowest_when_the_budget_does_not_reach_the_next_rung() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 1_000, safetyFactor: 0.5);   // budget 500: rung 0 only

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void The_first_decision_falls_back_to_the_lowest_when_the_budget_is_below_it() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 400, safetyFactor: 0.5);   // budget 200: below the lowest

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    [Fact]
    public void A_decision_is_for_the_next_segment_and_is_left_pending() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Same(session.NextSegment, decision.Segment);
        Assert.Same(decision, session.PendingDecision);
        Assert.Same(decision, Assert.Single(session.History));
    }

    [Fact]
    public void The_prediction_is_the_latency_plus_the_download_time_at_the_estimated_throughput() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8);   // startup on rung 1: 4 000 kilobits

        AdaptationDecision decision = session.DecideNext(new Latency(250));

        Assert.Equal(0.25 + (4_000.0 / 10_000.0), decision.PredictedDownloadSeconds, precision: 9);
    }

    [Fact]
    public void A_decision_cannot_be_taken_while_a_download_is_pending() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8);
        session.DecideNext(Any.Latency().Generate());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => session.DecideNext(Any.Latency().Generate()));

        Assert.Contains("still being downloaded", exception.Message);
        Assert.Single(session.History);
    }

    [Fact]
    public void A_decision_cannot_be_taken_without_a_latency() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8);

        Assert.Throws<ArgumentNullException>(() => session.DecideNext(null!));
    }

    [Fact]
    public void A_decision_cannot_be_taken_when_the_next_segment_does_not_fit_in_the_buffer() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8, levelSeconds: 96.5);   // 3.5 s free for a 4 s segment

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => session.DecideNext(Any.Latency().Generate()));

        Assert.Contains("full", exception.Message);
        Assert.Null(session.PendingDecision);
        Assert.Empty(session.History);
    }

    [Fact]
    public void A_decision_can_be_taken_when_the_next_segment_exactly_fits() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8, levelSeconds: 96);   // 4 s free for a 4 s segment

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.NotNull(decision);
    }

    [Fact]
    public void Playing_makes_room_for_the_next_decision() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 10_000, safetyFactor: 0.8, levelSeconds: 100);
        Latency         anyLatency = Any.Latency().Generate();
        Assert.Throws<InvalidOperationException>(() => session.DecideNext(anyLatency));

        session.Idle(4);

        Assert.NotNull(session.DecideNext(anyLatency));
    }

    // ---- DecideNext: once the estimate is trusted ---------------------------------------------------------------------

    [Fact]
    public void A_trusted_session_climbs_one_rung_at_a_time_while_the_buffer_is_comfortable() {
        PlaybackSession session = Fixtures.Climbed(cycles: 1);   // on rung 1, budget 11 200 would allow rung 4

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_holds_when_the_budget_matches_the_current_rung() {
        PlaybackSession session = Fixtures.Climbed(cycles: 4);   // on the top rung

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Same(session.CurrentVariant, decision.Chosen);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void A_trusted_session_holds_instead_of_climbing_just_below_the_comfort_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);   // on rung 2
        Fixtures.DrainTo(session, 29.75);                        // comfort is 30

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_climbs_when_the_buffer_is_exactly_at_the_comfort_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);   // on rung 2
        Fixtures.DrainTo(session, 30);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Same(session.Ladder.Variants[3], decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_panics_to_the_lowest_variant_when_the_buffer_is_below_the_panic_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);   // on rung 2, plenty of bandwidth
        Fixtures.DrainTo(session, 7.75);                         // panic is 8

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
        Assert.Same(session.Ladder.Variants[2], decision.Previous);
    }

    [Fact]
    public void A_trusted_session_panics_on_an_empty_buffer() {
        PlaybackSession session = Fixtures.Climbed(cycles: 3);
        Fixtures.DrainTo(session, 0);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_does_not_panic_when_the_buffer_is_exactly_at_the_panic_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);
        Fixtures.DrainTo(session, 8);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_session_already_on_the_lowest_variant_still_reports_a_panic() {
        PlaybackSession session = Fixtures.Climbed(cycles: 1, throughputKbps: 3_200, safetyFactor: 0.1);   // budget 320: stays on rung 0
        Fixtures.DrainTo(session, 5);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Previous);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_drops_at_once_to_the_sustainable_variant_even_across_several_rungs() {
        PlaybackSession session = Fixtures.Climbed(cycles: 4);                        // on rung 4 at 16 000 kbps
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(8);                                                  // 32 000 kb in 8 s: the estimate becomes 4 000 kbps

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());   // budget 2 800: rung 2

        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_drops_even_when_the_buffer_is_below_the_comfort_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 4);
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(8);
        Fixtures.DrainTo(session, 15);                                                // above panic (8), below comfort (30)

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_drops_to_the_lowest_with_a_switch_down_when_the_budget_is_below_every_variant() {
        PlaybackSession session = Fixtures.Climbed(cycles: 4);
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(60);                                                 // 32 000 kb in 60 s: about 533 kbps, budget about 373

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    [Fact]
    public void A_trusted_session_predicts_the_latency_plus_the_download_time_of_the_chosen_variant() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);   // climbs to rung 3: 16 000 kilobits at 16 000 kbps

        AdaptationDecision decision = session.DecideNext(new Latency(500));

        Assert.Equal(0.5 + 1.0, decision.PredictedDownloadSeconds, precision: 9);
    }

    [Fact]
    public void The_history_lists_the_decisions_in_order_with_their_segments_and_reasons() {
        PlaybackSession session = Fixtures.Climbed(cycles: 3);

        Assert.Equal([0, 1, 2], session.History.Select(d => d.Segment.Index));
        Assert.Equal([AdaptationReason.Startup, AdaptationReason.SwitchUp, AdaptationReason.SwitchUp], session.History.Select(d => d.Reason));
        Assert.Equal(3, session.NextSegment.Index);
    }

    // ---- DecideNext: while the estimate is still warming up ----------------------------------------------------------

    [Fact]
    public void A_warming_up_session_climbs_one_rung_even_with_the_buffer_below_the_comfort_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 1, sampleCount: 1);   // two samples after the first download: still warming up
        Fixtures.DrainTo(session, 20);                                           // comfort is 30

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.True(session.Bandwidth.IsWarmingUp);
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Same(session.Ladder.Variants[2], decision.Chosen);
    }

    [Fact]
    public void A_session_whose_estimate_just_stopped_warming_up_holds_below_the_comfort_threshold() {
        PlaybackSession session = Fixtures.Climbed(cycles: 1, sampleCount: 2);   // three samples after the first download: trusted
        Fixtures.DrainTo(session, 20);

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.False(session.Bandwidth.IsWarmingUp);
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
    }

    [Fact]
    public void A_warming_up_session_does_not_panic_even_with_an_almost_empty_buffer() {
        // This follows the model's order (warm-up before panic); whether a near-empty buffer should climb is a product question.
        PlaybackSession session = Fixtures.Climbed(cycles: 1, sampleCount: 1);
        Fixtures.DrainTo(session, 2);                                            // panic is 8

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
    }

    [Fact]
    public void A_warming_up_session_drops_to_the_variant_its_budget_allows() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7, sampleCount: 1);
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(16);                                            // 4 000 kb in 16 s: the estimate becomes 250 kbps

        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        Assert.True(session.Bandwidth.IsWarmingUp);
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
    }

    // ---- CompleteDownload --------------------------------------------------------------------------------------------

    [Fact]
    public void Completing_a_download_adds_the_segment_to_the_buffer_after_the_playback_of_the_elapsed_time() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);   // buffer holds 70 s
        session.DecideNext(Any.Latency().Generate());

        double stall = session.CompleteDownload(0.25);

        Assert.Equal(0, stall);
        Assert.Equal(70 - 0.25 + 4, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void Completing_a_download_adopts_the_chosen_variant_and_moves_to_the_next_segment() {
        PlaybackSession    session  = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        session.CompleteDownload(0.25);

        Assert.Same(decision.Chosen, session.CurrentVariant);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Null(session.PendingDecision);
        Assert.Single(session.History);
    }

    [Fact]
    public void Completing_a_download_feeds_the_observed_throughput_to_the_estimate() {
        PlaybackSession session = Any.PlaybackSession()
                                     .WithLadder(Fixtures.FiveRungLadder())
                                     .WithBuffer(new PlaybackBuffer(70, 100))
                                     .WithPolicy(new AdaptationPolicy(1.0, 8, 30))
                                     .WithBandwidth(new BandwidthEstimate(1_000, 5, 0.5))
                                     .WithSegmentDurationSeconds(4)
                                     .Generate();
        session.DecideNext(Any.Latency().Generate());   // startup on rung 1: 4 000 kilobits

        session.CompleteDownload(2);                    // observed 2 000 kbps

        Assert.Equal(1_500.0, session.Bandwidth.ThroughputKbps);   // 0.5 * 2000 + 0.5 * 1000
        Assert.Equal(6, session.Bandwidth.SampleCount);
    }

    [Fact]
    public void A_download_longer_than_the_buffer_stalls_for_the_difference() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7, levelSeconds: 2);
        session.DecideNext(Any.Latency().Generate());

        double stall = session.CompleteDownload(5);

        Assert.Equal(3, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(3, session.TotalStallSeconds);
        Assert.Equal(4, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void Stalls_accumulate_over_downloads() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7, levelSeconds: 2);
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(5);        // 3 s stall, the buffer then holds 4 s
        session.DecideNext(Any.Latency().Generate());

        session.CompleteDownload(10);       // 6 s stall

        Assert.Equal(2, session.StallCount);
        Assert.Equal(9, session.TotalStallSeconds);
    }

    [Fact]
    public void A_download_ending_exactly_as_the_buffer_empties_is_not_a_stall() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7, levelSeconds: 5);
        session.DecideNext(Any.Latency().Generate());

        double stall = session.CompleteDownload(5);

        Assert.Equal(0, stall);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void A_download_of_the_longest_accepted_duration_is_accepted() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        session.DecideNext(Any.Latency().Generate());

        double stall = session.CompleteDownload(600);

        Assert.Equal(530, stall);
    }

    [Fact]
    public void Completing_without_a_pending_download_is_refused() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        double          anyElapsed = Any.Double().Between(0.1, 10).Generate();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => session.CompleteDownload(anyElapsed));

        Assert.Contains("No segment download is pending", exception.Message);
    }

    [Fact]
    public void A_download_cannot_be_completed_twice() {
        PlaybackSession session    = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        double          anyElapsed = Any.Double().Between(0.1, 10).Generate();
        session.DecideNext(Any.Latency().Generate());
        session.CompleteDownload(anyElapsed);

        Assert.Throws<InvalidOperationException>(() => session.CompleteDownload(anyElapsed));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(600.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_elapsed_time_outside_the_accepted_range_is_refused_and_the_download_stays_pending(double elapsedSeconds) {
        PlaybackSession    session  = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => session.CompleteDownload(elapsedSeconds));

        Assert.Equal("elapsedSeconds", exception.ParamName);
        Assert.Same(decision, session.PendingDecision);
        Assert.Equal(70, session.Buffer.LevelSeconds);
        Assert.Equal(5, session.Bandwidth.SampleCount);
    }

    // ---- Idle -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Idling_plays_the_buffer_down_without_a_stall() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);

        double stall = session.Idle(10);

        Assert.Equal(0, stall);
        Assert.Equal(60, session.Buffer.LevelSeconds);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void Idling_past_the_buffer_counts_a_stall() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7, levelSeconds: 5);

        double stall = session.Idle(8);

        Assert.Equal(3, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(3, session.TotalStallSeconds);
    }

    [Fact]
    public void Idling_does_not_touch_the_history_or_the_current_variant() {
        PlaybackSession session = Fixtures.Climbed(cycles: 2);
        EncodingVariant before  = session.CurrentVariant;

        session.Idle(5);

        Assert.Same(before, session.CurrentVariant);
        Assert.Equal(2, session.History.Count);
    }

    [Fact]
    public void Idling_while_a_download_is_pending_is_refused() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);
        double          anySeconds = Any.Double().Between(0.1, 10).Generate();
        session.DecideNext(Any.Latency().Generate());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => session.Idle(anySeconds));

        Assert.Contains("pending", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Idling_for_a_negative_or_NaN_time_is_refused_and_counts_nothing(double seconds) {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Idle(seconds));

        Assert.Equal(70, session.Buffer.LevelSeconds);
        Assert.Equal(0, session.StallCount);
    }

    // ---- whole sessions ----------------------------------------------------------------------------------------------

    [Fact]
    public void With_plenty_of_bandwidth_a_session_settles_on_the_top_variant_without_stalling() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 16_000, safetyFactor: 0.7);

        for (int i = 0; i < 40; i++) {
            if (session.Buffer.FreeSeconds < 4) { session.Idle(4 - session.Buffer.FreeSeconds); }

            AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());
            session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / 16_000);
        }

        Assert.Same(session.Ladder.Highest, session.CurrentVariant);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(40, session.History.Count);
    }

    [Fact]
    public void When_even_the_lowest_variant_is_slower_than_playback_the_buffer_eventually_runs_dry() {
        PlaybackSession session = Fixtures.NewSession(throughputKbps: 300, safetyFactor: 0.7, levelSeconds: 20);   // 1 600 kb at 300 kbps: 5.33 s per 4 s segment

        int rounds = 0;
        while (session.StallCount == 0 && rounds < 200) {
            AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());
            session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / 300);
            rounds++;
        }

        Assert.True(session.StallCount > 0);
        Assert.True(session.TotalStallSeconds > 0);
        Assert.All(session.History, d => Assert.Same(session.Ladder.Lowest, d.Chosen));
    }

}
