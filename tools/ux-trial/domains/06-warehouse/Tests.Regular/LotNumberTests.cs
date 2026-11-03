namespace Warehouse.Domain.Tests.Regular;

public sealed class LotNumberTests {

    [Fact]
    public void A_lot_is_stored_exactly_as_given() {
        // Arrange: free text, leading and trailing spaces included
        string anyText = Any.String().NotBlank().WithMaxLength(20).Generate();

        // Act
        LotNumber lot = new LotNumber(anyText);

        // Assert
        Assert.Equal(anyText, lot.Value);
        Assert.Equal(anyText, lot.ToString());
    }

    [Fact]
    public void A_lot_of_exactly_20_characters_is_accepted() {
        // Arrange
        string anyTwentyCharacters = Any.String().NotBlank().WithLength(20).Generate();

        // Act
        LotNumber lot = new LotNumber(anyTwentyCharacters);

        // Assert
        Assert.Equal(20, lot.Value.Length);
    }

    [Fact]
    public void A_lot_of_a_single_character_is_accepted() {
        // Act
        LotNumber lot = new LotNumber("7");

        // Assert
        Assert.Equal("7", lot.Value);
    }

    [Fact]
    public void A_lot_of_21_characters_or_more_is_refused() {
        // Arrange
        string anyTooLong = Any.String().NotBlank().WithLengthBetween(21, 100).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LotNumber(anyTooLong));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_null_lot_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LotNumber(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_lot_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LotNumber(blank));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_generated_lot_is_always_accepted() {
        // Act
        LotNumber anyLot = new AnyLotNumber().Generate();

        // Assert
        Assert.InRange(anyLot.Value.Length, 1, 20);
    }

}
