using JustDummies;
using JustDummies.Xunit;

using Shop.Domain;

using Xunit;

namespace Shop.Tests;

[Reproducible]
public sealed class OrderTests {

    private static Money Eur(decimal amount) => new Money(amount, "EUR");

    private static OrderLine AnyLineOf(int units, Money unitPrice) {
        return Any.OrderLine().WithQuantity(new Quantity(units)).WithUnitPrice(unitPrice).Generate();
    }

    [Fact]
    public void A_line_costs_its_unit_price_times_its_quantity() {
        // Arrange
        OrderLine anyLineOfThree = AnyLineOf(3, Eur(4.50m));

        // Act
        Money total = anyLineOfThree.Total;

        // Assert
        Assert.Equal(Eur(13.50m), total);
    }

    [Fact]
    public void The_subtotal_adds_up_every_line() {
        // Arrange
        OrderLine[] lines = [AnyLineOf(2, Eur(10.00m)), AnyLineOf(1, Eur(7.25m))];
        Order       anyOrder = Any.Order().WithLines(lines).Generate();

        // Act
        Money subtotal = anyOrder.Subtotal;

        // Assert
        Assert.Equal(Eur(27.25m), subtotal);
    }

    [Fact]
    public void A_20_percent_discount_takes_a_fifth_off_the_subtotal() {
        // Arrange
        Order anyOrder = Any.Order()
                            .WithLines([AnyLineOf(1, Eur(80.00m))])
                            .WithDiscount(new DiscountPercentage(20m))
                            .Generate();

        // Act
        Money discounted = anyOrder.DiscountedSubtotal;

        // Assert
        Assert.Equal(Eur(64.00m), discounted);
    }

    [Fact]
    public void A_discount_is_rounded_half_away_from_zero_to_the_cent() {
        // Arrange: 12.5 % of 0.20 is 0.025, which rounds to 0.03.
        Money anyTwentyCents = new Money(0.20m, Any.OneOf("EUR", "USD", "GBP").Generate());

        // Act
        Money discounted = anyTwentyCents.Less(new DiscountPercentage(12.5m));

        // Assert
        Assert.Equal(0.17m, discounted.Amount);
    }

    [Fact]
    public void Standard_shipping_is_free_from_the_threshold_up() {
        // Arrange
        Order anyOrderAtThreshold = Any.Order()
                                       .WithLines([AnyLineOf(1, Eur(Order.FreeShippingThreshold))])
                                       .WithDiscount(DiscountPercentage.None)
                                       .WithShippingMethod(ShippingMethod.Standard)
                                       .Generate();

        // Act
        Money fee = anyOrderAtThreshold.ShippingFee;

        // Assert
        Assert.Equal(Eur(0m), fee);
    }

    [Fact]
    public void Standard_shipping_below_the_threshold_costs_4_90_in_France() {
        // Arrange
        Order anyDomesticOrder = Any.Order()
                                    .WithLines([AnyLineOf(1, Eur(30.00m))])
                                    .WithDiscount(DiscountPercentage.None)
                                    .WithShippingMethod(ShippingMethod.Standard)
                                    .WithShippingAddress(address => address.WithCountryCode("FR"))
                                    .Generate();

        // Act
        Money fee = anyDomesticOrder.ShippingFee;

        // Assert
        Assert.Equal(Eur(4.90m), fee);
    }

    [Fact]
    public void Standard_shipping_below_the_threshold_costs_9_90_in_Germany() {
        // Arrange
        Order anyGermanOrder = Any.Order()
                                  .WithLines([AnyLineOf(1, Eur(30.00m))])
                                  .WithDiscount(DiscountPercentage.None)
                                  .WithShippingMethod(ShippingMethod.Standard)
                                  .WithShippingAddress(address => address.WithCountryCode("DE"))
                                  .Generate();

        // Act
        Money fee = anyGermanOrder.ShippingFee;

        // Assert
        Assert.Equal(Eur(9.90m), fee);
    }

