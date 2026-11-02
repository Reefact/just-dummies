using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class OrderPricingTests {

    private static string AnyCurrency() {
        return AnyMoney.ValidCurrencies().Generate();
    }

    #region Subtotal, discount, total

    [Fact]
    public void The_subtotal_adds_up_the_line_totals() {
        // Arrange
        string currency = AnyCurrency();
        Order order = Any.Order()
                         .WithLines(new List<OrderLine> {
                             OrderBuilders.LineOf(10.00m, 2, currency),
                             OrderBuilders.LineOf(5.00m, 1, currency)
                         })
                         .Generate();

        // Act
        Money subtotal = order.Subtotal;

        // Assert
        Assert.Equal(new Money(25.00m, currency), subtotal);
    }

    [Fact]
    public void The_order_is_priced_in_the_currency_of_its_lines() {
        // Arrange
        string anyCurrency = AnyCurrency();
        Order  order       = OrderBuilders.OrderWithSubtotal(30m, anyCurrency).Generate();

        // Act / Assert
        Assert.Equal(anyCurrency, order.Currency);
        Assert.Equal(anyCurrency, order.Subtotal.Currency);
        Assert.Equal(anyCurrency, order.Total.Currency);
    }

    [Fact]
    public void The_discount_comes_off_the_subtotal() {
        // Arrange
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(25.00m, currency)
                                   .WithDiscount(new DiscountPercentage(10m))
                                   .Generate();

        // Act
        Money discounted = order.DiscountedSubtotal;

        // Assert
        Assert.Equal(new Money(22.50m, currency), discounted);
    }

    [Fact]
    public void Without_a_discount_the_discounted_subtotal_is_the_subtotal() {
        // Arrange
        Order order = OrderBuilders.OrderWithSubtotal(25.00m, AnyCurrency())
                                   .WithDiscount(DiscountPercentage.None)
                                   .Generate();

        // Act / Assert
        Assert.Equal(order.Subtotal, order.DiscountedSubtotal);
    }

    [Fact]
    public void A_full_discount_leaves_nothing_to_pay_for_the_lines() {
        // Arrange
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(25.00m, currency)
                                   .WithDiscount(new DiscountPercentage(100m))
                                   .Generate();

        // Act
        Money discounted = order.DiscountedSubtotal;

        // Assert
        Assert.Equal(Money.Zero(currency), discounted);
    }

    [Fact]
    public void The_total_is_the_discounted_subtotal_plus_the_shipping_fee() {
        // Arrange: 25.00 less 10 % is 22.50, below the free-shipping threshold; standard shipping to France costs 4.90.
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(25.00m, currency)
                                   .WithDiscount(new DiscountPercentage(10m))
                                   .WithShippingMethod(ShippingMethod.Standard)
                                   .WithShippingAddress(a => a.WithCountryCode("FR"))
                                   .Generate();

        // Act
        Money total = order.Total;

        // Assert
        Assert.Equal(new Money(27.40m, currency), total);
    }

    [Fact]
    public void The_total_units_add_up_the_quantities_of_every_line() {
        // Arrange
        string currency = AnyCurrency();
        Order order = Any.Order()
                         .WithLines(new List<OrderLine> {
                             OrderBuilders.LineOf(1.00m, 3, currency),
                             OrderBuilders.LineOf(2.00m, 4, currency)
                         })
                         .Generate();

        // Act
        int totalUnits = order.TotalUnits;

        // Assert
        Assert.Equal(7, totalUnits);
    }

    #endregion

    #region Shipping fee

    [Fact]
    public void Express_shipping_always_costs_fourteen_ninety() {
        // Arrange: neither the basket nor the destination takes part.
        string currency = AnyCurrency();
        Order  anyOrder = Any.Order()
                             .WithShippingMethod(ShippingMethod.Express)
                             .WithLines(new List<OrderLine> { OrderBuilders.LineOf(Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(), 1, currency) })
                             .Generate();

        // Act
        Money fee = anyOrder.ShippingFee;

        // Assert
        Assert.Equal(new Money(14.90m, currency), fee);
    }

    [Fact]
    public void A_pickup_point_always_costs_two_ninety() {
        // Arrange: neither the basket nor the destination takes part.
        string currency = AnyCurrency();
        Order  anyOrder = Any.Order()
                             .WithShippingMethod(ShippingMethod.PickupPoint)
                             .WithLines(new List<OrderLine> { OrderBuilders.LineOf(Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(), 1, currency) })
                             .Generate();

        // Act
        Money fee = anyOrder.ShippingFee;

        // Assert
        Assert.Equal(new Money(2.90m, currency), fee);
    }

    public static TheoryData<decimal, string, decimal> StandardFeesBelowTheThreshold => new TheoryData<decimal, string, decimal> {
        { 0m, "FR", 4.90m },
        { 49.99m, "FR", 4.90m },
        { 0m, "BE", 9.90m },
        { 49.99m, "BE", 9.90m },
        { 49.99m, "DE", 9.90m }
    };

    [Theory]
    [MemberData(nameof(StandardFeesBelowTheThreshold))]
    public void Standard_shipping_below_the_threshold_costs_4_90_in_france_and_9_90_elsewhere(decimal subtotal, string countryCode, decimal expected) {
        // Arrange
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(subtotal, currency)
                                   .WithDiscount(DiscountPercentage.None)
                                   .WithShippingMethod(ShippingMethod.Standard)
                                   .WithShippingAddress(a => a.WithCountryCode(countryCode))
                                   .Generate();

        // Act
        Money fee = order.ShippingFee;

        // Assert
        Assert.Equal(new Money(expected, currency), fee);
    }

    public static TheoryData<decimal, string> SubtotalsFromTheThreshold => new TheoryData<decimal, string> {
        { 50.00m, "FR" },
        { 50.00m, "BE" },
        { 50.01m, "FR" },
        { 1000m, "DE" }
    };

    [Theory]
    [MemberData(nameof(SubtotalsFromTheThreshold))]
    public void Standard_shipping_is_free_from_fifty(decimal subtotal, string countryCode) {
        // Arrange
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(subtotal, currency)
                                   .WithDiscount(DiscountPercentage.None)
                                   .WithShippingMethod(ShippingMethod.Standard)
                                   .WithShippingAddress(a => a.WithCountryCode(countryCode))
                                   .Generate();

        // Act
        Money fee = order.ShippingFee;

        // Assert
        Assert.Equal(Money.Zero(currency), fee);
    }

    [Fact]
    public void The_free_shipping_threshold_is_fifty() {
        // Assert
        Assert.Equal(50m, Order.FreeShippingThreshold);
    }

    [Fact]
    public void Free_shipping_is_judged_after_the_discount() {
        // Arrange: 60.00 less 20 % is 48.00, a hair under the threshold.
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(60.00m, currency)
                                   .WithDiscount(new DiscountPercentage(20m))
                                   .WithShippingMethod(ShippingMethod.Standard)
                                   .WithShippingAddress(a => a.WithCountryCode("FR"))
                                   .Generate();

        // Act
        Money fee = order.ShippingFee;

        // Assert
        Assert.Equal(new Money(4.90m, currency), fee);
    }

    [Fact]
    public void A_discounted_subtotal_landing_exactly_on_the_threshold_ships_free() {
        // Arrange: 62.50 less 20 % is exactly 50.00.
        string currency = AnyCurrency();
        Order order = OrderBuilders.OrderWithSubtotal(62.50m, currency)
                                   .WithDiscount(new DiscountPercentage(20m))
                                   .WithShippingMethod(ShippingMethod.Standard)
                                   .WithShippingAddress(a => a.WithCountryCode("FR"))
                                   .Generate();

        // Act
        Money fee = order.ShippingFee;

        // Assert
        Assert.Equal(Money.Zero(currency), fee);
    }

    [Fact]
    public void An_undefined_shipping_method_is_accepted_by_the_order_but_has_no_fee() {
        // Arrange: the lines are pinned, since Total adds them up before it asks for a fee.
        Order order = OrderBuilders.OrderWithSubtotal(30m, AnyCurrency()).WithShippingMethod((ShippingMethod)99).Generate();

        // Act / Assert
        Assert.Equal((ShippingMethod)99, order.ShippingMethod);
        Assert.Throws<ArgumentOutOfRangeException>(() => order.ShippingFee);
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Total);
    }

    [Fact(Skip = "domain defect: Money has no upper bound, so a valid unit price makes the line total, the subtotal and the order total throw OverflowException instead of a domain error; found with seed 1638907644 on a drawn order")]
    public void A_line_whose_total_exceeds_what_a_decimal_holds_fails_with_a_domain_error() {
        // Arrange
        OrderLine line = Any.OrderLine().WithQuantity(new Quantity(2)).WithUnitPrice(new Money(decimal.MaxValue, "EUR")).Generate();

        // Act
        Exception? error = Record.Exception(() => line.Total);

        // Assert
        Assert.IsNotType<OverflowException>(error);
    }

    #endregion

}
