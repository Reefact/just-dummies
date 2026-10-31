using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Tests.Rows;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class StitchTests {

    [Theory]
    [InlineData(StitchKind.Knit, 4, 4)]
    [InlineData(StitchKind.Purl, 4, 4)]
    [InlineData(StitchKind.Slip, 4, 4)]
    [InlineData(StitchKind.YarnOver, 0, 4)]
    [InlineData(StitchKind.KnitFrontAndBack, 4, 8)]
    [InlineData(StitchKind.KnitTwoTogether, 8, 4)]
    [InlineData(StitchKind.SlipSlipKnit, 8, 4)]
    [InlineData(StitchKind.CentralDoubleDecrease, 12, 4)]
    public void A_run_of_four_stitches_consumes_and_produces_four_times_its_stitch(StitchKind kind, int consumes, int produces) {
        // Arrange
        StitchRun run = new(kind, 4);

        // Act
        (int Consumes, int Produces) arithmetic = (run.Consumes, run.Produces);

        // Assert
        Assert.Equal((consumes, produces), arithmetic);
    }

    [Fact]
    public void A_repeat_works_its_runs_in_order_as_many_times_as_written() {
        // Arrange
        StitchRepeat ribbing = Rep(3, K(2), P(2));

        // Act
        StitchKind[] operations = ribbing.Expand().ToArray();

        // Assert
        StitchKind k = StitchKind.Knit;
        StitchKind p = StitchKind.Purl;
        Assert.Equal([k, k, p, p, k, k, p, p, k, k, p, p], operations);
    }

    [Fact]
    public void A_lace_repeat_of_yarn_overs_and_decreases_keeps_the_stitch_count() {
        // Arrange
        StitchRepeat eyelets = Rep(8, Yo(1), K2Tog(1));

        // Act
        (int Consumes, int Produces, int Operations) counts = (eyelets.Consumes, eyelets.Produces, eyelets.OperationCount);

        // Assert
        Assert.Equal((16, 16, 16), counts);
    }

    [Fact]
    public void A_repeat_expanding_beyond_ten_thousand_operations_is_refused() {
        // Arrange
        StitchRun[] thousandStitches = [K(500), P(500)];

        // Act
        Exception? refusal = Record.Exception(() => new StitchRepeat(thousandStitches, times: 11));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("11000", argumentException.Message);
    }

}