    [Fact]
    public void Express_shipping_costs_14_90_whatever_the_basket() {
        // Arrange: the lines, the address and the discount are all drawn — none of them should matter.
        Order anyExpressOrder = Any.Order()
                                   .WithLines(lines => lines.WithCountBetween(1, 5))
                                   .WithShippingMethod(ShippingMethod.Express)
                                   .Generate();

        // Act
        Money fee = anyExpressOrder.ShippingFee;

        // Assert
        Assert.Equal(14.90m, fee.Amount);
    }

    [Fact]
    public void A_discount_that_drops_the_basket_below_the_threshold_makes_shipping_paid_again() {
        // Arrange: 60.00 less 20 % is 48.00, below the 50.00 threshold.
        Order anyDomesticOrder = Any.Order()
                                    .WithLines([AnyLineOf(2, Eur(30.00m))])
                                    .WithDiscount(new DiscountPercentage(20m))
                                    .WithShippingMethod(ShippingMethod.Standard)
                                    .WithShippingAddress(address => address.WithCountryCode("FR"))
                                    .Generate();

        // Act
        Money total = anyDomesticOrder.Total;

        // Assert
        Assert.Equal(Eur(52.90m), total);
    }

    [Fact]
    public void A_placed_order_becomes_paid() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        anyOrder.MarkPaid();

        // Assert
        Assert.Equal(OrderStatus.Paid, anyOrder.Status);
    }

    [Fact]
    public void An_order_cannot_ship_before_it_is_paid() {
        // Arrange
        Order anyPlacedOrder = Any.Order().Generate();

        // Act
        Action ship = anyPlacedOrder.Ship;

        // Assert
        Assert.Throws<InvalidOperationException>(ship);
    }

    [Fact]
    public void A_shipped_order_cannot_be_cancelled() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();
        anyOrder.Ship();

        // Act
        Action cancel = anyOrder.Cancel;

        // Assert
        Assert.Throws<InvalidOperationException>(cancel);
    }

    [Fact]
    public void An_order_refuses_lines_in_two_currencies() {
        // Arrange
        OrderLine anyEuroLine   = Any.OrderLine().WithUnitPrice(price => price.WithCurrency("EUR")).Generate();
        OrderLine anyDollarLine = Any.OrderLine().WithUnitPrice(price => price.WithCurrency("USD")).Generate();

        // Act
        Func<Order> placing = () => Any.Order().WithLines([anyEuroLine, anyDollarLine]).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(placing);
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void An_order_refuses_a_deadline_that_is_not_after_its_placement() {
        // Arrange
        DateTime placedAt = new DateTime(2026, 10, 27, 9, 0, 0);

        // Act
        Func<Order> placing = () => Any.Order().WithPlacedAt(placedAt).WithDeliveryDeadline(placedAt).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(placing);
        Assert.Equal("deliveryDeadline", refusal.ParamName);
    }

    [Fact]
    public void A_quantity_cannot_grow_past_99_units() {
        // Arrange
        Quantity sixty = new Quantity(60);
        Quantity forty = new Quantity(40);

        // Act
        Func<Quantity> adding = () => sixty.Add(forty);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(adding);
    }

    [Fact]
    public void A_sku_category_is_its_three_letter_prefix() {
        // Arrange
        Sku anyShirtSku = Any.StringMatching(@"TSH-\d{4}").As(value => new Sku(value)).Generate();

        // Act
        string category = anyShirtSku.Category;

        // Assert
        Assert.Equal("TSH", category);
    }

    [Fact]
    public void A_belgian_address_takes_a_four_digit_postal_code() {
        // Arrange
        string anyStreet = Any.String().NotBlank().WithMaxLength(80).Generate();
        string anyCity   = Any.String().NotBlank().WithMaxLength(50).Generate();
        string anyBelgianPostalCode = Any.StringMatching(@"\d{4}").Generate();

        // Act
        ShippingAddress address = new ShippingAddress(anyStreet, anyCity, anyBelgianPostalCode, "BE");

        // Assert
        Assert.False(address.IsDomestic);
    }

}
