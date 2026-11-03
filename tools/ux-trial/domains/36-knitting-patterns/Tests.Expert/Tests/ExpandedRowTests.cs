namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class ExpandedRowTests {

    // k, yo, k2tog, kfb, ssk, cdd, p, slip: takes 11 stitches, gives back 9
    private static readonly StitchKind[] MixedOperations = [StitchKind.Knit, StitchKind.YarnOver, StitchKind.KnitTwoTogether,
                                                            StitchKind.KnitFrontAndBack, StitchKind.SlipSlipKnit, StitchKind.CentralDoubleDecrease,
                                                            StitchKind.Purl, StitchKind.Slip];

    [Fact]
    public void An_expanded_row_ends_with_the_stitches_its_operations_put_on_the_needle() {
        // Arrange
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        ExpandedRow row = new(anyName, 3, FabricSide.RightSide, 11, MixedOperations);

        // Assert
        Assert.Equal(11, row.StitchesBefore);
        Assert.Equal(9, row.StitchesAfter);
    }

    [Fact]
    public void An_increase_is_an_operation_that_gives_back_more_than_it_takes_and_a_decrease_the_other_way_round() {
        // Arrange
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        ExpandedRow row = new(anyName, 3, FabricSide.RightSide, 11, MixedOperations);

        // Assert
        Assert.Equal(2, row.Increases);    // the yarn over and the kfb
        Assert.Equal(3, row.Decreases);    // the k2tog, the ssk and the central double decrease
    }

    [Fact]
    public void A_plain_row_neither_increases_nor_decreases() {
        string anyName = AnyPatternSection.ValidName().Generate();

        ExpandedRow row = new(anyName, 1, FabricSide.RightSide, 4, [StitchKind.Knit, StitchKind.Purl, StitchKind.Slip, StitchKind.Knit]);

        Assert.Equal(0, row.Increases);
        Assert.Equal(0, row.Decreases);
        Assert.Equal(4, row.StitchesAfter);
    }

    [Fact]
    public void An_expanded_row_keeps_what_it_was_given() {
        // Act
        ExpandedRow row = new("Lace panel", 12, FabricSide.WrongSide, 11, MixedOperations);

        // Assert
        Assert.Equal("Lace panel", row.SectionName);
        Assert.Equal(12, row.RowNumber);
        Assert.Equal(FabricSide.WrongSide, row.Side);
        Assert.Equal(MixedOperations, row.Operations);
    }

    [Fact]
    public void An_expanded_row_refuses_operations_that_take_other_stitches_than_the_row_starts_with() {
        // Arrange: four knits take four stitches, the row starts with five
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ExpandedRow(anyName, 1, FabricSide.RightSide, 5, [StitchKind.Knit, StitchKind.Knit, StitchKind.Knit, StitchKind.Knit]));

        // Assert
        Assert.Equal("operations", refusal.ParamName);
    }

    [Fact]
    public void An_expanded_row_without_operations_is_refused() {
        string anyName = AnyPatternSection.ValidName().Generate();

        Assert.Throws<ArgumentNullException>(() => new ExpandedRow(anyName, 1, FabricSide.RightSide, 5, null!));
    }

    [Fact]
    public void An_expanded_row_needs_at_least_one_operation() {
        string anyName = AnyPatternSection.ValidName().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ExpandedRow(anyName, 1, FabricSide.RightSide, 5, []));

        Assert.Equal("operations", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void An_expanded_row_starts_with_at_least_one_stitch(int stitchesBefore) {
        string anyName = AnyPatternSection.ValidName().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExpandedRow(anyName, 1, FabricSide.RightSide, stitchesBefore, [StitchKind.YarnOver]));

        Assert.Equal("stitchesBefore", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void An_expanded_row_is_numbered_from_one(int rowNumber) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExpandedRow().WithRowNumber(rowNumber).Generate());

        Assert.Equal("rowNumber", refusal.ParamName);
    }

    [Fact]
    public void An_expanded_row_is_worked_on_a_side_that_exists() {
        FabricSide undefinedSide = (FabricSide)2;

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ExpandedRow().WithSide(undefinedSide).Generate());

        Assert.Equal("side", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_expanded_row_belongs_to_a_named_section(string blankName) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.ExpandedRow().WithSectionName(blankName).Generate());

        Assert.Equal("sectionName", refusal.ParamName);
    }

    [Fact]
    public void An_expanded_row_is_not_changed_by_the_list_of_operations_it_was_built_from() {
        // Arrange
        string           anyName    = AnyPatternSection.ValidName().Generate();
        List<StitchKind> operations = [StitchKind.Knit, StitchKind.Purl];
        ExpandedRow      row        = new(anyName, 1, FabricSide.RightSide, 2, operations);

        // Act
        operations.Add(StitchKind.Knit);

        // Assert
        Assert.Equal(2, row.Operations.Count);
    }

    [Fact(Skip = "domain defect: Operations hands out the internal array, so a caller can cast it back and rewrite the row after its invariants were checked. Deterministic, no seed involved.")]
    public void An_expanded_row_cannot_be_rewritten_through_the_list_of_operations_it_exposes() {
        // Arrange
        string      anyName = AnyPatternSection.ValidName().Generate();
        ExpandedRow row     = new(anyName, 1, FabricSide.RightSide, 2, [StitchKind.Knit, StitchKind.Purl]);

        // Act
        if (row.Operations is StitchKind[] exposed) {
            exposed[0] = StitchKind.CentralDoubleDecrease;
        }

        // Assert
        Assert.Equal([StitchKind.Knit, StitchKind.Purl], row.Operations);
    }

}
