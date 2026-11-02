using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class MoneyTests {

    [Fact]
    public void A_money_keeps_the_amount_and_the_currency_it_was_given() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Money money = new Money(12.50m, anyCurrency);

        // Assert
        Assert.Equal(12.50m, money.Amount);
        Assert.Equal(anyCurrency, money.Currency);
    }

    [Fact]
    public void A_zero_amount_is_accepted() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Money money = new Money(0m, anyCurrency);

        // Assert
        Assert.Equal(0m, money.Amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void A_negative_amount_is_refused(double negative) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Money((decimal)negative, anyCurrency));

        // Assert
        Assert.Equal("amount", error.ParamName);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U1D")]
    [InlineData("")]
    [InlineData(" USD")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string badCurrency) {
        // Arrange
        decimal anyAmount = Any.Decimal().GreaterThanOrEqualTo(0m).LessThanOrEqualTo(1_000_000m).WithScale(2).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Money(anyAmount, badCurrency));

        // Assert
        Assert.Equal("currency", error.ParamName);
    }

    [Fact]
    public void A_null_currency_is_refused_as_a_null() {
        // Arrange
        decimal anyAmount = Any.Decimal().GreaterThanOrEqualTo(0m).LessThanOrEqualTo(1_000_000m).WithScale(2).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Money(anyAmount, null!));
    }

    [Theory]
    [InlineData(1.005)]
    [InlineData(0.001)]
    [InlineData(10.123)]
    public void An_amount_with_more_than_two_decimal_places_is_refused(double tooPrecise) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Money((decimal)tooPrecise, anyCurrency));

        // Assert
        Assert.Equal("amount", error.ParamName);
    }

    [Fact]
    public void The_amount_is_checked_before_the_currency() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "nope"));

        // Assert
        Assert.Equal("amount", error.ParamName);
    }

    [Fact]
    public void The_currency_is_checked_before_the_number_of_decimals() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Money(1.005m, "nope"));

        // Assert
        Assert.Equal("currency", error.ParamName);
    }

    [Fact]
    public void Two_moneys_with_the_same_amount_and_currency_are_equal() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Money first  = new Money(10m, anyCurrency);
        Money second = new Money(10.00m, anyCurrency);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_moneys_in_different_currencies_are_not_equal() {
        // Arrange
        string anyCurrency      = Any.StringMatching("[A-Z]{3}").Generate();
        string anyOtherCurrency = Any.StringMatching("[A-Z]{3}").DifferentFrom(anyCurrency).Generate();

        // Act
        Money first  = new Money(10m, anyCurrency);
        Money second = new Money(10m, anyOtherCurrency);

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Two_moneys_with_different_amounts_are_not_equal() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act / Assert
        Assert.NotEqual(new Money(10m, anyCurrency), new Money(10.01m, anyCurrency));
    }

    [Fact]
    public void A_money_is_not_equal_to_something_that_is_not_a_money() {
        // Arrange
        Money anyMoney = Any.Money().Generate();

        // Act / Assert
        Assert.False(anyMoney.Equals("10 EUR"));
        Assert.False(anyMoney.Equals(null));
    }

    [Fact]
    public void Times_multiplies_the_amount_and_keeps_the_currency() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        Money  night       = new Money(25.50m, anyCurrency);

        // Act
        Money total = night.Times(3);

        // Assert
        Assert.Equal(new Money(76.50m, anyCurrency), total);
    }

    [Fact]
    public void Times_zero_gives_a_zero_amount() {
        // Arrange
        Money night = new Money(25.50m, Any.StringMatching("[A-Z]{3}").Generate());

        // Act
        Money total = night.Times(0);

        // Assert
        Assert.Equal(0m, total.Amount);
    }

    [Fact]
    public void Times_a_negative_factor_is_refused() {
        // Arrange
        Money night = new Money(25.50m, Any.StringMatching("[A-Z]{3}").Generate());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => night.Times(-1));
    }

    [Theory]
    [InlineData(200, 0.25, 50)]
    [InlineData(200, 1, 200)]
    [InlineData(200, 0, 0)]
    [InlineData(10, 2, 20)]
    public void Percent_takes_a_share_of_the_amount(double amount, double rate, double expected) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        Money  money       = new Money((decimal)amount, anyCurrency);

        // Act
        Money share = money.Percent((decimal)rate);

        // Assert
        Assert.Equal(new Money((decimal)expected, anyCurrency), share);
    }

    [Fact]
    public void Percent_rounds_to_two_decimals_half_away_from_zero() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        Money  money       = new Money(0.05m, anyCurrency);

        // Act
        Money share = money.Percent(0.5m);   // 0.025

        // Assert
        Assert.Equal(0.03m, share.Amount);
    }

    [Fact]
    public void Percent_with_a_negative_rate_is_refused() {
        // Arrange
        Money money = new Money(100m, Any.StringMatching("[A-Z]{3}").Generate());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => money.Percent(-0.1m));
    }

}
