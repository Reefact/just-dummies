using KnittingPatterns.Domain;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

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
    public void Each_stitch_takes_and_leaves_its_own_number_of_stitches(StitchKind kind, int consumed, int produced) {
        // Arrange
        // (the stitch is the case)

        // Act
        (int consumes, int produces) = (kind.Consumes(), kind.Produces());

        // Assert
        Assert.Equal((consumed, produced), (consumes, produces));
    }

    [Fact]
    public void A_run_multiplies_its_stitch_arithmetic_by_its_count() {
        // Arrange
        StitchRun threeK2Tog = K2Tog(3);

        // Act
        (int consumes, int produces, int operations) = (threeK2Tog.Consumes, threeK2Tog.Produces, threeK2Tog.OperationCount);

        // Assert
        Assert.Equal((6, 3, 3), (consumes, produces, operations));
    }

    [Fact]
    public void A_bracketed_repeat_expands_its_motif_in_order_as_many_times_as_written() {
        // Arrange
        StitchRepeat k2P2ThreeTimes = Repeat(3, K(2), P(2));

        // Act
        IEnumerable<StitchKind> operations = k2P2ThreeTimes.Expand();

        // Assert
        StitchKind k = StitchKind.Knit, p = StitchKind.Purl;
        Assert.Equal([k, k, p, p, k, k, p, p, k, k, p, p], operations);
    }

    [Fact]
    public void A_lace_repeat_keeps_the_count_because_each_yarn_over_pays_back_a_decrease() {
        // Arrange
        StitchRepeat yoK2TogEightTimes = Repeat(8, Yo(), K2Tog());

        // Act
        (int consumes, int produces) = (yoK2TogEightTimes.Consumes, yoK2TogEightTimes.Produces);

        // Assert
        Assert.Equal((16, 16), (consumes, produces));
    }

}
