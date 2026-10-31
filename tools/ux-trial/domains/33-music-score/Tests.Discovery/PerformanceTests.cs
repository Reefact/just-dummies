using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class PlayedOrderTests {

    [Fact]
    public void Without_repeats_measures_are_played_as_written() {
        // Arrange
        Score anyScore = Bars.SingleStaff(Bars.Plain(1), Bars.Plain(2), Bars.Plain(3));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void An_end_repeat_without_a_start_replays_from_the_beginning() {
        // Arrange
        Score anyScore = Bars.SingleStaff(Bars.Plain(1), Bars.Plain(2, repeatEndTimes: 2), Bars.Plain(3));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 3], order);
    }

    [Fact]
    public void An_end_repeat_replays_back_to_the_latest_start_repeat_as_many_times_as_marked() {
        // Arrange
        Score anyScore = Bars.SingleStaff(Bars.Plain(1), Bars.Plain(2, repeatStart: true), Bars.Plain(3, repeatEndTimes: 3), Bars.Plain(4));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3, 2, 3, 2, 3, 4], order);
    }

    [Fact]
    public void A_second_end_repeat_without_a_start_replays_from_the_measure_after_the_previous_end() {
        // Arrange
        Score anyScore = Bars.SingleStaff(Bars.Plain(1, repeatEndTimes: 2), Bars.Plain(2), Bars.Plain(3, repeatEndTimes: 2));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 1, 2, 3, 2, 3], order);
    }

    [Fact]
    public void The_played_length_counts_every_repeated_measure() {
        // Arrange
        TimeSignature threeFour = new TimeSignature(3, 4);
        Score anyScore = Bars.SingleStaff(
            Any.Measure().WithNumber(1).WithTimeSignature(threeFour).WithRepeatStart(false).WithRepeatEndTimes(0).Generate(),
            Any.Measure().WithNumber(2).WithTimeSignature(threeFour).WithRepeatStart(false).WithRepeatEndTimes(2).Generate());

        // Act
        Duration played = anyScore.PlayedLength();

        // Assert
        Assert.Equal(new Duration(3, 1), played);   // four measures of 3/4
    }

    [Fact]
    public void A_four_four_measure_at_120_quarters_per_minute_plays_for_two_seconds() {
        // Arrange
        Measure anyCommonTimeBar = Any.Measure().WithNumber(1).WithTimeSignature(new TimeSignature(4, 4)).WithRepeatEndTimes(0).WithRepeatStart(false).Generate();
        Score   anyScore         = Any.Score()
                                      .WithTempo(new Tempo(120, new Duration(1, 4)))
                                      .WithStaves([Any.Staff().WithMeasures([anyCommonTimeBar]).Generate()])
                                      .Generate();

        // Act
        double seconds = anyScore.PlayedSeconds();

        // Assert
        Assert.Equal(2.0, seconds, 10);
    }

}

public sealed class InterpretationTests {

    private static readonly TimeSignature TwoFour = new TimeSignature(2, 4);
    private static readonly Duration      Quarter = new Duration(1, 4);

