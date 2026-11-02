using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class ShippingFeePolicyTests {

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static AnyOrder OrderOfSubtotal(string subtotal, string country, ShippingMethod method, string currency = "EUR") {
        ShippingAddress address = Any.ShippingAddress().WithCountryCode(country).Generate();

        return Lines.OrderOf(Lines.Of(D(subtotal), 1, currency))
                    .WithDiscount(DiscountPercentage.None)
                    .WithShippingAddress(address)
                    .WithShippingMethod(method);
    }

    [Fact]
    public void The_free_shipping_threshold_is_fifty() {
        Assert.Equal(50m, Order.FreeShippingThreshold);
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Express_is_fourteen_ninety_wherever_it_ships(string country) {
        Order order = OrderOfSubtotal("10.00", country, ShippingMethod.Express).Generate();

        Assert.Equal(new Money(14.90m, "EUR"), order.ShippingFee);
    }

    [Fact]
    public void Express_is_not_free_above_the_threshold() {
        Order order = OrderOfSubtotal("500.00", "FR", ShippingMethod.Express).Generate();

        Assert.Equal(new Money(14.90m, "EUR"), order.ShippingFee);
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void A_pickup_point_is_two_ninety_wherever_it_ships(string country) {
        Order order = OrderOfSubtotal("10.00", country, ShippingMethod.PickupPoint).Generate();

        Assert.Equal(new Money(2.90m, "EUR"), order.ShippingFee);
    }

    [Fact]
    public void A_pickup_point_is_not_free_above_the_threshold() {
        Order order = OrderOfSubtotal("500.00", "DE", ShippingMethod.PickupPoint).Generate();

        Assert.Equal(new Money(2.90m, "EUR"), order.ShippingFee);
    }

    [Fact]
    public void Standard_inside_france_is_four_ninety_under_the_threshold() {
        Order order = OrderOfSubtotal("49.99", "FR", ShippingMethod.Standard).Generate();

        Assert.Equal(new Money(4.90m, "EUR"), order.ShippingFee);
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Standard_abroad_is_nine_ninety_under_the_threshold(string country) {
        Order order = OrderOfSubtotal("49.99", country, ShippingMethod.Standard).Generate();

        Assert.Equal(new Money(9.90m, "EUR"), order.ShippingFee);
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Standard_is_free_from_exactly_the_threshold(string country) {
        Order order = OrderOfSubtotal("50.00", country, ShippingMethod.Standard).Generate();

        Assert.Equal(new Money(0m, "EUR"), order.ShippingFee);
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("BE")]
    [InlineData("DE")]
    public void Standard_is_free_above_the_threshold(string country) {
        Order order = OrderOfSubtotal("50.01", country, ShippingMethod.Standard).Generate();

        Assert.Equal(Money.Zero("EUR"), order.ShippingFee);
    }

    [Fact]
    public void The_threshold_is_judged_after_the_discount() {
        ShippingAddress address = Any.ShippingAddress().WithCountryCode("FR").Generate();

        // 60.00 - 20 % = 48.00: below the threshold although the subtotal is above it.
        Order order = Lines.OrderOf(Lines.Of(60m, 1))
                           .WithDiscount(new DiscountPercentage(20m))
                           .WithShippingAddress(address)
                           .WithShippingMethod(ShippingMethod.Standard)
                           .Generate();

        Assert.Equal(new Money(4.90m, "EUR"), order.ShippingFee);
    }

    [Fact]
    public void A_discount_landing_exactly_on_the_threshold_still_ships_free() {
        ShippingAddress address = Any.ShippingAddress().WithCountryCode("BE").Generate();

        // 100.00 - 50 % = 50.00.
        Order order = Lines.OrderOf(Lines.Of(100m, 1))
                           .WithDiscount(new DiscountPercentage(50m))
                           .WithShippingAddress(address)
                           .WithShippingMethod(ShippingMethod.Standard)
                           .Generate();

        Assert.Equal(Money.Zero("EUR"), order.ShippingFee);
    }

    [Fact]
    public void A_discount_one_tenth_of_a_percent_short_of_the_threshold_charges_the_fee() {
        ShippingAddress address = Any.ShippingAddress().WithCountryCode("BE").Generate();

        // 100.00 - 50.1 % = 49.90.
        Order order = Lines.OrderOf(Lines.Of(100m, 1))
                           .WithDiscount(new DiscountPercentage(50.1m))
                           .WithShippingAddress(address)
                           .WithShippingMethod(ShippingMethod.Standard)
                           .Generate();

        Assert.Equal(new Money(9.90m, "EUR"), order.ShippingFee);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void The_fee_is_expressed_in_the_currency_of_the_order(string currency) {
        Order order = OrderOfSubtotal("10.00", "FR", ShippingMethod.Express, currency).Generate();

        Assert.Equal(currency, order.ShippingFee.Currency);
    }

    [Fact]
    public void The_threshold_is_compared_to_the_amount_whatever_the_currency() {
        Order order = OrderOfSubtotal("50.00", "FR", ShippingMethod.Standard, "GBP").Generate();

        Assert.Equal(Money.Zero("GBP"), order.ShippingFee);
    }

    [Fact]
    public void FeeFor_is_the_order_s_shipping_fee() {
        Order anyOrder = Lines.OrderOf(Lines.Of(10m, 1)).WithShippingMethod(ShippingMethod.Express).Generate();

        Assert.Equal(anyOrder.ShippingFee, ShippingFeePolicy.FeeFor(anyOrder));
    }

    [Fact]
    public void An_undefined_shipping_method_is_refused_when_the_fee_is_asked_for() {
        Order order = Lines.OrderOf(Lines.Of(10m, 1)).WithShippingMethod((ShippingMethod)42).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.ShippingFee);
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Total);
    }

}
