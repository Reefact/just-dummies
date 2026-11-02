using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class OrderLineTests {

    [Fact]
    public void A_line_keeps_its_sku_quantity_and_unit_price() {
        // Arrange
        Sku      anySku      = Any.Sku().Generate();
        Quantity anyQuantity = Any.Quantity().Generate();
        Money    anyPrice    = Any.Money().Generate();

        // Act
        OrderLine line = new OrderLine(anySku, anyQuantity, anyPrice);

        // Assert
        Assert.Same(anySku, line.Sku);
        Assert.Same(anyQuantity, line.Quantity);
        Assert.Same(anyPrice, line.UnitPrice);
    }

    [Fact]
    public void A_missing_sku_is_refused() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();
        Money    anyPrice    = Any.Money().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new OrderLine(null!, anyQuantity, anyPrice));
        Assert.Equal("sku", error.ParamName);
    }

    [Fact]
    public void A_missing_quantity_is_refused() {
        // Arrange
        Sku   anySku   = Any.Sku().Generate();
        Money anyPrice = Any.Money().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, null!, anyPrice));
        Assert.Equal("quantity", error.ParamName);
    }

    [Fact]
    public void A_missing_unit_price_is_refused() {
        // Arrange
        Sku      anySku      = Any.Sku().Generate();
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new OrderLine(anySku, anyQuantity, null!));
        Assert.Equal("unitPrice", error.ParamName);
    }

    [Fact]
    public void Its_total_is_the_unit_price_times_the_units() {
        // Arrange
        string    anyCurrency = AnyMoney.ValidCurrencies().Generate();
        OrderLine line        = Any.OrderLine()
                                   .WithQuantity(new Quantity(4))
                                   .WithUnitPrice(new Money(2.50m, anyCurrency))
                                   .Generate();

        // Act
        Money total = line.Total;

        // Assert
        Assert.Equal(new Money(10.00m, anyCurrency), total);
    }

    [Fact]
    public void The_total_of_a_single_unit_is_the_unit_price() {
        // Arrange
        Money     unitPrice = new Money(19.99m, "GBP");
        OrderLine line      = Any.OrderLine().WithQuantity(new Quantity(1)).WithUnitPrice(unitPrice).Generate();

        // Act
        Money total = line.Total;

        // Assert
        Assert.Equal(unitPrice, total);
    }

}
