using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class FabricTests {

    private static readonly IAny<string> AnySectionName = Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength);

    private static RowInstruction Row(int number, FabricSide side, params IRowElement[] elements) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side).WithElements(elements).Generate();
    }

    [Fact]
    public void A_fresh_fabric_presents_its_right_side_with_no_row_worked() {
        // Arrange
        int anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act
        Fabric fabric = new(anyCastOn);

        // Assert
        Assert.Equal(0, fabric.RowsWorked);
        Assert.Equal(FabricSide.RightSide, fabric.NextSide);
    }

    [Fact]
    public void Working_an_increase_row_leaves_more_stitches_on_the_needle() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(10);
        // "Row 1 (RS): *kfb; rep from * 10 times (20 sts)"
        RowInstruction row = Row(1, FabricSide.RightSide, new StitchRepeat([new StitchRun(StitchKind.KnitFrontAndBack, 1)], 10));

        // Act
        ExpandedRow expanded = fabric.Work(anySectionName, row);

        // Assert
        Assert.Equal(20, fabric.LiveStitches);
        Assert.Equal(1, fabric.RowsWorked);
        Assert.Equal(FabricSide.WrongSide, fabric.NextSide);
        Assert.Equal(10, expanded.StitchesBefore);
        Assert.Equal(20, expanded.StitchesAfter);
        Assert.Equal(10, expanded.Increases);
    }

    [Fact]
    public void An_expanded_row_counts_its_increases_and_decreases() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(12);
        // "Row 1 (RS): k1, *yo, k1, cdd, k1, yo; rep from * 2 times, k1": a lace row keeping 12 sts.
        RowInstruction row = Row(1, FabricSide.RightSide,
                                 new StitchRun(StitchKind.Knit, 1),
                                 new StitchRepeat([
                                     new StitchRun(StitchKind.YarnOver, 1),
                                     new StitchRun(StitchKind.Knit, 1),
                                     new StitchRun(StitchKind.CentralDoubleDecrease, 1),
                                     new StitchRun(StitchKind.Knit, 1),
                                     new StitchRun(StitchKind.YarnOver, 1),
                                 ], 2),
                                 new StitchRun(StitchKind.Knit, 1));

        // Act
        ExpandedRow expanded = fabric.Work(anySectionName, row);

        // Assert
        Assert.Equal(4, expanded.Increases);
        Assert.Equal(2, expanded.Decreases);
        Assert.Equal(12, expanded.StitchesAfter);
        Assert.Equal(12, expanded.Operations.Count);
    }

    [Fact]
    public void A_row_that_needs_more_stitches_than_the_needle_holds_is_refused() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(20);
        RowInstruction row = Row(1, FabricSide.RightSide, new StitchRun(StitchKind.Knit, 24));

        // Act
        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => fabric.Work(anySectionName, row));

        // Assert
        Assert.Equal(1, error.RowNumber);
        Assert.Equal(20, error.LiveStitches);
        Assert.Equal(24, error.RequiredStitches);
        Assert.Equal(0, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_worked_out_of_sequence_is_refused() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(8);
        RowInstruction row = Row(2, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 8));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work(anySectionName, row));

        // Assert
        Assert.Equal("Row 2 cannot be worked: row 1 is next.", error.Message);
    }

    [Fact]
    public void A_first_row_written_on_the_wrong_side_is_refused() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(8);
        RowInstruction row = Row(1, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 8));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work(anySectionName, row));

        // Assert
        Assert.IsNotType<StitchCountMismatchException>(error);
        Assert.Contains("WrongSide", error.Message);
    }

}
