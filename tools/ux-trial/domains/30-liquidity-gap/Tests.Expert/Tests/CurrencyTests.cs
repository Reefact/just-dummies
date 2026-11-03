
namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class CurrencyTests {

    [Fact]
    public void A_currency_is_made_of_three_uppercase_letters() {
        // Act
        Currency currency = new Currency("EUR");

        // Assert
        Assert.Equal("EUR", currency.Code);
        Assert.Equal("EUR", currency.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("E1R")]
    [InlineData("EU ")]
    [InlineData("ÉUR")]
    public void A_code_that_is_not_three_uppercase_ASCII_letters_is_refused(string code) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Currency(code));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void A_missing_code_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Currency(null!));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void Two_currencies_with_the_same_code_are_equal_and_hash_alike() {
        // Arrange
        Currency euro      = new Currency("EUR");
        Currency otherEuro = new Currency("EUR");

        // Assert
        Assert.Equal(euro, otherEuro);
        Assert.True(euro.Equals(otherEuro));
        Assert.Equal(euro.GetHashCode(), otherEuro.GetHashCode());
    }

    [Fact]
    public void Currencies_with_different_codes_are_different() {
        // Assert
        Assert.NotEqual(new Currency("EUR"), new Currency("USD"));
    }

    [Fact]
    public void A_currency_is_not_equal_to_nothing() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Assert
        Assert.False(anyCurrency.Equals(null));
        Assert.False(anyCurrency.Equals((object?)null));
    }

}
