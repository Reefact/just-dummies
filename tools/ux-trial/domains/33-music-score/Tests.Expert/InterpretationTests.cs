using JustDummies;

using MusicScore.Domain;
using MusicScore.Tests.Expert.Arbitrary;

using Xunit;

using static MusicScore.Tests.Expert.Notation;

namespace MusicScore.Tests.Expert;

/// <summary>
///     From the written score to the notes to perform. Rhythm, ties and barlines are what these
///     tests are about, so they are written out; which pitch sounds, and how loud, is arbitrary
///     unless the rule under test is about it.
/// </summary>
public sealed class InterpretationTests {

    private static readonly IAny<List<Pitch>> TwoDifferentPitches = Any.ListOf(Any.Pitch()).WithCount(2).Distinct();

    [Fact]
    public void A_rest_is_silent_and_delays_the_note_after_it() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score().WithMeasures(m => m.WithSingleVoice(CommonTime, Rest(Quarter), Note(DottedHalf, anyPitch))).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        PerformedEvent only = Assert.Single(events);
        Assert.Equal(Quarter, only.Onset);
        Assert.Equal(DottedHalf, only.Length);
    }

    [Fact]
    public void Tied_notes_sound_once_for_their_combined_length_across_the_barline() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score()
                            .WithMeasures(m => m.WithSingleVoice(CommonTime, Note(HalfNote, anyPitch), TiedNote(HalfNote, anyPitch)),
                                          m => m.WithSingleVoice(CommonTime, Note(Quarter, anyPitch), Rest(DottedHalf)))
                            .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([(Duration.Zero, HalfNote), (HalfNote, DottedHalf)], events.Select(performed => (performed.Onset, performed.Length)));
    }

    [Fact]
    public void A_tied_note_keeps_the_loudness_of_the_measure_where_it_starts() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score()
                            .WithMeasures(m => m.WithDynamic(Dynamic.Piano).WithSingleVoice(TwoFour, TiedNote(HalfNote, anyPitch)),
                                          m => m.WithDynamic(Dynamic.Fortissimo).WithSingleVoice(TwoFour, Note(HalfNote, anyPitch)))
                            .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        PerformedEvent merged = Assert.Single(events);
        Assert.Equal(49, merged.Velocity);
    }

    [Fact]
    public void A_tie_onto_a_different_pitch_cannot_be_performed() {
        // Arrange
        List<Pitch> anyPitches = TwoDifferentPitches.Generate();
        Score anyScore = Any.Score()
                            .WithMeasures(m => m.WithSingleVoice(TwoFour, TiedNote(Quarter, anyPitches[0]), Note(Quarter, anyPitches[1])))
                            .Generate();

        // Act
        Exception? refused = Record.Exception(() => anyScore.Interpret());

        // Assert
        Assert.IsType<InvalidOperationException>(refused);
    }

    [Fact]
    public void A_tie_on_the_last_note_of_the_piece_cannot_be_performed() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score().WithMeasures(AnyBar, m => m.WithSingleVoice(TwoFour, Rest(Quarter), TiedNote(Quarter, anyPitch))).Generate();

        // Act
        Exception? refused = Record.Exception(() => anyScore.Interpret());

        // Assert
        InvalidOperationException invalid = Assert.IsType<InvalidOperationException>(refused);
        Assert.Contains("tied to nothing", invalid.Message);
    }

    [Fact]
    public void A_repeated_measure_is_performed_again_one_measure_later() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score().WithMeasures(m => m.WithSingleVoice(ThreeFour, Note(DottedHalf, anyPitch)).ClosingRepeat(3)).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([Duration.Zero, DottedHalf, DottedHalf * 2], events.Select(performed => performed.Onset));
    }

    [Fact]
    public void A_tie_across_an_end_repeat_resolves_onto_the_first_note_of_the_repeated_section() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score()
                            .WithMeasures(m => m.WithSingleVoice(TwoFour, Note(Quarter, anyPitch), Rest(Quarter)),
                                          m => m.WithSingleVoice(TwoFour, Rest(Quarter), TiedNote(Quarter, anyPitch)).ClosingRepeat(2),
                                          m => m.WithSingleVoice(TwoFour, Note(HalfNote, anyPitch)))
                            .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert — played 1 2 1 2 3: the first tie lands on measure 1 replayed, the second on measure 3.
        Assert.Equal([(Duration.Zero, Quarter), (new Duration(3, 4), new Duration(2, 4)), (new Duration(7, 4), new Duration(3, 4))],
                     events.Select(performed => (performed.Onset, performed.Length)));
    }

    [Fact]
    public void Simultaneous_notes_are_ordered_by_staff_as_written_then_by_voice() {
        // Arrange
        TimeSignature anyMeter = Any.TimeSignature().Generate();
        List<Pitch>   anyPitches = TwoDifferentPitches.Generate();
        Func<AnyMeasure, AnyMeasure> twoVoicesWrittenLowerFirst =
            m => m.WithTimeSignature(anyMeter)
                  .WithVoices([new Voice(2, [Note(anyMeter.MeasureLength, anyPitches[0])]),
                               new Voice(1, [Note(anyMeter.MeasureLength, anyPitches[1])])]);
        List<Staff> anyStaves = Any.ListOf(Any.Staff().WithMeasures(twoVoicesWrittenLowerFirst)).WithCount(2).Distinct(StaffNameComparer.Instance).Generate();
        Score anyScore = Any.Score().WithStaves(anyStaves).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([(anyStaves[0].Name, 1), (anyStaves[0].Name, 2), (anyStaves[1].Name, 1), (anyStaves[1].Name, 2)],
                     events.Select(performed => (performed.StaffName, performed.VoiceNumber)));
    }

    [Theory]
    [InlineData(Dynamic.Pianissimo, 33)]
    [InlineData(Dynamic.Piano, 49)]
    [InlineData(Dynamic.MezzoPiano, 64)]
    [InlineData(Dynamic.MezzoForte, 80)]
    [InlineData(Dynamic.Forte, 96)]
    [InlineData(Dynamic.Fortissimo, 112)]
    public void A_note_is_performed_at_the_velocity_of_its_measures_dynamic(Dynamic dynamic, int expectedVelocity) {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(m => m.WithDynamic(dynamic)).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.All(events, performed => Assert.Equal(expectedVelocity, performed.Velocity));
    }

    [Fact]
    public void A_note_starts_and_lasts_in_seconds_at_the_scores_tempo() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = Any.Score()
                            .WithTempo(new Tempo(120, Quarter))
                            .WithMeasures(m => m.WithSingleVoice(CommonTime, Rest(HalfNote), Note(Quarter, anyPitch), Rest(Quarter)))
                            .Generate();

        // Act
        PerformedEvent performed = Assert.Single(anyScore.Interpret());

        // Assert
        Assert.Equal(1.0, performed.StartSeconds, precision: 12);
        Assert.Equal(0.5, performed.DurationSeconds, precision: 12);
    }

    private static readonly Func<AnyMeasure, AnyMeasure> AnyBar = measure => measure;

    private sealed class StaffNameComparer : IEqualityComparer<Staff> {

        public static readonly StaffNameComparer Instance = new();

        public bool Equals(Staff? x, Staff? y) {
            return string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);
        }

        public int GetHashCode(Staff obj) {
            return StringComparer.Ordinal.GetHashCode(obj.Name);
        }

    }

}
