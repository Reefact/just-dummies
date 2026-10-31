using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Tests.Rows;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class FabricTests {

    private static readonly IAny<string> AnySectionName = Any.String().Alpha().WithLengthBetween(3, 20);

    [Fact]
    public void Working_a_decrease_row_leaves_the_stitches_it_produces_on_the_needle() {
        // Arrange
        Fabric fabric = new(20);

        // Act
        fabric.Work(AnySectionName.Generate(), Row(1, K(16), K2Tog(2)));

        // Assert
        Assert.Equal((18, 1, FabricSide.WrongSide), (fabric.LiveStitches, fabric.RowsWorked, fabric.NextSide));
    }

    [Fact]
    public void An_expanded_row_counts_its_increases_and_decreases() {
        // Arrange — "k1, kfb, yo, k2tog, ssk, cdd, k1" on 10 stitches
        Fabric fabric = new(10);

        // Act
        ExpandedRow expanded = fabric.Work(AnySectionName.Generate(), Row(1, K(1), Kfb(1), Yo(1), K2Tog(1), Ssk(1), Cdd(1), K(1)));

        // Assert
        Assert.Equal((10, 8, 2, 3), (expanded.StitchesBefore, expanded.StitchesAfter, expanded.Increases, expanded.Decreases));
    }

    [Fact]
    public void A_row_worked_out_of_sequence_is_refused_and_leaves_the_fabric_untouched() {
        // Arrange
        StitchRun      anyStitches = Arbitrary.StitchRun().WithKind(k => k.Except(StitchKind.YarnOver)).Generate();
        RowInstruction anyRow3     = Arbitrary.RowInstruction().WithRowNumber(3).WithSide(FabricSide.RightSide).WithElements([anyStitches]).Generate();
        Fabric         fabric      = new(anyRow3.Consumes);

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(AnySectionName.Generate(), anyRow3));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(0, fabric.RowsWorked);
    }

    [Fact]
    public void A_first_row_written_for_the_wrong_side_is_refused() {
        // Arrange
        StitchRun      anyStitches = Arbitrary.StitchRun().WithKind(k => k.Except(StitchKind.YarnOver)).Generate();
        RowInstruction anyWrongSideRow1 = Arbitrary.RowInstruction().WithRowNumber(1).WithSide(FabricSide.WrongSide).WithElements([anyStitches]).Generate();
        Fabric         fabric      = new(anyWrongSideRow1.Consumes);

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(AnySectionName.Generate(), anyWrongSideRow1));

        // Assert
        InvalidOperationException invalidOperation = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("presents its RightSide", invalidOperation.Message);
    }

    [Fact]
    public void A_row_needing_more_stitches_than_the_needle_holds_reports_both_counts() {
        // Arrange
        Fabric fabric = new(20);
        fabric.Work(AnySectionName.Generate(), Row(1, Rep(5, K(2), K2Tog(1))));

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(AnySectionName.Generate(), Row(2, P(20))));

        // Assert
        StitchCountMismatchException mismatch = Assert.IsType<StitchCountMismatchException>(refusal);
        Assert.Equal((2, 15, 20), (mismatch.RowNumber, mismatch.LiveStitches, mismatch.RequiredStitches));
        Assert.Equal(15, fabric.LiveStitches);
    }

    [Fact]
    public void An_expanded_row_whose_operations_do_not_consume_its_starting_stitches_is_refused() {
        // Arrange
        string     anySectionName = AnySectionName.Generate();
        FabricSide anySide        = Any.Enum<FabricSide>().Generate();
        StitchKind[] operations   = [StitchKind.Knit, StitchKind.KnitTwoTogether];

        // Act
        Exception? refusal = Record.Exception(() => new ExpandedRow(anySectionName, 4, anySide, stitchesBefore: 4, operations));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("consumes 3 stitches but starts with 4", argumentException.Message);
    }

}
