using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class TimeSignatureTests {

    [Theory]
    [InlineData(4, 4, 1, 1)]
    [InlineData(3, 4, 3, 4)]
    [InlineData(6, 8, 3, 4)]
    [InlineData(5, 8, 5, 8)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(32, 64, 1, 2)]
    public void A_measure_lasts_as_many_beats_as_the_meter_counts(int beats, int beatValue, long expectedNumerator, long expectedDenominator) {
        // Act
        TimeSignature meter = new(beats, beatValue);

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), meter.MeasureLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    [InlineData(-4)]
    public void Beats_outside_one_to_thirty_two_are_refused(int beats) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.TimeSignature().WithBeats(beats).Generate());

        // Assert
        Assert.Equal("beats", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(65)]
    [InlineData(128)]
    [InlineData(-4)]
    public void A_beat_value_that_is_not_a_power_of_two_up_to_sixty_four_is_refused(int beatValue) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.TimeSignature().WithBeatValue(beatValue).Generate());

        // Assert
        Assert.Equal("beatValue", refusal.ParamName);
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
    public void Meters_with_the_same_beats_over_the_same_beat_value_are_equal_and_hash_alike() {
        Assert.Equal(new TimeSignature(3, 4), new TimeSignature(3, 4));
        Assert.Equal(new TimeSignature(3, 4).GetHashCode(), new TimeSignature(3, 4).GetHashCode());
        Assert.NotEqual(new TimeSignature(6, 8), new TimeSignature(3, 4));
    }

    [Fact]
    public void A_meter_reads_as_its_fraction() {
        Assert.Equal("6/8", new TimeSignature(6, 8).ToString());
    }

}
