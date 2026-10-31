using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class PatternSectionTests {

    [Fact]
    public void A_section_whose_rows_skip_a_number_is_refused() {
        // Arrange
        string anyName = Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength).Generate();
        RowInstruction[] rows = [
            Any.RowInstruction().WithRowNumber(4).WithSide(FabricSide.WrongSide).Generate(),
            Any.RowInstruction().WithRowNumber(6).WithSide(FabricSide.RightSide).Generate(),
        ];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, rows));

        // Assert
        Assert.Equal("rows", error.ParamName);
        Assert.Contains("from row 4 to row 6", error.Message);
    }

    [Fact]
    public void A_flat_section_working_two_rows_on_the_same_side_is_refused() {
        // Arrange
        string     anyName = Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength).Generate();
        FabricSide anySide = Any.Enum<FabricSide>().Generate();
        RowInstruction[] rows = [
            Any.RowInstruction().WithRowNumber(1).WithSide(anySide).Generate(),
            Any.RowInstruction().WithRowNumber(2).WithSide(anySide).Generate(),
        ];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, rows));

        // Assert
        Assert.Contains("same side", error.Message);
    }

    [Fact]
    public void A_section_spans_from_its_first_to_its_last_row() {
        // Arrange
        RowInstruction[] rows = [
            Any.RowInstruction().WithRowNumber(5).WithSide(FabricSide.RightSide).Generate(),
            Any.RowInstruction().WithRowNumber(6).WithSide(FabricSide.WrongSide).Generate(),
            Any.RowInstruction().WithRowNumber(7).WithSide(FabricSide.RightSide).Generate(),
        ];

        // Act
        PatternSection section = Any.PatternSection().WithRows(rows).Generate();

        // Assert
        Assert.Equal(5, section.FirstRow);
        Assert.Equal(7, section.LastRow);
    }

}
