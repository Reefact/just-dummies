using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace GenealogyTests;

public sealed class LotTests {

    [Fact]
    public void A_new_lot_awaits_quality_release() {
        // Arrange
        AnyLot anyLot = Arbitrary.Lot();

        // Act
        Lot lot = anyLot.Generate();

        // Assert
        Assert.Equal(LotStatus.Quarantined, lot.Status);
    }

    [Fact]
    public void A_lot_cannot_expire_the_day_it_is_manufactured() {
        // Arrange
        DateOnly anyDay = Arbitrary.ProductionDay().Generate();
        AnyLot   anyLot = Arbitrary.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay);

        // Act
        Exception? error = Record.Exception(() => anyLot.Generate());

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_quarantined_lot_cannot_be_consumed() {
        // Arrange
        Lot      anyLot = Arbitrary.Lot().WithInitialQuantity(new Quantity(10m, QuantityUnit.Kilogram)).Generate();
        DateOnly anyDay = Arbitrary.ProductionDay().Generate();

        // Act
        Exception? error = Record.Exception(() => anyLot.Consume(new Quantity(1m, QuantityUnit.Kilogram), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(new Quantity(10m, QuantityUnit.Kilogram), anyLot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_a_released_lot_withdraws_the_quantity_in_its_own_unit() {
        // Arrange
        Lot      anyLot = Arbitrary.Lot().WithInitialQuantity(new Quantity(100m, QuantityUnit.Kilogram)).Generate();
        DateOnly anyDay = Arbitrary.ProductionDay().Generate();
        anyLot.Release();

        // Act
        anyLot.Consume(new Quantity(2500m, QuantityUnit.Gram), anyDay);

        // Assert
        Assert.Equal(97.5m, anyLot.RemainingQuantity.Amount);
        Assert.Equal(QuantityUnit.Kilogram, anyLot.RemainingQuantity.Unit);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_on_its_expiry_date() {
        // Arrange
        Lot anyLot = Arbitrary.Lot().WithInitialQuantity(new Quantity(10m, QuantityUnit.Litre)).Generate();
        anyLot.Release();

        // Act
        Exception? error = Record.Exception(() => anyLot.Consume(new Quantity(1m, QuantityUnit.Litre), anyLot.ExpiresOn));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_lot_cannot_give_more_than_it_holds() {
        // Arrange
        Lot      anyLot = Arbitrary.Lot().WithInitialQuantity(new Quantity(10m, QuantityUnit.Kilogram)).Generate();
        DateOnly anyDay = Arbitrary.ProductionDay().Generate();
        anyLot.Release();

        // Act
        Exception? error = Record.Exception(() => anyLot.Consume(new Quantity(10.001m, QuantityUnit.Kilogram), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_mass_lot_cannot_be_consumed_by_volume() {
        // Arrange
        Lot      anyLot    = Arbitrary.Lot().WithInitialQuantity(q => q.WithUnit(QuantityUnit.Gram)).Generate();
        Quantity anyVolume = Arbitrary.Quantity().WithUnit(QuantityUnit.Millilitre).Generate();
        DateOnly anyDay    = Arbitrary.ProductionDay().Generate();
        anyLot.Release();

        // Act
        Exception? error = Record.Exception(() => anyLot.Consume(anyVolume, anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_rejected_lot_cannot_return_to_quarantine() {
        // Arrange
        Lot anyLot = Arbitrary.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? error = Record.Exception(anyLot.Quarantine);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

}
