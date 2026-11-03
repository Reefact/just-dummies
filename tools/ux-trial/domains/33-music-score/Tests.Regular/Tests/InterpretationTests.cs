using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class InterpretationTests {

    [Fact]
    public void A_whole_note_becomes_one_event_at_the_start() {
        // Arrange
        Score score = ScoreAt(60, D(1, 4), StaffOf(Bar(1, Note("C4", 1, 1))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        PerformedEvent performed = Assert.Single(events);
        Assert.Equal(P("C4"), performed.Pitch);
        Assert.Equal(D(0, 1), performed.Onset);
        Assert.Equal(D(1, 1), performed.Length);
        Assert.Equal(0.0, performed.StartSeconds);
        Assert.Equal(4.0, performed.DurationSeconds, precision: 9);
    }

    [Fact]
    public void Notes_follow_one_another_with_exact_onsets() {
        // Arrange
        Score score = ScoreAt(60, D(1, 4), StaffOf(Bar(1, Note("C4", 1, 4), Note("D4", 3, 8), Note("E4", 1, 8), Note("F4", 1, 4))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "C4@0/1+1/4", "D4@1/4+3/8", "E4@5/8+1/8", "F4@3/4+1/4" }, events.Select(e => $"{e.Pitch}@{e.Onset}+{e.Length}"));
        Assert.Equal(new[] { 0.0, 1.0, 2.5, 3.0 }, events.Select(e => e.StartSeconds));
        Assert.Equal(new[] { 1.0, 1.5, 0.5, 1.0 }, events.Select(e => e.DurationSeconds));
    }

    [Fact]
    public void Triplets_keep_their_exact_rational_onsets() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Note("C4", 1, 4), Note("D4", 1, 12), Note("E4", 1, 12), Note("F4", 1, 12), Note("G4", 1, 2))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { D(0, 1), D(1, 4), D(1, 3), D(5, 12), D(1, 2) }, events.Select(e => e.Onset));
    }

    [Fact]
    public void A_rest_is_silent_but_still_takes_its_time() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Rest(1, 2), Note("A4", 1, 4), Rest(1, 4))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        PerformedEvent performed = Assert.Single(events);
        Assert.Equal(D(1, 2), performed.Onset);
        Assert.Equal(D(1, 4), performed.Length);
    }

    [Fact]
    public void A_score_of_rests_has_no_event() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Rest(1, 1)), Bar(2, Rest(1, 2), Rest(1, 2))));

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
    public void A_note_takes_the_velocity_of_its_measure_dynamic(Dynamic dynamic, int expectedVelocity) {
        // Arrange
        Measure measure = Bar(1, "4/4", new[] { V(1, Note("C4", 1, 1)) }, dynamic);
        Score   score   = ScoreOf(StaffOf(measure));

        // Act
        PerformedEvent performed = Assert.Single(score.Interpret());

        // Assert
        Assert.Equal(expectedVelocity, performed.Velocity);
    }

    [Fact]
    public void Every_dynamic_has_a_velocity_in_the_documented_range() {
        // Arrange
        Dynamic[] dynamics = Enum.GetValues<Dynamic>();

        // Act
        int[] velocities = dynamics.Select(dynamic => ScoreOf(StaffOf(Bar(1, "4/4", new[] { V(1, Note("C4", 1, 1)) }, dynamic))).Interpret().Single().Velocity).ToArray();

        // Assert
        Assert.Equal(velocities.OrderBy(v => v), velocities);
        Assert.Equal(33, velocities.Min());
        Assert.Equal(112, velocities.Max());
    }

    [Fact]
    public void Each_measure_applies_its_own_dynamic() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, "4/4", new[] { V(1, Note("C4", 1, 1)) }, Dynamic.Piano),
                                      Bar(2, "4/4", new[] { V(1, Note("C4", 1, 1)) }, Dynamic.Forte)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { 49, 96 }, events.Select(e => e.Velocity));
    }

    [Fact]
    public void Onsets_accumulate_across_measures_of_different_meters() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, "3/4", new[] { V(1, Note("C4", 3, 4)) }),
                                      Bar(2, "6/8", new[] { V(1, Note("D4", 3, 8), Note("E4", 3, 8)) }),
                                      Bar(3, "2/2", new[] { V(1, Note("F4", 1, 1)) })));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { D(0, 1), D(3, 4), D(9, 8), D(3, 2) }, events.Select(e => e.Onset));
    }

    [Fact]
    public void A_tied_note_merges_with_the_note_it_is_tied_to() {
        // Arrange
        Score score = ScoreAt(60, D(1, 4), StaffOf(Bar(1, Rest(1, 4), Tied("C4", 1, 4), Note("C4", 1, 4), Note("D4", 1, 4))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "C4@1/4+1/2", "D4@3/4+1/4" }, events.Select(e => $"{e.Pitch}@{e.Onset}+{e.Length}"));
        Assert.Equal(1.0, events[0].StartSeconds, precision: 9);
        Assert.Equal(2.0, events[0].DurationSeconds, precision: 9);
    }

    [Fact]
    public void A_chain_of_ties_merges_into_one_event() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Tied("G3", 1, 4), Tied("G3", 1, 4), Tied("G3", 1, 4), Note("G3", 1, 4))));

        // Act
        PerformedEvent performed = Assert.Single(score.Interpret());

        // Assert
        Assert.Equal(D(1, 1), performed.Length);
    }

    [Fact]
    public void A_tie_across_a_barline_merges_into_one_event() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Rest(3, 4), Tied("C4", 1, 4)),
                                      Bar(2, Note("C4", 1, 2), Rest(1, 2))));

        // Act
        PerformedEvent performed = Assert.Single(score.Interpret());

        // Assert
        Assert.Equal(D(3, 4), performed.Onset);
        Assert.Equal(D(3, 4), performed.Length);
    }

    [Fact]
    public void A_tied_note_keeps_the_dynamic_of_the_measure_where_it_starts() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, "4/4", new[] { V(1, Rest(3, 4), Tied("C4", 1, 4)) }, Dynamic.Piano),
                                      Bar(2, "4/4", new[] { V(1, Note("C4", 1, 4), Rest(3, 4)) }, Dynamic.Fortissimo)));

        // Act
        PerformedEvent performed = Assert.Single(score.Interpret());

        // Assert
        Assert.Equal(49, performed.Velocity);
    }

    [Fact]
    public void A_tie_from_the_end_of_a_repeat_into_the_measure_after_it_merges_on_every_pass() {
        // Arrange: bar 1 ends on a tied G3; bar 2 starts on G3 and repeats back to bar 1
        Score score = ScoreOf(StaffOf(Bar(1, Rest(3, 4), Tied("G3", 1, 4)),
                                      Bar(2, "4/4", new[] { V(1, Note("G3", 1, 4), Rest(3, 4)) }, repeatStart: false, repeatEndTimes: 2, dynamic: null)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert: played 1 2 1 2, and the tie of each pass of bar 1 resolves onto the matching pass of bar 2
        Assert.Equal(new[] { "G3@3/4+1/2", "G3@11/4+1/2" }, events.Select(e => $"{e.Pitch}@{e.Onset}+{e.Length}"));
    }

    [Fact]
    public void A_tie_leaving_the_end_of_a_repeated_section_resolves_onto_its_start_on_the_next_pass() {
        // Arrange: bar 1 is G3 | G3 tied and repeats once, bar 2 starts on G3 too
        Measure repeated = Bar(1, "2/2", new[] { V(1, Note("G3", 1, 2), Tied("G3", 1, 2)) }, repeatEndTimes: 2);
        Measure after    = Bar(2, "2/2", new[] { V(1, Note("G3", 1, 1)) });
        Score   score    = ScoreOf(StaffOf(repeated, after));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert: the tie of pass one merges with pass two, the tie of pass two with bar 2
        Assert.Equal(new[] { "G3@0/1+1/2", "G3@1/2+1/1", "G3@3/2+3/2" }, events.Select(e => $"{e.Pitch}@{e.Onset}+{e.Length}"));
    }

    [Fact]
    public void Repeats_are_expanded_in_played_order() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Note("C4", 1, 1)),
                                      Bar(2, "4/4", new[] { V(1, Note("D4", 1, 1)) }, repeatStart: true),
                                      Bar(3, "4/4", new[] { V(1, Note("E4", 1, 1)) }, repeatEndTimes: 3),
                                      Bar(4, Note("F4", 1, 1))));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "C4", "D4", "E4", "D4", "E4", "D4", "E4", "F4" }, events.Select(e => e.Pitch.ToString()));
        Assert.Equal(Enumerable.Range(0, 8).Select(n => D(n, 1)), events.Select(e => e.Onset));
    }

    [Fact]
    public void The_seconds_of_a_repeated_note_follow_its_later_onset() {
        // Arrange
        Score score = ScoreAt(60, D(1, 4), StaffOf(Bar(1, "4/4", new[] { V(1, Note("C4", 1, 1)) }, repeatEndTimes: 2)));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { 0.0, 4.0 }, events.Select(e => e.StartSeconds));
    }

    [Fact]
    public void Voices_of_a_measure_sound_together() {
        // Arrange
        Measure measure = Bar(1, "4/4", new[] { V(2, Note("C3", 1, 1)),
                                                V(1, Note("E4", 1, 2), Note("G4", 1, 2)) });
        Score score = ScoreOf(StaffOf(measure));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert: ordered by onset, then voice
        Assert.Equal(new[] { "E4:1@0/1", "C3:2@0/1", "G4:1@1/2" }, events.Select(e => $"{e.Pitch}:{e.VoiceNumber}@{e.Onset}"));
    }

    [Fact]
    public void Staves_sound_together_and_are_ordered_by_position_in_the_score_not_by_name() {
        // Arrange
        Staff violin = StaffOf("Violin", Bar(1, Note("A4", 1, 1)));
        Staff cello  = StaffOf("Cello", Bar(1, Note("A2", 1, 1)));
        Score score  = ScoreOf(violin, cello);

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Violin", "Cello" }, events.Select(e => e.StaffName));
    }

    [Fact]
    public void At_the_same_onset_the_staff_comes_before_the_voice() {
        // Arrange
        Staff first  = StaffOf("First", Bar(1, "4/4", new[] { V(2, Note("C3", 1, 1)) }));
        Staff second = StaffOf("Second", Bar(1, "4/4", new[] { V(1, Note("C5", 1, 1)) }));
        Score score  = ScoreOf(first, second);

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "First:2", "Second:1" }, events.Select(e => $"{e.StaffName}:{e.VoiceNumber}"));
    }

    [Fact]
    public void Events_are_ordered_by_onset_across_staves() {
        // Arrange
        Staff upper = StaffOf("Upper", Bar(1, Rest(1, 2), Note("C5", 1, 2)));
        Staff lower = StaffOf("Lower", Bar(1, Note("C3", 1, 4), Rest(3, 4)));
        Score score = ScoreOf(upper, lower);

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "Lower", "Upper" }, events.Select(e => e.StaffName));
    }

    [Fact]
    public void A_voice_absent_from_a_measure_stays_silent_there_and_resumes_in_time() {
        // Arrange
        Measure first  = Bar(1, "4/4", new[] { V(1, Note("C4", 1, 1)), V(2, Note("C3", 1, 1)) });
        Measure second = Bar(2, "4/4", new[] { V(1, Note("D4", 1, 1)) });
        Measure third  = Bar(3, "4/4", new[] { V(1, Note("E4", 1, 1)), V(2, Note("E3", 1, 1)) });
        Score   score  = ScoreOf(StaffOf(first, second, third));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret().Where(e => e.VoiceNumber == 2).ToArray();

        // Assert
        Assert.Equal(new[] { D(0, 1), D(2, 1) }, events.Select(e => e.Onset));
    }

    [Fact]
    public void The_same_pitch_in_two_voices_stays_two_events() {
        // Arrange
        Measure measure = Bar(1, "4/4", new[] { V(1, Tied("C4", 1, 2), Note("C4", 1, 2)), V(2, Note("C4", 1, 1)) });
        Score   score   = ScoreOf(StaffOf(measure));

        // Act
        IReadOnlyList<PerformedEvent> events = score.Interpret();

        // Assert
        Assert.Equal(new[] { "C4:1@0/1+1/1", "C4:2@0/1+1/1" }, events.Select(e => $"{e.Pitch}:{e.VoiceNumber}@{e.Onset}+{e.Length}"));
    }

    [Fact]
    public void A_performed_event_carries_the_name_of_its_staff() {
        // Arrange
        string anyStaffName = Arbitrary.Staff().Generate().Name;
        Score  score        = ScoreOf(StaffOf(anyStaffName, Bar(1, Note("C4", 1, 1))));

        // Act
        PerformedEvent performed = Assert.Single(score.Interpret());

        // Assert
        Assert.Equal(anyStaffName, performed.StaffName);
    }

    [Fact]
    public void A_tie_onto_a_different_pitch_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Tied("C4", 1, 2), Note("D4", 1, 2))));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("does not resolve onto the same pitch", error.Message);
    }

    [Fact]
    public void A_tie_onto_a_rest_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Tied("C4", 1, 2), Rest(1, 2))));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_onto_the_same_pitch_in_another_octave_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Tied("C4", 1, 2), Note("C5", 1, 2))));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_onto_the_enharmonic_spelling_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Tied("C#4", 1, 2), Note("Db4", 1, 2))));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void A_tie_on_the_last_note_of_the_piece_is_refused() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Note("C4", 1, 2), Tied("C4", 1, 2))));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("tied to nothing", error.Message);
    }

    [Fact]
    public void A_tie_running_into_a_measure_where_the_voice_is_absent_is_refused() {
        // Arrange
        Measure first  = Bar(1, "4/4", new[] { V(1, Note("C4", 1, 2), Rest(1, 4), Tied("D4", 1, 4)), V(2, Note("C3", 1, 1)) });
        Measure second = Bar(2, "4/4", new[] { V(2, Note("C3", 1, 1)) });
        Score   score  = ScoreOf(StaffOf(first, second));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => score.Interpret());

        // Assert
        Assert.Contains("where that voice is absent", error.Message);
    }

    [Fact]
    public void A_tie_looping_back_through_an_end_repeat_onto_another_pitch_is_refused() {
        // Arrange: bar 1 ends on a tied D4 and repeats onto itself, whose first note is C4
        Score score = ScoreOf(StaffOf(Bar(1, "4/4", new[] { V(1, Note("C4", 1, 2), Tied("D4", 1, 2)) }, repeatEndTimes: 2),
                                      Bar(2, Note("D4", 1, 1))));

        // Assert
        Assert.Throws<InvalidOperationException>(() => score.Interpret());
    }

    [Fact]
    public void Interpreting_twice_gives_the_same_events() {
        // Arrange
        Score score = ScoreOf(StaffOf(Bar(1, Note("C4", 1, 2), Tied("D4", 1, 2)), Bar(2, Note("D4", 1, 1))));

        // Act
        string first  = Spell(score.Interpret());
        string second = Spell(score.Interpret());

        // Assert
        Assert.Equal(first, second);
        Assert.Equal("C4@0/1+1/2 D4@1/2+3/2", first);
    }

}
