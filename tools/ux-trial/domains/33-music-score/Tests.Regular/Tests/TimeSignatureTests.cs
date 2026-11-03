using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class TimeSignatureTests {

    [Theory]
    [InlineData(4, 4, 1, 1)]
    [InlineData(3, 4, 3, 4)]
    [InlineData(6, 8, 3, 4)]
    [InlineData(2, 2, 1, 1)]
    [InlineData(5, 4, 5, 4)]
    [InlineData(7, 8, 7, 8)]
    [InlineData(32, 64, 1, 2)]
    [InlineData(32, 1, 32, 1)]
    [InlineData(1, 64, 1, 64)]
    public void A_measure_length_is_the_exact_sum_of_its_beats(int beats, int beatValue, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration length = new TimeSignature(beats, beatValue).MeasureLength;

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), length);
    }

    [Fact]
    public void Six_eight_and_three_four_fill_the_same_time_but_are_different_meters() {
        // Arrange
        TimeSignature sixEight  = new TimeSignature(6, 8);
        TimeSignature threeFour = new TimeSignature(3, 4);

        // Assert
        Assert.Equal(threeFour.MeasureLength, sixEight.MeasureLength);
        Assert.NotEqual(threeFour, sixEight);
    }

    [Fact]
    public void The_extreme_meters_are_accepted() {
        // Assert
        Assert.Equal(32, new TimeSignature(32, 64).Beats);
        Assert.Equal(1, new TimeSignature(1, 1).Beats);
    }

    [Fact]
    public void A_number_of_beats_outside_one_to_thirty_two_is_refused() {
        // Arrange
        int anyTooFew  = Any.Int32().LessThanOrEqualTo(0).Generate();
        int anyTooMany = Any.Int32().GreaterThan(32).Generate();

        // Assert
        Assert.Equal("beats", Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.TimeSignature().WithBeats(anyTooFew).Generate()).ParamName);
        Assert.Equal("beats", Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.TimeSignature().WithBeats(anyTooMany).Generate()).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(63)]
    [InlineData(65)]
    [InlineData(128)]
    public void A_beat_value_must_be_a_power_of_two_up_to_sixty_four(int beatValue) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.TimeSignature().WithBeatValue(beatValue).Generate());

        // Assert
        Assert.Equal("beatValue", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void Every_power_of_two_up_to_sixty_four_is_a_beat_value(int beatValue) {
        // Act
        TimeSignature meter = Arbitrary.TimeSignature().WithBeatValue(beatValue).Generate();

        // Assert
        Assert.Equal(beatValue, meter.BeatValue);
    }

    [Fact]
    public void A_meter_reads_as_beats_over_beat_value() {
        // Assert
        Assert.Equal("6/8", new TimeSignature(6, 8).ToString());
    }

    [Fact]
    public void Meters_with_the_same_beats_and_beat_value_are_equal() {
        // Assert
        Assert.Equal(new TimeSignature(7, 8), new TimeSignature(7, 8));
        Assert.Equal(new TimeSignature(7, 8).GetHashCode(), new TimeSignature(7, 8).GetHashCode());
        Assert.False(new TimeSignature(7, 8).Equals(null));
    }

}
