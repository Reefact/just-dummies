using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class OrderTests {

    private static List<OrderLine> AnyLines() {
        return [Any.OrderLine().Generate()];
    }

    private static Order BuildOrder(Guid? id = null,
                                    IReadOnlyList<OrderLine>? lines = null,
                                    ShippingAddress? address = null,
                                    DiscountPercentage? discount = null,
                                    DateTime? placedAt = null,
                                    DateTime? deadline = null,
                                    bool nullAddress = false,
                                    bool nullDiscount = false,
                                    bool nullLines = false) {
        DateTime placed = placedAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return new Order(id ?? Any.Guid().NonEmpty().Generate(),
                         nullLines ? null! : lines ?? [Dummies.Line(10m, 1)],
                         nullAddress ? null! : address ?? Any.ShippingAddress().Generate(),
                         nullDiscount ? null! : discount ?? Any.DiscountPercentage().Generate(),
                         Any.Enum<ShippingMethod>().Generate(),
                         placed,
                         deadline ?? placed.AddDays(3));
    }

    // ----- construction

    [Fact]
    public void A_new_order_is_placed_and_keeps_what_it_was_built_from() {
        // Arrange
        Guid               id       = Guid.Parse("11111111-2222-3333-4444-555555555555");
        List<OrderLine>    lines    = [Dummies.Line(10m, 2)];
        ShippingAddress    address  = Any.ShippingAddress().Generate();
        DiscountPercentage discount = new DiscountPercentage(10m);
        DateTime           placedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime           deadline = new DateTime(2026, 5, 4, 10, 0, 0, DateTimeKind.Utc);

        // Act
        Order order = new Order(id, lines, address, discount, ShippingMethod.Express, placedAt, deadline);

        // Assert
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(id, order.Id);
        Assert.Same(address, order.ShippingAddress);
        Assert.Same(discount, order.Discount);
        Assert.Equal(ShippingMethod.Express, order.ShippingMethod);
        Assert.Equal(placedAt, order.PlacedAt);
        Assert.Equal(deadline, order.DeliveryDeadline);
        Assert.Same(lines[0], Assert.Single(order.Lines));
    }

    [Fact]
    public void An_order_without_identifier_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(id: Guid.Empty));

        // Assert
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void An_order_without_lines_collection_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BuildOrder(nullLines: true));
    }

    [Fact]
    public void An_order_with_no_line_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(lines: new List<OrderLine>()));

        // Assert
        Assert.Equal("lines", exception.ParamName);
    }

    [Fact]
    public void An_order_with_lines_in_two_currencies_is_refused() {
        // Arrange
        string anyCurrency      = Dummies.Currency().Generate();
        string anyOtherCurrency = Dummies.Currency().DifferentFrom(anyCurrency).Generate();
        List<OrderLine> lines   = [Dummies.Line(10m, 1, anyCurrency), Dummies.Line(10m, 1, anyOtherCurrency)];

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(lines: lines));

        // Assert
        Assert.Equal("lines", exception.ParamName);
    }

    [Fact]
    public void An_order_whose_lines_all_share_a_currency_is_accepted() {
        // Arrange
        string          anyCurrency = Dummies.Currency().Generate();
        List<OrderLine> lines       = [Dummies.Line(10m, 1, anyCurrency), Dummies.Line(5m, 4, anyCurrency), Dummies.Line(1m, 9, anyCurrency)];

        // Act
        Order order = BuildOrder(lines: lines);

        // Assert
        Assert.Equal(3, order.Lines.Count);
    }

    [Fact]
    public void An_order_without_address_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => BuildOrder(nullAddress: true));

        // Assert
        Assert.Equal("shippingAddress", exception.ParamName);
    }

    [Fact]
    public void An_order_without_discount_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => BuildOrder(nullDiscount: true));

        // Assert
        Assert.Equal("discount", exception.ParamName);
    }

    [Fact]
    public void A_deadline_equal_to_the_placement_instant_is_refused() {
        // Arrange
        DateTime anyInstant = Dummies.Instant().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(placedAt: anyInstant, deadline: anyInstant));

        // Assert
        Assert.Equal("deliveryDeadline", exception.ParamName);
    }

    [Fact]
    public void A_deadline_before_the_placement_instant_is_refused() {
        // Arrange
        DateTime anyPlacedAt        = Dummies.Instant().Generate();
        DateTime anEarlierDeadline  = Any.DateTime().Between(new DateTime(1990, 1, 1), anyPlacedAt.AddTicks(-1)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(placedAt: anyPlacedAt, deadline: anEarlierDeadline));

        // Assert
        Assert.Equal("deliveryDeadline", exception.ParamName);
    }

    [Fact]
    public void A_deadline_one_tick_after_the_placement_instant_is_accepted() {
        // Arrange
        DateTime anyPlacedAt = Dummies.Instant().Generate();

        // Act
        Order order = BuildOrder(placedAt: anyPlacedAt, deadline: anyPlacedAt.AddTicks(1));

        // Assert
        Assert.Equal(OrderStatus.Placed, order.Status);
    }

    [Fact]
    public void The_identifier_is_checked_before_the_lines() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(id: Guid.Empty, lines: new List<OrderLine>()));

        // Assert
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void The_lines_are_checked_before_the_address() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BuildOrder(lines: new List<OrderLine>(), nullAddress: true));

        // Assert
        Assert.Equal("lines", exception.ParamName);
    }

    [Fact]
    public void The_address_is_checked_before_the_deadline() {
        // Arrange
        DateTime anyInstant = Dummies.Instant().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => BuildOrder(nullAddress: true, placedAt: anyInstant, deadline: anyInstant));

        // Assert
        Assert.Equal("shippingAddress", exception.ParamName);
    }

    [Fact]
    public void The_lines_are_copied_so_later_changes_to_the_source_list_do_not_reach_the_order() {
        // Arrange
        List<OrderLine> source = [Dummies.Line(10m, 1), Dummies.Line(10m, 1)];
        Order           order  = BuildOrder(lines: source);

        // Act
        source.Add(Dummies.Line(10m, 1));
        source.RemoveAt(0);

        // Assert
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(new Money(20m, "EUR"), order.Subtotal);
    }

    // ----- derived values

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void The_currency_is_the_one_of_the_lines(string currency) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(1m, 1, currency), Dummies.Line(2m, 3, currency)]);

        // Act
        string orderCurrency = order.Currency;

        // Assert
        Assert.Equal(currency, orderCurrency);
    }

    [Fact]
    public void The_subtotal_is_the_sum_of_the_line_totals() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(12.50m, 3), Dummies.Line(0.99m, 10), Dummies.Line(100m, 1)]);

        // Act
        Money subtotal = order.Subtotal;

        // Assert
        Assert.Equal(new Money(147.40m, "EUR"), subtotal);   // 37.50 + 9.90 + 100.00
    }

    [Fact]
    public void The_subtotal_of_a_single_line_is_its_total() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(19.99m, 2)]);

        // Act & Assert
        Assert.Equal(new Money(39.98m, "EUR"), order.Subtotal);
    }

    [Theory]
    [InlineData("0", "200.00")]
    [InlineData("25", "150.00")]
    [InlineData("12.5", "175.00")]
    [InlineData("100", "0.00")]
    public void The_discounted_subtotal_is_the_subtotal_less_the_discount(string discount, string expected) {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(100m, 2)], discount: decimal.Parse(discount, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        Money discounted = order.DiscountedSubtotal;

        // Assert
        Assert.Equal(new Money(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), "EUR"), discounted);
    }

    [Fact]
    public void The_total_is_the_discounted_subtotal_plus_the_shipping_fee() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(20m, 2)], ShippingMethod.Express, discount: 10m);

        // Act
        Money total = order.Total;

        // Assert
        Assert.Equal(new Money(36m + 14.90m, "EUR"), total);
    }

    [Fact]
    public void The_total_of_an_order_shipped_for_free_is_its_discounted_subtotal() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(100m, 1)], ShippingMethod.Standard, discount: 10m);

        // Act & Assert
        Assert.Equal(new Money(90m, "EUR"), order.Total);
    }

    [Fact]
    public void The_total_of_a_fully_discounted_order_is_the_shipping_fee_alone() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(100m, 1)], ShippingMethod.PickupPoint, discount: 100m);

        // Act & Assert
        Assert.Equal(new Money(2.90m, "EUR"), order.Total);
    }

    [Fact]
    public void The_total_units_is_the_sum_of_the_line_quantities() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(1m, 3), Dummies.Line(1m, 99), Dummies.Line(1m, 1)]);

        // Act & Assert
        Assert.Equal(103, order.TotalUnits);
    }

    [Fact]
    public void The_shipping_fee_is_the_one_of_the_policy() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(10m, 1)], ShippingMethod.PickupPoint);

        // Act & Assert
        Assert.Equal(ShippingFeePolicy.FeeFor(order), order.ShippingFee);
    }

    [Fact]
    public void An_undefined_shipping_method_is_accepted_at_construction_but_refused_when_the_fee_is_needed() {
        // Arrange
        Order order = Dummies.OrderOf([Dummies.Line(10m, 1)], (ShippingMethod)42);

        // Act & Assert
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Throws<ArgumentOutOfRangeException>(() => order.ShippingFee);
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Total);
    }

    // ----- lifecycle

    [Fact]
    public void A_placed_order_can_be_paid() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        anyOrder.MarkPaid();

        // Assert
        Assert.Equal(OrderStatus.Paid, anyOrder.Status);
    }

    [Fact]
    public void A_paid_order_cannot_be_paid_again() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.MarkPaid());
        Assert.Equal(OrderStatus.Paid, anyOrder.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_paid() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.MarkPaid());
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void A_shipped_order_cannot_be_paid() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();
        anyOrder.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.MarkPaid());
        Assert.Equal(OrderStatus.Shipped, anyOrder.Status);
    }

    [Fact]
    public void A_paid_order_can_be_shipped() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();

        // Act
        anyOrder.Ship();

        // Assert
        Assert.Equal(OrderStatus.Shipped, anyOrder.Status);
    }

    [Fact]
    public void A_placed_order_cannot_be_shipped_before_it_is_paid() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.Ship());
        Assert.Equal(OrderStatus.Placed, anyOrder.Status);
    }

    [Fact]
    public void A_shipped_order_cannot_be_shipped_again() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();
        anyOrder.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.Ship());
        Assert.Equal(OrderStatus.Shipped, anyOrder.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_shipped() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();
        anyOrder.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.Ship());
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void A_placed_order_can_be_cancelled() {
        // Arrange
        Order anyOrder = Any.Order().Generate();

        // Act
        anyOrder.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void A_paid_order_can_be_cancelled() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();

        // Act
        anyOrder.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void Cancelling_an_already_cancelled_order_is_accepted_and_changes_nothing() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.Cancel();

        // Act
        anyOrder.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, anyOrder.Status);
    }

    [Fact]
    public void A_shipped_order_cannot_be_cancelled() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.MarkPaid();
        anyOrder.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyOrder.Cancel());
        Assert.Equal(OrderStatus.Shipped, anyOrder.Status);
    }

    [Fact]
    public void Refused_transitions_say_which_state_the_order_is_in() {
        // Arrange
        Order anyOrder = Any.Order().Generate();
        anyOrder.Cancel();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyOrder.Ship());

        // Assert
        Assert.Contains("Cancelled", exception.Message);
    }

}
