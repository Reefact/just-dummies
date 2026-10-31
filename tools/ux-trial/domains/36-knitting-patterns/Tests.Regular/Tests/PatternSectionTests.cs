using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Tests.Rows;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class PatternSectionTests {

    [Fact]
    public void A_section_name_is_kept_without_its_surrounding_blanks() {
        // Arrange
        IReadOnlyList<RowInstruction> anyRows = Arbitrary.PatternSection().Generate().Rows;

        // Act
        PatternSection section = new("  Ribbing  ", anyRows);

        // Assert
        Assert.Equal("Ribbing", section.Name);
    }

    [Fact]
    public void Two_consecutive_rows_on_the_same_side_are_refused_since_flat_rows_alternate() {
        // Arrange
        string         anyName = Any.String().Alpha().WithLengthBetween(3, 20).Generate();
        RowInstruction row3    = new(3, FabricSide.RightSide, [K(10)], null);
        RowInstruction row4    = new(4, FabricSide.RightSide, [K(10)], null);

        // Act
        Exception? refusal = Record.Exception(() => new PatternSection(anyName, [row3, row4]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("same side", argumentException.Message);
    }

    [Fact]
    public void A_section_that_skips_a_row_is_refused() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(3, 20).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PatternSection(anyName, [Row(1, K(10)), Row(2, P(10)), Row(5, K(10))]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("jumps from row 2 to row 5", argumentException.Message);
    }

}
