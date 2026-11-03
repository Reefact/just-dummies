using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class PitchTests {

    [Theory]
    [InlineData('C', 0, 4, 60)]
    [InlineData('A', 0, 4, 69)]
    [InlineData('B', -1, 3, 58)]
    [InlineData('F', 2, 3, 55)]
    [InlineData('C', 0, 0, 12)]
    [InlineData('C', -2, 0, 10)]
    [InlineData('G', 0, 9, 127)]
    [InlineData('F', 2, 9, 127)]
    public void A_pitch_has_the_midi_number_of_its_written_note(char step, int alter, int octave, int expectedMidi) {
        // Act
        Pitch pitch = new Pitch(step, alter, octave);

        // Assert
        Assert.Equal(expectedMidi, pitch.MidiNumber);
        Assert.Equal(step, pitch.Step);
        Assert.Equal(alter, pitch.Alter);
        Assert.Equal(octave, pitch.Octave);
    }

    [Fact]
    public void A_pitch_that_would_overshoot_the_midi_range_is_refused() {
        // G## in octave 9 would be MIDI 129; the written note itself is valid on every field taken alone.
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Pitch('G', 2, 9));

        Assert.Contains("MIDI", error.Message);
    }

    [Fact]
    public void The_first_pitch_beyond_the_midi_range_is_refused() {
        Assert.Throws<ArgumentException>(() => new Pitch('G', 1, 9));
        Assert.Throws<ArgumentException>(() => new Pitch('A', 0, 9));
    }

    [Theory]
    [InlineData('c')]
    [InlineData('H')]
    [InlineData(' ')]
    [InlineData('\0')]
    public void A_step_that_is_not_an_upper_case_letter_from_A_to_G_is_refused(char step) {
        // Arrange
        int anyOctave = Any.Int32().Between(0, 8).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Pitch(step, 0, anyOctave));

        // Assert
        Assert.Equal("step", error.ParamName);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(3)]
    public void An_alteration_beyond_a_double_flat_or_double_sharp_is_refused(int alter) {
        // Arrange
        char anyStep   = Any.Char().OneOf('A', 'B', 'C', 'D', 'E', 'F', 'G').Generate();
        int  anyOctave = Any.Int32().Between(1, 8).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Pitch(anyStep, alter, anyOctave));

        // Assert
        Assert.Equal("alter", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void An_octave_outside_zero_to_nine_is_refused(int octave) {
        // Arrange
        char anyStep  = Any.Char().OneOf('A', 'B', 'C', 'D', 'E', 'F', 'G').Generate();
        int  anyAlter = Any.Int32().Between(-2, 2).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Pitch(anyStep, anyAlter, octave));

        // Assert
        Assert.Equal("octave", error.ParamName);
    }

    [Fact]
    public void Enharmonic_pitches_share_a_midi_number_but_are_different_pitches() {
        // Arrange
        Pitch cSharp = new Pitch('C', 1, 4);
        Pitch dFlat  = new Pitch('D', -1, 4);

        // Assert
        Assert.Equal(cSharp.MidiNumber, dFlat.MidiNumber);
        Assert.NotEqual(cSharp, dFlat);
    }

    [Fact]
    public void Pitches_written_the_same_way_are_equal() {
        // Arrange
        Pitch first  = new Pitch('E', -1, 5);
        Pitch second = new Pitch('E', -1, 5);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(null));
    }

    [Theory]
    [InlineData('C', 0, 4, "C4")]
    [InlineData('B', -1, 2, "Bb2")]
    [InlineData('B', -2, 2, "Bbb2")]
    [InlineData('F', 1, 3, "F#3")]
    [InlineData('F', 2, 3, "F##3")]
    public void A_pitch_reads_in_scientific_notation(char step, int alter, int octave, string expected) {
        Assert.Equal(expected, new Pitch(step, alter, octave).ToString());
    }

}
