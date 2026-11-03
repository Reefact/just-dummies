using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class PitchTests {

    [Theory]
    [InlineData('C', 0, 4, 60)]
    [InlineData('A', 0, 4, 69)]
    [InlineData('C', 1, 4, 61)]
    [InlineData('B', -1, 3, 58)]
    [InlineData('C', -1, 4, 59)]
    [InlineData('B', 1, 3, 60)]
    [InlineData('C', 0, 0, 12)]
    [InlineData('C', -2, 0, 10)]
    [InlineData('G', 0, 9, 127)]
    [InlineData('F', 2, 9, 127)]
    [InlineData('A', -2, 9, 127)]
    [InlineData('E', 0, 5, 76)]
    public void A_pitch_maps_to_its_midi_number(char step, int alter, int octave, int expectedMidi) {
        // Act
        Pitch pitch = new Pitch(step, alter, octave);

        // Assert
        Assert.Equal(expectedMidi, pitch.MidiNumber);
    }

    [Theory]
    [InlineData('G', 1, 9)]
    [InlineData('G', 2, 9)]
    [InlineData('A', 0, 9)]
    [InlineData('B', 0, 9)]
    [InlineData('A', -1, 9)]
    public void A_pitch_above_midi_127_is_refused(char step, int alter, int octave) {
        // Act
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() => new Pitch(step, alter, octave));

        // Assert
        Assert.Contains("0-127", error.Message);
    }

    [Fact]
    public void The_highest_octave_accepts_the_steps_below_g()
    {
        // Act and assert: C9 .. G9 are inside the range
        foreach (char step in "CDEFG") {
            Assert.True(new Pitch(step, 0, 9).MidiNumber <= 127);
        }
    }

    [Theory]
    [InlineData('c')]
    [InlineData('H')]
    [InlineData('Z')]
    [InlineData(' ')]
    [InlineData('1')]
    [InlineData('\0')]
    public void A_step_must_be_an_upper_case_letter_from_a_to_g(char step) {
        // Arrange
        int anyAlter  = Any.Int32().Between(-2, 2).Generate();
        int anyOctave = Any.Int32().Between(1, 8).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Pitch(step, anyAlter, anyOctave));

        // Assert
        Assert.Equal("step", error.ParamName);
    }

    [Fact]
    public void An_alteration_beyond_a_double_flat_or_double_sharp_is_refused() {
        // Arrange
        int anyTooHigh = Any.Int32().GreaterThan(2).Generate();
        int anyTooLow  = Any.Int32().LessThan(-2).Generate();

        // Assert
        Assert.Equal("alter", Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithAlter(anyTooHigh).Generate()).ParamName);
        Assert.Equal("alter", Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithAlter(anyTooLow).Generate()).ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_octave_outside_zero_to_nine_is_refused(int octave) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Pitch().WithOctave(octave).Generate());

        // Assert
        Assert.Equal("octave", error.ParamName);
    }

    [Fact]
    public void Enharmonic_spellings_share_a_midi_number_but_are_different_pitches() {
        // Arrange
        Pitch cSharp = P("C#4");
        Pitch dFlat  = P("Db4");

        // Assert
        Assert.Equal(cSharp.MidiNumber, dFlat.MidiNumber);
        Assert.NotEqual(cSharp, dFlat);
    }

    [Fact]
    public void Pitches_with_the_same_spelling_are_equal() {
        // Assert
        Assert.Equal(P("F#3"), P("F#3"));
        Assert.Equal(P("F#3").GetHashCode(), P("F#3").GetHashCode());
        Assert.NotEqual(P("F#3"), P("F#4"));
        Assert.False(P("F#3").Equals(null));
    }

    [Theory]
    [InlineData('C', 0, 4, "C4")]
    [InlineData('C', 1, 4, "C#4")]
    [InlineData('B', -1, 3, "Bb3")]
    [InlineData('F', 2, 9, "F##9")]
    [InlineData('D', -2, 0, "Dbb0")]
    public void A_pitch_reads_in_scientific_notation(char step, int alter, int octave, string expected) {
        // Assert
        Assert.Equal(expected, new Pitch(step, alter, octave).ToString());
    }

}
