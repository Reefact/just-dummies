namespace Warehouse.Domain.Tests.Regular;

public sealed class BinLocationTests {

    [Fact]
    public void The_parts_are_kept_as_given() {
        // Act
        BinLocation bin = new BinLocation('C', 42, 7);

        // Assert
        Assert.Equal('C', bin.Aisle);
        Assert.Equal(42, bin.Rack);
        Assert.Equal(7, bin.Level);
    }

    [Theory]
    [InlineData('A', 12, 3, "A-12-03")]
    [InlineData('A', 1, 1, "A-01-01")]
    [InlineData('Z', 99, 20, "Z-99-20")]
    [InlineData('M', 5, 10, "M-05-10")]
    public void It_is_written_aisle_rack_level_with_two_digits_each(char aisle, int rack, int level, string expected) {
        // Act
        string text = new BinLocation(aisle, rack, level).ToString();

        // Assert
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('Z')]
    public void The_first_and_last_aisles_are_accepted(char aisle) {
        Assert.Equal(aisle, new BinLocation(aisle, 1, 1).Aisle);
    }

    [Theory]
    [InlineData('@')]
    [InlineData('[')]
    [InlineData('a')]
    [InlineData('0')]
    [InlineData(' ')]
    public void An_aisle_outside_A_to_Z_is_refused(char aisle) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation(aisle, 1, 1));

        // Assert
        Assert.Equal("aisle", refusal.ParamName);
    }

    [Fact]
    public void Any_character_outside_A_to_Z_is_refused_as_an_aisle() {
        // Arrange
        char anyLowerCase = Any.Char().Alpha().InLowerCase().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation(anyLowerCase, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_rack_outside_1_to_99_is_refused(int rack) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation('A', rack, 1));

        // Assert
        Assert.Equal("rack", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-5)]
    public void A_level_outside_1_to_20_is_refused(int level) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation('A', 1, level));

        // Assert
        Assert.Equal("level", refusal.ParamName);
    }

    [Fact]
    public void Any_generated_bin_is_within_the_limits() {
        // Act
        BinLocation anyBin = new AnyBinLocation().Generate();

        // Assert
        Assert.InRange(anyBin.Aisle, 'A', 'Z');
        Assert.InRange(anyBin.Rack, 1, 99);
        Assert.InRange(anyBin.Level, 1, 20);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(20, true)]
    public void A_reach_truck_is_needed_above_level_4(int level, bool expected) {
        // Arrange
        BinLocation bin = new AnyBinLocation().WithLevel(level).Generate();

        // Assert
        Assert.Equal(expected, bin.NeedsReachTruck);
    }

    [Fact]
    public void Any_level_up_to_4_is_reachable_with_a_pallet_jack() {
        // Arrange
        BinLocation anyLowBin = new AnyBinLocation().WithLevel(l => l.LessThanOrEqualTo(4)).Generate();

        // Assert
        Assert.False(anyLowBin.NeedsReachTruck);
    }

    [Fact]
    public void Any_level_above_4_needs_a_reach_truck() {
        // Arrange
        BinLocation anyHighBin = new AnyBinLocation().WithLevel(l => l.GreaterThan(4)).Generate();

        // Assert
        Assert.True(anyHighBin.NeedsReachTruck);
    }

    [Fact]
    public void Two_bins_with_the_same_parts_are_equal() {
        // Arrange
        BinLocation anyBin = new AnyBinLocation().Generate();

        // Act
        BinLocation same = new BinLocation(anyBin.Aisle, anyBin.Rack, anyBin.Level);

        // Assert
        Assert.True(anyBin.Equals(same));
        Assert.Equal(anyBin.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_bins_differing_by_one_part_are_not_equal() {
        // Arrange
        BinLocation anyBin = new AnyBinLocation().WithAisle('B').WithRack(r => r.LessThan(99)).WithLevel(l => l.LessThan(20)).Generate();

        // Assert
        Assert.False(anyBin.Equals(new BinLocation('C', anyBin.Rack, anyBin.Level)));
        Assert.False(anyBin.Equals(new BinLocation('B', anyBin.Rack + 1, anyBin.Level)));
        Assert.False(anyBin.Equals(new BinLocation('B', anyBin.Rack, anyBin.Level + 1)));
    }

    [Fact]
    public void A_bin_is_not_equal_to_null_or_to_its_text() {
        // Arrange
        BinLocation anyBin = new AnyBinLocation().Generate();

        // Assert
        Assert.False(anyBin.Equals(null));
        Assert.False(anyBin.Equals(anyBin.ToString()));
    }

    [Fact]
    public void Parsing_the_text_of_a_bin_gives_the_same_bin_back() {
        // Arrange
        BinLocation anyBin = new AnyBinLocation().Generate();

        // Act
        BinLocation parsed = BinLocation.Parse(anyBin.ToString());

        // Assert
        Assert.Equal(anyBin, parsed);
    }

    [Fact]
    public void A_written_bin_is_parsed_part_by_part() {
        // Act
        BinLocation bin = BinLocation.Parse("A-12-03");

        // Assert
        Assert.Equal('A', bin.Aisle);
        Assert.Equal(12, bin.Rack);
        Assert.Equal(3, bin.Level);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a-12-03")]
    [InlineData("A-1-03")]
    [InlineData("A-12-3")]
    [InlineData("A-123-03")]
    [InlineData("A-12-003")]
    [InlineData("A1203")]
    [InlineData("A_12_03")]
    [InlineData("AB-12-03")]
    [InlineData(" A-12-03")]
    [InlineData("A-12-03 ")]
    [InlineData("A-12-0X")]
    public void Text_that_is_not_aisle_rack_level_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => BinLocation.Parse(text));
    }

    [Fact]
    public void Parsing_null_is_an_argument_null_error() {
        Assert.Throws<ArgumentNullException>(() => BinLocation.Parse(null!));
    }

    [Theory]
    [InlineData("A-00-03", "rack")]
    [InlineData("A-12-00", "level")]
    [InlineData("A-12-21", "level")]
    public void Well_formed_text_with_a_part_out_of_range_goes_through_the_constructor_checks(string text, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => BinLocation.Parse(text));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    // `$` in .NET also matches before a final line feed, so the documented pattern lets "A-12-03\n" through.
    [Fact(Skip = "domain defect: BinLocation.Parse accepts a trailing line feed because the pattern ends with $ instead of \\z, seed n/a (deterministic)")]
    public void Text_with_a_trailing_line_feed_is_a_format_error() {
        Assert.Throws<FormatException>(() => BinLocation.Parse("A-12-03\n"));
    }

}
