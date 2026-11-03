using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class TimeSignatureTests {

    [Theory]
    [InlineData(4, 4, 1, 1)]
    [InlineData(3, 4, 3, 4)]
    [InlineData(6, 8, 3, 4)]
    [InlineData(5, 8, 5, 8)]
    [InlineData(32, 64, 1, 2)]
    public void A_measure_in_this_meter_lasts_beats_over_beat_value(int beats, int beatValue, long expectedNumerator, long expectedDenominator) {
        // Act
        TimeSignature timeSignature = new TimeSignature(beats, beatValue);

        // Assert
        Assert.Equal(new Duration(expectedNumerator, expectedDenominator), timeSignature.MeasureLength);
        Assert.Equal($"{beats}/{beatValue}", timeSignature.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    public void A_measure_holds_between_one_and_thirty_two_beats(int beats) {
        // Arrange
        int anyBeatValue = Any.Int32().OneOf(1, 2, 4, 8, 16, 32, 64).Generate();

        // Act
        TimeSignature timeSignature = new TimeSignature(beats, anyBeatValue);

        // Assert
        Assert.Equal(beats, timeSignature.Beats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(33)]
    public void A_beat_count_outside_one_to_thirty_two_is_refused(int beats) {
        // Arrange
        int anyBeatValue = Any.Int32().OneOf(1, 2, 4, 8, 16, 32, 64).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignature(beats, anyBeatValue));

        // Assert
        Assert.Equal("beats", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void A_power_of_two_up_to_sixty_four_is_a_beat_value(int beatValue) {
        // Arrange
        int anyBeats = Any.Int32().Between(1, 32).Generate();

        // Act
        TimeSignature timeSignature = new TimeSignature(anyBeats, beatValue);

        // Assert
        Assert.Equal(beatValue, timeSignature.BeatValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(63)]
    [InlineData(128)]
    public void A_beat_value_that_is_not_a_power_of_two_up_to_sixty_four_is_refused(int beatValue) {
        // Arrange
        int anyBeats = Any.Int32().Between(1, 32).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignature(anyBeats, beatValue));

        // Assert
        Assert.Equal("beatValue", error.ParamName);
    }

    [Fact]
    public void Equal_meters_are_equal_but_six_eight_is_not_three_four() {
        Assert.Equal(new TimeSignature(6, 8), new TimeSignature(6, 8));
        Assert.Equal(new TimeSignature(6, 8).GetHashCode(), new TimeSignature(6, 8).GetHashCode());
        Assert.NotEqual(new TimeSignature(6, 8), new TimeSignature(3, 4));
        Assert.Equal(new TimeSignature(6, 8).MeasureLength, new TimeSignature(3, 4).MeasureLength);
    }

}
