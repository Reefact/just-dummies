using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class MoneyTests {

    private static readonly AnyDecimal AnyValidAmount = AnyMoney.ValidAmounts();

    #region Construction

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void A_supported_currency_is_kept_as_given(string currency) {
        // Arrange
        decimal anyAmount = AnyValidAmount.Generate();

        // Act
        Money money = new Money(anyAmount, currency);

        // Assert
        Assert.Equal(currency, money.Currency);
    }

    [Fact]
    public void A_zero_amount_is_accepted() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();

        // Act
        Money money = new Money(0m, anyCurrency);

        // Assert
        Assert.Equal(0m, money.Amount);
    }

    public static TheoryData<decimal> NegativeAmounts => new TheoryData<decimal> { -0.01m, -1m };

    [Theory]
    [MemberData(nameof(NegativeAmounts))]
    public void A_negative_amount_is_refused(decimal amount) {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, anyCurrency));

        // Assert
        Assert.Equal("amount", error.ParamName);
    }

    public static TheoryData<decimal> AmountsFinerThanACent => new TheoryData<decimal> { 0.001m, 12.505m };

    [Theory]
    [MemberData(nameof(AmountsFinerThanACent))]
    public void An_amount_finer_than_a_cent_is_refused(decimal amount) {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Money(amount, anyCurrency));

        // Assert
        Assert.Equal("amount", error.ParamName);
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("eur")]
    [InlineData(" EUR")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unsupported_currency_is_refused(string? currency) {
        // Arrange
        decimal anyAmount = AnyValidAmount.Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Money(anyAmount, currency!));

        // Assert
        Assert.Equal("currency", error.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_reported_before_an_unsupported_currency() {
        // Act
        Exception error = Assert.ThrowsAny<Exception>(() => new Money(-1m, "CHF"));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void Zero_is_an_amount_of_nothing_in_the_given_currency() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();

        // Act
        Money zero = Money.Zero(anyCurrency);

        // Assert
        Assert.Equal(new Money(0m, anyCurrency), zero);
    }

    #endregion

    #region Equality and display

    [Fact]
    public void Two_amounts_of_the_same_value_and_currency_are_equal() {
        // Arrange
        Money anyMoney = Any.Money().Generate();
        Money sameMoney = new Money(anyMoney.Amount, anyMoney.Currency);

        // Act
        bool areEqual = anyMoney.Equals(sameMoney);

        // Assert
        Assert.True(areEqual);
        Assert.Equal(anyMoney.GetHashCode(), sameMoney.GetHashCode());
    }

    [Fact]
    public void Two_amounts_of_different_values_are_not_equal() {
        // Arrange
        Money anyMoney      = Any.Money().Generate();
        Money anyOtherMoney = Any.Money().WithCurrency(anyMoney.Currency).WithAmount(a => a.DifferentFrom(anyMoney.Amount)).Generate();

        // Act
        bool areEqual = anyMoney.Equals(anyOtherMoney);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Two_amounts_of_different_currencies_are_not_equal() {
        // Arrange
        Money anyMoney      = Any.Money().Generate();
        Money anyOtherMoney = Any.Money().WithAmount(anyMoney.Amount).WithCurrency(c => c.DifferentFrom(anyMoney.Currency)).Generate();

        // Act
        bool areEqual = anyMoney.Equals(anyOtherMoney);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void An_amount_is_not_equal_to_something_that_is_not_an_amount() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act
        bool areEqual = anyMoney.Equals(anyMoney.Amount);

        // Assert
        Assert.False(areEqual);
    }

    public static TheoryData<decimal, string, string> Displays => new TheoryData<decimal, string, string> {
        { 12.5m, "EUR", "12.50 EUR" },
        { 0m, "USD", "0.00 USD" },
        { 1234.56m, "GBP", "1234.56 GBP" }
    };

    [Theory]
    [MemberData(nameof(Displays))]
    public void It_is_displayed_with_two_decimals_then_the_currency(decimal amount, string currency, string expected) {
        // Arrange
        Money money = new Money(amount, currency);

        // Act
        string text = money.ToString();

        // Assert
        Assert.Equal(expected, text);
    }

    #endregion

    #region Plus and Times

    [Fact]
    public void Adding_two_amounts_of_one_currency_sums_them() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();
        Money  price       = new Money(10.25m, anyCurrency);
        Money  shipping    = new Money(0.75m, anyCurrency);

        // Act
        Money sum = price.Plus(shipping);

        // Assert
        Assert.Equal(new Money(11.00m, anyCurrency), sum);
    }

    [Fact]
    public void Adding_amounts_of_different_currencies_is_refused() {
        // Arrange
        Money anyMoney      = Any.Money().Generate();
        Money anyOtherMoney = Any.Money().WithCurrency(c => c.DifferentFrom(anyMoney.Currency)).Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyMoney.Plus(anyOtherMoney));

        // Assert
        Assert.Contains(anyMoney.Currency, error.Message);
        Assert.Contains(anyOtherMoney.Currency, error.Message);
    }

    public static TheoryData<decimal, int, decimal> Multiplications => new TheoryData<decimal, int, decimal> {
        { 2.50m, 4, 10.00m },
        { 19.99m, 1, 19.99m },
        { 19.99m, 0, 0m }
    };

    [Theory]
    [MemberData(nameof(Multiplications))]
    public void Multiplying_by_a_whole_factor_scales_the_amount(decimal amount, int factor, decimal expected) {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();
        Money  unitPrice   = new Money(amount, anyCurrency);

        // Act
        Money total = unitPrice.Times(factor);

        // Assert
        Assert.Equal(new Money(expected, anyCurrency), total);
    }

    [Fact]
    public void Multiplying_by_a_negative_factor_is_refused() {
        // Arrange
        string anyCurrency = AnyMoney.ValidCurrencies().Generate();
        Money  price       = new Money(5m, anyCurrency);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => price.Times(-1));
    }

    #endregion

    #region Less

    public static TheoryData<decimal, decimal, decimal> Reductions => new TheoryData<decimal, decimal, decimal> {
        { 200.00m, 10m, 180.00m },
        { 80.00m, 0m, 80.00m },
        { 80.00m, 100m, 0.00m },
        { 33.33m, 33.3m, 22.23m }
    };

    [Theory]
    [MemberData(nameof(Reductions))]
    public void A_percentage_comes_off_the_amount(decimal amount, decimal percentage, decimal expected) {
        // Arrange
        string             anyCurrency = AnyMoney.ValidCurrencies().Generate();
        Money              price       = new Money(amount, anyCurrency);
        DiscountPercentage discount    = new DiscountPercentage(percentage);

        // Act
        Money discounted = price.Less(discount);

        // Assert
        Assert.Equal(new Money(expected, anyCurrency), discounted);
    }

    public static TheoryData<decimal, decimal, decimal> MidpointReductions => new TheoryData<decimal, decimal, decimal> {
        { 0.05m, 10m, 0.04m },     // the reduction is 0.005: it rounds up to 0.01
        { 1.00m, 12.5m, 0.87m },   // the reduction is 0.125: it rounds up to 0.13 (banker's rounding would give 0.12)
        { 0.04m, 12.5m, 0.03m }    // the reduction is 0.005 again
    };

    [Theory]
    [MemberData(nameof(MidpointReductions))]
    public void A_reduction_exactly_between_two_cents_is_rounded_away_from_zero(decimal amount, decimal percentage, decimal expected) {
        // Arrange
        string             anyCurrency = AnyMoney.ValidCurrencies().Generate();
        Money              price       = new Money(amount, anyCurrency);
        DiscountPercentage discount    = new DiscountPercentage(percentage);

        // Act
        Money discounted = price.Less(discount);

        // Assert
        Assert.Equal(new Money(expected, anyCurrency), discounted);
    }

    #endregion

}
