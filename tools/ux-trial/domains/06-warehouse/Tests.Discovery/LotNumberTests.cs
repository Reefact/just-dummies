using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class LotNumberTests {

    [Fact]
    public void A_lot_number_is_stored_exactly_as_given() {
        LotNumber lot = new LotNumber("  L-2026/03 ");

        Assert.Equal("  L-2026/03 ", lot.Value);
        Assert.Equal("  L-2026/03 ", lot.ToString());
    }

    [Fact]
    public void Twenty_characters_are_accepted() {
        // Arrange
        string anyTwentyCharacters = Any.String().NotBlank().WithLength(20).Generate();

        // Act
        LotNumber lot = new LotNumber(anyTwentyCharacters);

        // Assert
        Assert.Equal(20, lot.Value.Length);
    }

    [Fact]
    public void Twenty_one_characters_are_refused() {
        // Arrange
        string anyTwentyOneCharacters = Any.String().NotBlank().WithLength(21).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new LotNumber(anyTwentyOneCharacters));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_null_lot_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LotNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_lot_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new LotNumber(blank));
    }

}
