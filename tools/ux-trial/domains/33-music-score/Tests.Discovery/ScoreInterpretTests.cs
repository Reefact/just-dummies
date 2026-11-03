using JustDummies;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Discovery.Scores;

namespace MusicScore.Tests.Discovery;

public sealed class ScoreInterpretTests {

    private static readonly Pitch MiddleC = new Pitch('C', 0, 4);
    private static readonly Pitch D4      = new Pitch('D', 0, 4);

    private static string Describe(PerformedEvent performed) {
        return $"{performed.StaffName}/{performed.VoiceNumber} {performed.Pitch} @{performed.Onset} +{performed.Length}";
    }

    private static string[] Describe(IEnumerable<PerformedEvent> events) {
        return events.Select(Describe).ToArray();
    }

    private static Score ScoreOfOneStaff(params Measure[] bars) {
        return ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", bars));
    }

    [Fact]
    public void Notes_become_events_at_their_exact_rational_onsets() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, new Voice(1, [Note(MiddleC, new Duration(1, 4)), Note(D4, new Duration(1, 8)), Note(MiddleC, new Duration(1, 8)), Note(D4, new Duration(1, 2))])));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] {
                         "Violin/1 C4 @0/1 +1/4",
                         "Violin/1 D4 @1/4 +1/8",
                         "Violin/1 C4 @3/8 +1/8",
                         "Violin/1 D4 @1/2 +1/2",
                     },
                     Describe(events));
    }

    [Fact]
    public void Events_carry_their_start_and_duration_in_seconds_at_the_tempo() {
        // Arrange: quarter = 120, so a quarter note lasts half a second
        Score score = ScoreOf(QuarterEquals(120), StaffOf("Violin", Bar(1, FourQuarters(1, MiddleC))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { 0.0, 0.5, 1.0, 1.5 }, events.Select(performed => performed.StartSeconds));
        Assert.All(events, performed => Assert.Equal(0.5, performed.DurationSeconds, 10));
    }

    [Fact]
    public void Rests_are_silent_but_take_their_time() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, new Voice(1, [Note(MiddleC, Quarter), Rest(new Duration(1, 2)), Note(D4, Quarter)])));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +1/4", "Violin/1 D4 @3/4 +1/4" }, Describe(events));
    }

    [Fact]
    public void A_score_of_rests_has_no_events() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeRest(1)), Bar(2, WholeRest(1)));

        // Assert
        Assert.Empty(score.Interpret());
    }

    [Theory]
    [InlineData(Dynamic.Pianissimo, 33)]
    [InlineData(Dynamic.Piano, 49)]
    [InlineData(Dynamic.MezzoPiano, 64)]
    [InlineData(Dynamic.MezzoForte, 80)]
    [InlineData(Dynamic.Forte, 96)]
    [InlineData(Dynamic.Fortissimo, 112)]
    public void The_dynamic_of_a_measure_sets_the_velocity_of_its_notes(Dynamic dynamic, int expectedVelocity) {
        // Arrange
        Score score = ScoreOfOneStaff(BarWithDynamic(1, dynamic, FourQuarters(1, MiddleC)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.All(events, performed => Assert.Equal(expectedVelocity, performed.Velocity));
    }

    [Fact]
    public void Each_measure_applies_its_own_dynamic() {
        // Arrange
        Score score = ScoreOfOneStaff(BarWithDynamic(1, Dynamic.Pianissimo, WholeNote(1, MiddleC)), BarWithDynamic(2, Dynamic.Fortissimo, WholeNote(1, MiddleC)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { 33, 112 }, events.Select(performed => performed.Velocity));
    }

    private static Voice WholeNote(int number, Pitch pitch, bool tiedToNext = false) {
        return new Voice(number, [Note(pitch, new Duration(1, 1), tiedToNext)]);
    }

    [Fact]
    public void Two_tied_notes_in_one_measure_sound_as_one() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, new Voice(1, [Note(MiddleC, new Duration(1, 2), tiedToNext: true), Note(MiddleC, new Duration(1, 4)), Note(D4, new Duration(1, 4))])));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +3/4", "Violin/1 D4 @3/4 +1/4" }, Describe(events));
    }

    [Fact]
    public void A_tie_runs_across_a_barline() {
        // Arrange
        Voice endingOnATie   = new Voice(1, [Rest(new Duration(3, 4)), Note(MiddleC, Quarter, tiedToNext: true)]);
        Voice startingOnTied = new Voice(1, [Note(MiddleC, new Duration(1, 2)), Rest(new Duration(1, 2))]);
        Score score          = ScoreOfOneStaff(Bar(1, endingOnATie), Bar(2, startingOnTied));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @3/4 +3/4" }, Describe(events));
    }

    [Fact]
    public void A_chain_of_ties_over_three_measures_is_one_event() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC, tiedToNext: true)), Bar(2, WholeNote(1, MiddleC, tiedToNext: true)), Bar(3, WholeNote(1, MiddleC)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +3/1" }, Describe(events));
    }

    [Fact]
    public void A_tied_note_merges_across_a_repeat() {
        // Arrange: |: C4 half, rest, C4 quarter tied :|x2 then a whole-note C4.
        Voice repeated = new Voice(1, [Note(MiddleC, new Duration(1, 2)), Rest(Quarter), Note(MiddleC, Quarter, tiedToNext: true)]);
        Score score    = ScoreOfOneStaff(RepeatBarWith(1, repeated, repeatStart: true, repeatEndTimes: 2), Bar(2, WholeNote(1, MiddleC)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +1/2", "Violin/1 C4 @3/4 +3/4", "Violin/1 C4 @7/4 +5/4" }, Describe(events));
    }

    private static Measure RepeatBarWith(int number, Voice voice, bool repeatStart, int repeatEndTimes) {
        return new Measure(number, FourFour, Any.Enum<Dynamic>().Generate(), [voice], repeatStart, repeatEndTimes);
    }

    [Fact]
    public void A_tie_onto_another_pitch_is_refused() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC, tiedToNext: true)), Bar(2, WholeNote(1, D4)));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("C4", error.Message);
        Assert.Contains("measure 2", error.Message);
    }

    [Fact]
    public void An_enharmonic_tie_is_not_the_same_pitch() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, new Pitch('C', 1, 4), tiedToNext: true)), Bar(2, WholeNote(1, new Pitch('D', -1, 4))));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_onto_a_rest_is_refused() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC, tiedToNext: true)), Bar(2, WholeRest(1)));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_that_ends_the_piece_is_refused() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC)), Bar(2, WholeNote(1, MiddleC, tiedToNext: true)));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("tied to nothing", error.Message);
    }

    [Fact]
    public void A_tie_into_a_measure_where_the_voice_is_absent_is_refused() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC, tiedToNext: true), WholeNote(2, D4)), Bar(2, WholeNote(2, D4)));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("voice 1", error.Message);
    }

    [Fact]
    public void Voices_of_a_staff_sound_together_ordered_by_voice_at_the_same_onset() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(2, D4), new Voice(1, [Note(MiddleC, new Duration(1, 2)), Note(MiddleC, new Duration(1, 2))])));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +1/2", "Violin/2 D4 @0/1 +1/1", "Violin/1 C4 @1/2 +1/2" }, Describe(events));
    }

    [Fact]
    public void Staves_are_ordered_by_their_place_in_the_score_not_by_name() {
        // Arrange
        Score score = ScoreOf(Any.Tempo().Generate(), StaffOf("Zither", Bar(1, WholeNote(1, MiddleC))), StaffOf("Alto", Bar(1, WholeNote(1, D4))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Zither/1 C4 @0/1 +1/1", "Alto/1 D4 @0/1 +1/1" }, Describe(events));
    }

    [Fact]
    public void Events_are_ordered_by_onset_before_staff() {
        // Arrange
        Voice lateBassNote = new Voice(1, [Rest(new Duration(1, 2)), Note(MiddleC, new Duration(1, 2))]);
        Voice earlyTopNote = new Voice(1, [Note(D4, new Duration(1, 4)), Rest(new Duration(3, 4))]);
        Score score        = ScoreOf(Any.Tempo().Generate(), StaffOf("First", Bar(1, lateBassNote)), StaffOf("Second", Bar(1, earlyTopNote)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Second/1 D4 @0/1 +1/4", "First/1 C4 @1/2 +1/2" }, Describe(events));
    }

    [Fact]
    public void A_voice_absent_from_a_measure_leaves_a_gap_of_that_measure() {
        // Arrange
        Score score = ScoreOfOneStaff(Bar(1, WholeNote(1, MiddleC), WholeNote(2, D4)), Bar(2, WholeNote(1, MiddleC)), Bar(3, WholeNote(2, D4)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret().Where(performed => performed.VoiceNumber == 2).ToList();

        // Assert
        Assert.Equal(new[] { "Violin/2 D4 @0/1 +1/1", "Violin/2 D4 @2/1 +1/1" }, Describe(events));
    }

    [Fact]
    public void A_repeated_section_is_played_again_and_its_events_follow_the_played_order() {
        // Arrange: |: m1 m2 :|x2 -- played 1 2 1 2, quarter = 60
        Voice firstBar  = new Voice(1, [Note(MiddleC, new Duration(1, 1))]);
        Voice secondBar = new Voice(1, [Note(D4, new Duration(1, 1))]);
        Score score     = ScoreOf(QuarterEquals(60),
                                  StaffOf("Violin", RepeatBarWith(1, firstBar, repeatStart: true, repeatEndTimes: 0), RepeatBarWith(2, secondBar, repeatStart: false, repeatEndTimes: 2)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin/1 C4 @0/1 +1/1", "Violin/1 D4 @1/1 +1/1", "Violin/1 C4 @2/1 +1/1", "Violin/1 D4 @3/1 +1/1" }, Describe(events));
        Assert.Equal(new[] { 0.0, 4.0, 8.0, 12.0 }, events.Select(performed => performed.StartSeconds));
    }

    [Fact]
    public void A_score_drawn_by_the_generator_interprets_inside_its_played_length() {
        // Arrange: this one is about the draw itself (a property, run once), kept as a sanity check of the generator
        Score score = Any.Score().Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert: a statement about the whole draw, one run of it
        Duration playedLength = score.PlayedLength();
        Assert.All(events, performed => Assert.True(performed.End <= playedLength));
    }

}
