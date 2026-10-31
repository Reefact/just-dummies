using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Tests.Rows;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class RowInstructionTests {

    [Fact]
    public void An_eyelet_row_consumes_and_produces_what_its_elements_add_up_to() {
        // Arrange — "k2, *yo, k2tog; rep from * 8 times, k2 (20 sts)"
        RowInstruction eyeletRow = new(5, FabricSide.RightSide, [K(2), Rep(8, Yo(1), K2Tog(1)), K(2)], expectedStitchCount: 20);

        // Act
        (int Consumes, int Produces) counts = (eyeletRow.Consumes, eyeletRow.Produces);

        // Assert
        Assert.Equal((20, 20), counts);
    }

    [Fact]
    public void A_printed_count_that_disagrees_with_the_instructions_is_refused() {
        // Arrange
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new RowInstruction(anyRowNumber, anySide, [K(2), Kfb(4), K(2)], expectedStitchCount: 8));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("expectedStitchCount", argumentException.ParamName);
        Assert.Contains("produce 12", argumentException.Message);
    }

    [Fact]
    public void A_row_expands_its_elements_in_working_order() {
        // Arrange
        RowInstruction anyDecreaseRow = Arbitrary.RowInstruction()
                                                 .WithElements([Ssk(1), K(2), K2Tog(1)])
                                                 .Generate();

        // Act
        StitchKind[] operations = anyDecreaseRow.Expand().ToArray();

        // Assert
        Assert.Equal([StitchKind.SlipSlipKnit, StitchKind.Knit, StitchKind.Knit, StitchKind.KnitTwoTogether], operations);
    }

}
