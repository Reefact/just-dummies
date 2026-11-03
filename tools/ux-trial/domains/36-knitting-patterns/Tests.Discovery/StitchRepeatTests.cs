using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class StitchRepeatTests {

    // A run of one stitch keeps any repeat count well under the expansion limit.
    private static readonly IAny<StitchRun> SingleStitchRun = Any.StitchRun().WithCount(1);

    [Fact]
    public void A_repeat_expands_its_runs_in_order_as_many_times_as_asked() {
        // *k2, p1; rep from * 3 times
        StitchRepeat repeat = new StitchRepeat(new[] { new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 1) }, 3);

        Assert.Equal(new[] {
            StitchKind.Knit, StitchKind.Knit, StitchKind.Purl,
            StitchKind.Knit, StitchKind.Knit, StitchKind.Purl,
            StitchKind.Knit, StitchKind.Knit, StitchKind.Purl
        }, repeat.Expand());
    }

    [Fact]
    public void A_repeat_counts_stitches_and_operations_across_all_its_runs_and_repetitions() {
        // *k2tog x2, yo x2; rep from * 3 times
        StitchRepeat repeat = new StitchRepeat(new[] { new StitchRun(StitchKind.KnitTwoTogether, 2), new StitchRun(StitchKind.YarnOver, 2) }, 3);

        Assert.Equal(12, repeat.Consumes);
        Assert.Equal(12, repeat.Produces);
        Assert.Equal(12, repeat.OperationCount);
        Assert.Equal(3, repeat.Times);
    }

    [Fact]
    public void A_repeat_that_decreases_consumes_more_than_it_produces() {
        StitchRepeat repeat = new StitchRepeat(new[] { new StitchRun(StitchKind.CentralDoubleDecrease, 1) }, 4);

        Assert.Equal(12, repeat.Consumes);
        Assert.Equal(4, repeat.Produces);
        Assert.Equal(4, repeat.OperationCount);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1000)]
    public void A_repeat_accepts_the_fewest_and_the_most_repetitions(int times) {
        StitchRun anyRun = SingleStitchRun.Generate();

        StitchRepeat repeat = new StitchRepeat(new[] { anyRun }, times);

        Assert.Equal(times, repeat.Times);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(1001)]
    public void A_repeat_refuses_fewer_than_two_or_more_than_a_thousand_repetitions(int times) {
        StitchRun anyRun = SingleStitchRun.Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRepeat(new[] { anyRun }, times));

        Assert.Equal("times", error.ParamName);
    }

    [Fact]
    public void A_repeat_may_expand_to_exactly_ten_thousand_operations() {
        StitchRepeat repeat = new StitchRepeat(new[] { new StitchRun(StitchKind.Knit, 500) }, 20);

        Assert.Equal(10_000, repeat.OperationCount);
    }

    [Theory]
    [InlineData(500, 21)]
    [InlineData(101, 100)]
    public void A_repeat_that_would_expand_beyond_ten_thousand_operations_is_refused(int runCount, int times) {
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new StitchRepeat(new[] { new StitchRun(anyKind, runCount) }, times));

        Assert.Equal("times", error.ParamName);
    }

    [Fact]
    public void The_expansion_limit_counts_every_run_of_the_repeat() {
        // 60 + 41 operations per repetition, a hundred times: 10 100.
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        Assert.Throws<ArgumentException>(() => new StitchRepeat(new[] { new StitchRun(anyKind, 60), new StitchRun(anyKind, 41) }, 100));
    }

    [Fact]
    public void A_repeat_without_runs_is_refused() {
        int anyTimes = Any.Int32().Between(2, 1000).Generate();

        Assert.Throws<ArgumentNullException>(() => new StitchRepeat(null!, anyTimes));
        Assert.Throws<ArgumentException>(() => new StitchRepeat(Array.Empty<StitchRun>(), anyTimes));
    }

    [Fact]
    public void A_repeat_cannot_hold_a_null_run() {
        int      anyTimes = Any.Int32().Between(2, 1000).Generate();
        StitchRun anyRun   = SingleStitchRun.Generate();

        Assert.Throws<ArgumentException>(() => new StitchRepeat(new StitchRun?[] { anyRun, null }!, anyTimes));
    }

    [Fact]
    public void A_repeat_keeps_its_own_copy_of_the_runs() {
        List<StitchRun> runs = new List<StitchRun> { new StitchRun(StitchKind.Knit, 1) };
        StitchRepeat    repeat = new StitchRepeat(runs, 5);

        runs.Add(new StitchRun(StitchKind.Purl, 1));

        Assert.Single(repeat.Runs);
        Assert.Equal(5, repeat.OperationCount);
    }

}
