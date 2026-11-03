using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class PitchTests {

    [Theory]
    [InlineData('C', 0, 4, 60)]
    [InlineData('A', 0, 4, 69)]
    [InlineData('C', 0, 0, 12)]
    [InlineData('G', 0, 9, 127)]
    [InlineData('F', 2, 9, 127)]
    [InlineData('B', 1, 3, 60)]
    [InlineData('C', -1, 4, 59)]
    [InlineData('D', 2, 4, 64)]
    [InlineData('C', -2, 0, 10)]
    public void A_pitch_has_the_midi_number_of_its_sounding_note(char step, int alter, int octave, int expectedMidi) {
        // Act
        Pitch pitch = new(step, alter, octave);

        // Assert
        Assert.Equal(expectedMidi, pitch.MidiNumber);
    }

    [Fact]
    public void A_pitch_keeps_its_spelling() {
        // Act
        Pitch pitch = new('E', -1, 5);

        // Assert
        Assert.Equal('E', pitch.Step);
        Assert.Equal(-1, pitch.Alter);
        Assert.Equal(5, pitch.Octave);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('H')]
    [InlineData(' ')]
    [InlineData('1')]
    [InlineData('\0')]
    public void A_step_outside_the_upper_case_letters_a_to_g_is_refused(char step) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithStep(step).Generate());

        // Assert
        Assert.Equal("step", refusal.ParamName);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void An_alteration_beyond_a_double_flat_or_a_double_sharp_is_refused(int alter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithAlter(alter).Generate());

        // Assert
        Assert.Equal("alter", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void An_octave_outside_zero_to_nine_is_refused(int octave) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithOctave(octave).Generate());

        // Assert
        Assert.Equal("octave", refusal.ParamName);
    }

    [Theory]
    [InlineData('G', 1, 9)]
    [InlineData('G', 2, 9)]
    [InlineData('A', 0, 9)]
    [InlineData('B', -2, 9)]
    public void A_pitch_above_the_midi_range_is_refused(char step, int alter, int octave) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Pitch(step, alter, octave));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
        Assert.Contains("MIDI", refusal.Message);
    }

    [Fact]
    public void Enharmonic_spellings_sound_alike_but_are_not_the_same_pitch() {
        // Arrange
        Pitch cSharp = P("C#4");
        Pitch dFlat  = P("Db4");

        // Assert
        Assert.Equal(cSharp.MidiNumber, dFlat.MidiNumber);
        Assert.NotEqual(cSharp, dFlat);
    }

    [Fact]
    public void Pitches_with_the_same_spelling_are_equal_and_hash_alike() {
        // Act
        Pitch first  = new('F', 1, 3);
        Pitch second = new('F', 1, 3);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData('C', 0, 4, "C4")]
    [InlineData('F', 1, 3, "F#3")]
    [InlineData('B', -1, 2, "Bb2")]
    [InlineData('F', 2, 9, "F##9")]
    [InlineData('D', -2, 5, "Dbb5")]
    public void A_pitch_reads_in_scientific_notation(char step, int alter, int octave, string expected) {
        Assert.Equal(expected, new Pitch(step, alter, octave).ToString());
    }

}
