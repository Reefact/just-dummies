using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class RowInstructionTests {

    private static readonly IAny<IReadOnlyList<IRowElement>> AnyElements = Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 4);

    /// <summary>Row 5 (RS): k2, *yo, k2tog; rep from * 8 times, k2 (20 sts)</summary>
    private static IReadOnlyList<IRowElement> LaceRowElements() {
        return new IRowElement[] {
            new StitchRun(StitchKind.Knit, 2),
            new StitchRepeat(new[] { new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1) }, 8),
            new StitchRun(StitchKind.Knit, 2)
        };
    }

    [Fact]
    public void A_row_adds_up_what_its_elements_consume_and_produce() {
        FabricSide anySide = Any.Enum<FabricSide>().Generate();

        RowInstruction row = new RowInstruction(5, anySide, LaceRowElements(), 20);

        Assert.Equal(20, row.Consumes);
        Assert.Equal(20, row.Produces);
        Assert.Equal(5, row.RowNumber);
        Assert.Equal(20, row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_of_increases_produces_more_stitches_than_it_consumes() {
        FabricSide anySide = Any.Enum<FabricSide>().Generate();

        RowInstruction row = new RowInstruction(1, anySide, new IRowElement[] { new StitchRun(StitchKind.KnitFrontAndBack, 5) }, 10);

        Assert.Equal(5, row.Consumes);
        Assert.Equal(10, row.Produces);
    }

    [Fact]
    public void A_row_expands_its_elements_one_after_the_other() {
        FabricSide anySide = Any.Enum<FabricSide>().Generate();
        RowInstruction row = new RowInstruction(1, anySide, new IRowElement[] {
            new StitchRun(StitchKind.Knit, 2),
            new StitchRepeat(new[] { new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1) }, 2),
            new StitchRun(StitchKind.Purl, 1)
        }, null);

        Assert.Equal(new[] {
            StitchKind.Knit, StitchKind.Knit,
            StitchKind.YarnOver, StitchKind.KnitTwoTogether,
            StitchKind.YarnOver, StitchKind.KnitTwoTogether,
            StitchKind.Purl
        }, row.Expand());
    }

    [Fact]
    public void A_row_does_not_have_to_print_its_stitch_count() {
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();

        RowInstruction row = new RowInstruction(anyRowNumber, anySide, AnyElements.Generate(), null);

        Assert.Null(row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_refuses_a_printed_count_that_differs_from_what_its_elements_produce() {
        FabricSide anySide = Any.Enum<FabricSide>().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new RowInstruction(5, anySide, LaceRowElements(), 19));

        Assert.Equal("expectedStitchCount", error.ParamName);
        Assert.Contains("Row 5 states 19 stitches but its instructions produce 20", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_row_refuses_a_printed_count_below_one(int printed) {
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RowInstruction(anyRowNumber, anySide, AnyElements.Generate(), printed));

        Assert.Equal("expectedStitchCount", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_row_is_numbered_from_one(int rowNumber) {
        FabricSide anySide = Any.Enum<FabricSide>().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RowInstruction(rowNumber, anySide, AnyElements.Generate(), null));

        Assert.Equal("rowNumber", error.ParamName);
    }

    [Fact]
    public void A_row_refuses_a_side_the_enum_does_not_declare() {
        int anyRowNumber = Any.Int32().Positive().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RowInstruction(anyRowNumber, (FabricSide)2, AnyElements.Generate(), null));

        Assert.Equal("side", error.ParamName);
    }

    [Fact]
    public void A_row_without_elements_is_refused() {
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();

        Assert.Throws<ArgumentNullException>(() => new RowInstruction(anyRowNumber, anySide, null!, null));
        Assert.Throws<ArgumentException>(() => new RowInstruction(anyRowNumber, anySide, Array.Empty<IRowElement>(), null));
    }

    [Fact]
    public void A_row_cannot_hold_a_null_element() {
        int         anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide  anySide      = Any.Enum<FabricSide>().Generate();
        IRowElement anyElement   = new AnyIRowElement().Generate();

        Assert.Throws<ArgumentException>(() => new RowInstruction(anyRowNumber, anySide, new IRowElement?[] { anyElement, null }!, null));
    }

    [Fact]
    public void A_row_keeps_its_own_copy_of_the_elements() {
        int              anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide       anySide      = Any.Enum<FabricSide>().Generate();
        List<IRowElement> elements     = new List<IRowElement> { new StitchRun(StitchKind.Knit, 4) };
        RowInstruction   row          = new RowInstruction(anyRowNumber, anySide, elements, null);

        elements.Add(new StitchRun(StitchKind.Purl, 4));

        Assert.Equal(4, row.Consumes);
        Assert.Single(row.Elements);
    }

}
