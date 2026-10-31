using JustDummies;

using KnittingPatterns.Domain;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

public sealed class PatternSectionTests {

    private static readonly AnyString AnySectionName = Any.String().Alpha().WithLengthBetween(3, PatternSection.MaxNameLength);

    [Fact]
    public void A_section_skipping_a_row_is_refused() {
        // Arrange
        string         anyName = AnySectionName.Generate();
        RowInstruction row1    = Plain(1, 24).Generate();
        RowInstruction row3    = Plain(3, 24).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PatternSection(anyName, [row1, row3]));

        // Assert
        ArgumentException gap = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("jumps from row 1 to row 3", gap.Message);
    }

    [Fact]
    public void A_section_working_two_rows_on_the_same_side_is_refused() {
        // Arrange
        string         anyName = AnySectionName.Generate();
        RowInstruction row1    = new(1, FabricSide.RightSide, [K(24)], null);
        RowInstruction row2    = new(2, FabricSide.RightSide, [K(24)], null);

        // Act
        Exception? refusal = Record.Exception(() => new PatternSection(anyName, [row1, row2]));

        // Assert
        ArgumentException sameSide = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("on the same side", sameSide.Message);
    }

    [Fact]
    public void A_section_spans_from_its_first_to_its_last_row() {
        // Arrange
        string anyName = AnySectionName.Generate();
        RowInstruction[] rows5To8 = [Plain(5, 30).Generate(), Plain(6, 30).Generate(), Plain(7, 30).Generate(), Plain(8, 30).Generate()];

        // Act
        PatternSection section = new(anyName, rows5To8);

        // Assert
        Assert.Equal((5, 8), (section.FirstRow, section.LastRow));
    }

    [Fact]
    public void A_section_name_is_kept_without_its_surrounding_spaces() {
        // Arrange
        PatternSection anySection = Any.PatternSection().WithName("  Garter border ").Generate();

        // Act
        string name = anySection.Name;

        // Assert
        Assert.Equal("Garter border", name);
    }

}
