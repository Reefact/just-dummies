namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class LotQuantityTests {

    [Fact]
    public void A_lot_quantity_keeps_its_lot_and_its_quantity() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        LotQuantity lotQuantity = new LotQuantity(anyLot, Kg.Of(12m));

        // Assert
        Assert.Same(anyLot, lotQuantity.Lot);
        Assert.Equal(Kg.Of(12m), lotQuantity.Quantity);
    }

    [Fact]
    public void A_zero_quantity_is_refused_in_any_unit() {
        // Arrange
        LotNumber    anyLot  = Any.LotNumber().Generate();
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LotQuantity(anyLot, new Quantity(0m, anyUnit)));
    }

    [Fact]
    public void A_missing_lot_or_quantity_is_refused() {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyQuantity = Any.LotQuantity().Generate().Quantity;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new LotQuantity(null!, anyQuantity));
        Assert.Throws<ArgumentNullException>(() => new LotQuantity(anyLot, null!));
    }

    [Fact]
    public void A_drawn_lot_quantity_is_always_strictly_positive() {
        // Arrange
        LotQuantity anyLotQuantity = Any.LotQuantity().Generate();

        // Assert: the generator is the domain's constructor in miniature, so a zero here would be a defect of the generator
        Assert.False(anyLotQuantity.Quantity.IsZero);
    }

    [Fact]
    public void Printing_says_how_much_of_which_lot() {
        // Act
        LotQuantity lotQuantity = new LotQuantity(new LotNumber("API-240117"), Kg.Of(3m));

        // Assert
        Assert.Equal("3 kg of API-240117", lotQuantity.ToString());
    }

}
