using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class OrderLineTests {

    [Fact]
    public void A_line_keeps_its_sku_quantity_and_unit_price() {
        // Arrange
        Sku      sku       = new Sku("TSH-0042");
        Quantity quantity  = new Quantity(3);
        Money    unitPrice = new Money(12.50m, "EUR");

        // Act
        OrderLine line = new OrderLine(sku, quantity, unitPrice);

        // Assert
        Assert.Same(sku, line.Sku);
        Assert.Same(quantity, line.Quantity);
        Assert.Same(unitPrice, line.UnitPrice);
    }

    [Fact]
    public void A_line_without_sku_is_refused() {
        // Arrange
        Quantity anyQuantity  = Any.Quantity().Generate();
        Money    anyUnitPrice = Any.Money().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new OrderLine(null!, anyQuantity, anyUnitPrice));

        // Assert
        Assert.Equal("sku", exception.ParamName);
    }

    [Fact]
    public void A_line_without_quantity_is_refused() {
        // Arrange
        Sku   anySku       = Any.Sku().Generate();
        Money anyUnitPrice = Any.Money().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, null!, anyUnitPrice));

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void A_line_without_unit_price_is_refused() {
        // Arrange
        Sku      anySku      = Any.Sku().Generate();
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, anyQuantity, null!));

        // Assert
        Assert.Equal("unitPrice", exception.ParamName);
    }

    [Theory]
    [InlineData("12.50", 3, "37.50")]
    [InlineData("0.01", 99, "0.99")]
    [InlineData("0.00", 5, "0.00")]
    [InlineData("19.99", 1, "19.99")]
    public void The_total_is_the_unit_price_times_the_units(string unitPrice, int units, string expected) {
        // Arrange
        OrderLine line = Any.OrderLine()
                            .WithQuantity(new Quantity(units))
                            .WithUnitPrice(new Money(decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture), "EUR"))
                            .Generate();

        // Act
        Money total = line.Total;

        // Assert
        Assert.Equal(new Money(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), "EUR"), total);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void The_total_is_in_the_currency_of_the_unit_price(string currency) {
        // Arrange
        OrderLine line = Dummies.Line(unitPrice: 2m, units: 2, currency: currency);

        // Act
        Money total = line.Total;

        // Assert
        Assert.Equal(new Money(4m, currency), total);
    }

}