    [Fact]
    public void Rests_are_silent_and_notes_start_at_their_exact_onset() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = SingleVoice(Bar(1, VoiceEntry.Rest(Quarter), Note(anyPitch)));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        PerformedEvent single = Assert.Single(events);
        Assert.Equal(Quarter, single.Onset);
        Assert.Equal(Quarter, single.Length);
    }

    [Fact]
    public void Notes_tied_across_a_barline_sound_as_one_note() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = SingleVoice(
            Bar(1, Note(Any.Pitch().Generate()), Note(anyPitch, tied: true)),
            Bar(2, Note(anyPitch), Note(Any.Pitch().Generate())));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal(Quarter,           events[1].Onset);
        Assert.Equal(new Duration(1, 2), events[1].Length);
    }

    [Fact]
    public void A_repeated_measure_is_performed_again_after_its_first_pass() {
        // Arrange
        Score anyScore = SingleVoice(Bar(1, Note(Any.Pitch().Generate()), Note(Any.Pitch().Generate()), repeatEndTimes: 2));

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([new Duration(0, 1), Quarter, new Duration(1, 2), new Duration(3, 4)], events.Select(performed => performed.Onset));
    }

    [Fact]
    public void A_tie_onto_a_different_pitch_cannot_be_performed() {
        // Arrange
        List<Pitch> anyTwoPitches = Any.ListOf(Any.Pitch()).WithCount(2).Distinct().Generate();
        Score anyScore = SingleVoice(
            Bar(1, Note(Any.Pitch().Generate()), Note(anyTwoPitches[0], tied: true)),
            Bar(2, Note(anyTwoPitches[1]), Note(Any.Pitch().Generate())));

        // Act
        Func<IReadOnlyList<PerformedEvent>> interpret = anyScore.Interpret;

        // Assert
        Assert.Throws<InvalidOperationException>(interpret);
    }

    [Fact]
    public void A_tie_on_the_last_note_of_the_piece_cannot_be_performed() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Score anyScore = SingleVoice(Bar(1, Note(Any.Pitch().Generate()), Note(anyPitch, tied: true)));

        // Act
        Func<IReadOnlyList<PerformedEvent>> interpret = anyScore.Interpret;

        // Assert
        Assert.Throws<InvalidOperationException>(interpret);
    }

    [Fact]
    public void A_forte_measure_is_performed_at_velocity_96() {
        // Arrange
        Voice anyVoice   = Any.Voice().WithNumber(1).WithEntries([Note(Any.Pitch().Generate()), Note(Any.Pitch().Generate())]).Generate();
        Measure forteBar = Any.Measure().WithNumber(1).WithTimeSignature(TwoFour).WithDynamic(Dynamic.Forte).WithVoices([anyVoice])
                              .WithRepeatStart(false).WithRepeatEndTimes(0).Generate();
        Score anyScore = Bars.SingleStaff(forteBar);

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.All(events, performed => Assert.Equal(96, performed.Velocity));
    }

    [Fact]
    public void Simultaneous_staves_are_ordered_by_onset_then_by_staff() {
        // Arrange
        Staff anyUpper = Any.Staff().WithName("Upper").WithMeasures([Bar(1, Note(Any.Pitch().Generate()), Note(Any.Pitch().Generate()))]).Generate();
        Staff anyLower = Any.Staff().WithName("Lower").WithMeasures([Bar(1, HalfNote(Any.Pitch().Generate()))]).Generate();
        Score anyScore = Any.Score().WithStaves([anyUpper, anyLower]).Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal(["Upper", "Lower", "Upper"], events.Select(performed => performed.StaffName));
        Assert.Equal([new Duration(0, 1), new Duration(0, 1), Quarter], events.Select(performed => performed.Onset));
    }

    [Fact]
    public void Onsets_in_seconds_follow_the_tempo() {
        // Arrange
        Score anyScore = Any.Score()
                            .WithTempo(new Tempo(60, Quarter))
                            .WithStaves([Any.Staff().WithMeasures([Bar(1, Note(Any.Pitch().Generate()), Note(Any.Pitch().Generate()))]).Generate()])
                            .Generate();

        // Act
        IReadOnlyList<PerformedEvent> events = anyScore.Interpret();

        // Assert
        Assert.Equal([0.0, 1.0], events.Select(performed => performed.StartSeconds));
    }

    private static VoiceEntry Note(Pitch pitch, bool tied = false) {
        return new VoiceEntry(Quarter, pitch, tied);
    }

    private static VoiceEntry HalfNote(Pitch pitch) {
        return new VoiceEntry(new Duration(1, 2), pitch, false);
    }

    /// <summary>A 2/4 measure holding one voice; its dynamic does not matter to the test.</summary>
    private static Measure Bar(int number, VoiceEntry first, VoiceEntry? second = null, int repeatEndTimes = 0) {
        VoiceEntry[] entries = second is null ? [first] : [first, second];
        Voice        voice   = Any.Voice().WithNumber(1).WithEntries(entries).Generate();

        return Any.Measure().WithNumber(number).WithTimeSignature(TwoFour).WithVoices([voice])
                  .WithRepeatStart(false).WithRepeatEndTimes(repeatEndTimes).Generate();
    }

    private static Score SingleVoice(params Measure[] measures) {
        return Bars.SingleStaff(measures);
    }

}
