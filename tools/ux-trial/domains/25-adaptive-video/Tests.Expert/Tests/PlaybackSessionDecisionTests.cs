using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

// The four-rung ladder is 500, 1500, 3000 and 6000 kbps; every segment lasts four seconds.
public sealed class PlaybackSessionDecisionTests {

    // --- the first segment -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(400, 500)]
    [InlineData(1_000, 500)]
    [InlineData(1_500, 1_500)]
    [InlineData(2_000, 1_500)]
    [InlineData(3_500, 1_500)]
    [InlineData(1_000_000, 1_500)]
    public void The_first_segment_is_the_best_rung_within_the_budget_but_never_more_than_one_rung_up(double throughputKbps, int expectedKbps) {
        // Arrange
        PlaybackSession session = Playback.Recipe(throughputKbps, levelSeconds: 0, safetyFactor: 1.0).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(expectedKbps, decision.Chosen.Bitrate.Kbps);
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
    }

    [Fact]
    public void The_budget_is_the_estimated_throughput_times_the_safety_factor() {
        // Arrange
        PlaybackSession cautious = Playback.Recipe(2_000, levelSeconds: 0, safetyFactor: 0.5).Generate();
        PlaybackSession bolder   = Playback.Recipe(2_000, levelSeconds: 0, safetyFactor: 0.75).Generate();

        // Act
        AdaptationDecision cautiousDecision = cautious.DecideNext(Playback.NoLatency);
        AdaptationDecision bolderDecision   = bolder.DecideNext(Playback.NoLatency);

        // Assert: a budget of 1000 kbps, then of 1500
        Assert.Equal(500, cautiousDecision.Chosen.Bitrate.Kbps);
        Assert.Equal(1_500, bolderDecision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void A_first_segment_that_stays_on_the_lowest_variant_is_a_startup_and_not_a_hold() {
        // Arrange
        PlaybackSession session = Playback.Recipe(400, levelSeconds: 0).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Same(session.Ladder.Lowest, decision.Previous);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
        Assert.Equal(AdaptationReason.Startup, decision.Reason);
        Assert.False(decision.IsSwitch);
    }

    [Fact]
    public void A_decision_is_about_the_next_segment_and_waits_for_its_download() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 0).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(0, decision.Segment.Index);
        Assert.Same(decision, session.PendingDecision);
        Assert.Same(decision, Assert.Single(session.History));
    }

    // --- the prediction ----------------------------------------------------------------------------------------

    [Fact]
    public void The_prediction_is_the_round_trip_plus_the_time_to_download_the_segment_at_the_estimated_throughput() {
        // Arrange: 1500 kbps x 4 s = 6000 kilobits, over 12 000 kbps
        PlaybackSession session = Playback.Recipe(12_000, levelSeconds: 0, safetyFactor: 0.5).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(250));

