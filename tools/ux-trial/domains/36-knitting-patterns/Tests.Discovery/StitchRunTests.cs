using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class StitchRunTests {

    [Fact]
    public void A_run_of_plain_stitches_consumes_and_produces_one_stitch_per_repeat() {
        // Arrange
        StitchRun run = new StitchRun(StitchKind.Knit, 12);

        // Act + Assert
        Assert.Equal(StitchKind.Knit, run.Kind);
        Assert.Equal(12, run.Count);
        Assert.Equal(12, run.Consumes);
        Assert.Equal(12, run.Produces);
        Assert.Equal(12, run.OperationCount);
    }

    [Fact]
    public void A_run_of_decreases_consumes_more_than_it_produces() {
        StitchRun run = new StitchRun(StitchKind.KnitTwoTogether, 3);

        Assert.Equal(6, run.Consumes);
        Assert.Equal(3, run.Produces);
        Assert.Equal(3, run.OperationCount);
    }

    [Fact]
    public void A_run_of_increases_produces_more_than_it_consumes() {
        StitchRun run = new StitchRun(StitchKind.KnitFrontAndBack, 4);

        Assert.Equal(4, run.Consumes);
        Assert.Equal(8, run.Produces);
    }

    [Fact]
    public void A_run_of_yarn_overs_consumes_nothing() {
        StitchRun run = new StitchRun(StitchKind.YarnOver, 5);

        Assert.Equal(0, run.Consumes);
        Assert.Equal(5, run.Produces);
    }

    [Fact]
    public void A_run_expands_into_one_operation_per_stitch() {
        StitchRun run = new StitchRun(StitchKind.Purl, 3);

        Assert.Equal(new[] { StitchKind.Purl, StitchKind.Purl, StitchKind.Purl }, run.Expand());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void A_run_accepts_the_shortest_and_the_longest_count(int count) {
        // The kind plays no part in whether a count is accepted.
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        StitchRun run = new StitchRun(anyKind, count);

        Assert.Equal(count, run.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void A_run_refuses_a_count_outside_1_to_500(int count) {
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun(anyKind, count));

        Assert.Equal("count", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    [InlineData(99)]
    public void A_run_refuses_a_kind_the_enum_does_not_declare(int undefinedKind) {
        int anyCount = Any.Int32().Between(1, 500).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun((StitchKind)undefinedKind, anyCount));

        Assert.Equal("kind", error.ParamName);
    }

    [Fact]
    public void A_single_stitch_prints_as_its_kind_alone() {
        Assert.Equal("Knit", new StitchRun(StitchKind.Knit, 1).ToString());
    }

    [Fact]
    public void A_longer_run_prints_its_count() {
        Assert.Equal("Purl x3", new StitchRun(StitchKind.Purl, 3).ToString());
    }

}
