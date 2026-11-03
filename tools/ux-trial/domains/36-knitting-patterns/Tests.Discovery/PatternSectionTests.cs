using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class PatternSectionTests {

    private static readonly IAny<IReadOnlyList<RowInstruction>> AnyRows = Any.PatternSection().As(section => section.Rows);

    private static RowInstruction Row(int number, FabricSide side) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side).Generate();
    }

    [Fact]
    public void A_section_reports_the_first_and_last_row_of_its_block() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        PatternSection section = new PatternSection(anyName, new[] {
            Row(7, FabricSide.WrongSide), Row(8, FabricSide.RightSide), Row(9, FabricSide.WrongSide)
        });

        Assert.Equal(7, section.FirstRow);
        Assert.Equal(9, section.LastRow);
        Assert.Equal(3, section.Rows.Count);
    }

    [Fact]
    public void A_section_may_be_a_single_row() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        PatternSection section = new PatternSection(anyName, new[] { Row(4, FabricSide.RightSide) });

        Assert.Equal(4, section.FirstRow);
        Assert.Equal(4, section.LastRow);
    }

    [Fact]
    public void A_section_name_is_trimmed() {
        PatternSection section = new PatternSection("  Ribbing ", AnyRows.Generate());

        Assert.Equal("Ribbing", section.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_section_needs_a_name_with_something_in_it(string blank) {
        Assert.Throws<ArgumentException>(() => new PatternSection(blank, AnyRows.Generate()));
        Assert.Throws<ArgumentNullException>(() => new PatternSection(null!, AnyRows.Generate()));
    }

    [Fact]
    public void A_section_name_may_hold_sixty_characters() {
        string anyLongName = Any.String().Alpha().WithLength(60).Generate();

        PatternSection section = new PatternSection(anyLongName, AnyRows.Generate());

        Assert.Equal(anyLongName, section.Name);
    }

    [Fact]
    public void A_section_name_of_sixty_one_characters_is_refused() {
        string anyTooLongName = Any.String().Alpha().WithLength(61).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyTooLongName, AnyRows.Generate()));

        Assert.Equal("name", error.ParamName);
    }

    [Fact]
    public void A_section_needs_rows() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        Assert.Throws<ArgumentNullException>(() => new PatternSection(anyName, null!));
        Assert.Throws<ArgumentException>(() => new PatternSection(anyName, Array.Empty<RowInstruction>()));
    }

    [Fact]
    public void A_section_cannot_hold_a_null_row() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        Assert.Throws<ArgumentException>(() => new PatternSection(anyName, new RowInstruction?[] { Row(1, FabricSide.RightSide), null }!));
    }

    [Theory]
    [InlineData(3, 5)]   // a gap
    [InlineData(5, 4)]   // backwards
    [InlineData(5, 5)]   // repeated
    public void A_section_refuses_rows_that_do_not_follow_each_other(int first, int second) {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, new[] {
            Row(first, FabricSide.RightSide), Row(second, FabricSide.WrongSide)
        }));

        Assert.Equal("rows", error.ParamName);
        Assert.Contains($"jumps from row {first} to row {second}", error.Message);
    }

    [Fact]
    public void A_section_refuses_two_consecutive_rows_on_the_same_side() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new PatternSection(anyName, new[] {
            Row(3, FabricSide.RightSide), Row(4, FabricSide.RightSide)
        }));

        Assert.Contains("rows 3 and 4 on the same side", error.Message);
    }

    [Fact]
    public void A_section_keeps_its_own_copy_of_the_rows() {
        string        anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();
        List<RowInstruction> rows = new List<RowInstruction> { Row(1, FabricSide.RightSide) };
        PatternSection section = new PatternSection(anyName, rows);

        rows.Add(Row(2, FabricSide.WrongSide));

        Assert.Single(section.Rows);
        Assert.Equal(1, section.LastRow);
    }

}
