using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class CurrencyTests {

    [Fact]
    public void A_currency_keeps_the_three_letter_code_it_was_given() {
        // Arrange
        string anyCode = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Currency currency = new Currency(anyCode);

        // Assert
        Assert.Equal(anyCode, currency.Code);
        Assert.Equal(anyCode, currency.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("E1R")]
    [InlineData("E R")]
    [InlineData("ÉUR")]
    [InlineData(" EU")]
    public void A_code_that_is_not_three_uppercase_ASCII_letters_is_refused(string invalidCode) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Currency(invalidCode));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void A_missing_code_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Currency(null!));
    }

    [Fact]
    public void Two_currencies_with_the_same_code_are_equal_and_hash_alike() {
        // Arrange
        string   anyCode = Any.StringMatching("[A-Z]{3}").Generate();
        Currency first   = new Currency(anyCode);
        Currency second  = new Currency(anyCode);

        // Act & Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_currencies_with_different_codes_are_not_equal() {
        // Arrange
        Currency euro   = new Currency("EUR");
        Currency dollar = new Currency("USD");

        // Act & Assert
        Assert.False(euro.Equals(dollar));
        Assert.False(euro.Equals((object)dollar));
    }

    [Fact]
    public void A_currency_is_not_equal_to_nothing_or_to_a_plain_string() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act & Assert
        Assert.False(anyCurrency.Equals((Currency?)null));
        Assert.False(anyCurrency.Equals((object?)null));
        Assert.False(anyCurrency.Equals((object)anyCurrency.Code));
    }

}
