using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class CurrencyTests {

    [Fact]
    public void A_currency_is_built_from_three_uppercase_letters() {
        // Act
        Currency currency = new Currency("EUR");

        // Assert
        Assert.Equal("EUR", currency.Code);
        Assert.Equal("EUR", currency.ToString());
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("EU ")]
    [InlineData("")]
    [InlineData("ÉUR")]
    public void A_code_that_is_not_three_uppercase_ASCII_letters_is_refused(string code) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Currency(code));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Currency(null!));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void Two_currencies_with_the_same_code_are_equal() {
        // Arrange
        Currency first  = Any.Currency().WithCode("CHF").Generate();
        Currency second = new Currency("CHF");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_currencies_with_different_codes_are_not_equal() {
        // Arrange
        Currency euro   = new Currency("EUR");
        Currency dollar = new Currency("USD");

        // Assert
        Assert.NotEqual(euro, dollar);
        Assert.False(euro.Equals(null));
        Assert.False(euro.Equals((object)"EUR"));
    }

}
