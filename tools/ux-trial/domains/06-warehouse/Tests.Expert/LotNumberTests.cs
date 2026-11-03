using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class LotNumberTests {

    [Fact]
    public void A_lot_number_of_twenty_characters_is_accepted() {
        // Arrange: the content is arbitrary, the length is the point.
        string anyLongestLot = Any.String().Alpha().WithLength(20).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new LotNumber(anyLongestLot));

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void A_lot_number_of_twenty_one_characters_is_refused() {
        // Arrange
        string anyTooLongLot = Any.String().Alpha().WithLength(21).Generate();

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new LotNumber(anyTooLongLot));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void A_lot_number_of_a_single_character_is_accepted() {
        // Arrange
        char anyLot = Any.Char().Alpha().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new LotNumber(anyLot.ToString()));

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void A_lot_number_is_stored_exactly_as_given_without_trimming() {
        // Act
        LotNumber lot = new LotNumber("  L-24/07 b ");

        // Assert
        Assert.Equal("  L-24/07 b ", lot.Value);
        Assert.Equal("  L-24/07 b ", lot.ToString());
    }

    [Fact]
    public void A_missing_lot_number_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new LotNumber(null!));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\r\n ")]
    public void A_blank_lot_number_is_refused(string blank) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new LotNumber(blank));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void A_blank_lot_number_is_refused_whatever_its_length_up_to_the_maximum() {
        // Arrange
        string anyBlank = Any.String().WithChars(" \t").WithLengthBetween(1, 20).Generate();

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new LotNumber(anyBlank));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

}
