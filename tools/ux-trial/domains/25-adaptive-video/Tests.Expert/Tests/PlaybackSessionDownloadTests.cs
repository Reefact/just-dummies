using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class PlaybackSessionDownloadTests {

    [Fact]
    public void A_delivered_segment_drains_the_buffer_by_the_time_it_took_and_adds_its_own_duration() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 30, capacitySeconds: 60).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        double stall = session.CompleteDownload(2);

        // Assert: 30 - 2 + 4
        Assert.Equal(0, stall);
        Assert.Equal(32, session.Buffer.LevelSeconds);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void A_download_slower_than_the_buffer_stalls_playback_for_the_difference() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 1.5, capacitySeconds: 60).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        double stall = session.CompleteDownload(4);

        // Assert: it ran dry after 1.5 seconds, then the segment landed
        Assert.Equal(2.5, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(2.5, session.TotalStallSeconds);
        Assert.Equal(4, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void Stalls_add_up_across_downloads() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 1.5, capacitySeconds: 60).Generate();
        Playback.Download(session, 4);

        // Act
        Playback.Download(session, 10);

        // Assert: 2.5 seconds, then 10 - 4 = 6 seconds
        Assert.Equal(2, session.StallCount);
        Assert.Equal(8.5, session.TotalStallSeconds);
    }

    [Fact]
    public void A_download_that_just_empties_the_buffer_is_not_a_stall() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 5, capacitySeconds: 60).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        double stall = session.CompleteDownload(5);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(4, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void A_delivered_segment_becomes_the_current_variant_and_the_next_segment_follows() {
        // Arrange
        PlaybackSession    session  = Playback.Recipe(10_000).Generate();
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Act
        session.CompleteDownload(1);

        // Assert
        Assert.Same(decision.Chosen, session.CurrentVariant);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Equal(4, session.NextSegment.DurationSeconds);
        Assert.Null(session.PendingDecision);
        Assert.Same(decision, Assert.Single(session.History));
    }

    [Fact]
    public void A_delivered_segment_teaches_the_estimate_the_throughput_it_was_delivered_at() {
        // Arrange: the startup segment is 1500 kbps x 4 s = 6000 kilobits
        PlaybackSession session = Playback.Recipe(10_000, sampleCount: 4).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        session.CompleteDownload(2);

        // Assert: 3000 kbps, and the estimate follows the sample alone
        Assert.Equal(3_000, session.Bandwidth.ThroughputKbps, precision: 9);
        Assert.Equal(5, session.Bandwidth.SampleCount);
    }

    [Fact]
    public void Nothing_is_delivered_when_nothing_was_requested() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000).Generate();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => session.CompleteDownload(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(600.0001)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_download_time_outside_zero_exclusive_to_ten_minutes_is_refused_and_the_download_stays_pending(double elapsedSeconds) {
        // Arrange
        PlaybackSession    session  = Playback.Recipe(10_000, levelSeconds: 30, capacitySeconds: 60).Generate();
        AdaptationDecision decision = session.DecideNext(Playback.NoLatency);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => session.CompleteDownload(elapsedSeconds));

        // Assert
        Assert.Equal("elapsedSeconds", refusal.ParamName);
        Assert.Same(decision, session.PendingDecision);
        Assert.Equal(30, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void A_download_of_ten_minutes_is_accepted_and_stalls_all_the_way() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 0, capacitySeconds: 60).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act
        double stall = session.CompleteDownload(600);

        // Assert
        Assert.Equal(600, stall);
        Assert.Equal(600, session.TotalStallSeconds);
    }

    [Fact]
    public void An_idle_session_just_plays() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 30, capacitySeconds: 60).Generate();

        // Act
        double stall = session.Idle(10);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(20, session.Buffer.LevelSeconds);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void An_idle_session_that_runs_dry_stalls() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 3, capacitySeconds: 60).Generate();

        // Act
        double stall = session.Idle(8);

        // Assert
        Assert.Equal(5, stall);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(5, session.TotalStallSeconds);
        Assert.True(session.Buffer.IsEmpty);
    }

    [Fact]
    public void An_idle_session_that_plays_nothing_does_not_stall_even_on_an_empty_buffer() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 0, capacitySeconds: 60).Generate();

        // Act
        double stall = session.Idle(0);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(0, session.StallCount);
    }

    [Fact]
    public void A_session_cannot_idle_while_a_download_is_pending() {
        // Arrange
        PlaybackSession session = Playback.Recipe(10_000, levelSeconds: 30, capacitySeconds: 60).Generate();
        session.DecideNext(Playback.NoLatency);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => session.Idle(1));
        Assert.Equal(30, session.Buffer.LevelSeconds);
    }

    [Fact]
    public void A_session_cannot_idle_for_a_negative_time() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anySession.Idle(-1));

        // Assert
        Assert.Equal("seconds", refusal.ParamName);
    }

    [Fact]
    public void On_a_steady_network_that_carries_the_top_rung_playback_climbs_to_it_without_a_stall() {
        // Arrange: the estimate is right (8000 kbps, a budget of 6400 kbps for the 6000 kbps rung), the buffer starts at ten seconds
        PlaybackSession session = Playback.Recipe(8_000, sampleCount: 1, safetyFactor: 0.8, levelSeconds: 10, capacitySeconds: 60).Generate();

        // Act
        for (int segment = 0; segment < 25; segment++) {
            if (session.Buffer.FreeSeconds < session.NextSegment.DurationSeconds) {
                session.Idle(session.NextSegment.DurationSeconds);
            }

            AdaptationDecision decision = session.DecideNext(Playback.NoLatency);
            session.CompleteDownload(decision.Segment.SizeKilobits(decision.Chosen) / 8_000);
        }

        // Assert
        Assert.Equal(6_000, session.CurrentVariant.Bitrate.Kbps);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(AdaptationReason.Startup, session.History[0].Reason);
        Assert.All(session.History, decision => Assert.NotEqual(AdaptationReason.SwitchDown, decision.Reason));
        Assert.All(session.History, decision => Assert.NotEqual(AdaptationReason.Panic, decision.Reason));
        Assert.Equal(session.History.Select(d => d.Chosen.Bitrate.Kbps).OrderBy(kbps => kbps), session.History.Select(d => d.Chosen.Bitrate.Kbps));
    }

}
