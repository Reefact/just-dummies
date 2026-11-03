using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class PriceTests {

    [Theory]
    [InlineData("0.0001")]
    [InlineData("1")]
    [InlineData("12.3456")]
    [InlineData("999999.9999")]
    public void A_positive_amount_below_the_ceiling_with_at_most_four_decimals_is_a_price(string amount) {
        // Act
        Price price = new Price(Literal.Decimal(amount));

        // Assert
        Assert.Equal(Literal.Decimal(amount), price.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.0001")]
    [InlineData("-1")]
    [InlineData("-79228162514264337593543950335")]
    public void An_amount_that_is_not_strictly_positive_is_refused(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Price(Literal.Decimal(amount)));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("1000000")]
    [InlineData("1000000.0001")]
    [InlineData("79228162514264337593543950335")]
    public void An_amount_at_or_above_the_exchange_ceiling_is_refused(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Price(Literal.Decimal(amount)));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("1.00005")]
    [InlineData("999999.99999")]
    public void An_amount_with_a_fifth_significant_decimal_is_refused_as_an_invalid_argument(string amount) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Price(Literal.Decimal(amount)));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.00001")]
    [InlineData("1000000.00001")]
    public void The_range_is_checked_before_the_decimals(string amount) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Price(Literal.Decimal(amount)));
    }

    [Fact]
    public void The_ceiling_is_one_million() {
        // Act & Assert
        Assert.Equal(1_000_000m, Price.Ceiling);
    }

    [Fact]
    public void A_price_compares_by_its_amount() {
        // Arrange
        Price cheaper = new Price(1.5m);
        Price dearer  = new Price(2.5m);

        // Act & Assert
        Assert.True(cheaper.CompareTo(dearer) < 0);
        Assert.True(dearer.CompareTo(cheaper) > 0);
        Assert.Equal(0, cheaper.CompareTo(new Price(1.5m)));
    }

    [Fact]
    public void A_price_is_greater_than_no_price() {
        // Arrange
        Price anyPrice = Any.Price().Generate();

        // Act
        int comparison = anyPrice.CompareTo(null);

        // Assert
        Assert.True(comparison > 0);
    }

    [Fact]
    public void Prices_sort_from_the_cheapest_to_the_dearest() {
        // Arrange
        List<Price> prices = [new Price(10m), new Price(0.5m), new Price(7.25m)];

        // Act
        prices.Sort();

        // Assert
        Assert.Equal([0.5m, 7.25m, 10m], prices.Select(price => price.Amount));
    }

    [Fact]
    public void Two_prices_of_the_same_amount_are_equal_whatever_the_trailing_zeros() {
        // Arrange
        Price once  = new Price(1.5m);
        Price twice = new Price(1.5000m);

        // Act & Assert
        Assert.Equal(once, twice);
        Assert.Equal(once.GetHashCode(), twice.GetHashCode());
    }

    [Fact]
    public void Two_prices_of_different_amounts_are_not_equal() {
        // Arrange
        Price anyPrice      = Any.Price().Generate();
        Price anyOtherPrice = Any.Price().WithAmount(amount => amount.DifferentFrom(anyPrice.Amount)).Generate();

        // Act & Assert
        Assert.NotEqual(anyPrice, anyOtherPrice);
    }

    [Fact]
    public void A_price_shows_as_its_amount_in_the_invariant_culture() {
        // Arrange
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try {
            // Act
            string shown = new Price(1234.5m).ToString();

            // Assert
            Assert.Equal("1234.5", shown);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
