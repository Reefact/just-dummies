using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Regular.Notes;

namespace MusicScore.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void A_duration_is_normalised_so_two_eighths_equal_a_quarter() {
        // Arrange
        Duration twoEighths = new(2, 8);

        // Act
        bool same = twoEighths == Quarter;

        // Assert
        Assert.True(same);
        Assert.Equal("1/4", twoEighths.ToString());
    }

    [Fact]
    public void Durations_add_up_exactly_as_fractions() {
        // Arrange
        Duration dottedQuarter = Quarter + Eighth;

        // Act
        Duration twoDottedQuarters = dottedQuarter * 2;

        // Assert
        Assert.Equal(new Duration(3, 8), dottedQuarter);
        Assert.Equal(new Duration(3, 4), twoDottedQuarters);
    }

    [Fact, Reproducible]
    public void Adding_nothing_leaves_a_duration_unchanged() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Act
        Duration sum = anyDuration + Duration.Zero;

        // Assert
        Assert.Equal(anyDuration, sum);
    }

    [Theory]
    [InlineData('C', 0, 4, 60)]
    [InlineData('A', 0, 4, 69)]
    [InlineData('F', 1, 3, 54)]
    [InlineData('B', -1, 2, 46)]
    public void A_pitch_maps_onto_its_midi_number(char step, int alter, int octave, int expectedMidi) {
        // Arrange
        Pitch pitch = new(step, alter, octave);

        // Act
        int midi = pitch.MidiNumber;

        // Assert
        Assert.Equal(expectedMidi, midi);
    }

    [Fact]
    public void Enharmonic_spellings_share_a_midi_number_but_remain_different_pitches() {
        // Arrange
        Pitch bSharp3 = new('B', 1, 3);
        Pitch c4      = new('C', 0, 4);

        // Act
        bool equal = bSharp3.Equals(c4);

        // Assert
        Assert.False(equal);
        Assert.Equal(c4.MidiNumber, bSharp3.MidiNumber);
    }

    [Fact]
    public void A_pitch_beyond_the_midi_range_is_refused_even_with_valid_fields() {
        // Arrange
        // Act
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() => new Pitch('G', 2, 9));

        // Assert
        Assert.Contains("MIDI range", error.Message);
    }

    [Fact]
    public void A_six_eight_measure_lasts_three_quarters_of_a_whole_note() {
        // Arrange
        TimeSignature sixEight = new(6, 8);

        // Act
        Duration length = sixEight.MeasureLength;

        // Assert
        Assert.Equal(new Duration(3, 4), length);
    }

    [Fact, Reproducible]
    public void A_beat_value_that_is_not_a_power_of_two_is_refused() {
        // Arrange
        int anyBeats = Any.Int32().Between(1, 32).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignature(anyBeats, 3));

        // Assert
        Assert.Equal("beatValue", error.ParamName);
    }

    [Fact]
    public void At_quarter_equals_120_a_whole_note_lasts_two_seconds() {
        // Arrange
        Tempo tempo = new(120, Quarter);

        // Act
        double seconds = tempo.SecondsFor(Whole);

        // Assert
        Assert.Equal(2.0, seconds, precision: 10);
    }

    [Fact]
    public void At_dotted_quarter_equals_60_a_six_eight_measure_lasts_two_seconds() {
        // Arrange
        Tempo tempo = new(60, new Duration(3, 8));

        // Act
        double seconds = tempo.SecondsFor(new TimeSignature(6, 8).MeasureLength);

        // Assert
        Assert.Equal(2.0, seconds, precision: 10);
    }

    [Fact, Reproducible]
    public void A_rest_cannot_be_tied() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new VoiceEntry(anyDuration, null, tiedToNext: true));

        // Assert
        Assert.Equal("tiedToNext", error.ParamName);
    }

    [Fact, Reproducible]
    public void A_voice_lasts_the_exact_sum_of_its_entries() {
        // Arrange
        int anyNumber = Any.Int32().Between(1, Voice.MaxVoices).Generate();
        VoiceEntry[] entries = [AnyNote(Quarter), VoiceEntry.Rest(Eighth), AnyNote(Eighth)];

        // Act
        Voice voice = new(anyNumber, entries);

        // Assert
        Assert.Equal(HalfNote, voice.Length);
    }

}
