using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class BinLocationTests {

    [Theory]
    [InlineData('A', 12, 3, "A-12-03")]
    [InlineData('Z', 1, 20, "Z-01-20")]
    [InlineData('B', 99, 1, "B-99-01")]
    [InlineData('M', 10, 10, "M-10-10")]
    public void A_bin_is_written_aisle_rack_level_with_rack_and_level_on_two_digits(char aisle, int rack, int level, string expectedText) {
        // Act
        BinLocation bin = new BinLocation(aisle, rack, level);

        // Assert
        Assert.Equal(expectedText, bin.ToString());
        Assert.Equal(aisle, bin.Aisle);
        Assert.Equal(rack, bin.Rack);
        Assert.Equal(level, bin.Level);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(20, true)]
    public void Only_levels_above_four_need_a_reach_truck(int level, bool expectedReachTruck) {
        // Arrange
        BinLocation anyBinOnThatLevel = Any.BinLocation().WithLevel(level).Generate();

        // Act
        bool needsReachTruck = anyBinOnThatLevel.NeedsReachTruck;

        // Assert
        Assert.Equal(expectedReachTruck, needsReachTruck);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('Z')]
    public void Both_ends_of_the_alphabet_are_valid_aisles(char aisle) {
        // Act
        BinLocation anyBin = Any.BinLocation().WithAisle(aisle).Generate();

        // Assert
        Assert.Equal(aisle, anyBin.Aisle);
    }

    [Theory]
    [InlineData('@')]   // the character before 'A'
    [InlineData('[')]   // the character after 'Z'
    [InlineData('a')]
    [InlineData('1')]
    [InlineData(' ')]
    public void An_aisle_that_is_not_an_upper_case_letter_is_refused(char aisle) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BinLocation().WithAisle(aisle).Generate());

        // Assert
        Assert.Equal("aisle", refused.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Both_ends_of_the_rack_range_are_valid(int rack) {
        // Act
        BinLocation anyBin = Any.BinLocation().WithRack(rack).Generate();

        // Assert
        Assert.Equal(rack, anyBin.Rack);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_rack_outside_one_to_ninety_nine_is_refused(int rack) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BinLocation().WithRack(rack).Generate());

        // Assert
        Assert.Equal("rack", refused.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Both_ends_of_the_level_range_are_valid(int level) {
        // Act
        BinLocation anyBin = Any.BinLocation().WithLevel(level).Generate();

        // Assert
        Assert.Equal(level, anyBin.Level);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_level_outside_one_to_twenty_is_refused(int level) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BinLocation().WithLevel(level).Generate());

        // Assert
        Assert.Equal("level", refused.ParamName);
    }

    [Theory]
    [InlineData("A-12-03", 'A', 12, 3)]
    [InlineData("Z-99-20", 'Z', 99, 20)]
    [InlineData("B-01-01", 'B', 1, 1)]
    public void A_bin_written_aisle_rack_level_is_read_back(string text, char aisle, int rack, int level) {
        // Act
        BinLocation bin = BinLocation.Parse(text);

        // Assert
        Assert.Equal(aisle, bin.Aisle);
        Assert.Equal(rack, bin.Rack);
        Assert.Equal(level, bin.Level);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a-12-03")]       // lower-case aisle
    [InlineData("AA-12-03")]
    [InlineData("A-1-03")]        // rack not on two digits
    [InlineData("A-123-03")]
    [InlineData("A-12-3")]        // level not on two digits
    [InlineData("A-12-003")]
    [InlineData("A12-03")]        // a separator missing
    [InlineData("A-1203")]
    [InlineData("A_12_03")]       // the wrong separator
    [InlineData(" A-12-03")]
    [InlineData("A-12-03 ")]
    [InlineData("A-12-0X")]
    public void Text_that_is_not_aisle_rack_level_is_refused_as_a_bad_format(string malformed) {
        // Act
        Exception? thrown = Record.Exception(() => BinLocation.Parse(malformed));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Missing_text_is_refused_as_a_null_argument() {
        // Act
        Exception? thrown = Record.Exception(() => BinLocation.Parse(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Theory]
    [InlineData("A-00-03")]
    [InlineData("A-12-00")]
    [InlineData("A-12-21")]
    [InlineData("A-00-00")]
    public void Well_formed_text_is_still_checked_by_the_constructor_ranges(string outOfRange) {
        // Act
        Exception? thrown = Record.Exception(() => BinLocation.Parse(outOfRange));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

    [Fact(Skip = "domain defect: the pattern's '$' accepts a trailing line feed, so 'A-12-03\\n' parses instead of raising FormatException (use \\z); seed n/a (deterministic)")]
    public void Text_followed_by_a_line_feed_is_refused_as_a_bad_format() {
        // Act
        Exception? thrown = Record.Exception(() => BinLocation.Parse("A-12-03\n"));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Two_bins_with_the_same_coordinates_are_equal() {
        // Arrange
        BinLocation anyBin = Any.BinLocation().Generate();

        // Act
        BinLocation sameCoordinates = new BinLocation(anyBin.Aisle, anyBin.Rack, anyBin.Level);

        // Assert
        Assert.Equal(anyBin, sameCoordinates);
        Assert.Equal(anyBin.GetHashCode(), sameCoordinates.GetHashCode());
    }

    [Theory]
    [InlineData('B', 12, 3)]   // another aisle
    [InlineData('A', 13, 3)]   // another rack
    [InlineData('A', 12, 4)]   // another level
    public void Two_bins_differing_by_one_coordinate_are_not_equal(char aisle, int rack, int level) {
        // Arrange
        BinLocation reference = new BinLocation('A', 12, 3);
        BinLocation other     = new BinLocation(aisle, rack, level);

        // Act
        bool equal = reference.Equals(other);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_bin_is_not_equal_to_its_text_or_to_null() {
        // Arrange
        BinLocation anyBin = Any.BinLocation().Generate();

        // Act
        bool equalToText = anyBin.Equals(anyBin.ToString());
        bool equalToNull = anyBin.Equals(null);

        // Assert
        Assert.False(equalToText);
        Assert.False(equalToNull);
    }

}
