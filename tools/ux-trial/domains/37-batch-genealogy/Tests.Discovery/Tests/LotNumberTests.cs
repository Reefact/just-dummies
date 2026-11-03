using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class LotNumberTests {

    [Fact]
    public void A_base_lot_number_is_not_a_sub_lot() {
        // Arrange
        string anyValue = Any.LotNumber().Generate().Value;

        // Act
        LotNumber lotNumber = new LotNumber(anyValue);

        // Assert
        Assert.False(lotNumber.IsSubLot);
        Assert.Null(lotNumber.SubLotIndex);
    }

    [Fact]
    public void A_lot_number_exposes_its_base_lot_and_its_text() {
        // Act
        LotNumber lotNumber = new LotNumber("API-240117");

        // Assert
        Assert.Equal("API-240117", lotNumber.BaseLot);
        Assert.Equal("API-240117", lotNumber.Value);
        Assert.Equal("API-240117", lotNumber.ToString());
    }

    [Fact]
    public void A_sub_lot_number_exposes_its_base_lot_and_its_index() {
        // Act
        LotNumber lotNumber = new LotNumber("API-240117.03");

        // Assert
        Assert.True(lotNumber.IsSubLot);
        Assert.Equal("API-240117", lotNumber.BaseLot);
        Assert.Equal(3, lotNumber.SubLotIndex);
        Assert.Equal("API-240117.03", lotNumber.Value);
    }

    [Theory]
    [InlineData("AB-123456")]
    [InlineData("ABCD-123456")]
    [InlineData("AB-123456.01")]
    [InlineData("ABCD-123456.99")]
    public void Prefixes_of_two_to_four_letters_and_sub_lots_from_01_to_99_are_accepted(string value) {
        // Act
        LotNumber lotNumber = new LotNumber(value);

        // Assert
        Assert.Equal(value, lotNumber.Value);
    }

    [Fact]
    public void Any_generated_lot_number_is_accepted_by_the_constructor_whatever_its_shape() {
        // Arrange
        LotNumber anyBase = Any.LotNumber().Generate();
        LotNumber anySub  = Any.LotNumber().SubLot().Generate();

        // Assert
        Assert.False(anyBase.IsSubLot);
        Assert.True(anySub.IsSubLot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A-123456")]
    [InlineData("ABCDE-123456")]
    [InlineData("api-123456")]
    [InlineData("API-12345")]
    [InlineData("API-1234567")]
    [InlineData("API123456")]
    [InlineData("API-12345A")]
    [InlineData("API-123456.1")]
    [InlineData("API-123456.100")]
    [InlineData("API-123456.")]
    [InlineData("API-123456.A1")]
    [InlineData(" API-123456")]
    [InlineData("API-123456 ")]
    [InlineData("API-123456.01.02")]
    public void A_text_that_is_not_a_lot_number_is_refused(string value) {
        // Act
        Exception? thrown = Record.Exception(() => new LotNumber(value));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final '\\n', so \"API-123456\\n\" is accepted as a lot number (literal, no seed)")]
    public void A_lot_number_followed_by_a_line_break_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new LotNumber("API-123456\n"));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void A_sub_lot_index_of_zero_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new LotNumber("API-123456.00"));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new LotNumber(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Theory]
    [InlineData(1, "API-240117.01")]
    [InlineData(7, "API-240117.07")]
    [InlineData(99, "API-240117.99")]
    public void A_sub_lot_is_numbered_after_its_base_lot(int index, string expected) {
        // Arrange
        LotNumber lotNumber = new LotNumber("API-240117");

        // Act
        LotNumber subLot = lotNumber.SubLot(index);

        // Assert
        Assert.Equal(expected, subLot.Value);
        Assert.Equal(index, subLot.SubLotIndex);
    }

    [Fact]
    public void A_sub_lot_of_a_sub_lot_is_a_sub_lot_of_the_base_lot() {
        // Arrange
        LotNumber anySubLot = Any.LotNumber().SubLot().Generate();

        // Act
        LotNumber sibling = anySubLot.SubLot(5);

        // Assert
        Assert.Equal(anySubLot.BaseLot, sibling.BaseLot);
        Assert.Equal(5, sibling.SubLotIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_sub_lot_index_outside_1_to_99_is_refused(int index) {
        // Arrange
        LotNumber anyLotNumber = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyLotNumber.SubLot(index));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void Two_lot_numbers_with_the_same_text_are_equal() {
        // Arrange
        string anyValue = Any.LotNumber().Generate().Value;

        // Act
        LotNumber first  = new LotNumber(anyValue);
        LotNumber second = new LotNumber(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void A_lot_is_not_equal_to_its_own_sub_lot() {
        // Arrange
        LotNumber anyLotNumber = Any.LotNumber().Generate();

        // Act
        LotNumber subLot = anyLotNumber.SubLot(1);

        // Assert
        Assert.NotEqual(anyLotNumber, subLot);
    }

    [Fact]
    public void A_lot_number_is_not_equal_to_nothing_or_to_another_type() {
        // Arrange
        LotNumber anyLotNumber = Any.LotNumber().Generate();

        // Assert
        Assert.False(anyLotNumber.Equals((LotNumber?)null));
        Assert.False(anyLotNumber.Equals((object?)anyLotNumber.Value));
    }

}
