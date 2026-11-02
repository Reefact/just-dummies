using System.Globalization;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Shop.Domain;

public sealed class MoneyTests {

    [Fact]
    public void A_money_keeps_its_amount_and_its_currency() {
        Money money = new Money(12.50m, "USD");

        Assert.Equal(12.50m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void The_three_shop_currencies_are_accepted(string currency) {
        Money money = new Money(1m, currency);

        Assert.Equal(currency, money.Currency);
    }

    [Fact]
    public void Zero_is_an_accepted_amount() {
        string anyCurrency = Any.Money().Generate().Currency;

        Money money = new Money(0m, anyCurrency);

        Assert.Equal(0m, money.Amount);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-1")]
    [InlineData("-1000000")]
    public void A_negative_amount_is_refused(string amount) {
        string anyCurrency = Any.Money().Generate().Currency;

        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("0.005")]
    [InlineData("12.345")]
    public void An_amount_with_more_than_two_decimal_places_is_refused(string amount) {
        string anyCurrency = Any.Money().Generate().Currency;

        Assert.Throws<ArgumentException>(() => new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), anyCurrency));
    }

    [Fact]
    public void Trailing_zeros_beyond_two_decimal_places_are_not_a_precision_problem() {
        string anyCurrency = Any.Money().Generate().Currency;

        Money money = new Money(1.500m, anyCurrency);

        Assert.Equal(1.5m, money.Amount);
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("eur")]
    [InlineData("EUR ")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unsupported_currency_is_refused(string? currency) {
        decimal anyAmount = Any.Money().Generate().Amount;

        Assert.Throws<ArgumentException>(() => new Money(anyAmount, currency!));
    }

    [Fact]
    public void A_negative_amount_is_reported_before_an_unsupported_currency() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "CHF"));
    }

    [Fact]
    public void Zero_builds_a_nil_amount_in_the_currency_asked() {
        Money zero = Money.Zero("GBP");

        Assert.Equal(new Money(0m, "GBP"), zero);
    }

    [Fact]
    public void Two_moneys_with_the_same_amount_and_currency_are_equal() {
        Money first  = new Money(10m, "EUR");
        Money second = new Money(10.00m, "EUR");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_moneys_in_different_currencies_are_not_equal() {
        Assert.NotEqual(new Money(10m, "EUR"), new Money(10m, "USD"));
    }

    [Fact]
    public void Two_moneys_with_different_amounts_are_not_equal() {
        Assert.NotEqual(new Money(10m, "EUR"), new Money(10.01m, "EUR"));
    }

    [Fact]
    public void A_money_is_not_equal_to_something_that_is_not_a_money() {
        Money money = new Money(10m, "EUR");

        Assert.False(money.Equals("10.00 EUR"));
        Assert.False(money.Equals(null));
    }

    [Fact]
    public void ToString_writes_two_decimals_then_the_currency() {
        Assert.Equal("12.50 EUR", new Money(12.5m, "EUR").ToString());
        Assert.Equal("0.00 GBP", Money.Zero("GBP").ToString());
        Assert.Equal("7.00 USD", new Money(7m, "USD").ToString());
    }

    [Fact(Skip = "domain defect: Money.ToString() formats the amount with the current culture ('12,50 EUR' under fr-FR), although the model says the form is '0.00 EUR'")]
    public void ToString_does_not_depend_on_the_current_culture() {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            Assert.Equal("12.50 EUR", new Money(12.5m, "EUR").ToString());
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Plus_adds_two_amounts_of_the_same_currency() {
        Money sum = new Money(10.25m, "EUR").Plus(new Money(0.80m, "EUR"));

        Assert.Equal(new Money(11.05m, "EUR"), sum);
    }

    [Fact]
    public void Plus_refuses_two_different_currencies() {
        string anyCurrency      = Any.Money().Generate().Currency;
        string anyOtherCurrency = Any.String().OneOf("EUR", "USD", "GBP").DifferentFrom(anyCurrency).Generate();

        Money money = new Money(1m, anyCurrency);

        Assert.Throws<InvalidOperationException>(() => money.Plus(new Money(1m, anyOtherCurrency)));
    }

    [Fact]
    public void Plus_leaves_both_operands_untouched() {
        Money left  = new Money(3m, "EUR");
        Money right = new Money(4m, "EUR");

        left.Plus(right);

        Assert.Equal(new Money(3m, "EUR"), left);
        Assert.Equal(new Money(4m, "EUR"), right);
    }

    [Theory]
    [InlineData("2.50", 3, "7.50")]
    [InlineData("2.50", 0, "0.00")]
    [InlineData("2.50", 1, "2.50")]
    [InlineData("0.01", 99, "0.99")]
    public void Times_multiplies_the_amount_by_the_factor(string amount, int factor, string expected) {
        Money result = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "EUR").Times(factor);

        Assert.Equal(new Money(decimal.Parse(expected, CultureInfo.InvariantCulture), "EUR"), result);
    }

    [Fact]
    public void Times_a_negative_factor_is_refused_by_the_constructor() {
        Money money = new Money(2.50m, "EUR");

        Assert.Throws<ArgumentOutOfRangeException>(() => money.Times(-1));
    }

    [Fact]
    public void Times_keeps_the_currency() {
        Money result = new Money(2m, "GBP").Times(4);

        Assert.Equal("GBP", result.Currency);
    }

    [Theory]
    [InlineData("100.00", "10", "90.00")]
    [InlineData("100.00", "0", "100.00")]
    [InlineData("100.00", "100", "0.00")]
    [InlineData("80.00", "12.5", "70.00")]
    [InlineData("19.99", "10", "17.99")]
    public void Less_takes_a_percentage_off(string amount, string percent, string expected) {
        Money result = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "EUR")
            .Less(new DiscountPercentage(decimal.Parse(percent, CultureInfo.InvariantCulture)));

        Assert.Equal(new Money(decimal.Parse(expected, CultureInfo.InvariantCulture), "EUR"), result);
    }

    [Theory]
    [InlineData("0.05", "10", "0.04")]    // reduction 0.005 rounds up to 0.01
    [InlineData("0.15", "50", "0.07")]    // reduction 0.075 rounds up to 0.08
    [InlineData("0.04", "10", "0.04")]    // reduction 0.004 rounds down to 0.00
    [InlineData("10.01", "0.5", "9.96")]  // reduction 0.05005 rounds down to 0.05
    public void Less_rounds_the_reduction_half_away_from_zero(string amount, string percent, string expected) {
        Money result = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "USD")
            .Less(new DiscountPercentage(decimal.Parse(percent, CultureInfo.InvariantCulture)));

        Assert.Equal(new Money(decimal.Parse(expected, CultureInfo.InvariantCulture), "USD"), result);
    }

    [Fact(Skip = "domain defect: Money has no upper bound, so Times()/Plus() throw OverflowException on a valid amount (about 1 draw in 10 of an unconstrained amount), seed 1306754213")]
    [Reproducible(Seed = 1306754213)]
    public void Times_a_valid_price_by_a_valid_quantity_does_not_overflow() {
        Money anyPrice = Any.Money().Generate();

        Money result = anyPrice.Times(Quantity.Maximum);

        Assert.Equal(anyPrice.Currency, result.Currency);
    }

    [Fact(Skip = "domain defect: Money has no upper bound, so Plus() of two valid amounts throws OverflowException (literal case)")]
    public void Plus_two_valid_amounts_does_not_overflow() {
        Money large = new Money(decimal.MaxValue, "EUR");

        Assert.Equal("EUR", large.Plus(new Money(1m, "EUR")).Currency);
    }

    [Fact]
    public void Less_keeps_the_currency() {
        DiscountPercentage anyDiscount = Any.DiscountPercentage().Generate();

        Money result = new Money(100m, "GBP").Less(anyDiscount);

        Assert.Equal("GBP", result.Currency);
    }

    [Fact]
    public void Less_never_leaves_a_negative_amount_for_a_valid_discount() {
        // 100 % off the smallest amount: the reduction cannot exceed the amount.
        Money result = new Money(0.01m, "EUR").Less(new DiscountPercentage(100m));

        Assert.Equal(Money.Zero("EUR"), result);
    }

}
