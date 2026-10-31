using JustDummies;

using KnittingPatterns.Domain;
using KnittingPatterns.Tests.Expert.Recipes;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

public sealed class FabricTests {

    private static readonly AnyString AnySectionName = Any.String().Alpha().WithLengthBetween(3, PatternSection.MaxNameLength);

    [Fact]
    public void An_increase_row_leaves_one_more_stitch_per_increase_for_the_next_row() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(20);
        // "Row 1 (RS): k1, kfb, k16, kfb, k1 (22 sts)"
        RowInstruction increaseRow = new(1, FabricSide.RightSide, [K(1), Kfb(), K(16), Kfb(), K(1)], 22);

        // Act
        ExpandedRow worked = fabric.Work(anySectionName, increaseRow);

        // Assert
        Assert.Equal((20, 22, 2, 0), (worked.StitchesBefore, worked.StitchesAfter, worked.Increases, worked.Decreases));
        Assert.Equal((22, 1, FabricSide.WrongSide), (fabric.LiveStitches, fabric.RowsWorked, fabric.NextSide));
    }

    [Fact]
    public void A_crown_decrease_row_takes_a_third_of_the_stitches_off() {
        // Arrange
        string anySectionName = AnySectionName.Generate();
        Fabric fabric         = new(24);
        // "Row 1 (RS): *k1, k2tog; rep from * 8 times (16 sts)"
        RowInstruction crownRow = Written(1, Repeat(8, K(1), K2Tog()));

        // Act
        ExpandedRow worked = fabric.Work(anySectionName, crownRow);

        // Assert
        Assert.Equal((16, 0, 8, 16), (worked.StitchesAfter, worked.Increases, worked.Decreases, worked.Operations.Count));
    }

    [Fact]
    public void A_row_worked_out_of_sequence_is_refused() {
        // Arrange
        string         anySectionName = AnySectionName.Generate();
        Fabric         fabric         = new(24);
        RowInstruction anyRow2        = Plain(2, 24).Generate();

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(anySectionName, anyRow2));

        // Assert
        InvalidOperationException outOfSequence = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("row 1 is next", outOfSequence.Message);
    }

    [Fact]
    public void A_first_row_written_for_the_wrong_side_is_refused() {
        // Arrange
        string        anySectionName = AnySectionName.Generate();
        Fabric        fabric         = new(24);
        IRowElement[] anyPlainRow    = Stitches.NeutralRunsOver(24).Generate().ToArray();

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(anySectionName, new RowInstruction(1, FabricSide.WrongSide, anyPlainRow, null)));

        // Assert
        InvalidOperationException wrongSide = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("the fabric presents its RightSide", wrongSide.Message);
    }

    [Fact]
    public void A_row_needing_more_stitches_than_the_needle_holds_is_refused_and_the_fabric_is_left_as_it_was() {
        // Arrange
        string         anySectionName = AnySectionName.Generate();
        Fabric         fabric         = new(24);
        RowInstruction anyRowFor26    = Plain(1, 26).Generate();

        // Act
        Exception? refusal = Record.Exception(() => fabric.Work(anySectionName, anyRowFor26));

        // Assert
        StitchCountMismatchException mismatch = Assert.IsType<StitchCountMismatchException>(refusal);
        Assert.Equal((1, 24, 26), (mismatch.RowNumber, mismatch.LiveStitches, mismatch.RequiredStitches));
        Assert.Equal((24, 0), (fabric.LiveStitches, fabric.RowsWorked));
    }

}
