using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class InterpretTests {

    [Fact]
    public void A_whole_note_sounds_for_a_whole_measure() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        PerformedEvent only = Assert.Single(events);
        AssertEvent(only, staff: "Violin I", voice: 1, pitch: "C4", onset: D(0, 1), length: D(1, 1));
        Assert.Equal(0.0, only.StartSeconds, 10);
        Assert.Equal(4.0, only.DurationSeconds, 10);
    }

    [Fact]
    public void Rests_are_silent_but_they_take_their_time() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Rest(1, 4), Note("E4", 3, 4))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        PerformedEvent only = Assert.Single(events);
        AssertEvent(only, staff: "Violin I", voice: 1, pitch: "E4", onset: D(1, 4), length: D(3, 4));
        Assert.Equal(1.0, only.StartSeconds, 10);
        Assert.Equal(3.0, only.DurationSeconds, 10);
    }

    [Fact]
    public void A_score_of_rests_has_nothing_to_perform() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Rest(1, 1))).Generate()));

        // Act & Assert
        Assert.Empty(score.Interpret());
    }

    [Theory]
    [InlineData(Dynamic.Pianissimo, 33)]
    [InlineData(Dynamic.Piano, 49)]
    [InlineData(Dynamic.MezzoPiano, 64)]
    [InlineData(Dynamic.MezzoForte, 80)]
    [InlineData(Dynamic.Forte, 96)]
    [InlineData(Dynamic.Fortissimo, 112)]
    public void A_dynamic_sets_the_velocity_of_the_notes_in_its_measure(Dynamic dynamic, int expectedVelocity) {
        // Arrange
        Measure measure = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2), Note("G4", 1, 2))).WithDynamic(dynamic).Generate();
        Score   score   = ScoreOf(Arbitrary.Staff().WithMeasures([measure]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, performed => Assert.Equal(expectedVelocity, performed.Velocity));
    }

    [Fact]
    public void Each_measure_applies_its_own_dynamic() {
        // Arrange
        Measure soft = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1))).WithDynamic(Dynamic.Piano).Generate();
        Measure loud = Bar(2, FourFour, VoiceOf(1, Note("C4", 1, 1))).WithDynamic(Dynamic.Forte).Generate();
        Score   score = ScoreOf(Arbitrary.Staff().WithMeasures([soft, loud]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal([49, 96], events.Select(performed => performed.Velocity));
    }

    [Fact]
    public void Two_tied_notes_are_one_sounding_note() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2, tied: true), Note("C4", 1, 2))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        AssertEvent(Assert.Single(events), staff: "Violin I", voice: 1, pitch: "C4", onset: D(0, 1), length: D(1, 1));
    }

    [Fact]
    public void A_chain_of_ties_is_one_sounding_note() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 4, tied: true), Note("C4", 1, 4, tied: true), Note("C4", 1, 2))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        AssertEvent(Assert.Single(events), staff: "Violin I", voice: 1, pitch: "C4", onset: D(0, 1), length: D(1, 1));
    }

    [Fact]
    public void A_tie_runs_across_the_barline() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 3, 4), Note("G4", 1, 4, tied: true))).WithDynamic(Dynamic.Piano).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(1, Note("G4", 1, 4), Note("C5", 3, 4))).WithDynamic(Dynamic.Forte).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithName("Violin I").WithMeasures([first, second]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(3, events.Count);
        AssertEvent(events[0], staff: "Violin I", voice: 1, pitch: "C4", onset: D(0, 1), length: D(3, 4));
        AssertEvent(events[1], staff: "Violin I", voice: 1, pitch: "G4", onset: D(3, 4), length: D(1, 2));
        AssertEvent(events[2], staff: "Violin I", voice: 1, pitch: "C5", onset: D(5, 4), length: D(3, 4));
    }

    [Fact]
    public void A_tied_note_keeps_the_dynamic_it_started_with() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 3, 4), Note("G4", 1, 4, tied: true))).WithDynamic(Dynamic.Piano).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(1, Note("G4", 1, 4), Note("C5", 3, 4))).WithDynamic(Dynamic.Forte).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithMeasures([first, second]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal([49, 49, 96], events.Select(performed => performed.Velocity));
    }

    [Fact]
    public void A_tie_runs_from_the_end_of_a_repeat_back_to_its_start() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1))).WithRepeatStart(true).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(1, Note("C4", 1, 1, tied: true))).WithRepeatEndTimes(2).Generate();
        Measure third  = Bar(3, FourFour, VoiceOf(1, Note("C4", 1, 1))).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithName("Violin I").WithMeasures([first, second, third]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(3, events.Count);
        AssertEvent(events[0], staff: "Violin I", voice: 1, pitch: "C4", onset: D(0, 1), length: D(1, 1));
        AssertEvent(events[1], staff: "Violin I", voice: 1, pitch: "C4", onset: D(1, 1), length: D(2, 1));
        AssertEvent(events[2], staff: "Violin I", voice: 1, pitch: "C4", onset: D(3, 1), length: D(2, 1));
    }

    [Fact]
    public void A_tie_onto_another_pitch_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2, tied: true), Note("D4", 1, 2))).Generate()));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("C4", refusal.Message);
        Assert.Contains("Violin I", refusal.Message);
    }

    [Fact]
    public void A_tie_onto_the_same_sound_spelled_differently_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C#4", 1, 2, tied: true), Note("Db4", 1, 2))).Generate()));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_onto_a_rest_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2, tied: true), Rest(1, 2))).Generate()));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_out_of_the_last_note_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2), Note("G4", 1, 2, tied: true))).Generate()));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("tied to nothing", refusal.Message);
        Assert.Contains("G4", refusal.Message);
    }

    [Fact]
    public void A_tie_into_a_measure_where_the_voice_is_absent_is_refused() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1, tied: true))).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(2, Note("C4", 1, 1))).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithName("Violin I").WithMeasures([first, second]).Generate());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("absent", refusal.Message);
    }

    [Fact]
    public void Simultaneous_voices_overlap_and_come_out_by_onset_then_voice() {
        // Arrange
        Voice melody = VoiceOf(1, Note("C5", 1, 1));
        Voice bass   = VoiceOf(2, Note("C3", 1, 2), Note("G3", 1, 2));
        Score score  = ScoreOf(StaffOf("Piano RH", Bar(1, FourFour, bass, melody).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(3, events.Count);
        AssertEvent(events[0], staff: "Piano RH", voice: 1, pitch: "C5", onset: D(0, 1), length: D(1, 1));
        AssertEvent(events[1], staff: "Piano RH", voice: 2, pitch: "C3", onset: D(0, 1), length: D(1, 2));
        AssertEvent(events[2], staff: "Piano RH", voice: 2, pitch: "G3", onset: D(1, 2), length: D(1, 2));
    }

    [Fact]
    public void Staves_come_out_in_the_order_they_were_declared_not_in_alphabetical_order() {
        // Arrange
        Staff zeta  = StaffOf("Zeta", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1)), VoiceOf(2, Note("E4", 1, 1))).Generate());
        Staff alpha = StaffOf("Alpha", Bar(1, FourFour, VoiceOf(1, Note("G4", 1, 1))).Generate());
        Score score = Arbitrary.Score().WithStaves([zeta, alpha]).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal([("Zeta", 1), ("Zeta", 2), ("Alpha", 1)], events.Select(performed => (performed.StaffName, performed.VoiceNumber)));
    }

    [Fact]
    public void Onsets_stay_exact_in_triplets() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, new TimeSignature(1, 4), VoiceOf(1, Note("C4", 1, 12), Note("D4", 1, 12), Note("E4", 1, 12))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal([D(0, 1), D(1, 12), D(1, 6)], events.Select(performed => performed.Onset));
        Assert.Equal([D(1, 12), D(1, 12), D(1, 12)], events.Select(performed => performed.Length));
        Assert.Equal(1.0 / 3.0, events[1].StartSeconds, 10);
        Assert.Equal(2.0 / 3.0, events[2].StartSeconds, 10);
    }

    [Fact]
    public void A_repeated_section_is_performed_again_after_the_first_pass() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1))).WithRepeatStart(true).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(1, Note("D4", 1, 1))).WithRepeatEndTimes(2).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithName("Violin I").WithMeasures([first, second]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(["C4", "D4", "C4", "D4"], events.Select(performed => performed.Pitch.ToString()));
        Assert.Equal([D(0, 1), D(1, 1), D(2, 1), D(3, 1)], events.Select(performed => performed.Onset));
    }

    [Fact]
    public void A_voice_that_enters_late_starts_at_its_measure_not_at_zero() {
        // Arrange
        Measure first  = Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 1))).Generate();
        Measure second = Bar(2, FourFour, VoiceOf(1, Note("D4", 1, 1)), VoiceOf(2, Note("A2", 1, 1))).Generate();
        Score   score  = ScoreOf(Arbitrary.Staff().WithName("Piano LH").WithMeasures([first, second]).Generate());

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        PerformedEvent entrance = Assert.Single(events, performed => performed.VoiceNumber == 2);
        AssertEvent(entrance, staff: "Piano LH", voice: 2, pitch: "A2", onset: D(1, 1), length: D(1, 1));
    }

    [Fact]
    public void Seconds_follow_the_tempo_of_the_score() {
        // Arrange
        Tempo quarterEquals120 = new(120, D(1, 4));
        Score score            = Arbitrary.Score()
                                          .WithTempo(quarterEquals120)
                                          .WithStaves([StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2), Note("G4", 1, 2))).Generate())])
                                          .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal([0.0, 1.0], events.Select(performed => performed.StartSeconds));
        Assert.Equal([1.0, 1.0], events.Select(performed => performed.DurationSeconds));
    }

    [Fact]
    public void Interpreting_twice_gives_the_same_notes() {
        // Arrange
        Score score = ScoreOf(StaffOf("Violin I", Bar(1, FourFour, VoiceOf(1, Note("C4", 1, 2), Note("G4", 1, 2))).Generate()));

        // Act
        IReadOnlyList<PerformedEvent> first  = score.Interpret();
        IReadOnlyList<PerformedEvent> second = score.Interpret();

        // Assert
        Assert.Equal(first.Select(Describe), second.Select(Describe));
    }

    private static Score ScoreOf(Staff staff) {
        return Arbitrary.Score().WithTempo(QuarterEquals60).WithStaves([staff]).Generate();
    }

    private static void AssertEvent(PerformedEvent actual, string staff, int voice, string pitch, Duration onset, Duration length) {
        Assert.Equal(staff, actual.StaffName);
        Assert.Equal(voice, actual.VoiceNumber);
        Assert.Equal(P(pitch), actual.Pitch);
        Assert.Equal(onset, actual.Onset);
        Assert.Equal(length, actual.Length);
    }

    private static string Describe(PerformedEvent performed) {
        return $"{performed.StaffName}/{performed.VoiceNumber}/{performed.Pitch}/{performed.Velocity}/{performed.Onset}/{performed.Length}/{performed.StartSeconds}/{performed.DurationSeconds}";
    }

}
