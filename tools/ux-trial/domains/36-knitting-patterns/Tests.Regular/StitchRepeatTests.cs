using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Rows;

namespace KnittingPatterns.Tests.Regular;

public sealed class StitchRepeatTests {

    [Fact]
    public void A_repeat_adds_up_the_runs_of_one_pass_and_multiplies_by_its_times() {
        // Arrange: "*k2, k2tog, yo; rep from * 4 times"
        StitchRepeat repeat = Rep(4, K(2), Of(StitchKind.KnitTwoTogether), Of(StitchKind.YarnOver));

        // Assert
        Assert.Equal(4, repeat.Times);
        Assert.Equal((2 + 2 + 0) * 4, repeat.Consumes);
        Assert.Equal((2 + 1 + 1) * 4, repeat.Produces);
        Assert.Equal((2 + 1 + 1) * 4, repeat.OperationCount);
    }

    [Fact]
    public void A_repeat_expands_pass_after_pass_in_written_order() {
        // Arrange
        StitchRepeat repeat = Rep(3, K(2), P(1));

        // Act
        List<StitchKind> expanded = repeat.Expand().ToList();

        // Assert
        Assert.Equal(
            new[] {
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl,
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl,
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl,
            },
            expanded);
    }

    [Fact]
    public void A_repeat_does_not_follow_a_run_list_changed_after_it_was_written() {
        // Arrange
        List<StitchRun> runs   = new() { K(2) };
        StitchRepeat    repeat = new(runs, 2);

        // Act
        runs.Add(P(5));

        // Assert
        Assert.Single(repeat.Runs);
        Assert.Equal(4, repeat.OperationCount);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(StitchRepeat.MaxTimes)]
    public void A_repeat_accepts_the_ends_of_its_range(int times) {
        // Arrange
        StitchRun anyRun = new AnyStitchRun().WithCount(1).Generate();

        // Act
        StitchRepeat repeat = new(new[] { anyRun }, times);

        // Assert
        Assert.Equal(times, repeat.Times);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(StitchRepeat.MaxTimes + 1)]
    public void A_repeat_refuses_a_number_of_times_outside_two_to_a_thousand(int times) {
        // Arrange
        StitchRun anyRun = new AnyStitchRun().WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("times", () => new StitchRepeat(new[] { anyRun }, times));
    }

    [Fact]
    public void A_repeat_refuses_to_expand_past_ten_thousand_operations() {
        // Arrange: 21 x 500 = 10 500
        StitchRun[] runs = { K(500) };

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new StitchRepeat(runs, 21));

        // Assert
        Assert.Equal("times", error.ParamName);
        Assert.Contains("10500", error.Message);
    }

    [Fact]
    public void A_repeat_accepts_exactly_ten_thousand_operations() {
        // Act: 20 x 500 = 10 000
        StitchRepeat repeat = Rep(20, K(500));

        // Assert
        Assert.Equal(StitchRepeat.MaxOperations, repeat.OperationCount);
    }

    [Fact]
    public void A_repeat_counts_every_run_of_its_pass_against_the_operation_limit() {
        // Arrange: 2 runs x 500 x 11 = 11 000, though neither run alone is near the limit
        StitchRun[] runs = { K(500), P(500) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new StitchRepeat(runs, 11));
    }

    [Fact]
    public void A_repeat_refuses_an_empty_list_of_runs() {
        // Arrange
        int anyTimes = Any.Int32().Between(2, 20).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("runs", () => new StitchRepeat(Array.Empty<StitchRun>(), anyTimes));
    }

    [Fact]
    public void A_repeat_refuses_a_missing_list_of_runs() {
        // Arrange
        int anyTimes = Any.Int32().Between(2, 20).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("runs", () => new StitchRepeat(null!, anyTimes));
    }

    [Fact]
    public void A_repeat_refuses_a_missing_run() {
        // Arrange
        StitchRun anyRun   = new AnyStitchRun().Generate();
        int       anyTimes = Any.Int32().Between(2, 20).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("runs", () => new StitchRepeat(new[] { anyRun, null! }, anyTimes));
    }

}
