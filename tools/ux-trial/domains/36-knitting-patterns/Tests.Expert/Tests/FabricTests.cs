namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class FabricTests {

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(KnittingPattern.MaxCastOn + 1)]
    public void A_cast_on_counts_between_one_and_two_thousand_stitches_and_refuses_any_other_count(int castOnCount) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Fabric(castOnCount));

        Assert.Equal("castOnCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(KnittingPattern.MaxCastOn)]
    public void A_cast_on_accepts_the_fewest_and_the_most_stitches(int castOnCount) {
        Fabric fabric = new(castOnCount);

        Assert.Equal(castOnCount, fabric.LiveStitches);
    }

    [Fact]
    public void A_fabric_right_after_the_cast_on_has_worked_no_row_and_presents_its_right_side() {
        // Arrange + Act
        Fabric anyFabric = Any.Fabric().Generate();

        // Assert
        Assert.Equal(0, anyFabric.RowsWorked);
        Assert.Equal(FabricSide.RightSide, anyFabric.NextSide);
    }

    [Fact]
    public void Working_a_row_moves_the_fabric_to_the_stitch_count_the_row_leaves() {
        // Arrange: "Row 1 (RS): k2tog x4, k4 (8 sts)" on 12 stitches
        Fabric         fabric = new(12);
        RowInstruction row    = new(1, FabricSide.RightSide, [new StitchRun(StitchKind.KnitTwoTogether, 4), new StitchRun(StitchKind.Knit, 4)], 8);

        // Act
        fabric.Work("Decrease", row);

        // Assert
        Assert.Equal(8, fabric.LiveStitches);
        Assert.Equal(1, fabric.RowsWorked);
    }

    [Fact]
    public void Working_a_row_reports_the_row_as_it_was_worked() {
        // Arrange
        Fabric         fabric = new(12);
        RowInstruction row    = new(1, FabricSide.RightSide, [new StitchRun(StitchKind.KnitTwoTogether, 4), new StitchRun(StitchKind.Knit, 4)], null);

        // Act
        ExpandedRow worked = fabric.Work("Decrease", row);

        // Assert
        Assert.Equal("Decrease", worked.SectionName);
        Assert.Equal(1, worked.RowNumber);
        Assert.Equal(FabricSide.RightSide, worked.Side);
        Assert.Equal(12, worked.StitchesBefore);
        Assert.Equal(8, worked.StitchesAfter);
        Assert.Equal(4, worked.Decreases);
        Assert.Equal(row.Expand(), worked.Operations);
    }

    [Fact]
    public void A_row_with_yarn_overs_makes_the_fabric_wider() {
        // Arrange: "yo x2, k5" on 5 stitches
        Fabric         fabric = new(5);
        RowInstruction row    = new(1, FabricSide.RightSide, [new StitchRun(StitchKind.YarnOver, 2), new StitchRun(StitchKind.Knit, 5)], null);

        // Act
        fabric.Work("Lace panel", row);

        // Assert
        Assert.Equal(7, fabric.LiveStitches);
    }

    [Fact]
    public void Each_row_worked_turns_the_fabric_over() {
        // Arrange
        Fabric anyFabric = Any.Fabric().Generate();

        // Act + Assert
        anyFabric.Work("Body", Rows.PlainRow(1, anyFabric.LiveStitches));
        Assert.Equal(FabricSide.WrongSide, anyFabric.NextSide);

        anyFabric.Work("Body", Rows.PlainRow(2, anyFabric.LiveStitches));
        Assert.Equal(FabricSide.RightSide, anyFabric.NextSide);

        anyFabric.Work("Body", Rows.PlainRow(3, anyFabric.LiveStitches));
        Assert.Equal(FabricSide.WrongSide, anyFabric.NextSide);
        Assert.Equal(3, anyFabric.RowsWorked);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void The_first_row_worked_is_row_one(int rowNumber) {
        // Arrange
        Fabric anyFabric = Any.Fabric().Generate();
        RowInstruction row = new(rowNumber, Rows.SideOf(rowNumber), Rows.Chunks(StitchKind.Knit, anyFabric.LiveStitches), null);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyFabric.Work("Body", row));
    }

    [Fact]
    public void A_row_cannot_be_worked_twice() {
        // Arrange
        Fabric anyFabric = Any.Fabric().Generate();
        anyFabric.Work("Body", Rows.PlainRow(1, anyFabric.LiveStitches));

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyFabric.Work("Body", Rows.PlainRow(1, anyFabric.LiveStitches)));
    }

    [Fact]
    public void A_row_cannot_be_skipped() {
        // Arrange
        Fabric anyFabric = Any.Fabric().Generate();
        anyFabric.Work("Body", Rows.PlainRow(1, anyFabric.LiveStitches));

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyFabric.Work("Body", Rows.PlainRow(3, anyFabric.LiveStitches)));
    }

    [Fact]
    public void The_first_row_is_worked_on_the_right_side() {
        // Arrange
        Fabric         anyFabric = Any.Fabric().Generate();
        RowInstruction row       = new(1, FabricSide.WrongSide, Rows.Chunks(StitchKind.Knit, anyFabric.LiveStitches), null);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyFabric.Work("Body", row));
    }

    [Fact]
    public void The_second_row_is_worked_on_the_wrong_side() {
        // Arrange
        Fabric anyFabric = Any.Fabric().Generate();
        anyFabric.Work("Body", Rows.PlainRow(1, anyFabric.LiveStitches));
        RowInstruction sameSideAgain = new(2, FabricSide.RightSide, Rows.Chunks(StitchKind.Knit, anyFabric.LiveStitches), null);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyFabric.Work("Body", sameSideAgain));
    }

    [Fact]
    public void A_row_that_needs_other_stitches_than_the_needle_holds_is_refused_with_both_counts() {
        // Arrange: the needle holds 12 stitches, the row knits 10
        Fabric         fabric = new(12);
        RowInstruction row    = Rows.PlainRow(1, 10);

        // Act
        StitchCountMismatchException mismatch = Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", row));

        // Assert
        Assert.Equal(1, mismatch.RowNumber);
        Assert.Equal(12, mismatch.LiveStitches);
        Assert.Equal(10, mismatch.RequiredStitches);
    }

    [Fact]
    public void A_row_that_starts_on_more_stitches_than_the_needle_holds_is_refused_too() {
        Fabric fabric = new(12);

        StitchCountMismatchException mismatch = Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Rows.PlainRow(1, 14)));

        Assert.Equal(12, mismatch.LiveStitches);
        Assert.Equal(14, mismatch.RequiredStitches);
    }

    [Fact]
    public void A_refused_row_leaves_the_fabric_as_it_was_and_the_right_row_can_still_be_worked() {
        // Arrange
        Fabric fabric = new(12);
        Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Rows.PlainRow(1, 10)));

        // Act
        ExpandedRow worked = fabric.Work("Body", Rows.PlainRow(1, 12));

        // Assert
        Assert.Equal(1, worked.RowNumber);
        Assert.Equal(1, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_refused_for_its_side_leaves_the_fabric_as_it_was() {
        // Arrange
        Fabric         fabric       = new(12);
        RowInstruction wrongSideRow = new(1, FabricSide.WrongSide, Rows.Chunks(StitchKind.Knit, 12), null);
        Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", wrongSideRow));

        // Act + Assert
        Assert.Equal(0, fabric.RowsWorked);
        Assert.Equal(12, fabric.LiveStitches);
        Assert.Equal(FabricSide.RightSide, fabric.NextSide);
    }

    [Fact]
    public void A_row_out_of_sequence_is_reported_as_such_before_its_stitch_count_is_looked_at() {
        // Arrange: row 2 is not next, and it would not fit the 12 stitches either
        Fabric fabric = new(12);

        // Act: an exact InvalidOperationException, not the StitchCountMismatchException that derives from it
        Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", Rows.PlainRow(2, 10)));
    }

    [Fact]
    public void A_row_on_the_wrong_side_is_reported_as_such_before_its_stitch_count_is_looked_at() {
        Fabric         fabric       = new(12);
        RowInstruction wrongSideRow = new(1, FabricSide.WrongSide, Rows.Chunks(StitchKind.Knit, 10), null);

        Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", wrongSideRow));
    }

    [Fact]
    public void Working_a_missing_row_is_refused() {
        Fabric anyFabric = Any.Fabric().Generate();

        Assert.Throws<ArgumentNullException>(() => anyFabric.Work("Body", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_row_is_worked_for_a_named_section(string blankName) {
        Fabric anyFabric = Any.Fabric().Generate();
        RowInstruction row = Rows.PlainRow(1, anyFabric.LiveStitches);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyFabric.Work(blankName, row));

        Assert.Equal("sectionName", refusal.ParamName);
    }

}
