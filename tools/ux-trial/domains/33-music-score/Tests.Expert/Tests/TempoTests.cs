using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class TempoTests {

    [Theory]
    [InlineData(20.0)]
    [InlineData(400.0)]
    [InlineData(72.5)]
    public void A_tempo_between_twenty_and_four_hundred_beats_per_minute_is_accepted(double beatsPerMinute) {
        // Act
        Tempo tempo = Arbitrary.Tempo().WithBeatsPerMinute(beatsPerMinute).Generate();

        // Assert
        Assert.Equal(beatsPerMinute, tempo.BeatsPerMinute);
    }

    [Theory]
    [InlineData(19.999)]
    [InlineData(400.001)]
    [InlineData(0.0)]
    [InlineData(-60.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_tempo_outside_twenty_to_four_hundred_beats_per_minute_is_refused(double beatsPerMinute) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Tempo().WithBeatsPerMinute(beatsPerMinute).Generate());

        // Assert
        Assert.Equal("beatsPerMinute", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 4)]
    [InlineData(1, 1024)]
    public void A_beat_unit_up_to_a_whole_note_is_accepted(long numerator, long denominator) {
        // Act
        Tempo tempo = Arbitrary.Tempo().WithBeatUnit(D(numerator, denominator)).Generate();

        // Assert
        Assert.Equal(D(numerator, denominator), tempo.BeatUnit);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 4)]
    [InlineData(2, 1)]
    public void A_beat_unit_that_is_zero_or_longer_than_a_whole_note_is_refused(long numerator, long denominator) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Tempo().WithBeatUnit(D(numerator, denominator)).Generate());

        // Assert
        Assert.Equal("beatUnit", refusal.ParamName);
    }

    [Fact]
    public void A_missing_beat_unit_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Tempo().WithBeatUnit((Duration)null!).Generate());
    }

    [Theory]
    [InlineData(60.0, 1, 4, 1, 4, 1.0)]
    [InlineData(60.0, 1, 4, 1, 1, 4.0)]
    [InlineData(120.0, 1, 4, 1, 2, 1.0)]
    [InlineData(90.0, 1, 2, 1, 1, 1.3333333333333333)]
    [InlineData(60.0, 3, 8, 1, 8, 1.0 / 3.0)]
    [InlineData(60.0, 1, 4, 0, 1, 0.0)]
    public void A_duration_lasts_as_many_beats_as_it_holds_beat_units_at_the_tempo(double beatsPerMinute, long unitNumerator, long unitDenominator, long numerator, long denominator, double expectedSeconds) {
        // Arrange
        Tempo tempo = new(beatsPerMinute, D(unitNumerator, unitDenominator));

        // Act
        double seconds = tempo.SecondsFor(D(numerator, denominator));

        // Assert
        Assert.Equal(expectedSeconds, seconds, 12);
    }

    [Fact]
    public void A_missing_duration_cannot_be_converted() {
        // Arrange
        Tempo anyTempo = Arbitrary.Tempo().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyTempo.SecondsFor(null!));
    }

}
