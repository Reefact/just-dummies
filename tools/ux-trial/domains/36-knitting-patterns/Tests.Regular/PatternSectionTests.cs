using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

public sealed class PatternSectionTests {

    private static AnyRowInstruction AnyRow(int number, FabricSide side) => new AnyRowInstruction().WithRowNumber(number).WithSide(side);

    [Fact]
    public void A_section_reports_the_first_and_the_last_row_it_works() {
        // Arrange
        IReadOnlyList<RowInstruction> rows = new[] {
            AnyRow(5, FabricSide.RightSide).Generate(),
            AnyRow(6, FabricSide.WrongSide).Generate(),
            AnyRow(7, FabricSide.RightSide).Generate(),
        };
        string anyName = Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength).Generate();

        // Act
        PatternSection section = new(anyName, rows);

        // Assert
        Assert.Equal(5, section.FirstRow);
        Assert.Equal(7, section.LastRow);
        Assert.Equal(rows, section.Rows);
    }

    [Fact]
    public void A_section_name_is_trimmed() {
        // Arrange
        IReadOnlyList<RowInstruction> anyRows = new AnyPatternSection().Generate().Rows;

        // Act
        PatternSection section = new("  Lace panel \t", anyRows);

        // Assert
        Assert.Equal("Lace panel", section.Name);
    }

    [Fact]
    public void A_section_does_not_follow_a_row_list_changed_after_it_was_written() {
        // Arrange
        List<RowInstruction> rows    = new() { AnyRow(1, FabricSide.RightSide).Generate() };
        PatternSection       section = new("Ribbing", rows);

        // Act
        rows.Add(AnyRow(2, FabricSide.WrongSide).Generate());

        // Assert
        Assert.Single(section.Rows);
        Assert.Equal(1, section.LastRow);
    }

    [Fact]
    public void A_section_accepts_a_name_of_exactly_sixty_characters_and_refuses_sixty_one() {
        // Arrange
        IReadOnlyList<RowInstruction> anyRows   = new AnyPatternSection().Generate().Rows;
        string                        sixty     = Any.String().Alpha().WithLength(PatternSection.MaxNameLength).Generate();
        string                        sixtyOne  = Any.String().Alpha().WithLength(PatternSection.MaxNameLength + 1).Generate();

        // Act / Assert
        Assert.Equal(sixty, new PatternSection(sixty, anyRows).Name);
        Assert.Throws<ArgumentException>("name", () => new PatternSection(sixtyOne, anyRows));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \n")]
    public void A_section_refuses_a_blank_name(string blank) {
        // Arrange
        IReadOnlyList<RowInstruction> anyRows = new AnyPatternSection().Generate().Rows;

        // Act / Assert
        Assert.Throws<ArgumentException>("name", () => new PatternSection(blank, anyRows));
    }

    [Fact]
    public void A_section_refuses_a_missing_name() {
        // Arrange
        IReadOnlyList<RowInstruction> anyRows = new AnyPatternSection().Generate().Rows;

        // Act / Assert
        Assert.Throws<ArgumentNullException>("name", () => new PatternSection(null!, anyRows));
    }

    [Fact]
    public void A_section_refuses_an_empty_list_of_rows() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("rows", () => new PatternSection(anyName, Array.Empty<RowInstruction>()));
    }

    [Fact]
    public void A_section_refuses_a_missing_list_of_rows() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("rows", () => new PatternSection(anyName, null!));
    }

    [Fact]
    public void A_section_refuses_a_missing_row() {
        // Arrange
        string         anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        RowInstruction anyRow  = new AnyRowInstruction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("rows", () => new PatternSection(anyName, new[] { anyRow, null! }));
    }

    [Fact]
    public void A_section_refuses_a_row_number_that_jumps() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        RowInstruction[] rows = { AnyRow(1, FabricSide.RightSide).Generate(), AnyRow(3, FabricSide.WrongSide).Generate() };

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, rows));

        // Assert
        Assert.Contains("jumps from row 1 to row 3", error.Message);
    }

    [Fact]
    public void A_section_refuses_a_row_number_that_goes_back() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        RowInstruction[] rows = { AnyRow(4, FabricSide.RightSide).Generate(), AnyRow(3, FabricSide.WrongSide).Generate() };

        // Act / Assert
        Assert.Throws<ArgumentException>("rows", () => new PatternSection(anyName, rows));
    }

    [Fact]
    public void A_section_refuses_two_consecutive_rows_on_the_same_side() {
        // Arrange
        string     anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        FabricSide anySide = Any.Enum<FabricSide>().Generate();
        RowInstruction[] rows = { AnyRow(1, anySide).Generate(), AnyRow(2, anySide).Generate() };

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, rows));

        // Assert
        Assert.Contains("same side", error.Message);
    }

    [Fact]
    public void A_section_may_start_on_either_side() {
        // Arrange
        string     anyName  = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        FabricSide wrongSide = FabricSide.WrongSide;

        // Act
        PatternSection section = new(anyName, new[] { AnyRow(2, wrongSide).Generate() });

        // Assert
        Assert.Equal(FabricSide.WrongSide, section.Rows[0].Side);
    }

}
