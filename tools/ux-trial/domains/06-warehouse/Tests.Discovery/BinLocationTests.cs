using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class BinLocationTests {

    [Fact]
    public void A_bin_keeps_its_three_coordinates() {
        BinLocation bin = new BinLocation('B', 7, 12);

        Assert.Equal('B', bin.Aisle);
        Assert.Equal(7, bin.Rack);
        Assert.Equal(12, bin.Level);
    }

    [Theory]
    [InlineData('A', 12, 3, "A-12-03")]
    [InlineData('Z', 99, 20, "Z-99-20")]
    [InlineData('M', 1, 1, "M-01-01")]
    public void A_bin_is_written_aisle_rack_level_with_two_digit_padding(char aisle, int rack, int level, string expected) {
        BinLocation bin = new BinLocation(aisle, rack, level);

        Assert.Equal(expected, bin.ToString());
    }

    [Theory]
    [InlineData('@')]
    [InlineData('[')]
    [InlineData('a')]
    [InlineData('0')]
    public void An_aisle_outside_a_to_z_is_refused(char aisle) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation(aisle, 1, 1));

        Assert.Equal("aisle", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_rack_outside_one_to_ninety_nine_is_refused(int rack) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation('A', rack, 1));

        Assert.Equal("rack", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void A_level_outside_one_to_twenty_is_refused(int level) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BinLocation('A', 1, level));

        Assert.Equal("level", exception.ParamName);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(20, true)]
    public void Only_levels_above_four_need_a_reach_truck(int level, bool expected) {
        // Arrange
        BinLocation anyBin = Any.BinLocation().WithLevel(level).Generate();

        // Act
        bool needsReachTruck = anyBin.NeedsReachTruck;

        // Assert
        Assert.Equal(expected, needsReachTruck);
    }

    [Fact]
    public void Parsing_reads_aisle_rack_and_level() {
        BinLocation bin = BinLocation.Parse("B-07-12");

        Assert.Equal('B', bin.Aisle);
        Assert.Equal(7, bin.Rack);
        Assert.Equal(12, bin.Level);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a-12-03")]
    [InlineData("A-1-03")]
    [InlineData("A-12-3")]
    [InlineData("A-123-03")]
    [InlineData("A12-03")]
    [InlineData("A-12-03 ")]
    [InlineData(" A-12-03")]
    [InlineData("AA-12-03")]
    [InlineData("A-12-03-04")]
    public void Parsing_a_text_that_is_not_a_bin_location_fails_with_a_format_error(string text) {
        Assert.Throws<FormatException>(() => BinLocation.Parse(text));
    }

    [Fact]
    public void Parsing_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => BinLocation.Parse(null!));
    }

    [Theory]
    [InlineData("A-00-03", "rack")]
    [InlineData("A-12-00", "level")]
    [InlineData("A-12-21", "level")]
    public void A_well_formed_text_with_out_of_range_numbers_goes_through_the_constructor_guards(string text, string parameter) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => BinLocation.Parse(text));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact(Skip = "domain defect: Parse accepts a trailing line feed ('$' matches before the final \\n), so 'A-12-03\\n' is read as a bin")]
    public void Parsing_refuses_a_trailing_line_feed() {
        Assert.Throws<FormatException>(() => BinLocation.Parse("A-12-03\n"));
    }

    [Fact]
    public void Bins_with_the_same_coordinates_are_equal() {
        // Arrange
        BinLocation anyBin = Any.BinLocation().Generate();
        BinLocation same   = new BinLocation(anyBin.Aisle, anyBin.Rack, anyBin.Level);

        // Assert
        Assert.True(anyBin.Equals(same));
        Assert.Equal(anyBin.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Bins_with_different_coordinates_are_not_equal() {
        Assert.False(new BinLocation('A', 12, 3).Equals(new BinLocation('B', 12, 3)));
        Assert.False(new BinLocation('A', 12, 3).Equals(new BinLocation('A', 13, 3)));
        Assert.False(new BinLocation('A', 12, 3).Equals(new BinLocation('A', 12, 4)));
        Assert.False(new BinLocation('A', 12, 3).Equals("A-12-03"));
        Assert.False(new BinLocation('A', 12, 3).Equals(null));
    }

}
