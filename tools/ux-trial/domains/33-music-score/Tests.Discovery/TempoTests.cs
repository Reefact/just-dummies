using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class TempoTests {

    [Theory]
    [InlineData(60, 1, 4, 1, 4, 1.0)]    // quarter = 60: a quarter note lasts a second
    [InlineData(60, 1, 4, 1, 1, 4.0)]    // ... and a whole note four
    [InlineData(120, 1, 4, 1, 8, 0.25)]  // quarter = 120: an eighth lasts a quarter second
    [InlineData(60, 3, 8, 3, 8, 1.0)]    // dotted quarter = 60
    [InlineData(90, 1, 2, 3, 4, 1.0)]    // half = 90: three quarters is one and a half beats of 2/3 s each
    public void A_duration_is_converted_into_seconds_at_the_tempo(double beatsPerMinute, long unitNumerator, long unitDenominator, long numerator, long denominator, double expectedSeconds) {
        // Arrange
        Tempo tempo = new Tempo(beatsPerMinute, new Duration(unitNumerator, unitDenominator));

        // Act
        double seconds = tempo.SecondsFor(new Duration(numerator, denominator));

        // Assert
        Assert.Equal(expectedSeconds, seconds, 10);
    }

    [Fact]
    public void A_zero_duration_takes_no_time() {
        // Arrange
        Tempo anyTempo = Any.Tempo().Generate();

        // Assert
        Assert.Equal(0.0, anyTempo.SecondsFor(Duration.Zero));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(400)]
    public void The_tempo_limits_themselves_are_accepted(double beatsPerMinute) {
        // Arrange
        Duration anyBeatUnit = Any.Duration().WithNumerator(1).Generate();

        // Act
        Tempo tempo = new Tempo(beatsPerMinute, anyBeatUnit);

        // Assert
        Assert.Equal(beatsPerMinute, tempo.BeatsPerMinute);
    }

    [Theory]
    [InlineData(19.99)]
    [InlineData(400.01)]
    [InlineData(0)]
    [InlineData(-60)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_tempo_outside_twenty_to_four_hundred_is_refused(double beatsPerMinute) {
        // Arrange
        Duration anyBeatUnit = Any.Duration().WithNumerator(1).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tempo(beatsPerMinute, anyBeatUnit));

        // Assert
        Assert.Equal("beatsPerMinute", error.ParamName);
    }

    [Fact]
    public void A_beat_unit_of_zero_is_refused() {
        // Arrange
        double anyBeatsPerMinute = Any.Double().Between(20, 400).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tempo(anyBeatsPerMinute, Duration.Zero));

        // Assert
        Assert.Equal("beatUnit", error.ParamName);
    }

    [Fact]
    public void A_beat_unit_longer_than_a_whole_note_is_refused() {
        // Arrange
        double anyBeatsPerMinute = Any.Double().Between(20, 400).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tempo(anyBeatsPerMinute, new Duration(5, 4)));
        Assert.Equal(new Duration(1, 1), new Tempo(anyBeatsPerMinute, new Duration(1, 1)).BeatUnit);
    }

    [Fact]
    public void A_missing_beat_unit_is_refused() {
        // Arrange
        double anyBeatsPerMinute = Any.Double().Between(20, 400).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Tempo(anyBeatsPerMinute, null!));
    }

}
