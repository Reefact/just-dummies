using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

/// <summary>The download cycle of a session: what it refuses, and what completing a download changes.</summary>
public sealed class PlaybackSessionTests {

    [Fact]
    public void A_comfort_threshold_above_the_buffer_capacity_is_refused() {
        // Arrange
        AnyPlaybackSession anySession = Any.PlaybackSession()
                                           .WithPolicy(p => p.WithComfortBufferSeconds(40))
                                           .WithBuffer(b => b.WithCapacitySeconds(30));

        // Act
        ArgumentException? refusal = Record.Exception(() => anySession.Generate()) as ArgumentException;

        // Assert
        Assert.NotNull(refusal);
        Assert.Equal("policy", refusal.ParamName);
    }

    [Fact]
    public void Deciding_again_while_a_download_is_pending_is_refused() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().Generate();
        anySession.DecideNext(Any.Latency().Generate());

        // Act
        Exception? refusal = Record.Exception(() => anySession.DecideNext(Any.Latency().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Deciding_with_no_room_left_for_a_segment_is_refused() {
        // Arrange: 58 s buffered out of 60, and segments of 4 s.
        PlaybackSession session = Any.PlaybackSession()
                                     .WithBuffer(b => b.WithLevelSeconds(58).WithCapacitySeconds(60))
                                     .WithSegmentDurationSeconds(4)
                                     .Generate();

        // Act
        Exception? refusal = Record.Exception(() => session.DecideNext(Any.Latency().Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Completing_a_download_with_nothing_pending_is_refused() {
        // Arrange
        PlaybackSession anySession = Any.PlaybackSession().Generate();
        double          anyElapsed = Any.Double().GreaterThan(0).LessThanOrEqualTo(600).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.CompleteDownload(anyElapsed));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Completing_a_download_adopts_the_chosen_rung_and_moves_on_to_the_next_segment() {
        // Arrange
        PlaybackSession    session  = Player.NewSession.WithBandwidth(b => b.WithThroughputKbps(100_000)).Generate();
        AdaptationDecision decision = session.DecideNext(Any.Latency().Generate());

        // Act
        session.CompleteDownload(decision.PredictedDownloadSeconds);

        // Assert
        Assert.Equal(1_000, session.CurrentVariant.Bitrate.Kbps);
        Assert.Equal(1, session.NextSegment.Index);
        Assert.Null(session.PendingDecision);
        Assert.Single(session.History);
    }

    [Fact]
    public void Completing_a_download_appends_the_segment_to_what_is_left_of_the_buffer() {
        // Arrange: 12 s buffered, 4 s segments.
        PlaybackSession session = Any.PlaybackSession()
                                     .WithBuffer(b => b.WithLevelSeconds(12))
                                     .WithSegmentDurationSeconds(4)
                                     .Generate();
        session.DecideNext(Any.Latency().Generate());

        // Act: the download takes 3 s of playback.
        double stall = session.CompleteDownload(3);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(13, session.Buffer.LevelSeconds, precision: 9);
    }

    [Fact]
    public void A_download_outlasting_the_buffer_counts_one_stall_of_the_difference() {
        // Arrange: 3 s buffered.
        PlaybackSession session = Any.PlaybackSession().WithBuffer(b => b.WithLevelSeconds(3)).Generate();
        session.DecideNext(Any.Latency().Generate());

        // Act: the download takes 8 s.
        double stall = session.CompleteDownload(8);

        // Assert
        Assert.Equal(5, stall, precision: 9);
        Assert.Equal(1, session.StallCount);
        Assert.Equal(5, session.TotalStallSeconds, precision: 9);
    }

    [Fact]
    public void Idling_past_the_end_of_the_buffer_stalls_playback() {
        // Arrange
        PlaybackSession session = Any.PlaybackSession().WithBuffer(b => b.WithLevelSeconds(6)).Generate();

        // Act
        double stall = session.Idle(10);

        // Assert
        Assert.Equal(4, stall, precision: 9);
        Assert.Equal(1, session.StallCount);
        Assert.True(session.Buffer.IsEmpty);
    }

    [Fact]
    public void Each_completed_download_teaches_the_estimate_one_more_sample() {
        // Arrange
        PlaybackSession session     = Player.NewSession.WithBandwidth(b => b.WithSampleCount(4)).Generate();
        double          anyObserved = Any.Double().Between(100, 100_000).Generate();

        // Act
        Player.Download(session, observedKbps: anyObserved);

        // Assert
        Assert.Equal(5, session.Bandwidth.SampleCount);
    }

}
