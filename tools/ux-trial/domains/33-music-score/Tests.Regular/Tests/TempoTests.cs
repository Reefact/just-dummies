using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class TempoTests {

    [Theory]
    [InlineData(20.0)]
    [InlineData(400.0)]
    [InlineData(120.5)]
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
    public void A_tempo_outside_twenty_to_four_hundred_or_not_a_number_is_refused(double beatsPerMinute) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Tempo().WithBeatsPerMinute(beatsPerMinute).Generate());

        // Assert
        Assert.Equal("beatsPerMinute", error.ParamName);
    }

    [Fact]
    public void A_beat_unit_of_a_whole_note_is_the_largest_accepted() {
        // Act
        Tempo tempo = Arbitrary.Tempo().WithBeatUnit(D(1, 1)).Generate();

        // Assert
        Assert.Equal(D(1, 1), tempo.BeatUnit);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(1025, 1024)]
    [InlineData(5, 4)]
    public void A_beat_unit_longer_than_a_whole_note_is_refused(long numerator, long denominator) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Tempo().WithBeatUnit(D(numerator, denominator)).Generate());

        // Assert
        Assert.Equal("beatUnit", error.ParamName);
    }

    [Fact]
    public void A_zero_beat_unit_is_refused() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Tempo().WithBeatUnit(Duration.Zero).Generate());
    }

    [Fact]
    public void A_null_beat_unit_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Tempo().WithBeatUnit((Duration)null!).Generate());
    }

    [Theory]
    [InlineData(60.0, 1, 4, 1, 4, 1.0)]       // quarter = 60: a quarter note lasts a second
    [InlineData(60.0, 1, 4, 1, 1, 4.0)]       // ... and a whole note four
    [InlineData(120.0, 1, 4, 1, 1, 2.0)]
    [InlineData(60.0, 3, 8, 1, 4, 2.0 / 3.0)] // dotted quarter = 60: a quarter lasts two thirds of a second
    [InlineData(90.0, 1, 2, 3, 4, 1.0)]       // half = 90: a dotted half (3/4) lasts 1.5 beats of 2/3 s
    [InlineData(20.0, 1, 1, 1, 1, 3.0)]
    [InlineData(400.0, 1, 4, 1, 4, 0.15)]
    public void A_duration_converts_into_seconds_at_the_tempo(double beatsPerMinute, long unitNumerator, long unitDenominator, long numerator, long denominator, double expectedSeconds) {
        // Arrange
        Tempo tempo = new Tempo(beatsPerMinute, D(unitNumerator, unitDenominator));

        // Act
        double seconds = tempo.SecondsFor(D(numerator, denominator));

        // Assert
        Assert.Equal(expectedSeconds, seconds, precision: 9);
    }

    [Fact]
    public void Nothing_lasts_no_time_at_any_tempo() {
        // Arrange
        Tempo anyTempo = Arbitrary.Tempo().Generate();

        // Act
        double seconds = anyTempo.SecondsFor(Duration.Zero);

        // Assert
        Assert.Equal(0.0, seconds);
    }

    [Fact]
    public void Converting_a_null_duration_is_refused() {
        // Arrange
        Tempo anyTempo = Arbitrary.Tempo().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyTempo.SecondsFor(null!));
    }

}
