namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class PatternSectionTests {

    private static RowInstruction AnyRow(int rowNumber, FabricSide side) {
        return Any.RowInstruction().WithRowNumber(rowNumber).WithSide(side).Generate();
    }

    private static List<RowInstruction> AnyRowsFrom(int firstRow, int count) {
        return Any.PatternSection().StartingAtRow(firstRow).WithRowCount(count).Generate().Rows.ToList();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \r\n")]
    public void A_section_needs_a_name_with_something_in_it(string blankName) {
        // Arrange
        List<RowInstruction> anyRows = AnyRowsFrom(1, 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PatternSection(blankName, anyRows));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_section_without_a_name_is_refused() {
        List<RowInstruction> anyRows = AnyRowsFrom(1, 2);

        Assert.Throws<ArgumentNullException>(() => new PatternSection(null!, anyRows));
    }

    [Fact]
    public void A_section_name_holds_at_most_sixty_characters() {
        // Arrange
        List<RowInstruction> anyRows        = AnyRowsFrom(1, 2);
        string               anyLongestName = Any.String().Alpha().WithLength(PatternSection.MaxNameLength).Generate();
        string               anyTooLongName = Any.String().Alpha().WithLength(PatternSection.MaxNameLength + 1).Generate();

        // Act
        PatternSection accepted = new(anyLongestName, anyRows);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PatternSection(anyTooLongName, anyRows));

        // Assert
        Assert.Equal(PatternSection.MaxNameLength, accepted.Name.Length);
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_section_keeps_its_name_without_the_spaces_around_it() {
        // Arrange
        List<RowInstruction> anyRows = AnyRowsFrom(1, 2);

        // Act
        PatternSection section = new("  Lace panel  ", anyRows);

        // Assert
        Assert.Equal("Lace panel", section.Name);
    }

    [Fact]
    public void A_section_without_a_row_list_is_refused() {
        string anyName = AnyPatternSection.ValidName().Generate();

        Assert.Throws<ArgumentNullException>(() => new PatternSection(anyName, null!));
    }

    [Fact]
    public void A_section_needs_at_least_one_row() {
        string anyName = AnyPatternSection.ValidName().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, []));

        Assert.Equal("rows", refusal.ParamName);
    }

    [Fact]
    public void A_section_cannot_hold_a_missing_row() {
        string         anyName = AnyPatternSection.ValidName().Generate();
        RowInstruction anyRow  = AnyRow(1, FabricSide.RightSide);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, [anyRow, null!]));

        Assert.Equal("rows", refusal.ParamName);
    }

    [Fact]
    public void A_section_may_be_a_single_row() {
        // Arrange
        string         anyName = AnyPatternSection.ValidName().Generate();
        RowInstruction row     = AnyRow(9, FabricSide.RightSide);

        // Act
        PatternSection section = new(anyName, [row]);

        // Assert
        Assert.Equal(9, section.FirstRow);
        Assert.Equal(9, section.LastRow);
    }

    [Fact]
    public void A_section_reports_the_first_and_the_last_of_its_consecutive_alternating_rows() {
        // Arrange
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        PatternSection section = new(anyName, [AnyRow(5, FabricSide.RightSide),
                                               AnyRow(6, FabricSide.WrongSide),
                                               AnyRow(7, FabricSide.RightSide),
                                               AnyRow(8, FabricSide.WrongSide)]);

        // Assert
        Assert.Equal(5, section.FirstRow);
        Assert.Equal(8, section.LastRow);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void A_section_refuses_rows_that_do_not_follow_each_other_one_by_one(int firstRowNumber, int secondRowNumber) {
        // Arrange
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PatternSection(anyName, [AnyRow(firstRowNumber, FabricSide.RightSide), AnyRow(secondRowNumber, FabricSide.WrongSide)]));

        // Assert
        Assert.Equal("rows", refusal.ParamName);
        Assert.Contains("jumps", refusal.Message);
    }

    [Theory]
    [InlineData(FabricSide.RightSide)]
    [InlineData(FabricSide.WrongSide)]
    public void A_section_refuses_two_consecutive_rows_worked_on_the_same_side(FabricSide side) {
        // Arrange
        string anyName = AnyPatternSection.ValidName().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PatternSection(anyName, [AnyRow(1, side), AnyRow(2, side)]));

        // Assert
        Assert.Equal("rows", refusal.ParamName);
        Assert.Contains("same side", refusal.Message);
    }

    [Fact]
    public void A_section_refuses_a_repeated_side_in_the_middle_of_its_rows() {
        string anyName = AnyPatternSection.ValidName().Generate();

        Assert.Throws<ArgumentException>(() => new PatternSection(anyName, [AnyRow(1, FabricSide.RightSide),
                                                                           AnyRow(2, FabricSide.WrongSide),
                                                                           AnyRow(3, FabricSide.WrongSide)]));
    }

    [Fact]
    public void A_section_is_not_changed_by_the_list_of_rows_it_was_built_from() {
        // Arrange
        string               anyName = AnyPatternSection.ValidName().Generate();
        List<RowInstruction> rows    = AnyRowsFrom(1, 3);
        PatternSection       section = new(anyName, rows);
        int                  lastRowBefore = section.LastRow;

        // Act
        rows.Add(AnyRow(lastRowBefore + 1, Rows.SideOf(lastRowBefore + 1)));

        // Assert
        Assert.Equal(lastRowBefore, section.LastRow);
    }

}
