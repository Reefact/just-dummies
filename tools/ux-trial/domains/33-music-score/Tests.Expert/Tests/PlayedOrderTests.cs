using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class PlayedOrderTests {

    [Fact]
    public void Without_a_repeat_measures_are_played_as_written() {
        AssertPlayed(measureCount: 4, repeats: [], expected: [1, 2, 3, 4]);
    }

    [Fact]
    public void An_end_repeat_without_a_start_goes_back_to_the_beginning() {
        AssertPlayed(measureCount: 4, repeats: [Until(2, times: 2)], expected: [1, 2, 1, 2, 3, 4]);
    }

    [Fact]
    public void An_end_repeat_goes_back_to_the_latest_start_repeat() {
        AssertPlayed(measureCount: 4, repeats: [Between(2, 3, times: 2)], expected: [1, 2, 3, 2, 3, 4]);
    }

    [Theory]
    [InlineData(2, new[] { 1, 2, 3, 2, 3, 4 })]
    [InlineData(3, new[] { 1, 2, 3, 2, 3, 2, 3, 4 })]
    [InlineData(4, new[] { 1, 2, 3, 2, 3, 2, 3, 2, 3, 4 })]
    [InlineData(8, new[] { 1, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 4 })]
    public void The_repeated_section_is_played_as_many_times_as_the_end_repeat_says(int times, int[] expected) {
        AssertPlayed(measureCount: 4, repeats: [Between(2, 3, times)], expected: expected);
    }

    [Fact]
    public void A_single_measure_repeat_replays_that_measure() {
        AssertPlayed(measureCount: 4, repeats: [Between(3, 3, times: 2)], expected: [1, 2, 3, 3, 4]);
    }

    [Fact]
    public void A_second_end_repeat_goes_back_to_the_measure_after_the_first_one() {
        AssertPlayed(measureCount: 5, repeats: [Until(2, times: 2), Until(4, times: 2)], expected: [1, 2, 1, 2, 3, 4, 3, 4, 5]);
    }

    [Fact]
    public void A_start_repeat_after_an_end_repeat_opens_the_next_section() {
        AssertPlayed(measureCount: 5, repeats: [Until(1, times: 2), Between(3, 4, times: 2)], expected: [1, 1, 2, 3, 4, 3, 4, 5]);
    }

    [Fact]
    public void A_whole_piece_can_be_repeated() {
        AssertPlayed(measureCount: 3, repeats: [Between(1, 3, times: 2)], expected: [1, 2, 3, 1, 2, 3]);
    }

    [Fact]
    public void A_start_repeat_which_is_the_first_measure_behaves_like_no_start_at_all() {
        AssertPlayed(measureCount: 3, repeats: [Between(1, 2, times: 3)], expected: [1, 2, 1, 2, 1, 2, 3]);
    }

    [Fact]
    public void The_played_length_counts_every_pass_of_a_repeat() {
        // Arrange
        Score score = ScoreOf(meters: [FourFour, ThreeFour, ThreeFour, FourFour], repeats: [Between(2, 3, times: 3)]);

        // Act
        Duration length = score.PlayedLength();

        // Assert
        Assert.Equal(D(13, 2), length);
    }

    [Fact]
    public void The_played_length_without_a_repeat_is_the_sum_of_the_measures() {
        // Arrange
        Score score = ScoreOf(meters: [FourFour, ThreeFour, new TimeSignature(6, 8)], repeats: []);

        // Assert
        Assert.Equal(D(5, 2), score.PlayedLength());
    }

    [Fact]
    public void The_played_duration_in_seconds_follows_the_tempo() {
        // Arrange
        Tempo quarterEquals120 = new(120, D(1, 4));
        Score score            = ScoreOf(meters: [FourFour, FourFour], repeats: [Until(2, times: 2)], tempo: quarterEquals120);

        // Act
        double seconds = score.PlayedSeconds();

        // Assert
        Assert.Equal(8.0, seconds, 10);
    }

    [Fact]
    public void A_dotted_beat_unit_stretches_the_played_duration() {
        // Arrange
        Tempo dottedQuarterEquals60 = new(60, D(3, 8));
        Score score                 = ScoreOf(meters: [FourFour], repeats: [], tempo: dottedQuarterEquals60);

        // Assert
        Assert.Equal(8.0 / 3.0, score.PlayedSeconds(), 10);
    }

    private static Repeat Between(int from, int to, int times) => new(from, to, times);

    private static Repeat Until(int to, int times) => new(null, to, times);

    private static void AssertPlayed(int measureCount, Repeat[] repeats, int[] expected) {
        // Arrange
        Score score = ScoreOf(meters: Enumerable.Repeat(FourFour, measureCount).ToArray(), repeats: repeats);

        // Act
        IReadOnlyList<int> order = score.PlayedMeasureOrder();

        // Assert
        Assert.Equal(expected, order);
    }

    private static Score ScoreOf(TimeSignature[] meters, Repeat[] repeats, Tempo? tempo = null) {
        List<Measure> measures = new();
        for (int number = 1; number <= meters.Length; number++) {
            int times = repeats.SingleOrDefault(repeat => repeat.To == number)?.Times ?? 0;

            measures.Add(Arbitrary.Measure()
                                  .WithNumber(number)
                                  .WithTimeSignature(meters[number - 1])
                                  .WithRepeatStart(repeats.Any(repeat => repeat.From == number))
                                  .WithRepeatEndTimes(times)
                                  .Generate());
        }

        AnyScore score = Arbitrary.Score().WithStaves([StaffOf("Staff", measures.ToArray())]);

        return (tempo is null ? score : score.WithTempo(tempo)).Generate();
    }

    private sealed record Repeat(int? From, int To, int Times);

}