        // Assert: half a second of download, a quarter of a second of round trip
        Assert.Equal(1_500, decision.Chosen.Bitrate.Kbps);
        Assert.Equal(0.75, decision.PredictedDownloadSeconds, precision: 9);
    }

    [Fact]
    public void Without_latency_the_prediction_is_the_download_alone() {
        // Arrange
        PlaybackSession session = Playback.Recipe(12_000, levelSeconds: 0, safetyFactor: 0.5).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(new Latency(0));

        // Assert
        Assert.Equal(0.5, decision.PredictedDownloadSeconds, precision: 9);
    }

    // --- while the estimate is warming up ----------------------------------------------------------------------

    [Fact]
    public void A_warming_up_estimate_is_followed_one_rung_at_a_time_even_when_the_budget_allows_more() {
        // Arrange: the startup segment (1500 kbps) is delivered in half a second, which reads as 12 000 kbps
        PlaybackSession session = Playback.Recipe(10_000, sampleCount: 1).Generate();
        Playback.Download(session, 0.5);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(3_000, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void A_warming_up_estimate_may_drop_several_rungs_at_once() {
        // Arrange: the startup segment is so slow that it reads as 200 kbps
        PlaybackSession session = Playback.Recipe(10_000, sampleCount: 1).Generate();
        Playback.Download(session, 30);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal(500, decision.Chosen.Bitrate.Kbps);
    }

    [Theory]
    [InlineData(1, AdaptationReason.SwitchUp, 3_000)]
    [InlineData(2, AdaptationReason.Panic, 500)]
    public void The_buffer_rules_take_over_once_the_third_sample_is_in(int samplesBeforeStartup, AdaptationReason expectedReason, int expectedKbps) {
        // Arrange: a nearly empty buffer after the startup segment, below the panic threshold of five seconds
        PlaybackSession session = Playback.Recipe(10_000, sampleCount: samplesBeforeStartup).Generate();
        Playback.Download(session, 0.5);
        Playback.DrainTo(session, 2);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(expectedReason, decision.Reason);
        Assert.Equal(expectedKbps, decision.Chosen.Bitrate.Kbps);
    }

    // --- once the estimate is trusted: the buffer decides -------------------------------------------------------

    // After the startup segment (1500 kbps) delivered in one second the estimate reads 6000 kbps, enough for the top
    // rung, and the buffer holds 100 - 1 + 4 = 103 seconds before the test lowers it.
    private static PlaybackSession TrustedOnTheSecondRungWithABufferOf(double levelSeconds) {
        PlaybackSession session = Playback.Recipe(10_000, sampleCount: 3).Generate();
        Playback.Download(session, 1);
        Playback.DrainTo(session, levelSeconds);

        return session;
    }

    [Theory]
    [InlineData(103, 3_000)]
    [InlineData(25, 3_000)]
    [InlineData(20, 3_000)]
    public void With_a_comfortable_buffer_the_session_climbs_one_rung_and_no_more(double levelSeconds, int expectedKbps) {
        // Arrange: the budget would carry it to 6000 kbps
        PlaybackSession session = TrustedOnTheSecondRungWithABufferOf(levelSeconds);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchUp, decision.Reason);
        Assert.Equal(expectedKbps, decision.Chosen.Bitrate.Kbps);
        Assert.Equal(1_500, decision.Previous.Bitrate.Kbps);
    }

    [Theory]
    [InlineData(19.5)]
    [InlineData(10)]
    [InlineData(5)]
    public void Between_the_panic_and_the_comfort_thresholds_the_session_holds_even_if_the_budget_allows_more(double levelSeconds) {
        // Arrange
        PlaybackSession session = TrustedOnTheSecondRungWithABufferOf(levelSeconds);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Same(decision.Previous, decision.Chosen);
    }

    [Theory]
    [InlineData(4.5)]
    [InlineData(0.5)]
    [InlineData(0)]
    public void Below_the_panic_threshold_the_session_drops_to_the_lowest_variant_whatever_the_budget(double levelSeconds) {
        // Arrange
        PlaybackSession session = TrustedOnTheSecondRungWithABufferOf(levelSeconds);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
        Assert.True(decision.IsSwitch);
    }

    [Fact]
    public void A_panic_on_the_lowest_variant_stays_a_panic_without_switching() {
        // Arrange: only 500 kbps fits the budget, so the startup segment is the lowest variant
        PlaybackSession session = Playback.Recipe(400, sampleCount: 3).Generate();
        Playback.Download(session, 0.5);
        Playback.DrainTo(session, 2);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.Panic, decision.Reason);
        Assert.Same(session.Ladder.Lowest, decision.Previous);
        Assert.Same(session.Ladder.Lowest, decision.Chosen);
        Assert.False(decision.IsSwitch);
    }

    [Theory]
    [InlineData(6, 3_000)]
    [InlineData(12, 1_500)]
    [InlineData(30, 500)]
    public void When_the_throughput_falls_the_session_drops_at_once_to_the_variant_it_can_sustain(double elapsedSeconds, int expectedKbps) {
        // Arrange: on the top rung, a segment of 24 000 kilobits that takes 6, 12 or 30 seconds reads as 4000, 2000 or 800 kbps
        PlaybackSession session = Playback.Recipe(10_000).Generate();
        Playback.ClimbTo(session, 6_000);
        Playback.Download(session, elapsedSeconds);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.SwitchDown, decision.Reason);
        Assert.Equal(6_000, decision.Previous.Bitrate.Kbps);
        Assert.Equal(expectedKbps, decision.Chosen.Bitrate.Kbps);
    }

    [Fact]
    public void The_session_holds_the_rung_the_budget_sustains() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000).Generate();
        Playback.ClimbTo(session, 6_000);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(AdaptationReason.Hold, decision.Reason);
        Assert.Same(session.CurrentVariant, decision.Chosen);
        Assert.False(decision.IsSwitch);
    }

    // --- what the session refuses ------------------------------------------------------------------------------

    [Fact]
    public void No_segment_is_requested_while_the_previous_one_is_still_downloading() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => session.DecideNext(Playback.NoLatency));

        // Assert
        Assert.Contains("Segment 0", refusal.Message);
        Assert.Single(session.History);
    }

    [Fact]
    public void A_new_segment_may_be_requested_once_the_previous_one_is_in() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000).Generate();
        Playback.Download(session, 1);

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Equal(1, decision.Segment.Index);
        Assert.Equal(2, session.History.Count);
    }

    [Fact]
    public void No_segment_is_requested_when_it_would_not_fit_in_the_buffer() {
        // Arrange: three seconds free, for a segment of four
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 57, capacitySeconds: 60).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => session.DecideNext(Playback.NoLatency));

        // Assert
        Assert.Contains("full", refusal.Message);
        Assert.Null(session.PendingDecision);
        Assert.Empty(session.History);
    }

    [Fact]
    public void A_segment_is_requested_when_it_fits_the_buffer_exactly() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 56, capacitySeconds: 60).Generate();

        // Act
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Assert
        Assert.Same(decision, session.PendingDecision);
    }

    [Fact]
    public void A_segment_cannot_be_requested_without_a_latency() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anySession.DecideNext(null!));

        // Assert
        Assert.Equal("latency", refusal.ParamName);
    }

}
