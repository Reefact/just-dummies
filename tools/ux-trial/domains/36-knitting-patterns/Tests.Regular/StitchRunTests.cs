using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

public sealed class StitchRunTests {

    [Fact]
    public void A_run_keeps_its_kind_and_its_count() {
        // Act
        StitchRun run = new(StitchKind.Purl, 12);

        // Assert
        Assert.Equal(StitchKind.Purl, run.Kind);
        Assert.Equal(12, run.Count);
        Assert.Equal(12, run.OperationCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(StitchRun.MaxCount)]
    public void A_run_accepts_the_ends_of_its_range(int count) {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        StitchRun run = new(anyKind, count);

        // Assert
        Assert.Equal(count, run.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(StitchRun.MaxCount + 1)]
    public void A_run_refuses_a_count_just_outside_its_range(int count) {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun(anyKind, count));

        // Assert
        Assert.Equal("count", error.ParamName);
    }

    [Fact]
    public void A_run_refuses_a_count_below_one() {
        // Arrange
        StitchKind anyKind  = Any.Enum<StitchKind>().Generate();
        int        anyCount = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("count", () => new StitchRun(anyKind, anyCount));
    }

    [Fact]
    public void A_run_refuses_a_count_above_the_longest_run() {
        // Arrange
        StitchKind anyKind  = Any.Enum<StitchKind>().Generate();
        int        anyCount = Any.Int32().GreaterThan(StitchRun.MaxCount).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("count", () => new StitchRun(anyKind, anyCount));
    }

    [Fact]
    public void A_run_refuses_a_stitch_kind_the_pattern_language_does_not_declare() {
        // Arrange
        int anyCount = Any.Int32().Between(1, StitchRun.MaxCount).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("kind", () => new StitchRun((StitchKind)99, anyCount));
    }

    [Theory]
    [InlineData(StitchKind.Knit, 12, 12, 12)]
    [InlineData(StitchKind.KnitFrontAndBack, 5, 5, 10)]
    [InlineData(StitchKind.KnitTwoTogether, 3, 6, 3)]
    [InlineData(StitchKind.CentralDoubleDecrease, 4, 12, 4)]
    [InlineData(StitchKind.YarnOver, 7, 0, 7)]
    public void A_run_multiplies_what_its_stitch_consumes_and_produces_by_its_count(StitchKind kind, int count, int consumes, int produces) {
        // Act
        StitchRun run = new(kind, count);

        // Assert
        Assert.Equal(consumes, run.Consumes);
        Assert.Equal(produces, run.Produces);
    }

    [Fact]
    public void A_run_expands_to_one_operation_per_stitch() {
        // Act
        StitchRun run = new(StitchKind.SlipSlipKnit, 3);

        // Assert
        Assert.Equal(new[] { StitchKind.SlipSlipKnit, StitchKind.SlipSlipKnit, StitchKind.SlipSlipKnit }, run.Expand());
    }

    [Fact]
    public void A_run_of_one_stitch_prints_its_kind_alone_and_a_longer_run_prints_its_count() {
        Assert.Equal("Knit", new StitchRun(StitchKind.Knit, 1).ToString());
        Assert.Equal("Purl x4", new StitchRun(StitchKind.Purl, 4).ToString());
    }

}
