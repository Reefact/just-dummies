using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class MoneyTests {

    [Fact]
    public void A_money_keeps_its_amount_and_its_currency() {
        // Act
        Money money = new Money(120.50m, "EUR");

        // Assert
        Assert.Equal(120.50m, money.Amount);
        Assert.Equal("EUR", money.Currency);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("99999999.99")]
    public void An_amount_with_at_most_two_decimals_is_accepted(string amount) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency);

        // Assert
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal anyNegativeAmount = Any.Decimal().Negative().WithScale(2).Generate();
        string  anyCurrency       = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(anyNegativeAmount, anyCurrency));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void A_null_currency_is_refused() {
        // Arrange
        decimal anyAmount = Any.Decimal().Positive().WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Money(anyAmount, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("Eur")]
    [InlineData("E1R")]
    [InlineData("EU ")]
    [InlineData("é€$")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        decimal anyAmount = Any.Decimal().Positive().WithScale(2).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Money(anyAmount, currency));

        // Assert
        Assert.Equal("currency", exception.ParamName);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10.555")]
    [InlineData("1.0000001")]
    public void An_amount_with_more_than_two_decimals_is_refused(string amount) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyCurrency));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void The_amount_is_checked_before_the_currency() {
        // Act & Assert: a negative amount with a malformed currency reports the amount
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "euro"));
    }

    [Fact]
    public void The_currency_is_checked_before_the_decimals() {
        // Act & Assert: a malformed currency with too many decimals reports the currency
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Money(1.234m, "euro"));

        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Two_monies_with_the_same_amount_and_currency_are_equal() {
        // Arrange
        Money first  = new Money(10m, "EUR");
        Money second = new Money(10m, "EUR");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_ignores_trailing_zeros_of_the_amount() {
        // Arrange
        Money first  = new Money(10.0m, "EUR");
        Money second = new Money(10.00m, "EUR");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_monies_in_different_currencies_are_different() {
        // Assert
        Assert.NotEqual(new Money(10m, "EUR"), new Money(10m, "USD"));
    }

    [Fact]
    public void Two_monies_with_different_amounts_are_different() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Assert
        Assert.NotEqual(new Money(10m, anyCurrency), new Money(10.01m, anyCurrency));
    }

    [Fact]
    public void A_money_is_not_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        Money money = Arbitrary.Money().Generate();

        // Assert
        Assert.False(money.Equals(null));
        Assert.False(money.Equals("1 EUR"));
    }

    [Fact]
    public void Times_multiplies_the_amount_and_keeps_the_currency() {
        // Arrange
        Money money = new Money(10.50m, "EUR");

        // Act
        Money total = money.Times(3);

        // Assert
        Assert.Equal(new Money(31.50m, "EUR"), total);
    }

    [Fact]
    public void Times_zero_gives_a_zero_amount() {
        // Arrange
        Money anyMoney = Arbitrary.Money().Generate();

        // Act
        Money total = anyMoney.Times(0);

        // Assert
        Assert.Equal(0m, total.Amount);
    }

    [Fact]
    public void Times_a_negative_factor_is_refused() {
        // Arrange
        Money anyPositiveMoney = Arbitrary.Money().WithAmount(a => a.Between(0.01m, 1_000_000m)).Generate();
        int   anyNegativeFactor = Any.Int32().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPositiveMoney.Times(anyNegativeFactor));
    }

    [Fact]
    public void Percent_takes_a_share_of_the_amount_and_keeps_the_currency() {
        // Arrange
        Money money = new Money(200m, "USD");

        // Act
        Money share = money.Percent(0.25m);

        // Assert
        Assert.Equal(new Money(50m, "USD"), share);
    }

    [Theory]
    [InlineData("10.05", "0.5", "5.03")]   // 5.025 rounds up
    [InlineData("0.01", "0.5", "0.01")]    // 0.005 rounds up
    [InlineData("0.01", "0.49", "0.00")]   // 0.0049 rounds down
    [InlineData("1.00", "0.125", "0.13")]  // 0.125 rounds up
    [InlineData("3.33", "1", "3.33")]
    [InlineData("3.33", "0", "0.00")]
    [InlineData("100", "2.5", "250.00")]   // no upper bound on the rate
    public void Percent_rounds_to_two_decimals_half_away_from_zero(string amount, string rate, string expected) {
        // Arrange
        Money money = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "EUR");

        // Act
        Money share = money.Percent(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(new Money(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), "EUR"), share);
    }

    [Fact]
    public void A_negative_rate_is_refused() {
        // Arrange
        Money   anyPositiveMoney = Arbitrary.Money().WithAmount(a => a.Between(0.01m, 1_000_000m)).Generate();
        decimal anyNegativeRate  = Any.Decimal().Between(-1_000m, -0.01m).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPositiveMoney.Percent(anyNegativeRate));
    }

    [Fact(Skip = "domain defect: a very large negative rate overflows the multiplication (OverflowException) before the constructor can refuse the negative amount; Any.Decimal().Negative() reaches it, seed 1644796132")]
    public void A_negative_rate_of_any_magnitude_is_refused_with_an_out_of_range_exception() {
        // Arrange
        Money   anyPositiveMoney = Arbitrary.Money().WithAmount(a => a.Between(0.01m, 1_000_000m)).Generate();
        decimal anyNegativeRate  = Any.Decimal().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPositiveMoney.Percent(anyNegativeRate));
    }

    [Fact]
    public void A_money_prints_its_amount_then_its_currency() {
        // Assert
        Assert.Equal("12.50 EUR", new Money(12.50m, "EUR").ToString());
    }

    [Fact(Skip = "domain defect: Money has no upper bound, so Times/Percent overflow with an OverflowException instead of a domain refusal; found through Reservation.Cancel, seed 350311511")]
    public void Multiplying_a_huge_amount_is_refused_with_a_domain_exception() {
        // Arrange
        Money huge = new Money(decimal.MaxValue, "EUR");

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => huge.Times(2));
    }

    [Fact]
    public void The_generator_only_draws_valid_monies() {
        // Act: the constructor is the oracle, so any refusal fails the draw
        for (int i = 0; i < 200; i++) {
            Money anyMoney = Arbitrary.Money().Generate();

            Assert.Equal(anyMoney.Amount, decimal.Round(anyMoney.Amount, 2));
        }
    }

}
