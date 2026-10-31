using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Regular.Notes;

namespace MusicScore.Tests.Regular;

public sealed class PlaybackTests {

    [Fact, Reproducible]
    public void Without_repeats_measures_are_played_in_written_order() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1), AnyBar(2), AnyBar(3)));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3], order);
    }

    [Fact, Reproducible]
    public void An_end_repeat_goes_back_to_the_latest_start_repeat() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1), AnyBar(2).WithRepeatStart(true), AnyBar(3).WithRepeatEndTimes(2), AnyBar(4)));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3, 2, 3, 4], order);
    }

    [Fact, Reproducible]
    public void An_end_repeat_without_a_start_goes_back_to_the_beginning() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1), AnyBar(2).WithRepeatEndTimes(2), AnyBar(3)));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 3], order);
    }

    [Fact, Reproducible]
    public void A_second_end_repeat_goes_back_to_the_measure_after_the_first_one() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithRepeatEndTimes(2), AnyBar(2), AnyBar(3).WithRepeatEndTimes(2)));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 1, 2, 3, 2, 3], order);
    }

    [Fact, Reproducible]
    public void A_repeat_marked_three_times_plays_its_section_three_times() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithRepeatStart(true), AnyBar(2).WithRepeatEndTimes(3)));

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 1, 2], order);
    }

    [Fact, Reproducible]
    public void The_played_length_counts_every_repeated_measure() {
        // Arrange
        Score anyScore = AnyScoreOf(AnyStaffOf(AnyBar(1).WithTimeSignature(ThreeFour), AnyBar(2).WithRepeatEndTimes(2)));

        // Act
        Duration length = anyScore.PlayedLength();

        // Assert
        Assert.Equal(new Duration(7, 2), length);
    }

    [Fact, Reproducible]
    public void The_played_seconds_follow_the_tempo() {
        // Arrange
        Score anyScore = Arbitrary.Score()
                                  .WithTempo(new Tempo(60, Quarter))
                                  .WithStaves([AnyStaffOf(AnyBar(1), AnyBar(2).WithRepeatEndTimes(2))])
                                  .Generate();

        // Act
        double seconds = anyScore.PlayedSeconds();

        // Assert
        Assert.Equal(16.0, seconds, precision: 10);
    }

}
