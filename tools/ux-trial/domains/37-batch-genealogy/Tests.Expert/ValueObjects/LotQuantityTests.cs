using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.ValueObjects;

public sealed class LotQuantityTests {

    [Fact]
    public void A_lot_quantity_pairs_a_lot_with_the_quantity_taking_part() {
        // Arrange
        LotNumber lot      = new LotNumber("API-240117");
        Quantity  quantity = new Quantity(12.5m, QuantityUnit.Kilogram);

        // Act
        LotQuantity lotQuantity = new LotQuantity(lot, quantity);

        // Assert
        Assert.Same(lot, lotQuantity.Lot);
        Assert.Same(quantity, lotQuantity.Quantity);
    }

    [Theory]
    [InlineData(QuantityUnit.Milligram)]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Millilitre)]
    [InlineData(QuantityUnit.Litre)]
    public void A_zero_quantity_is_refused_in_every_unit(QuantityUnit unit) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.LotQuantity().WithQuantity(new Quantity(0m, unit)).Generate());
    }

    [Fact]
    public void The_smallest_positive_quantity_is_accepted() {
        // Act
        LotQuantity lotQuantity = Any.LotQuantity().WithQuantity(new Quantity(0.0000000000000000000000000001m, QuantityUnit.Kilogram)).Generate();

        // Assert
        Assert.False(lotQuantity.Quantity.IsZero);
    }

    [Fact]
    public void A_missing_lot_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LotQuantity(null!, Any.Quantity().WithAmount(amount => amount.Positive()).Generate()));
    }

    [Fact]
    public void A_missing_quantity_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LotQuantity(Any.LotNumber().Generate(), null!));
    }

    [Fact]
    public void A_lot_quantity_prints_the_quantity_and_the_lot() {
        // Arrange
        LotQuantity lotQuantity = new LotQuantity(new LotNumber("API-240117"), new Quantity(12.5m, QuantityUnit.Kilogram));

        // Act
        string text = lotQuantity.ToString();

        // Assert
        Assert.Equal("12.5 kg of API-240117", text);
    }

}
