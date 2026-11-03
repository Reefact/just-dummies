namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class StitchRunTests {

    [Fact]
    public void A_run_of_an_undefined_stitch_kind_is_refused() {
        // Arrange
        StitchKind undefinedKind = (StitchKind)99;
        int        anyCount      = Any.Int32().Between(1, StitchRun.MaxCount).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun(undefinedKind, anyCount));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(StitchRun.MaxCount + 1)]
    [InlineData(int.MaxValue)]
    public void A_run_counts_between_one_and_five_hundred_stitches_and_refuses_any_other_count(int count) {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun(anyKind, count));

        // Assert
        Assert.Equal("count", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(StitchRun.MaxCount)]
    public void A_run_accepts_the_shortest_and_the_longest_count(int count) {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        StitchRun run = new(anyKind, count);

        // Assert
        Assert.Equal(count, run.Count);
    }

    [Theory]
    [InlineData(StitchKind.Knit, 12, 12, 12)]
    [InlineData(StitchKind.KnitTwoTogether, 3, 6, 3)]
    [InlineData(StitchKind.SlipSlipKnit, 5, 10, 5)]
    [InlineData(StitchKind.CentralDoubleDecrease, 2, 6, 2)]
    [InlineData(StitchKind.KnitFrontAndBack, 4, 4, 8)]
    [InlineData(StitchKind.YarnOver, 5, 0, 5)]
    public void A_run_takes_and_gives_back_the_stitches_of_each_of_its_repetitions(StitchKind kind, int count, int consumed, int produced) {
        // Arrange
        StitchRun run = new(kind, count);

        // Act + Assert
        Assert.Equal(consumed, run.Consumes);
        Assert.Equal(produced, run.Produces);
    }

    [Fact]
    public void A_run_counts_one_operation_per_repetition_whatever_its_kind() {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();   // the kind is not what is counted

        StitchRun run = new(anyKind, 7);

        // Act + Assert
        Assert.Equal(7, run.OperationCount);
    }

    [Fact]
    public void A_run_expands_into_one_operation_per_repetition_of_its_kind() {
        // Arrange
        StitchRun run = new(StitchKind.Purl, 3);

        // Act
        List<StitchKind> operations = run.Expand().ToList();

        // Assert
        Assert.Equal([StitchKind.Purl, StitchKind.Purl, StitchKind.Purl], operations);
    }

    [Theory]
    [InlineData(StitchKind.Knit, 1, "Knit")]
    [InlineData(StitchKind.Purl, 12, "Purl x12")]
    [InlineData(StitchKind.KnitTwoTogether, 2, "KnitTwoTogether x2")]
    public void A_run_prints_its_kind_and_only_prints_a_multiplier_above_one(StitchKind kind, int count, string expected) {
        Assert.Equal(expected, new StitchRun(kind, count).ToString());
    }

}
