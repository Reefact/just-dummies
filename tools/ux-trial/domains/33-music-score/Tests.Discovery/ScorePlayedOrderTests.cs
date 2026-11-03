using JustDummies;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Discovery.Scores;

namespace MusicScore.Tests.Discovery;

public sealed class ScorePlayedOrderTests {

    private static Score ScoreWithBars(params Measure[] bars) {
        return ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", bars));
    }

    [Fact]
    public void Without_repeats_measures_are_played_as_written() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1), RepeatBar(2), RepeatBar(3), RepeatBar(4));

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void An_end_repeat_replays_back_to_the_latest_start_repeat() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1), RepeatBar(2, repeatStart: true), RepeatBar(3, repeatEndTimes: 2), RepeatBar(4));

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 2, 3, 4 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void An_end_repeat_without_a_start_replays_from_the_beginning() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1), RepeatBar(2, repeatEndTimes: 3), RepeatBar(3));

        // Assert
        Assert.Equal(new[] { 1, 2, 1, 2, 1, 2, 3 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void An_end_repeat_without_a_start_replays_from_the_measure_after_the_previous_end_repeat() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1, repeatEndTimes: 2), RepeatBar(2), RepeatBar(3, repeatEndTimes: 2));

        // Assert
        Assert.Equal(new[] { 1, 1, 2, 3, 2, 3 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void A_measure_can_open_and_close_a_repeat_at_once() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1), RepeatBar(2, repeatStart: true, repeatEndTimes: 3), RepeatBar(3));

        // Assert
        Assert.Equal(new[] { 1, 2, 2, 2, 3 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void Two_separate_repeated_sections_are_each_expanded_with_their_own_count() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1, repeatStart: true, repeatEndTimes: 2), RepeatBar(2, repeatStart: true, repeatEndTimes: 3));

        // Assert
        Assert.Equal(new[] { 1, 1, 2, 2, 2 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void A_section_is_played_as_many_times_as_the_end_repeat_says_up_to_eight() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1, repeatEndTimes: Measure.MaxRepeatTimes));

        // Assert
        Assert.Equal(Enumerable.Repeat(1, 8), score.PlayedMeasureOrder());
    }

    [Fact]
    public void A_repeated_section_of_several_measures_is_replayed_whole_each_time() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1, repeatStart: true), RepeatBar(2), RepeatBar(3), RepeatBar(4, repeatEndTimes: 3));

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 }, score.PlayedMeasureOrder());
    }

    [Fact]
    public void The_played_length_counts_every_pass_of_a_repeat_in_the_length_of_its_own_meter() {
        // Arrange: 2/4 + (3/4 repeated twice) + 6/8
        Measure first  = Any.Measure().WithNumber(1).WithTimeSignature(new TimeSignature(2, 4)).WithRepeatStart(false).WithRepeatEndTimes(0).Generate();
        Measure second = Any.Measure().WithNumber(2).WithTimeSignature(new TimeSignature(3, 4)).WithRepeatStart(true).WithRepeatEndTimes(2).Generate();
        Measure third  = Any.Measure().WithNumber(3).WithTimeSignature(new TimeSignature(6, 8)).WithRepeatStart(false).WithRepeatEndTimes(0).Generate();
        Score   score  = ScoreWithBars(first, second, third);

        // Act
        Duration length = score.PlayedLength();

        // Assert: 1/2 + 3/4 + 3/4 + 3/4
        Assert.Equal(new Duration(11, 4), length);
    }

    [Fact]
    public void A_score_without_repeats_plays_as_long_as_it_is_written() {
        // Arrange
        Score score = ScoreWithBars(RepeatBar(1), RepeatBar(2), RepeatBar(3));

        // Assert
        Assert.Equal(new Duration(3, 1), score.PlayedLength());
    }

    [Fact]
    public void The_played_duration_in_seconds_follows_the_tempo() {
        // Arrange: quarter = 120, a repeated 4/4 measure played twice is eight quarters, four seconds
        Score score = ScoreOf(QuarterEquals(120), StaffOf("Violin", RepeatBar(1, repeatEndTimes: 2)));

        // Assert
        Assert.Equal(4.0, score.PlayedSeconds(), 10);
    }

}
