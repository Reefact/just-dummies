using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Regular.Notes;

namespace MusicScore.Tests.Regular;

public sealed class InterpretationTests {

    [Fact, Reproducible]
    public void Rests_are_silent_and_notes_start_at_their_exact_onset() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithSingleVoice(AnyNote(Quarter), VoiceEntry.Rest(Quarter), AnyNote(HalfNote))));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([Duration.Zero, HalfNote], events.Select(performed => performed.Onset));
        Assert.Equal([Quarter, HalfNote], events.Select(performed => performed.Length));
    }

    [Fact, Reproducible]
    public void A_note_tied_across_the_barline_sounds_once_for_both_lengths() {
        // Arrange
        Pitch anyPitch = Arbitrary.Pitch().Generate();
        Score anyScore = AnyScoreOf(AnyStaffOf(
            AnyBar(1).WithSingleVoice(AnyNote(HalfNote), Note(HalfNote, anyPitch, tiedToNext: true)),
            AnyBar(2).WithSingleVoice(Note(Quarter, anyPitch), AnyNote(Quarter), AnyNote(HalfNote))));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        PerformedEvent tied = events[1];
        Assert.Equal(4, events.Count);
        Assert.Equal(HalfNote, tied.Onset);
        Assert.Equal(new Duration(3, 4), tied.Length);
    }

    [Fact, Reproducible]
    public void A_tie_onto_another_pitch_cannot_be_performed() {
        // Arrange
        Pitch anyPitch      = Arbitrary.Pitch().Generate();
        Pitch anyOtherPitch = Arbitrary.Pitch().WithStep(step => step.Except(anyPitch.Step)).Generate();
        Score anyScore = AnyScoreOf(AnyStaffOf(
            AnyBar(1).WithSingleVoice(Note(Whole, anyPitch, tiedToNext: true)),
            AnyBar(2).WithSingleVoice(Note(Whole, anyOtherPitch))));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyScore.Interpret());

        // Assert
        Assert.Contains("does not resolve onto the same pitch in measure 2", error.Message);
    }

    [Fact, Reproducible]
    public void A_tie_on_the_last_note_of_the_piece_cannot_be_performed() {
        // Arrange
        Pitch anyPitch = Arbitrary.Pitch().Generate();
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithSingleVoice(AnyNote(HalfNote), Note(HalfNote, anyPitch, tiedToNext: true))));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyScore.Interpret());

        // Assert
        Assert.Contains("is tied to nothing", error.Message);
    }

    [Fact, Reproducible]
    public void A_repeated_measure_is_performed_again_after_the_written_ones() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithSingleVoice(AnyNote(Whole)).WithRepeatEndTimes(2), AnyBar(2).WithSingleVoice(AnyNote(Whole))));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([Duration.Zero, Whole, new Duration(2, 1)], events.Select(performed => performed.Onset));
        Assert.Equal(events[0].Pitch, events[1].Pitch);
    }

    [Fact, Reproducible]
    public void A_tie_into_a_repeat_resolves_onto_the_repeated_measure() {
        // Arrange
        Pitch anyPitch = Arbitrary.Pitch().Generate();
        Score anyScore = AnyScoreOf(AnyStaffOf(
            AnyBar(1).WithSingleVoice(Note(HalfNote, anyPitch), Note(HalfNote, anyPitch, tiedToNext: true)).WithRepeatEndTimes(2),
            AnyBar(2).WithSingleVoice(Note(Whole, anyPitch))));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([Duration.Zero, HalfNote, new Duration(3, 2)], events.Select(performed => performed.Onset));
        Assert.Equal([HalfNote, Whole, new Duration(3, 2)], events.Select(performed => performed.Length));
    }

    [Theory]
    [InlineData(Dynamic.Pianissimo, 33)]
    [InlineData(Dynamic.MezzoForte, 80)]
    [InlineData(Dynamic.Fortissimo, 112)]
    [Reproducible]
    public void A_note_is_performed_at_the_velocity_of_its_measure_dynamic(Dynamic dynamic, int expectedVelocity) {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithDynamic(dynamic).WithSingleVoice(AnyNote(Whole))));

        // Act
        PerformedEvent performed = Assert.Single(anyScore.Interpret());

        // Assert
        Assert.Equal(expectedVelocity, performed.Velocity);
    }

    [Fact, Reproducible]
    public void Simultaneous_notes_are_ordered_by_staff_then_voice() {
        // Arrange
        Voice upper = new(1, [AnyNote(Whole)]);
        Voice lower = new(2, [AnyNote(HalfNote), AnyNote(HalfNote)]);
        Staff anyPiano = Arbitrary.Staff().WithName("Piano").WithMeasures([AnyBar(1).WithVoices([lower, upper]).Generate()]).Generate();
        Staff anyBass  = Arbitrary.Staff().WithName("Bass").WithMeasures([AnyBar(1).WithSingleVoice(AnyNote(Whole)).Generate()]).Generate();
        Score anyScore = AnyScoreOf(anyPiano, anyBass);

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([("Piano", 1), ("Piano", 2), ("Bass", 1), ("Piano", 2)], events.Select(performed => (performed.StaffName, performed.VoiceNumber)));
    }

    [Fact, Reproducible]
    public void Performed_notes_carry_their_start_and_length_in_seconds() {
        // Arrange
        Score anyScore = Arbitrary.Score()
                                  .WithTempo(new Tempo(120, Quarter))
                                  .WithStaves([AnyStaffOf(AnyBar(1).WithSingleVoice(AnyNote(HalfNote), AnyNote(HalfNote)))])
                                  .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal(1.0, events[1].StartSeconds, precision: 10);
        Assert.Equal(1.0, events[1].DurationSeconds, precision: 10);
    }

}
