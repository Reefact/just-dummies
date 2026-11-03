using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class PlaybackSessionConstructionTests {

    [Fact]
    public void A_new_session_starts_on_the_lowest_variant_at_the_first_segment_with_nothing_going_on() {
        // Arrange
        BitrateLadder     ladder    = Any.BitrateLadder().Generate();
        PlaybackBuffer    buffer    = Any.PlaybackBuffer().WithCapacitySeconds(60).Generate();
        AdaptationPolicy  policy    = Any.AdaptationPolicy().WithComfortBufferSeconds(c => c.LessThanOrEqualTo(60)).Generate();
        BandwidthEstimate bandwidth = Any.BandwidthEstimate().Generate();

        // Act
        PlaybackSession session = new PlaybackSession(ladder, buffer, policy, bandwidth, segmentDurationSeconds: 4);

        // Assert
        Assert.Same(ladder.Lowest, session.CurrentVariant);
        Assert.Equal(0, session.NextSegment.Index);
        Assert.Equal(4, session.NextSegment.DurationSeconds);
        Assert.Null(session.PendingDecision);
        Assert.Equal(0, session.StallCount);
        Assert.Equal(0, session.TotalStallSeconds);
        Assert.Empty(session.History);
    }

    [Fact]
    public void A_session_works_on_the_parts_it_was_given() {
        // Arrange
        BitrateLadder     ladder    = Any.BitrateLadder().Generate();
        PlaybackBuffer    buffer    = new PlaybackBuffer(levelSeconds: 30, capacitySeconds: 60);
        AdaptationPolicy  policy    = Any.AdaptationPolicy().WithComfortBufferSeconds(c => c.LessThanOrEqualTo(60)).Generate();
        BandwidthEstimate bandwidth = Any.BandwidthEstimate().Generate();

        // Act
        PlaybackSession session = new PlaybackSession(ladder, buffer, policy, bandwidth, segmentDurationSeconds: 4);
        session.Idle(10);

        // Assert
        Assert.Same(ladder, session.Ladder);
        Assert.Same(policy, session.Policy);
        Assert.Same(bandwidth, session.Bandwidth);
        Assert.Same(buffer, session.Buffer);
        Assert.Equal(20, buffer.LevelSeconds);
    }

    [Fact]
    public void A_comfort_threshold_above_the_buffer_capacity_is_refused() {
        // Arrange
        PlaybackBuffer   buffer = new PlaybackBuffer(levelSeconds: 10, capacitySeconds: 40);
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 5, comfortBufferSeconds: 40.5);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SessionOf(buffer, policy, segmentDurationSeconds: 4));

        // Assert
        Assert.Equal("policy", refusal.ParamName);
    }

    [Fact]
    public void A_comfort_threshold_equal_to_the_buffer_capacity_is_accepted() {
        // Arrange
        PlaybackBuffer   buffer = new PlaybackBuffer(levelSeconds: 10, capacitySeconds: 40);
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 5, comfortBufferSeconds: 40);

        // Act
        PlaybackSession session = SessionOf(buffer, policy, segmentDurationSeconds: 4);

        // Assert
        Assert.Same(policy, session.Policy);
    }

    [Fact]
    public void A_segment_longer_than_the_buffer_capacity_is_refused() {
        // Arrange
        PlaybackBuffer   buffer = new PlaybackBuffer(levelSeconds: 0, capacitySeconds: 4);
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 1, comfortBufferSeconds: 3);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SessionOf(buffer, policy, segmentDurationSeconds: 4.5));

        // Assert
        Assert.Equal("segmentDurationSeconds", refusal.ParamName);
    }

    [Fact]
    public void A_segment_as_long_as_the_buffer_capacity_is_accepted() {
        // Arrange
        PlaybackBuffer   buffer = new PlaybackBuffer(levelSeconds: 0, capacitySeconds: 4);
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 1, comfortBufferSeconds: 3);

        // Act
        PlaybackSession session = SessionOf(buffer, policy, segmentDurationSeconds: 4);

        // Assert
        Assert.Equal(4, session.NextSegment.DurationSeconds);
    }

    [Theory]
    [InlineData(0.4999)]
    [InlineData(10.0001)]
    [InlineData(0)]
    public void A_segment_duration_outside_what_a_segment_can_last_is_refused(double segmentDurationSeconds) {
        // Arrange
        PlaybackBuffer   buffer = new PlaybackBuffer(levelSeconds: 0, capacitySeconds: 300);
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 5, comfortBufferSeconds: 20);

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => SessionOf(buffer, policy, segmentDurationSeconds));
    }

    [Fact]
    public void A_session_needs_all_of_its_parts() {
        // Arrange
        PlaybackSession   anySession = Any.PlaybackSession().Generate();
        BitrateLadder     ladder     = anySession.Ladder;
        PlaybackBuffer    buffer     = anySession.Buffer;
        AdaptationPolicy  policy     = anySession.Policy;
        BandwidthEstimate bandwidth  = anySession.Bandwidth;

        // Act
        ArgumentNullException withoutLadder    = Assert.Throws<ArgumentNullException>(() => new PlaybackSession(null!, buffer, policy, bandwidth, 4));
        ArgumentNullException withoutBuffer    = Assert.Throws<ArgumentNullException>(() => new PlaybackSession(ladder, null!, policy, bandwidth, 4));
        ArgumentNullException withoutPolicy    = Assert.Throws<ArgumentNullException>(() => new PlaybackSession(ladder, buffer, null!, bandwidth, 4));
        ArgumentNullException withoutBandwidth = Assert.Throws<ArgumentNullException>(() => new PlaybackSession(ladder, buffer, policy, null!, 4));

        // Assert
        Assert.Equal("ladder", withoutLadder.ParamName);
        Assert.Equal("buffer", withoutBuffer.ParamName);
        Assert.Equal("policy", withoutPolicy.ParamName);
        Assert.Equal("bandwidth", withoutBandwidth.ParamName);
    }

    private static PlaybackSession SessionOf(PlaybackBuffer buffer, AdaptationPolicy policy, double segmentDurationSeconds) {
        BitrateLadder     anyLadder    = Any.BitrateLadder().Generate();
        BandwidthEstimate anyBandwidth = Any.BandwidthEstimate().Generate();

        return new PlaybackSession(anyLadder, buffer, policy, anyBandwidth, segmentDurationSeconds);
    }

}
