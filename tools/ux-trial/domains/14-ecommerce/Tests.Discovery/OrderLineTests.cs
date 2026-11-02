using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class OrderLineTests {

    [Fact]
    public void A_line_keeps_its_sku_quantity_and_unit_price() {
        Sku      sku       = new Sku("TSH-0042");
        Quantity quantity  = new Quantity(3);
        Money    unitPrice = new Money(12.50m, "EUR");

        OrderLine line = new OrderLine(sku, quantity, unitPrice);

        Assert.Same(sku, line.Sku);
        Assert.Same(quantity, line.Quantity);
        Assert.Same(unitPrice, line.UnitPrice);
    }

    [Fact]
    public void A_line_without_a_sku_is_refused() {
        Quantity anyQuantity  = Any.Quantity().Generate();
        Money    anyUnitPrice = Any.Money().Generate();

        Assert.Throws<ArgumentNullException>(() => new OrderLine(null!, anyQuantity, anyUnitPrice));
    }

    [Fact]
    public void A_line_without_a_quantity_is_refused() {
        Sku   anySku       = Any.Sku().Generate();
        Money anyUnitPrice = Any.Money().Generate();

        Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, null!, anyUnitPrice));
    }

    [Fact]
    public void A_line_without_a_unit_price_is_refused() {
        Sku      anySku      = Any.Sku().Generate();
        Quantity anyQuantity = Any.Quantity().Generate();

        Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, anyQuantity, null!));
    }

    [Theory]
    [InlineData("12.50", 3, "37.50")]
    [InlineData("0.99", 99, "98.01")]
    [InlineData("0.00", 5, "0.00")]
    [InlineData("100.00", 1, "100.00")]
    public void The_total_is_the_unit_price_times_the_quantity(string unitPrice, int units, string expected) {
        OrderLine line = Lines.Of(decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture), units, "USD");

        Assert.Equal(new Money(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), "USD"), line.Total);
    }

}
