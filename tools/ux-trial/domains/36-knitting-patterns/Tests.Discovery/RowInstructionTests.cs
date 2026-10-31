using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class RowInstructionTests {

    [Fact]
    public void A_row_sums_what_its_runs_and_repeats_consume_and_produce() {
        // Arrange
        // "k2, *yo, k2tog; rep from * 8 times, kfb, k1"
        IRowElement[] elements = [
            new StitchRun(StitchKind.Knit, 2),
            new StitchRepeat([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 8),
            new StitchRun(StitchKind.KnitFrontAndBack, 1),
            new StitchRun(StitchKind.Knit, 1),
        ];
        RowInstruction row = Any.RowInstruction().WithElements(elements).Generate();

        // Act
        int consumes = row.Consumes;
        int produces = row.Produces;

        // Assert
        Assert.Equal(20, consumes);
        Assert.Equal(21, produces);
    }

    [Fact]
    public void A_printed_count_that_disagrees_with_the_instructions_is_refused() {
        // Arrange
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();
        // "k2tog 10 times" leaves 10 stitches, the pattern prints 11.
        IRowElement[] elements = [new StitchRun(StitchKind.KnitTwoTogether, 10)];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new RowInstruction(anyRowNumber, anySide, elements, 11));

        // Assert
        Assert.Equal("expectedStitchCount", error.ParamName);
    }

    [Fact]
    public void A_printed_count_that_agrees_with_the_instructions_is_kept() {
        // Arrange
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();
        IRowElement[] elements = [new StitchRun(StitchKind.KnitTwoTogether, 10)];

        // Act
        RowInstruction row = new(anyRowNumber, anySide, elements, 10);

        // Assert
        Assert.Equal(10, row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_expands_its_elements_in_the_order_they_are_written() {
        // Arrange
        // "ssk, *k1; rep from * 2 times, k2tog" — a repeat of one stitch, as a pattern may write it.
        IRowElement[] elements = [
            new StitchRun(StitchKind.SlipSlipKnit, 1),
            new StitchRepeat([new StitchRun(StitchKind.Knit, 1)], 2),
            new StitchRun(StitchKind.KnitTwoTogether, 1),
        ];
        RowInstruction row = Any.RowInstruction().WithElements(elements).Generate();

        // Act
        StitchKind[] operations = row.Expand().ToArray();

        // Assert
        Assert.Equal(new[] { StitchKind.SlipSlipKnit, StitchKind.Knit, StitchKind.Knit, StitchKind.KnitTwoTogether }, operations);
    }

}
