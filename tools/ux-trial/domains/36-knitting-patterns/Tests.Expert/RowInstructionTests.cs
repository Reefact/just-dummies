using JustDummies;

using KnittingPatterns.Domain;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

public sealed class RowInstructionTests {

    [Fact]
    public void A_row_consumes_and_produces_the_sum_of_its_elements() {
        // Arrange
        // "Row 5 (RS): k2, *yo, k2tog; rep from * 8 times, k1, kfb (21 sts)"
        IRowElement[] elements = [K(2), Repeat(8, Yo(), K2Tog()), K(1), Kfb()];
        FabricSide    anySide  = Any.Enum<FabricSide>().Generate();

        // Act
        RowInstruction row = new(5, anySide, elements, expectedStitchCount: 21);

        // Assert
        Assert.Equal((20, 21), (row.Consumes, row.Produces));
    }

    [Fact]
    public void A_printed_count_that_disagrees_with_the_instructions_is_refused() {
        // Arrange
        int        anyRowNumber = Any.Int32().Between(1, 400).Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();
        // k1, kfb, k8 leaves 11 stitches; the pattern prints 10, a classic typo.
        IRowElement[] elements = [K(1), Kfb(), K(8)];

        // Act
        Exception? refusal = Record.Exception(() => new RowInstruction(anyRowNumber, anySide, elements, expectedStitchCount: 10));

        // Assert
        ArgumentException typo = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("expectedStitchCount", typo.ParamName);
        Assert.Contains("states 10 stitches but its instructions produce 11", typo.Message);
    }

    [Fact]
    public void A_row_without_printed_count_is_taken_at_its_word() {
        // Arrange
        // The row is an arbitrary one: what it says is not the point, only that no count is printed.
        RowInstruction anyRow = Any.RowInstruction().WithPrintedCount(false).Generate();

        // Act
        int? printed = anyRow.ExpectedStitchCount;

        // Assert
        Assert.Null(printed);
    }

    [Fact]
    public void A_row_expands_its_elements_in_the_order_they_are_written() {
        // Arrange
        RowInstruction row = Written(3, K(1), Ssk(), Repeat(2, Yo(), K(1)), K2Tog(), K(1));

        // Act
        StitchKind[] operations = row.Expand().ToArray();

        // Assert
        Assert.Equal(
            [StitchKind.Knit, StitchKind.SlipSlipKnit, StitchKind.YarnOver, StitchKind.Knit, StitchKind.YarnOver, StitchKind.Knit, StitchKind.KnitTwoTogether, StitchKind.Knit],
            operations);
    }

}
