using JustDummies;

using MusicScore.Domain;
using MusicScore.Tests.Expert.Arbitrary;

using Xunit;

using static MusicScore.Tests.Expert.Notation;

namespace MusicScore.Tests.Expert;

/// <summary>Written order is not played order. What each measure holds is arbitrary; its barlines are the point.</summary>
public sealed class PlayedOrderTests {

    private static readonly Func<AnyMeasure, AnyMeasure> AnyBar = measure => measure;

    [Fact]
    public void Without_repeats_measures_are_played_as_written() {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(AnyBar, AnyBar, AnyBar, AnyBar).Generate();

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3, 4], order);
    }

    [Fact]
    public void An_end_repeat_with_no_start_repeat_goes_back_to_the_beginning() {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(AnyBar, m => m.ClosingRepeat(2), AnyBar).Generate();

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 3], order);
    }

    [Fact]
    public void An_end_repeat_plays_its_section_as_many_times_as_it_says() {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(AnyBar, m => m.ClosingRepeat(3)).Generate();

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 1, 2], order);
    }

    [Fact]
    public void A_start_repeat_is_where_the_section_comes_back_to() {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(AnyBar, m => m.OpeningRepeat(), m => m.ClosingRepeat(2), AnyBar).Generate();

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 3, 2, 3, 4], order);
    }

    [Fact]
    public void A_second_end_repeat_goes_back_only_to_the_measure_after_the_first() {
        // Arrange
        Score anyScore = Any.Score().WithMeasures(AnyBar, m => m.ClosingRepeat(2), AnyBar, m => m.ClosingRepeat(2)).Generate();

        // Act
        IReadOnlyList<int> order = anyScore.PlayedMeasureOrder();

        // Assert
        Assert.Equal([1, 2, 1, 2, 3, 4, 3, 4], order);
    }

    [Fact]
    public void The_played_length_counts_every_pass_through_a_repeat_exactly() {
        // Arrange
        Score anyScore = Any.Score()
                            .WithMeasures(m => m.WithTimeSignature(ThreeFour),
                                          m => m.WithTimeSignature(TwoFour).ClosingRepeat(2))
                            .Generate();

        // Act
        Duration played = anyScore.PlayedLength();

        // Assert
        Assert.Equal(new Duration(5, 2), played);
    }

    [Fact]
    public void The_played_seconds_follow_the_tempo_through_the_repeats() {
        // Arrange
        Score anyScore = Any.Score()
                            .WithTempo(new Tempo(120, Quarter))
                            .WithMeasures(m => m.WithTimeSignature(CommonTime).ClosingRepeat(2), m => m.WithTimeSignature(CommonTime))
                            .Generate();

        // Act
        double seconds = anyScore.PlayedSeconds();

        // Assert
        Assert.Equal(6.0, seconds, precision: 12);
    }

}
