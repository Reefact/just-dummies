using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class ReimbursementPriceTests {

    [Theory]
    [InlineData("0.01")]
    [InlineData("0.50")]
    [InlineData("12.34")]
    [InlineData("99999.99")]
    [InlineData("100000")]      // the ceiling itself is accepted
    [InlineData("100000.00")]
    [InlineData("1.500")]       // a third decimal that is a zero is still to the cent
    public void A_price_to_the_cent_between_zero_and_100000_is_accepted(string amount) {
        // Arrange
        decimal parsed = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ReimbursementPrice price = new ReimbursementPrice(parsed);

        // Assert
        Assert.Equal(parsed, price.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1")]
    [InlineData("100000.01")]
    [InlineData("100000.001")]
    public void A_price_outside_zero_to_100000_is_refused_as_out_of_range(string amount) {
        // Arrange
        decimal parsed = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementPrice(parsed));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("12.345")]
    [InlineData("99999.999")]
    public void A_price_finer_than_the_cent_is_refused_as_an_argument_exception(string amount) {
        // Arrange
        decimal parsed = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ReimbursementPrice(parsed));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Prices_of_the_same_amount_are_equal_whatever_their_written_scale() {
        // Arrange
        ReimbursementPrice first  = new ReimbursementPrice(5.3m);
        ReimbursementPrice second = new ReimbursementPrice(5.30m);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Prices_of_different_amounts_are_not_equal() {
        // Assert
        Assert.False(new ReimbursementPrice(5.30m).Equals(new ReimbursementPrice(5.31m)));
    }

    [Fact]
    public void A_price_is_not_equal_to_null() {
        // Assert
        Assert.False(new ReimbursementPrice(5.30m).Equals(null));
    }

}
