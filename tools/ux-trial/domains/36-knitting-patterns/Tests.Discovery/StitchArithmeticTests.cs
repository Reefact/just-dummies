using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class StitchArithmeticTests {

    [Theory]
    [InlineData(StitchKind.Knit, 1, 1)]
    [InlineData(StitchKind.Purl, 1, 1)]
    [InlineData(StitchKind.Slip, 1, 1)]
    [InlineData(StitchKind.YarnOver, 0, 1)]
    [InlineData(StitchKind.KnitFrontAndBack, 1, 2)]
    [InlineData(StitchKind.KnitTwoTogether, 2, 1)]
    [InlineData(StitchKind.SlipSlipKnit, 2, 1)]
    [InlineData(StitchKind.CentralDoubleDecrease, 3, 1)]
    public void Each_stitch_takes_and_gives_its_own_number_of_stitches(StitchKind kind, int consumes, int produces) {
        // Arrange
        // Act
        int actualConsumes = kind.Consumes();
        int actualProduces = kind.Produces();

        // Assert
        Assert.Equal(consumes, actualConsumes);
        Assert.Equal(produces, actualProduces);
    }

    [Fact]
    public void A_run_of_k2tog_consumes_twice_its_count_and_produces_its_count() {
        // Arrange
        StitchRun run = new(StitchKind.KnitTwoTogether, 6);

        // Act
        int consumes = run.Consumes;
        int produces = run.Produces;

        // Assert
        Assert.Equal(12, consumes);
        Assert.Equal(6, produces);
    }

    [Fact]
    public void A_run_expands_to_one_operation_per_stitch() {
        // Arrange
        StitchRun run = new(StitchKind.Purl, 4);

        // Act
        StitchKind[] operations = run.Expand().ToArray();

        // Assert
        Assert.Equal(new[] { StitchKind.Purl, StitchKind.Purl, StitchKind.Purl, StitchKind.Purl }, operations);
        Assert.Equal(4, run.OperationCount);
    }

    [Fact]
    public void A_run_longer_than_a_single_instruction_may_express_is_refused() {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRun(anyKind, 501));

        // Assert
        Assert.Equal("count", error.ParamName);
    }

    [Fact]
    public void A_lace_repeat_keeps_the_stitch_count_while_doubling_the_operations() {
        // Arrange
        // "*yo, k2tog; rep from * 8 times"
        StitchRepeat repeat = new([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 8);

        // Act
        int consumes       = repeat.Consumes;
        int produces       = repeat.Produces;
        int operationCount = repeat.OperationCount;

        // Assert
        Assert.Equal(16, consumes);
        Assert.Equal(16, produces);
        Assert.Equal(16, operationCount);
    }

    [Fact]
    public void A_repeat_expands_its_runs_in_working_order_each_time() {
        // Arrange
        // "*k2, p1; rep from * 2 times"
        StitchRepeat repeat = new([new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 1)], 2);

        // Act
        StitchKind[] operations = repeat.Expand().ToArray();

        // Assert
        Assert.Equal(new[] { StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Knit, StitchKind.Knit, StitchKind.Purl },
                     operations);
    }

    [Fact]
    public void A_repeat_expanding_beyond_ten_thousand_operations_is_refused() {
        // Arrange
        // "*k500; rep from * 21 times" describes 10 500 operations.
        StitchRun[] runs = [new StitchRun(StitchKind.Knit, 500)];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new StitchRepeat(runs, 21));

        // Assert
        Assert.Equal("times", error.ParamName);
        Assert.Contains("10500", error.Message);
    }

    [Fact]
    public void A_repeat_worked_only_once_is_not_a_repeat() {
        // Arrange
        StitchRun[] anyRuns = Any.ArrayOf(new AnyStitchRun()).WithCountBetween(1, 3).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new StitchRepeat(anyRuns, 1));

        // Assert
        Assert.Equal("times", error.ParamName);
    }

}
