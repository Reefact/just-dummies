using System.Globalization;

using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class OrderTests {

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    // ---- construction ------------------------------------------------------------------------

    [Fact]
    public void An_order_starts_placed() {
        Order anyOrder = Any.Order().Generate();

        Assert.Equal(OrderStatus.Placed, anyOrder.Status);
    }

    [Fact]
    public void An_order_keeps_what_it_was_built_with() {
        Guid                     anyId      = Any.Guid().NonEmpty().Generate();
        ShippingAddress          anyAddress = Any.ShippingAddress().Generate();
        DiscountPercentage       anyDiscount = Any.DiscountPercentage().Generate();
        IReadOnlyList<OrderLine> lines      = [Lines.Of(10m, 2)];

        Order order = new Order(anyId, lines, anyAddress, anyDiscount, ShippingMethod.Express, new DateTime(2026, 1, 1), new DateTime(2026, 1, 8));

        Assert.Equal(anyId, order.Id);
        Assert.Same(anyAddress, order.ShippingAddress);
        Assert.Same(anyDiscount, order.Discount);
        Assert.Equal(ShippingMethod.Express, order.ShippingMethod);
        Assert.Equal(new DateTime(2026, 1, 1), order.PlacedAt);
        Assert.Equal(new DateTime(2026, 1, 8), order.DeliveryDeadline);
        Assert.Equal(lines, order.Lines);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Order().WithId(Guid.Empty).Generate());
    }

    [Fact]
    public void Missing_lines_are_refused_with_a_null_exception() {
        Order anyOrder = Any.Order().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new Order(anyOrder.Id, null!, anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.DeliveryDeadline));
    }

    [Fact]
    public void An_order_without_any_line_is_refused() {
        Order anyOrder = Any.Order().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Order(anyOrder.Id, [], anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.DeliveryDeadline));

        Assert.Equal("lines", refusal.ParamName);
    }

    [Theory]
    [InlineData("EUR", "USD")]
    [InlineData("USD", "GBP")]
    [InlineData("GBP", "EUR")]
    public void Lines_in_two_currencies_are_refused(string first, string second) {
        OrderLine[] lines = [Lines.Of(10m, 1, first), Lines.Of(10m, 1, second)];

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Lines.OrderOf(lines).Generate());

        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_currency_that_differs_only_on_the_third_line_is_refused() {
        OrderLine[] lines = [Lines.Of(10m, 1, "EUR"), Lines.Of(10m, 1, "EUR"), Lines.Of(10m, 1, "GBP")];

        Assert.Throws<ArgumentException>(() => Lines.OrderOf(lines).Generate());
    }

    [Fact(Skip = "domain defect: a null element in lines raises NullReferenceException (from line.UnitPrice) instead of an argument exception (literal case)")]
    public void A_null_line_is_refused_with_an_argument_exception() {
        Order anyOrder = Any.Order().Generate();
        OrderLine?[] linesWithANull = [Lines.Of(10m, 1), null];

        Assert.ThrowsAny<ArgumentException>(
            () => new Order(anyOrder.Id, linesWithANull!, anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.DeliveryDeadline));
    }

    [Fact]
    public void Missing_address_is_refused_with_a_null_exception() {
        Order anyOrder = Any.Order().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, null!, anyOrder.Discount, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.DeliveryDeadline));
    }

    [Fact]
    public void Missing_discount_is_refused_with_a_null_exception() {
        Order anyOrder = Any.Order().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, anyOrder.ShippingAddress, null!, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.DeliveryDeadline));
    }

    [Fact]
    public void A_deadline_equal_to_the_placement_is_refused() {
        DateTime anyInstant = Any.DateTime().Between(new DateTime(2000, 1, 1), new DateTime(2100, 1, 1)).Generate();
        Order    anyOrder   = Any.Order().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, anyInstant, anyInstant));

        Assert.Equal("deliveryDeadline", refusal.ParamName);
    }

    [Fact]
    public void A_deadline_before_the_placement_is_refused() {
        Order anyOrder = Any.Order().Generate();

        Assert.Throws<ArgumentException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, new DateTime(2026, 5, 2), new DateTime(2026, 5, 1)));
    }

    [Fact]
    public void A_deadline_one_tick_after_the_placement_is_accepted() {
        DateTime placed = new DateTime(2026, 5, 1);

        Order order = Any.Order().WithPlacedAt(placed).WithDeliveryDeadline(placed.AddTicks(1)).Generate();

        Assert.Equal(placed.AddTicks(1), order.DeliveryDeadline);
    }

    [Fact]
    public void The_checks_run_in_order_identifier_then_lines_then_currency_then_address_then_discount_then_dates() {
        Order anyOrder = Any.Order().Generate();
        OrderLine[] mixed = [Lines.Of(1m, 1, "EUR"), Lines.Of(1m, 1, "USD")];

        // Empty identifier wins over null lines.
        Assert.Equal("id", Assert.Throws<ArgumentException>(
            () => new Order(Guid.Empty, null!, null!, null!, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.PlacedAt)).ParamName);
        // Mixed currencies win over a null address.
        Assert.Equal("lines", Assert.Throws<ArgumentException>(
            () => new Order(anyOrder.Id, mixed, null!, null!, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.PlacedAt)).ParamName);
        // A null address wins over a null discount, which wins over bad dates.
        Assert.Equal("shippingAddress", Assert.Throws<ArgumentNullException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, null!, null!, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.PlacedAt)).ParamName);
        Assert.Equal("discount", Assert.Throws<ArgumentNullException>(
            () => new Order(anyOrder.Id, anyOrder.Lines, anyOrder.ShippingAddress, null!, anyOrder.ShippingMethod, anyOrder.PlacedAt, anyOrder.PlacedAt)).ParamName);
    }

    [Fact]
    public void The_lines_are_copied_so_the_caller_cannot_change_the_order() {
        List<OrderLine> callerLines = [Lines.Of(10m, 1), Lines.Of(20m, 1)];

        Order order = Any.Order().WithLines(callerLines).Generate();
        callerLines.Clear();

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(new Money(30m, "EUR"), order.Subtotal);
    }

    [Fact]
    public void An_undefined_shipping_method_does_not_stop_the_order_from_being_built() {
        Order order = Any.Order().WithShippingMethod((ShippingMethod)42).Generate();

        Assert.Equal((ShippingMethod)42, order.ShippingMethod);
    }

    [Fact]
    public void The_date_kind_is_not_validated() {
        DateTime placedLocal = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Local);
        DateTime deadlineUtc = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc);

        Order anyOrder = Any.Order().Generate();

        Order order = new Order(anyOrder.Id, anyOrder.Lines, anyOrder.ShippingAddress, anyOrder.Discount, anyOrder.ShippingMethod, placedLocal, deadlineUtc);

        Assert.Equal(DateTimeKind.Local, order.PlacedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, order.DeliveryDeadline.Kind);
    }

    // ---- derived amounts ---------------------------------------------------------------------

    [Fact]
    public void The_currency_is_the_one_of_the_lines() {
        Order order = Lines.OrderOf(Lines.Of(10m, 1, "GBP"), Lines.Of(5m, 2, "GBP")).Generate();

        Assert.Equal("GBP", order.Currency);
    }

    [Fact]
    public void The_subtotal_adds_the_line_totals() {
        Order order = Lines.OrderOf(Lines.Of(12.50m, 3), Lines.Of(0.99m, 10), Lines.Of(100m, 1)).Generate();

        Assert.Equal(new Money(147.40m, "EUR"), order.Subtotal);
    }

    [Fact]
    public void The_total_units_add_the_line_quantities() {
        Order order = Lines.OrderOf(Lines.Of(1m, 3), Lines.Of(1m, 10), Lines.Of(1m, 99)).Generate();

        Assert.Equal(112, order.TotalUnits);
    }

    [Fact]
    public void Without_a_discount_the_discounted_subtotal_is_the_subtotal() {
        Order order = Lines.OrderOf(Lines.Of(25m, 2)).WithDiscount(DiscountPercentage.None).Generate();

        Assert.Equal(order.Subtotal, order.DiscountedSubtotal);
    }

    [Theory]
    [InlineData("100.00", "20", "80.00")]
    [InlineData("100.00", "12.5", "87.50")]
    [InlineData("33.33", "10", "30.00")]
    [InlineData("100.00", "100", "0.00")]
    public void The_discounted_subtotal_takes_the_percentage_off_the_subtotal(string subtotal, string percent, string expected) {
        Order order = Lines.OrderOf(Lines.Of(D(subtotal), 1)).WithDiscount(new DiscountPercentage(D(percent))).Generate();

        Assert.Equal(new Money(D(expected), "EUR"), order.DiscountedSubtotal);
    }

    [Fact]
    public void The_total_is_the_discounted_subtotal_plus_the_shipping_fee() {
        ShippingAddress frenchAddress = Any.ShippingAddress().WithCountryCode("FR").Generate();

        Order order = Lines.OrderOf(Lines.Of(20m, 1))
                           .WithDiscount(new DiscountPercentage(10m))
                           .WithShippingAddress(frenchAddress)
                           .WithShippingMethod(ShippingMethod.Standard)
                           .Generate();

        // 20.00 - 10 % = 18.00, plus 4.90 of standard shipping inside France.
        Assert.Equal(new Money(22.90m, "EUR"), order.Total);
    }

    [Fact]
    public void A_full_discount_leaves_only_the_shipping_fee_to_pay() {
        ShippingAddress frenchAddress = Any.ShippingAddress().WithCountryCode("FR").Generate();

        Order order = Lines.OrderOf(Lines.Of(80m, 2))
                           .WithDiscount(new DiscountPercentage(100m))
                           .WithShippingAddress(frenchAddress)
                           .WithShippingMethod(ShippingMethod.Standard)
                           .Generate();

        Assert.Equal(new Money(4.90m, "EUR"), order.Total);
    }

    // ---- lifecycle ---------------------------------------------------------------------------

    private static Order OrderIn(OrderStatus status) {
        Order order = Any.Order().Generate();
        switch (status) {
            case OrderStatus.Placed:
                break;
            case OrderStatus.Paid:
                order.MarkPaid();
                break;
            case OrderStatus.Shipped:
                order.MarkPaid();
                order.Ship();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        return order;
    }

    [Fact]
    public void A_placed_order_can_be_paid() {
        Order order = OrderIn(OrderStatus.Placed);

        order.MarkPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void Only_a_placed_order_can_be_paid(OrderStatus status) {
        Order order = OrderIn(status);

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());
        Assert.Equal(status, order.Status);
    }

    [Fact]
    public void A_paid_order_can_ship() {
        Order order = OrderIn(OrderStatus.Paid);

        order.Ship();

        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void Only_a_paid_order_can_ship(OrderStatus status) {
        Order order = OrderIn(status);

        Assert.Throws<InvalidOperationException>(() => order.Ship());
        Assert.Equal(status, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid)]
    public void An_order_that_has_not_shipped_can_be_cancelled(OrderStatus status) {
        Order order = OrderIn(status);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancelling_an_already_cancelled_order_is_accepted() {
        Order order = OrderIn(OrderStatus.Cancelled);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void A_shipped_order_cannot_be_cancelled() {
        Order order = OrderIn(OrderStatus.Shipped);

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_paid_again_nor_shipped() {
        Order order = OrderIn(OrderStatus.Cancelled);

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());
        Assert.Throws<InvalidOperationException>(() => order.Ship());
    }

    [Fact]
    public void The_lifecycle_does_not_change_the_amounts() {
        Order order = Lines.OrderOf(Lines.Of(30m, 2)).WithDiscount(DiscountPercentage.None).Generate();
        Money totalBefore = order.Total;

        order.MarkPaid();
        order.Ship();

        Assert.Equal(totalBefore, order.Total);
    }

}
