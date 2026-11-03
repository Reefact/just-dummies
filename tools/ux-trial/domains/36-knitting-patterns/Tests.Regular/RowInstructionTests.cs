using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Rows;

namespace KnittingPatterns.Tests.Regular;

public sealed class RowInstructionTests {

    [Fact]
    public void A_row_keeps_its_number_its_side_and_its_elements_in_written_order() {
        // Arrange
        StitchRun    first  = K(2);
        StitchRepeat second = Rep(3, P(1), K(1));

        // Act
        RowInstruction row = Row(5, FabricSide.WrongSide, null, first, second);

        // Assert
        Assert.Equal(5, row.RowNumber);
        Assert.Equal(FabricSide.WrongSide, row.Side);
        Assert.Equal(new IRowElement[] { first, second }, row.Elements);
        Assert.Null(row.ExpectedStitchCount);
    }

    [Fact]
    public void A_row_adds_up_what_its_elements_consume_and_produce() {
        // Arrange: "k2, *yo, k2tog; rep from * 8 times, k2"
        RowInstruction row = Rs(5, null, K(2), Rep(8, Of(StitchKind.YarnOver), Of(StitchKind.KnitTwoTogether)), K(2));

        // Assert
        Assert.Equal(2 + 16 + 2, row.Consumes);
        Assert.Equal(2 + 16 + 2, row.Produces);   // yo + k2tog consumes two and gives two back
    }

    [Fact]
    public void A_row_expands_its_elements_one_after_the_other() {
        // Arrange
        RowInstruction row = Rs(1, null, K(1), Rep(2, Of(StitchKind.YarnOver), P(1)), Of(StitchKind.KnitFrontAndBack));

        // Act
        List<StitchKind> expanded = row.Expand().ToList();

        // Assert
        Assert.Equal(
            new[] { StitchKind.Knit, StitchKind.YarnOver, StitchKind.Purl, StitchKind.YarnOver, StitchKind.Purl, StitchKind.KnitFrontAndBack },
            expanded);
    }

    [Fact]
    public void A_row_does_not_follow_an_element_list_changed_after_it_was_written() {
        // Arrange
        List<IRowElement> elements = new() { K(3) };
        RowInstruction    row      = new(1, FabricSide.RightSide, elements, null);

        // Act
        elements.Add(P(4));

        // Assert
        Assert.Single(row.Elements);
        Assert.Equal(3, row.Consumes);
    }

    [Fact]
    public void A_row_accepts_a_printed_count_equal_to_what_its_elements_produce() {
        // Act: k2tog x5 then k4 produces 9
        RowInstruction row = Rs(3, 9, Of(StitchKind.KnitTwoTogether, 5), K(4));

        // Assert
        Assert.Equal(9, row.ExpectedStitchCount);
        Assert.Equal(9, row.Produces);
    }

    [Fact]
    public void A_row_refuses_a_printed_count_that_differs_from_what_its_elements_produce() {
        // Arrange: produces 12, prints 13
        IRowElement[] elements = { K(12) };

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new RowInstruction(7, FabricSide.RightSide, elements, 13));

        // Assert
        Assert.Equal("expectedStitchCount", error.ParamName);
        Assert.Contains("13", error.Message);
        Assert.Contains("12", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_row_refuses_a_printed_count_below_one_before_comparing_it_to_the_elements(int printed) {
        // Arrange
        IRowElement[] elements = { K(12) };

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("expectedStitchCount", () => new RowInstruction(7, FabricSide.RightSide, elements, printed));
    }

    [Fact]
    public void A_row_refuses_to_number_itself_below_one() {
        // Arrange
        FabricSide anySide     = Any.Enum<FabricSide>().Generate();
        IRowElement[] anyElements = Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 3).Generate().ToArray();
        int        anyNumber   = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("rowNumber", () => new RowInstruction(anyNumber, anySide, anyElements, null));
    }

    [Fact]
    public void A_row_refuses_a_side_the_fabric_does_not_have() {
        // Arrange
        RowInstruction anyRow = new AnyRowInstruction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("side", () => new RowInstruction(anyRow.RowNumber, (FabricSide)7, anyRow.Elements, null));
    }

    [Fact]
    public void A_row_refuses_an_empty_list_of_elements() {
        // Arrange
        int        anyNumber = Any.Int32().Positive().Generate();
        FabricSide anySide   = Any.Enum<FabricSide>().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("elements", () => new RowInstruction(anyNumber, anySide, Array.Empty<IRowElement>(), null));
    }

    [Fact]
    public void A_row_refuses_a_missing_list_of_elements() {
        // Arrange
        int        anyNumber = Any.Int32().Positive().Generate();
        FabricSide anySide   = Any.Enum<FabricSide>().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("elements", () => new RowInstruction(anyNumber, anySide, null!, null));
    }

    [Fact]
    public void A_row_refuses_a_missing_element() {
        // Arrange
        int         anyNumber  = Any.Int32().Positive().Generate();
        FabricSide  anySide    = Any.Enum<FabricSide>().Generate();
        IRowElement anyElement = new AnyIRowElement().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("elements", () => new RowInstruction(anyNumber, anySide, new[] { anyElement, null! }, null));
    }

    [Fact]
    public void A_row_of_yarn_overs_alone_consumes_nothing() {
        // Act
        RowInstruction row = Rs(1, 3, Of(StitchKind.YarnOver, 3));

        // Assert
        Assert.Equal(0, row.Consumes);
        Assert.Equal(3, row.Produces);
    }

    [Fact]
    public void A_generated_row_that_prints_its_count_prints_what_its_elements_produce() {
        // Arrange
        RowInstruction anyRowWithCount = new AnyRowInstruction().WithStatedStitchCount().Generate();

        // Act
        int? printed = anyRowWithCount.ExpectedStitchCount;

        // Assert
        Assert.Equal(anyRowWithCount.Elements.Sum(e => e.Produces), printed);
    }

}
