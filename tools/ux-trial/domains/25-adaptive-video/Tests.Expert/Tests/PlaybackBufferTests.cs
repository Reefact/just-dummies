using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class PlaybackBufferTests {

    [Fact]
    public void A_buffer_exposes_its_level_and_capacity_and_what_is_left() {
        // Act
        PlaybackBuffer buffer = new PlaybackBuffer(levelSeconds: 12.5, capacitySeconds: 60);

        // Assert
        Assert.Equal(12.5, buffer.LevelSeconds);
        Assert.Equal(60, buffer.CapacitySeconds);
        Assert.Equal(47.5, buffer.FreeSeconds);
        Assert.False(buffer.IsEmpty);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 4)]
    [InlineData(300, 300)]
    [InlineData(0, 300)]
    public void A_buffer_may_start_empty_or_full_at_either_end_of_the_capacity_range(double level, double capacity) {
        // Act
        PlaybackBuffer buffer = new PlaybackBuffer(level, capacity);

        // Assert
        Assert.Equal(level, buffer.LevelSeconds);
        Assert.Equal(capacity, buffer.CapacitySeconds);
    }

    [Theory]
    [InlineData(3.999)]
    [InlineData(0)]
    [InlineData(-60)]
    [InlineData(300.001)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_capacity_outside_4_to_300_seconds_is_refused(double capacity) {
        // Arrange
        double anyLevel = Any.Double().Between(0, 4).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackBuffer(anyLevel, capacity));

        // Assert
        Assert.Equal("capacitySeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void A_negative_level_is_refused(double level) {
        // Arrange
        double anyCapacity = Any.Double().Between(4, 300).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackBuffer(level, anyCapacity));

        // Assert
        Assert.Equal("levelSeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(60.001, 60)]
    [InlineData(301, 300)]
    [InlineData(double.PositiveInfinity, 300)]
    public void A_level_above_the_capacity_is_refused(double level, double capacity) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PlaybackBuffer(level, capacity));

        // Assert
        Assert.Equal("levelSeconds", refusal.ParamName);
    }

    [Fact]
    public void A_buffer_with_nothing_in_it_is_empty() {
        // Arrange
        PlaybackBuffer buffer = new PlaybackBuffer(levelSeconds: 0, capacitySeconds: 30);

        // Assert
        Assert.True(buffer.IsEmpty);
        Assert.Equal(30, buffer.FreeSeconds);
    }

    [Fact]
    public void Playing_less_than_the_level_lowers_it_without_a_stall() {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(4);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(6, buffer.LevelSeconds);
    }

    [Fact]
    public void Playing_exactly_the_level_empties_the_buffer_without_a_stall() {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        // Act
        double stall = buffer.Drain(10);

        // Assert
        Assert.Equal(0, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_more_than_the_level_stalls_for_the_difference_and_leaves_the_buffer_empty() {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(3).Generate();

        // Act
        double stall = buffer.Drain(8);

        // Assert
        Assert.Equal(5, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_from_an_empty_buffer_stalls_for_the_whole_time() {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(0).Generate();

        // Act
        double stall = buffer.Drain(2.5);

        // Assert
        Assert.Equal(2.5, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_for_no_time_changes_nothing() {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(7).Generate();

        // Act
        double stall = buffer.Drain(0);

        // Assert
        Assert.Equal(0, stall);
        Assert.Equal(7, buffer.LevelSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void Time_cannot_flow_backwards(double seconds) {
        // Arrange
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(7).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Drain(seconds));

        // Assert
        Assert.Equal("seconds", refusal.ParamName);
        Assert.Equal(7, buffer.LevelSeconds);
    }

    [Fact]
    public void Appending_a_segment_raises_the_level_by_its_duration() {
        // Arrange
        PlaybackBuffer buffer  = new PlaybackBuffer(levelSeconds: 10, capacitySeconds: 60);
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        // Act
        buffer.Append(segment);

        // Assert
        Assert.Equal(14, buffer.LevelSeconds);
        Assert.Equal(46, buffer.FreeSeconds);
    }

    [Fact]
    public void A_segment_that_fills_the_buffer_to_the_brim_still_fits() {
        // Arrange
        PlaybackBuffer buffer  = new PlaybackBuffer(levelSeconds: 6, capacitySeconds: 10);
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        // Act
        buffer.Append(segment);

        // Assert
        Assert.Equal(10, buffer.LevelSeconds);
        Assert.Equal(0, buffer.FreeSeconds);
    }

    [Fact]
    public void A_segment_that_does_not_fit_is_refused_and_the_level_is_left_alone() {
        // Arrange
        PlaybackBuffer buffer  = new PlaybackBuffer(levelSeconds: 7, capacitySeconds: 10);
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => buffer.Append(segment));

        // Assert
        Assert.Equal(7, buffer.LevelSeconds);
    }

    [Fact]
    public void There_is_nothing_to_append_without_a_segment() {
        // Arrange
        PlaybackBuffer anyBuffer = Any.PlaybackBuffer().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anyBuffer.Append(null!));
    }

}
