using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class PlaybackSessionConstructionTests {

    [Fact]
    public void A_new_session_starts_on_the_lowest_variant_at_the_first_segment_with_a_clean_record() {
        // Arrange
        BitrateLadder    ladder = Fixtures.FourRungLadder();
        PlaybackBuffer   buffer = new AnyPlaybackBuffer().Generate();
        AdaptationPolicy policy = new AnyAdaptationPolicy().WithComfortBufferSeconds(c => c.LessThanOrEqualTo(buffer.CapacitySeconds))
                                                           .WithPanicBufferSeconds(p => p.LessThan(buffer.CapacitySeconds))
                                                           .Generate();
        BandwidthEstimate bandwidth = new AnyBandwidthEstimate().Generate();

        // Act
        PlaybackSession session = new PlaybackSession(ladder, buffer, policy, bandwidth, segmentDurationSeconds: 4);

        // Assert
        Assert.Same(ladder, session.Ladder);
        Assert.Same(buffer, session.Buffer);
        Assert.Same(policy, session.Policy);
        Assert.Same(bandwidth, session.Bandwidth);
        Assert.Equal("240p30", session.CurrentVariant.Id);
        Assert.Equal(0, session.NextSegment.Index);
        Assert.Equal(4, session.NextSegment.DurationSeconds);
        Assert.Null(session.PendingDecision);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(0, session.TotalStallSeconds);
        Assert.Empty(session.History);
    }

    [Fact]
    public void A_comfort_threshold_equal_to_the_buffer_capacity_is_accepted() {
        // Arrange
        PlaybackBuffer   buffer = new AnyPlaybackBuffer().WithCapacitySeconds(30).Generate();
        AdaptationPolicy policy = new AnyAdaptationPolicy().WithPanicBufferSeconds(5).WithComfortBufferSeconds(30).Generate();

        // Act
        PlaybackSession session = new AnyPlaybackSession().WithBuffer(buffer).WithPolicy(policy).Generate();

        // Assert
        Assert.Same(policy, session.Policy);
    }

    [Fact]
    public void A_comfort_threshold_above_the_buffer_capacity_is_refused() {
        // Arrange
        AnyPlaybackSession anySession = new AnyPlaybackSession().WithBuffer(new AnyPlaybackBuffer().WithCapacitySeconds(30).Generate())
                                                                .WithPolicy(new AnyAdaptationPolicy().WithPanicBufferSeconds(5)
                                                                                                     .WithComfortBufferSeconds(Math.BitIncrement(30.0))
                                                                                                     .Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anySession.Generate());

        // Assert
        Assert.Equal("policy", refusal.ParamName);
    }

    [Fact]
    public void A_segment_as_long_as_the_buffer_capacity_is_accepted() {
        // Arrange: the smallest capacity there is, and a policy that fits in it.
        AnyPlaybackSession anySession = new AnyPlaybackSession().WithBuffer(new AnyPlaybackBuffer().WithCapacitySeconds(4).Generate())
                                                                .WithPolicy(new AnyAdaptationPolicy().WithPanicBufferSeconds(1).WithComfortBufferSeconds(3).Generate())
                                                                .WithSegmentDurationSeconds(4);

        // Act
        PlaybackSession session = anySession.Generate();

        // Assert
        Assert.Equal(4, session.NextSegment.DurationSeconds);
    }

    [Fact]
    public void A_segment_longer_than_the_buffer_capacity_is_refused() {
        // Arrange
        AnyPlaybackSession anySession = new AnyPlaybackSession().WithBuffer(new AnyPlaybackBuffer().WithCapacitySeconds(4).Generate())
                                                                .WithPolicy(new AnyAdaptationPolicy().WithPanicBufferSeconds(1).WithComfortBufferSeconds(3).Generate())
                                                                .WithSegmentDurationSeconds(4.5);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anySession.Generate());

        // Assert
        Assert.Equal("segmentDurationSeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(10.01)]
    public void A_segment_duration_outside_the_segment_range_is_refused(double duration) {
        // Arrange
        AnyPlaybackSession anySession = new AnyPlaybackSession().WithSegmentDurationSeconds(duration);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("durationSeconds", () => anySession.Generate());
    }

    [Fact]
    public void A_session_needs_a_ladder_a_buffer_a_policy_and_an_estimate() {
        // Arrange
        BitrateLadder     ladder    = new AnyBitrateLadder().Generate();
        PlaybackBuffer    buffer    = new AnyPlaybackBuffer().WithCapacitySeconds(60).Generate();
        AdaptationPolicy  policy    = new AnyAdaptationPolicy().WithPanicBufferSeconds(5).WithComfortBufferSeconds(15).Generate();
        BandwidthEstimate bandwidth = new AnyBandwidthEstimate().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("ladder", () => new PlaybackSession(null!, buffer, policy, bandwidth, 4));
        Assert.Throws<ArgumentNullException>("buffer", () => new PlaybackSession(ladder, null!, policy, bandwidth, 4));
        Assert.Throws<ArgumentNullException>("policy", () => new PlaybackSession(ladder, buffer, null!, bandwidth, 4));
        Assert.Throws<ArgumentNullException>("bandwidth", () => new PlaybackSession(ladder, buffer, policy, null!, 4));
    }

    [Fact]
    public void A_drawn_session_is_one_the_constructor_accepts() {
        // Arrange
        AnyPlaybackSession anySession = new AnyPlaybackSession();

        // Act / Assert: the generator is part of the test code, so it is checked over many draws.
        for (int i = 0; i < 300; i++) {
            Exception? failure = Record.Exception(() => anySession.Generate());

            Assert.Null(failure);
        }
    }

}
