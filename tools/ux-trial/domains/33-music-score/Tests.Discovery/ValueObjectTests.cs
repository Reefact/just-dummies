using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

[assembly: Reproducible]

namespace MusicScore.Tests.Discovery;

public sealed class DurationTests {

    [Fact]
    public void Two_quarters_and_one_half_are_the_same_duration() {
        // Arrange
        Duration twoQuarters = new Duration(2, 4);

        // Act
        bool same = twoQuarters == new Duration(1, 2);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void A_quarter_and_an_eighth_add_up_to_three_eighths() {
        // Arrange
        Duration quarter = new Duration(1, 4);
        Duration eighth  = new Duration(1, 8);

        // Act
        Duration sum = quarter + eighth;

        // Assert
        Assert.Equal(new Duration(3, 8), sum);
    }

    [Fact]
    public void Any_duration_played_zero_times_lasts_nothing() {
        // Arrange
        Duration anyDuration = Any.Duration().Generate();

        // Act
        Duration product = anyDuration * 0;

        // Assert
        Assert.Equal(Duration.Zero, product);
    }

    [Fact]
    public void A_fraction_finer_than_a_1024th_is_refused() {
        // Arrange
        // Act
        Action create = () => new Duration(1, 2048);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("denominator", create);
    }

    [Fact]
    public void The_notatable_limit_applies_to_the_reduced_fraction() {
        // Arrange
        // Act
        Duration reduced = new Duration(2, 2048);

        // Assert
        Assert.Equal(new Duration(1, 1024), reduced);
    }

}

public sealed class PitchTests {

    [Fact]
    public void Middle_C_is_MIDI_note_60() {
        // Arrange
        Pitch middleC = new Pitch('C', 0, 4);

        // Act
        int midi = middleC.MidiNumber;

        // Assert
        Assert.Equal(60, midi);
    }

    [Fact]
    public void C_sharp_and_D_flat_sound_the_same_but_are_written_differently() {
        // Arrange
        Pitch cSharp = new Pitch('C', 1, 4);
        Pitch dFlat  = new Pitch('D', -1, 4);

        // Act
        bool sameKey     = cSharp.MidiNumber == dFlat.MidiNumber;
        bool samePitch   = cSharp.Equals(dFlat);

        // Assert
        Assert.True(sameKey);
        Assert.False(samePitch);
    }

    [Fact]
    public void G_double_sharp_9_lies_above_the_MIDI_range_and_is_refused() {
        // Arrange
        // Act
        Action create = () => new Pitch('G', 2, 9);

        // Assert
        Assert.Throws<ArgumentException>("alter", create);
    }

}

public sealed class TimeSignatureAndTempoTests {

    [Fact]
    public void A_six_eight_measure_lasts_three_quarters_of_a_whole_note() {
        // Arrange
        TimeSignature sixEight = new TimeSignature(6, 8);

        // Act
        Duration length = sixEight.MeasureLength;

        // Assert
        Assert.Equal(new Duration(3, 4), length);
    }

    [Fact]
    public void A_beat_value_that_is_not_a_power_of_two_is_refused() {
        // Arrange
        int anyBeats = Any.Int32().Between(1, 32).Generate();

        // Act
        Action create = () => new TimeSignature(anyBeats, 3);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("beatValue", create);
    }

    [Fact]
    public void A_quarter_note_at_120_quarters_per_minute_lasts_half_a_second() {
        // Arrange
        Tempo tempo = new Tempo(120, new Duration(1, 4));

        // Act
        double seconds = tempo.SecondsFor(new Duration(1, 4));

        // Assert
        Assert.Equal(0.5, seconds, 10);
    }

    [Fact]
    public void A_six_eight_measure_at_60_dotted_quarters_per_minute_lasts_two_seconds() {
        // Arrange
        Tempo dottedQuarterAt60 = new Tempo(60, new Duration(3, 8));

        // Act
        double seconds = dottedQuarterAt60.SecondsFor(new TimeSignature(6, 8).MeasureLength);

        // Assert
        Assert.Equal(2.0, seconds, 10);
    }

    [Fact]
    public void A_rest_cannot_be_tied_to_the_next_entry() {
        // Arrange
        Duration anyDuration = Any.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        Action create = () => new VoiceEntry(anyDuration, null, tiedToNext: true);

        // Assert
        Assert.Throws<ArgumentException>("tiedToNext", create);
    }

}
