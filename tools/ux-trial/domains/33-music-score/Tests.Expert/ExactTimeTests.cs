using JustDummies;

using MusicScore.Domain;
using MusicScore.Tests.Expert.Arbitrary;

using Xunit;

using static MusicScore.Tests.Expert.Notation;

namespace MusicScore.Tests.Expert;

public sealed class ExactTimeTests {

    [Fact]
    public void Equivalent_fractions_are_one_and_the_same_duration() {
        // Arrange
        long anyFactor = Any.Int64().Between(2, 128).Generate();

        // Act
        Duration duration = new(3 * anyFactor, 8 * anyFactor);

        // Assert
        Assert.Equal(DottedQuarter, duration);
        Assert.Equal("3/8", duration.ToString());
    }

    [Fact]
    public void A_quarter_and_an_eighth_add_up_to_a_dotted_quarter() {
        // Act
        Duration sum = Quarter + Eighth;

        // Assert
        Assert.Equal(DottedQuarter, sum);
    }

    [Fact]
    public void A_duration_finer_than_a_1024th_of_a_whole_note_is_not_notatable() {
        // Act
        Exception? refused = Record.Exception(() => new Duration(1, 2048));

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refused);
        Assert.Equal("denominator", outOfRange.ParamName);
    }

    [Fact]
    public void A_rest_cannot_be_tied() {
        // Arrange
        Duration anyLength = Any.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        Exception? refused = Record.Exception(() => new VoiceEntry(anyLength, pitch: null, tiedToNext: true));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Equal("tiedToNext", invalid.ParamName);
    }

    [Fact]
    public void Enharmonic_spellings_sound_the_same_key_but_stay_different_pitches() {
        // Arrange
        Pitch bSharp3 = new('B', 1, 3);
        Pitch middleC = new('C', 0, 4);

        // Assert
        Assert.Equal(60, middleC.MidiNumber);
        Assert.Equal(middleC.MidiNumber, bSharp3.MidiNumber);
        Assert.NotEqual(middleC, bSharp3);
    }

    [Fact]
    public void A_pitch_above_the_midi_range_is_refused_even_with_each_field_in_range() {
        // Act
        Exception? refused = Record.Exception(() => new Pitch('G', 2, 9));

        // Assert
        Assert.IsType<ArgumentException>(refused);
    }

    [Fact]
    public void A_six_eight_measure_lasts_two_dotted_quarters() {
        // Arrange
        TimeSignature sixEight = new(6, 8);

        // Act
        Duration length = sixEight.MeasureLength;

        // Assert
        Assert.Equal(DottedQuarter * 2, length);
    }

    [Theory]
    [InlineData(120, 1, 4, 2.0)]   // a whole note is four quarters at two per second
    [InlineData(60, 3, 8, 8.0 / 3)] // a whole note is 8/3 dotted quarters at one per second
    [InlineData(90, 1, 2, 4.0 / 3)] // two half notes at one and a half per second
    public void A_whole_note_lasts_as_many_beats_as_it_holds_at_the_tempo(double beatsPerMinute, long beatNumerator, long beatDenominator, double expectedSeconds) {
        // Arrange
        Tempo tempo = new(beatsPerMinute, new Duration(beatNumerator, beatDenominator));

        // Act
        double seconds = tempo.SecondsFor(Whole);

        // Assert
        Assert.Equal(expectedSeconds, seconds, precision: 12);
    }

}
