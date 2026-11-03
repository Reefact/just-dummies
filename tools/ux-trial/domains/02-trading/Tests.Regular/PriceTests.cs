using System.Globalization;

namespace Trading.Tests.Regular;

public sealed class PriceTests {

    public static TheoryData<decimal> AcceptedAmounts => new() {
        0.0001m,
        0.0123m,
        1m,
        12.5m,
        100.1234m,
        999_999m,
        999_999.9999m
    };

    [Theory]
    [MemberData(nameof(AcceptedAmounts))]
    public void A_price_accepts_a_positive_amount_below_the_ceiling_with_at_most_four_decimals(decimal amount) {
        // Act
        Price price = new Price(amount);

        // Assert
        Assert.Equal(amount, price.Amount);
    }

    public static TheoryData<decimal> NonPositiveAmounts => new() {
        0m,
        -0.0001m,
        -1m,
        decimal.MinValue
    };

    [Theory]
    [MemberData(nameof(NonPositiveAmounts))]
    public void A_price_refuses_a_zero_or_negative_amount(decimal amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Price(amount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
        Assert.Equal(amount, refusal.ActualValue);
    }

    public static TheoryData<decimal> AmountsAtOrAboveTheCeiling => new() {
        1_000_000m,
        1_000_000.0001m,
        decimal.MaxValue
    };

    [Theory]
    [MemberData(nameof(AmountsAtOrAboveTheCeiling))]
    public void A_price_refuses_the_ceiling_itself_and_everything_above_it(decimal amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Price(amount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_ceiling_is_one_million() {
        Assert.Equal(1_000_000m, Price.Ceiling);
    }

    public static TheoryData<decimal> AmountsWithTooManyDecimals => new() {
        0.00001m,
        1.23456m,
        999_999.99999m,
        0.1000001m
    };

    [Theory]
    [MemberData(nameof(AmountsWithTooManyDecimals))]
    public void A_price_refuses_more_than_four_decimal_places_with_a_plain_argument_error(decimal amount) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Price(amount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_price_tolerates_trailing_zeros_beyond_the_fourth_decimal() {
        // Act
        Price price = new Price(0.10000m);

        // Assert
        Assert.Equal(0.1m, price.Amount);
    }

    [Theory]
    [InlineData("-0.00001")]
    [InlineData("0.00000")]
    public void A_non_positive_amount_is_refused_as_out_of_range_before_the_decimals_are_looked_at(string text) {
        // Arrange
        decimal amount = decimal.Parse(text, CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Price(amount));
    }

    [Fact]
    public void An_amount_above_the_ceiling_is_refused_as_out_of_range_before_the_decimals_are_looked_at() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Price(1_000_000.00001m));
    }

    [Fact]
    public void Prices_compare_by_amount() {
        // Arrange
        Price lower  = new Price(10.25m);
        Price higher = new Price(10.26m);

        // Assert
        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
        Assert.Equal(0, lower.CompareTo(new Price(10.25m)));
    }

    [Fact]
    public void Any_price_is_greater_than_null() {
        // Arrange
        Price anyPrice = Any.Price().Generate();

        // Assert
        Assert.Equal(1, anyPrice.CompareTo(null));
    }

    [Fact]
    public void Two_prices_of_the_same_amount_are_equal_whatever_their_trailing_zeros() {
        // Act
        Price first  = new Price(1.5m);
        Price second = new Price(1.5000m);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_prices_of_different_amounts_are_different() {
        Assert.False(new Price(1.5m).Equals(new Price(1.5001m)));
    }

    [Fact]
    public void A_price_is_not_equal_to_a_bare_decimal_nor_to_null() {
        // Arrange
        Price price = new Price(1.5m);

        // Assert
        Assert.False(price.Equals(1.5m));
        Assert.False(price.Equals(null));
    }

    [Fact]
    public void A_price_prints_its_amount_with_the_invariant_culture() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            string text = new Price(1234.5m).ToString();

            // Assert
            Assert.Equal("1234.5", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
