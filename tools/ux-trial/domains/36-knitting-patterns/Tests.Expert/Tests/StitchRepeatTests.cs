namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class StitchRepeatTests {

    // A repeat whose runs are small enough that any number of times stays under the expansion limit.
    private static IReadOnlyList<StitchRun> AnyRuns() {
        return Any.StitchRepeat().Generate().Runs;
    }

    [Fact]
    public void A_repeat_without_runs_is_refused() {
        // Arrange
        int anyTimes = Any.Int32().Between(2, StitchRepeat.MaxTimes).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StitchRepeat([], anyTimes));

        // Assert
        Assert.Equal("runs", refusal.ParamName);
    }

    [Fact]
    public void A_repeat_without_a_run_list_is_refused() {
        int anyTimes = Any.Int32().Between(2, StitchRepeat.MaxTimes).Generate();

        Assert.Throws<ArgumentNullException>(() => new StitchRepeat(null!, anyTimes));
    }

    [Fact]
    public void A_repeat_holding_a_missing_run_is_refused() {
        // Arrange
        StitchRun anyRun   = Any.StitchRun().WithCount(c => c.LessThanOrEqualTo(3)).Generate();
        int       anyTimes = Any.Int32().Between(2, StitchRepeat.MaxTimes).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StitchRepeat([anyRun, null!], anyTimes));

        // Assert
        Assert.Equal("runs", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(StitchRepeat.MaxTimes + 1)]
    public void A_repeat_is_worked_between_two_and_a_thousand_times_and_refuses_any_other_number(int times) {
        // Arrange
        IReadOnlyList<StitchRun> anyRuns = AnyRuns();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRepeat(anyRuns, times));

        // Assert
        Assert.Equal("times", refusal.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(StitchRepeat.MaxTimes)]
    public void A_repeat_accepts_the_fewest_and_the_most_repetitions(int times) {
        // Arrange
        IReadOnlyList<StitchRun> anyRuns = AnyRuns();

        // Act
        StitchRepeat repeat = new(anyRuns, times);

        // Assert
        Assert.Equal(times, repeat.Times);
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void A_repeat_may_expand_to_ten_thousand_operations_and_not_one_more(int times, bool accepted) {
        // Arrange
        StitchRun fiveHundredStitches = new(StitchKind.Knit, 500);

        // Act
        Func<StitchRepeat> build = () => new StitchRepeat([fiveHundredStitches], times);

        // Assert
        if (accepted) {
            Assert.Equal(10_000, build().OperationCount);
        } else {
            ArgumentException refusal = Assert.Throws<ArgumentException>(build);
            Assert.Equal("times", refusal.ParamName);
        }
    }

    [Fact]
    public void The_expansion_limit_counts_the_operations_of_all_the_runs_together() {
        // Arrange
        StitchRun[] twoRunsOfFiveHundred = [new StitchRun(StitchKind.Knit, 500), new StitchRun(StitchKind.Purl, 500)];

        // Act + Assert
        Assert.Equal(10_000, new StitchRepeat(twoRunsOfFiveHundred, 10).OperationCount);
        Assert.Throws<ArgumentException>(() => new StitchRepeat(twoRunsOfFiveHundred, 11));
    }

    [Fact]
    public void A_ribbing_repeat_takes_and_gives_back_as_many_stitches_as_it_works() {
        // Arrange: "*k2, p2; rep from * 10 times"
        StitchRepeat ribbing = new([new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 2)], 10);

        // Act + Assert
        Assert.Equal(40, ribbing.Consumes);
        Assert.Equal(40, ribbing.Produces);
        Assert.Equal(40, ribbing.OperationCount);
    }

    [Fact]
    public void An_eyelet_repeat_takes_two_stitches_and_gives_two_back_every_round() {
        // Arrange: "*yo, k2tog; rep from * 8 times"
        StitchRepeat eyelets = new([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 8);

        // Act + Assert
        Assert.Equal(16, eyelets.Consumes);
        Assert.Equal(16, eyelets.Produces);
        Assert.Equal(16, eyelets.OperationCount);
    }

    [Fact]
    public void A_repeat_that_increases_gives_back_more_stitches_than_it_takes() {
        // Arrange: "*kfb, k2; rep from * 5 times"
        StitchRepeat increases = new([new StitchRun(StitchKind.KnitFrontAndBack, 1), new StitchRun(StitchKind.Knit, 2)], 5);

        // Act + Assert
        Assert.Equal(15, increases.Consumes);
        Assert.Equal(20, increases.Produces);
    }

    [Fact]
    public void A_repeat_expands_all_its_runs_in_order_once_per_round() {
        // Arrange: "*k2, p2; rep from * 2 times"
        StitchRepeat ribbing = new([new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 2)], 2);

        // Act
        List<StitchKind> operations = ribbing.Expand().ToList();

        // Assert
        Assert.Equal([StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Purl,
                      StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Purl], operations);
    }

    [Fact]
    public void A_repeat_is_not_changed_by_the_list_of_runs_it_was_built_from() {
        // Arrange
        List<StitchRun> runs         = [.. AnyRuns()];
        StitchRepeat    repeat       = new(runs, 2);
        int             operationsBefore = repeat.OperationCount;

        // Act
        runs.Add(new StitchRun(StitchKind.Knit, 1));

        // Assert
        Assert.Equal(operationsBefore, repeat.OperationCount);
    }

}
