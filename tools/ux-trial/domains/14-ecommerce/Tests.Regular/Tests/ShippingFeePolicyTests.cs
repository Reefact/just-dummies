using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class ShippingFeePolicyTests {

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Express_costs_14_90_whatever_the_country_and_the_amount(string countryCode) {
        // Arrange
        Order orderBelow = Dummies.OrderOf([Dummies.Line(1m, 1)], ShippingMethod.Express, countryCode: countryCode);
        Order orderAbove = Dummies.OrderOf([Dummies.Line(500m, 1)], ShippingMethod.Express, countryCode: countryCode);

        // Act & Assert
        Assert.Equal(new Money(14.90m, "EUR"), ShippingFeePolicy.FeeFor(orderBelow));
        Assert.Equal(new Money(14.90m, "EUR"), ShippingFeePolicy.FeeFor(orderAbove));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void A_pickup_point_costs_2_90_whatever_the_country_and_the_amount(string countryCode) {
        // Arrange
        Order orderBelow = Dummies.OrderOf([Dummies.Line(1m, 1)], ShippingMethod.PickupPoint, countryCode: countryCode);
        Order orderAbove = Dummies.OrderOf([Dummies.Line(500m, 1)], ShippingMethod.PickupPoint, countryCode: countryCode);

        // Act & Assert
        Assert.Equal(new Money(2.90m, "EUR"), ShippingFeePolicy.FeeFor(orderBelow));
        Assert.Equal(new Money(2.90m, "EUR"), ShippingFeePolicy.FeeFor(orderAbove));
    }

    [Fact]
    public void Standard_shipping_below_the_threshold_costs_4_90_in_France() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(49.99m, 1)], ShippingMethod.Standard, countryCode: "FR");

        // Act & Assert
        Assert.Equal(new Money(4.90m, "EUR"), ShippingFeePolicy.FeeFor(order));
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Standard_shipping_below_the_threshold_costs_9_90_abroad(string countryCode) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(49.99m, 1)], ShippingMethod.Standard, countryCode: countryCode);

        // Act & Assert
        Assert.Equal(new Money(9.90m, "EUR"), ShippingFeePolicy.FeeFor(order));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Standard_shipping_is_free_when_the_discounted_subtotal_reaches_exactly_the_threshold(string countryCode) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(50m, 1)], ShippingMethod.Standard, countryCode: countryCode);

        // Act & Assert
        Assert.Equal(new Money(0m, "EUR"), ShippingFeePolicy.FeeFor(order));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    public void Standard_shipping_is_free_above_the_threshold(string countryCode) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(50.01m, 1)], ShippingMethod.Standard, countryCode: countryCode);

        // Act & Assert
        Assert.Equal(Money.Zero("EUR"), ShippingFeePolicy.FeeFor(order));
    }

    [Fact]
    public void The_threshold_is_the_published_constant() {
        // Act & Assert
        Assert.Equal(50m, Order.FreeShippingThreshold);
    }

    [Fact]
    public void The_threshold_applies_to_the_subtotal_after_the_discount() {
        // Arrange
        Order stillBelow = Dummies.OrderOf([Dummies.Line(60m, 1)], ShippingMethod.Standard, discount: 20m, countryCode: "FR");     // 48.00
        Order justAt     = Dummies.OrderOf([Dummies.Line(62.50m, 1)], ShippingMethod.Standard, discount: 20m, countryCode: "FR");  // 50.00

        // Act & Assert
        Assert.Equal(new Money(4.90m, "EUR"), ShippingFeePolicy.FeeFor(stillBelow));
        Assert.Equal(new Money(0m, "EUR"), ShippingFeePolicy.FeeFor(justAt));
    }

    [Fact]
    public void A_full_discount_brings_a_large_order_back_under_the_threshold() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(500m, 1)], ShippingMethod.Standard, discount: 100m, countryCode: "DE");

        // Act & Assert
        Assert.Equal(new Money(9.90m, "EUR"), ShippingFeePolicy.FeeFor(order));
    }

    [Theory]
    [InlineData("EUR", ShippingMethod.Standard)]
    [InlineData("USD", ShippingMethod.Express)]
    [InlineData("GBP", ShippingMethod.PickupPoint)]
    [InlineData("GBP", ShippingMethod.Standard)]
    public void The_fee_is_in_the_currency_of_the_order(string currency, ShippingMethod method) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(10m, 1, currency)], method);

        // Act
        Money fee = ShippingFeePolicy.FeeFor(order);

        // Assert
        Assert.Equal(currency, fee.Currency);
    }

    [Fact]
    public void The_policy_does_not_convert_amounts_between_currencies() {
        // Arrange: 50 USD and 50 GBP both reach the threshold of 50, whatever their worth
        Order dollars = Dummies.OrderOf([Dummies.Line(50m, 1, "USD")], ShippingMethod.Standard);
        Order pounds  = Dummies.OrderOf([Dummies.Line(50m, 1, "GBP")], ShippingMethod.Standard);

        // Act & Assert
        Assert.Equal(Money.Zero("USD"), ShippingFeePolicy.FeeFor(dollars));
        Assert.Equal(Money.Zero("GBP"), ShippingFeePolicy.FeeFor(pounds));
    }

    [Fact]
    public void An_undefined_shipping_method_is_refused() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(10m, 1)], (ShippingMethod)(-1));

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => ShippingFeePolicy.FeeFor(order));

        // Assert
        Assert.Equal("order", exception.ParamName);
    }

}
