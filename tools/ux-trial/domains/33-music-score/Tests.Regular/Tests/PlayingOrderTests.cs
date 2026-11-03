using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class PlayingOrderTests {

    [Theory]
    [InlineData("4/4,4/4,4/4", "1 2 3")]
    [InlineData("4/4", "1")]
    // an end repeat with no start repeat goes back to the beginning
    [InlineData("4/4,4/4]2,4/4", "1 2 1 2 3")]
    [InlineData("4/4]2,4/4", "1 1 2")]
    [InlineData("4/4]3", "1 1 1")]
    [InlineData("4/4]8", "1 1 1 1 1 1 1 1")]
    // a start repeat is where the end repeat goes back to
    [InlineData("4/4,[4/4,4/4]2,4/4", "1 2 3 2 3 4")]
    [InlineData("4/4,[4/4,4/4]3,4/4", "1 2 3 2 3 2 3 4")]
    [InlineData("[4/4]2", "1 1")]
    [InlineData("4/4,[4/4]2,4/4", "1 2 2 3")]
    [InlineData("[4/4,4/4,4/4]2", "1 2 3 1 2 3")]
    // without a start repeat, the section begins after the previous end repeat
    [InlineData("4/4]2,4/4,4/4]2,4/4", "1 1 2 3 2 3 4")]
    [InlineData("4/4,4/4]2,4/4,4/4]3", "1 2 1 2 3 4 3 4 3 4")]
    // a start repeat after an end repeat takes over
    [InlineData("4/4]2,[4/4,4/4]2", "1 1 2 3 2 3")]
    // a later start repeat bounds the section even when an earlier measure could start it
    [InlineData("4/4,4/4,[4/4,4/4]2", "1 2 3 4 3 4")]
    public void The_played_order_follows_the_repeat_barlines(string bars, string expectedOrder) {
        // Arrange
        Score score = Structure.ScoreOf(bars.Split(','));

        // Act
        string order = string.Join(" ", score.PlayedMeasureOrder());

        // Assert
        Assert.Equal(expectedOrder, order);
    }

    [Fact]
    public void The_played_order_is_the_written_order_when_there_is_no_repeat() {
        // Arrange
        Score score = Structure.ScoreOf("4/4", "3/4", "6/8", "4/4");

        // Act
        System.Collections.Generic.IReadOnlyList<int> order = score.PlayedMeasureOrder();

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, order);
    }

    [Fact]
    public void The_played_order_is_read_on_the_first_staff_which_shares_its_barlines_with_the_others() {
        // Arrange
        Staff violin = Structure.StaffOf("Violin", "4/4", "[4/4]2");
        Staff cello  = Structure.StaffOf("Cello", "4/4", "[4/4]2");
        Score score  = Arbitrary.Score().WithStaves(new[] { violin, cello }).Generate();

        // Act
        string order = string.Join(" ", score.PlayedMeasureOrder());

        // Assert
        Assert.Equal("1 2 2", order);
    }

    [Theory]
    [InlineData("4/4,4/4,4/4", 3, 1)]
    [InlineData("4/4,[4/4,4/4]2,4/4", 6, 1)]
    [InlineData("3/4,3/4]2", 3, 1)]
    [InlineData("6/8]3", 9, 4)]
    [InlineData("4/4,3/4]2,6/8", 17, 4)]
    public void The_played_length_counts_every_repetition_of_every_measure(string bars, long expectedNumerator, long expectedDenominator) {
        // Arrange
        Score score = Structure.ScoreOf(bars.Split(','));

        // Act
        Duration length = score.PlayedLength();

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), length);
    }

    [Fact]
    public void The_played_length_of_a_single_measure_is_its_meter_whatever_the_staff() {
        // Arrange
        Score score = Structure.ScoreOf("7/8");

        // Assert
        Assert.Equal(D(7, 8), score.PlayedLength());
    }

    [Theory]
    [InlineData(60.0, 1, 4, "4/4", 4.0)]
    [InlineData(120.0, 1, 4, "4/4", 2.0)]
    [InlineData(60.0, 1, 4, "4/4,[4/4]2", 12.0)]
    [InlineData(60.0, 3, 8, "6/8,6/8", 4.0)]
    [InlineData(90.0, 1, 2, "3/4]2", 2.0)]
    public void The_played_duration_in_seconds_follows_the_tempo(double beatsPerMinute, long unitNumerator, long unitDenominator, string bars, double expectedSeconds) {
        // Arrange
        Score score = Structure.ScoreAt(new Tempo(beatsPerMinute, D(unitNumerator, unitDenominator)), bars.Split(','));

        // Act
        double seconds = score.PlayedSeconds();

        // Assert
        Assert.Equal(expectedSeconds, seconds, precision: 9);
    }

}
