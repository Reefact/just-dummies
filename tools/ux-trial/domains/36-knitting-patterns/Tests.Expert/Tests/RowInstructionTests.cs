namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class RowInstructionTests {

    // "Row 5 (RS): k2, *yo, k2tog; rep from * 8 times, k2 (20 sts)"
    private static List<IRowElement> EyeletRowElements() {
        return [new StitchRun(StitchKind.Knit, 2),
                new StitchRepeat([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 8),
                new StitchRun(StitchKind.Knit, 2)];
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_row_is_numbered_from_one(int rowNumber) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RowInstruction().WithRowNumber(rowNumber).Generate());

        Assert.Equal("rowNumber", refusal.ParamName);
    }

    [Fact]
    public void The_first_row_is_row_one() {
        RowInstruction firstRow = Any.RowInstruction().WithRowNumber(1).Generate();

        Assert.Equal(1, firstRow.RowNumber);
    }

    [Fact]
    public void A_row_is_worked_on_a_side_that_exists() {
        FabricSide undefinedSide = (FabricSide)2;

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RowInstruction().WithSide(undefinedSide).Generate());

        Assert.Equal("side", refusal.ParamName);
    }

    [Fact]
    public void A_row_without_a_list_of_elements_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RowInstruction(1, FabricSide.RightSide, null!, null));
    }

    [Fact]
    public void A_row_needs_at_least_one_element() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RowInstruction(1, FabricSide.RightSide, [], null));

        Assert.Equal("elements", refusal.ParamName);
    }

    [Fact]
    public void A_row_cannot_hold_a_missing_element() {
        IRowElement anyElement = Any.RowElement().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RowInstruction(1, FabricSide.RightSide, [anyElement, null!], null));

        Assert.Equal("elements", refusal.ParamName);
    }

    [Fact]
    public void A_row_may_print_the_count_its_elements_produce() {
        // Act
        RowInstruction row = new(5, FabricSide.RightSide, EyeletRowElements(), expectedStitchCount: 20);

        // Assert
        Assert.Equal(20, row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_may_print_no_count_at_all() {
        RowInstruction row = new(5, FabricSide.RightSide, EyeletRowElements(), expectedStitchCount: null);

        Assert.Null(row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_refuses_a_printed_count_that_its_elements_do_not_produce() {
        // Arrange: the elements produce 20 stitches, the pattern prints 21
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RowInstruction(5, FabricSide.RightSide, EyeletRowElements(), expectedStitchCount: 21));

        // Assert
        Assert.Equal("expectedStitchCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_printed_count_is_at_least_one_stitch(int printedCount) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RowInstruction(5, FabricSide.RightSide, EyeletRowElements(), printedCount));

        Assert.Equal("expectedStitchCount", refusal.ParamName);
    }

    [Fact]
    public void An_eyelet_row_ends_with_the_stitches_it_started_with() {
        // Arrange + Act
        RowInstruction row = new(5, FabricSide.RightSide, EyeletRowElements(), null);

        // Assert
        Assert.Equal(20, row.Consumes);
        Assert.Equal(20, row.Produces);
    }

    [Fact]
    public void A_row_adds_up_what_each_of_its_elements_takes_and_gives_back() {
        // Arrange: "k3, kfb x2, k2tog"
        IRowElement[] elements = [new StitchRun(StitchKind.Knit, 3),
                                  new StitchRun(StitchKind.KnitFrontAndBack, 2),
                                  new StitchRun(StitchKind.KnitTwoTogether, 1)];

        // Act
        RowInstruction row = new(1, FabricSide.RightSide, elements, 8);

        // Assert
        Assert.Equal(7, row.Consumes);
        Assert.Equal(8, row.Produces);
    }

    [Fact]
    public void A_row_expands_its_elements_one_after_the_other_in_the_order_written() {
        // Arrange: "k2, *yo, k2tog; rep from * 2 times, p1"
        IRowElement[] elements = [new StitchRun(StitchKind.Knit, 2),
                                  new StitchRepeat([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 2),
                                  new StitchRun(StitchKind.Purl, 1)];
        RowInstruction row = new(1, FabricSide.RightSide, elements, null);

        // Act
        List<StitchKind> operations = row.Expand().ToList();

        // Assert
        Assert.Equal([StitchKind.Knit, StitchKind.Knit,
                      StitchKind.YarnOver, StitchKind.KnitTwoTogether, StitchKind.YarnOver, StitchKind.KnitTwoTogether,
                      StitchKind.Purl], operations);
    }

    [Fact]
    public void A_row_is_not_changed_by_the_list_of_elements_it_was_built_from() {
        // Arrange
        List<IRowElement> elements        = EyeletRowElements();
        RowInstruction    row             = new(5, FabricSide.RightSide, elements, null);
        int               consumesBefore  = row.Consumes;

        // Act
        elements.Add(Any.RowElement().Generate());

        // Assert
        Assert.Equal(consumesBefore, row.Consumes);
    }

}
