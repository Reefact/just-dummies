using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class PlaybackBufferTests {

    [Fact]
    public void A_valid_buffer_exposes_its_level_and_capacity() {
        PlaybackBuffer buffer = new PlaybackBuffer(12.5, 60);

        Assert.Equal(12.5, buffer.LevelSeconds);
        Assert.Equal(60, buffer.CapacitySeconds);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(300)]
    public void A_capacity_at_the_edge_of_the_range_is_accepted(double capacitySeconds) {
        PlaybackBuffer buffer = new PlaybackBuffer(0, capacitySeconds);

        Assert.Equal(capacitySeconds, buffer.CapacitySeconds);
    }

    [Theory]
    [InlineData(3.999)]
    [InlineData(300.001)]
    [InlineData(0)]
    [InlineData(-60)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_capacity_outside_the_range_is_refused(double capacitySeconds) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackBuffer(0, capacitySeconds));

        Assert.Equal("capacitySeconds", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_or_NaN_level_is_refused(double levelSeconds) {
        double anyCapacity = Any.Double().Between(4, 300).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackBuffer(levelSeconds, anyCapacity));

        Assert.Equal("levelSeconds", exception.ParamName);
    }

    [Fact]
    public void A_level_above_the_capacity_is_refused() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PlaybackBuffer(60.001, 60));

        Assert.Equal("levelSeconds", exception.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void A_full_buffer_is_accepted() {
        double anyCapacity = Any.Double().Between(4, 300).Generate();

        PlaybackBuffer buffer = new PlaybackBuffer(anyCapacity, anyCapacity);

        Assert.Equal(0, buffer.FreeSeconds);
    }

    [Fact]
    public void An_empty_buffer_reports_itself_empty_and_entirely_free() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(0).WithCapacitySeconds(40).Generate();

        Assert.True(buffer.IsEmpty);
        Assert.Equal(40, buffer.FreeSeconds);
    }

    [Fact]
    public void A_buffer_holding_media_is_not_empty_and_has_the_rest_free() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(15).WithCapacitySeconds(40).Generate();

        Assert.False(buffer.IsEmpty);
        Assert.Equal(25, buffer.FreeSeconds);
    }

    [Fact]
    public void Playing_less_than_the_level_lowers_it_without_stalling() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        double stall = buffer.Drain(4);

        Assert.Equal(0, stall);
        Assert.Equal(6, buffer.LevelSeconds);
    }

    [Fact]
    public void Playing_exactly_the_level_empties_the_buffer_without_stalling() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        double stall = buffer.Drain(10);

        Assert.Equal(0, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_more_than_the_level_empties_the_buffer_and_reports_the_stall() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        double stall = buffer.Drain(14.5);

        Assert.Equal(4.5, stall);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Playing_nothing_changes_nothing() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        double stall = buffer.Drain(0);

        Assert.Equal(0, stall);
        Assert.Equal(10, buffer.LevelSeconds);
    }

    [Fact]
    public void An_empty_buffer_stalls_for_the_whole_time_played() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(0).Generate();

        double stall = buffer.Drain(3);

        Assert.Equal(3, stall);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_or_NaN_play_time_is_refused(double seconds) {
        PlaybackBuffer buffer = Any.PlaybackBuffer().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Drain(seconds));

        Assert.Equal("seconds", exception.ParamName);
    }

    [Fact]
    public void A_refused_play_time_leaves_the_level_alone() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().WithLevelSeconds(10).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Drain(-1));

        Assert.Equal(10, buffer.LevelSeconds);
    }

    [Fact]
    public void Appending_a_segment_raises_the_level_by_its_duration() {
        PlaybackBuffer buffer  = Any.PlaybackBuffer().WithLevelSeconds(10).WithCapacitySeconds(60).Generate();
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        buffer.Append(segment);

        Assert.Equal(14, buffer.LevelSeconds);
    }

    [Fact]
    public void A_segment_that_exactly_fills_the_buffer_fits() {
        PlaybackBuffer buffer  = Any.PlaybackBuffer().WithLevelSeconds(56).WithCapacitySeconds(60).Generate();
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        buffer.Append(segment);

        Assert.Equal(0, buffer.FreeSeconds);
    }

    [Fact]
    public void A_segment_that_does_not_fit_is_refused_and_leaves_the_buffer_alone() {
        PlaybackBuffer buffer  = Any.PlaybackBuffer().WithLevelSeconds(57).WithCapacitySeconds(60).Generate();
        Segment        segment = Any.Segment().WithDurationSeconds(4).Generate();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => buffer.Append(segment));

        Assert.Equal(57, buffer.LevelSeconds);
        Assert.Contains("does not fit", exception.Message);
    }

    [Fact]
    public void Appending_nothing_is_refused() {
        PlaybackBuffer buffer = Any.PlaybackBuffer().Generate();

        Assert.Throws<ArgumentNullException>(() => buffer.Append(null!));
    }

}
