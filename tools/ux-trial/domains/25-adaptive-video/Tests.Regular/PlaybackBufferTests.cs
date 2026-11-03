using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class PlaybackBufferTests {

    [Fact]
    public void A_buffer_exposes_what_it_was_built_with() {
        // Act
        PlaybackBuffer buffer = new PlaybackBuffer(levelSeconds: 12, capacitySeconds: 60);

        // Assert
        Assert.Equal(12, buffer.LevelSeconds);
        Assert.Equal(60, buffer.CapacitySeconds);
        Assert.Equal(48, buffer.FreeSeconds);
        Assert.False(buffer.IsEmpty);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(300)]
    public void A_buffer_accepts_both_ends_of_the_capacity_range(double capacity) {
        Assert.Equal(capacity, new PlaybackBuffer(0, capacity).CapacitySeconds);
    }

    [Theory]
    [InlineData(3.99)]
    [InlineData(300.01)]
    [InlineData(double.NaN)]
    public void A_capacity_outside_the_range_is_refused(double capacity) {
        Assert.Throws<ArgumentOutOfRangeException>("capacitySeconds", () => new PlaybackBuffer(0, capacity));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    public void A_negative_or_undefined_level_is_refused(double level) {
        Assert.Throws<ArgumentOutOfRangeException>("levelSeconds", () => new PlaybackBuffer(level, 60));
    }

    [Fact]
    public void A_buffer_may_start_exactly_full_and_then_has_no_room() {
        // Act
        PlaybackBuffer buffer = new PlaybackBuffer(60, 60);

        // Assert
        Assert.Equal(0, buffer.FreeSeconds);
    }

    [Fact]
    public void A_level_above_the_capacity_is_refused() {
        // Arrange
        double capacity = 60;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PlaybackBuffer(Math.BitIncrement(capacity), capacity));

        // Assert
        Assert.Equal("levelSeconds", refusal.ParamName);
    }

    [Fact]
    public void An_empty_buffer_says_so() {
        Assert.True(new PlaybackBuffer(0, 60).IsEmpty);
    }

    [Fact]
    public void Playing_less_than_the_level_leaves_the_rest_and_no_stall() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(4);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(6, buffer.LevelSeconds);
    }

    [Fact]
    public void Playing_exactly_the_level_empties_the_buffer_without_a_stall() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(10);

        // Assert
        Assert.Equal(0, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_longer_than_the_level_stalls_for_the_difference() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(15);

        // Assert
        Assert.Equal(5, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void An_empty_buffer_stalls_for_the_whole_time_played() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(0).Generate();

        // Act
        double stall = buffer.Drain(2.5);

        // Assert
        Assert.Equal(2.5, stall);
    }

    [Fact]
    public void Playing_for_no_time_changes_nothing() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(0);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(10, buffer.LevelSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    public void Playing_for_a_negative_or_undefined_time_is_refused(double seconds) {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => buffer.Drain(seconds));
    }

    [Fact]
    public void Appending_a_segment_that_fits_raises_the_level_by_its_duration() {
        // Arrange
        PlaybackBuffer buffer  = new AnyPlaybackBuffer().WithCapacitySeconds(60).WithLevelSeconds(10).Generate();
        Segment        segment = new AnySegment().WithDurationSeconds(4).Generate();

        // Act
        buffer.Append(segment);

        // Assert
        Assert.Equal(14, buffer.LevelSeconds);
    }

    [Fact]
    public void Appending_a_segment_that_exactly_fills_the_buffer_is_accepted() {
        // Arrange
        PlaybackBuffer buffer  = new AnyPlaybackBuffer().WithCapacitySeconds(20).WithLevelSeconds(16).Generate();
        Segment        segment = new AnySegment().WithDurationSeconds(4).Generate();

        // Act
        buffer.Append(segment);

        // Assert
        Assert.Equal(0, buffer.FreeSeconds);
    }

    [Fact]
    public void Appending_a_segment_that_does_not_fit_is_refused_and_leaves_the_level_alone() {
        // Arrange
        PlaybackBuffer buffer  = new AnyPlaybackBuffer().WithCapacitySeconds(20).WithLevelSeconds(17).Generate();
        Segment        segment = new AnySegment().WithDurationSeconds(4).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => buffer.Append(segment));

        // Assert
        Assert.Equal(17, buffer.LevelSeconds);
    }

    [Fact]
    public void Appending_nothing_is_refused() {
        // Arrange
        PlaybackBuffer buffer = new AnyPlaybackBuffer().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("segment", () => buffer.Append(null!));
    }

}
