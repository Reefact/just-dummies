using BatchGenealogy.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

using static BatchGenealogy.Tests.Discovery.Plant;

namespace BatchGenealogy.Tests.Discovery;

[Reproducible]
public sealed class LotTests {

    [Fact]
    public void A_new_lot_awaits_quality_release() {
        // Arrange
        AnyLot anyLotRecipe = Any.Lot();

        // Act
        Lot anyLot = anyLotRecipe.Generate();

        // Assert
        Assert.Equal(LotStatus.Quarantined, anyLot.Status);
    }

    [Fact]
    public void A_lot_cannot_expire_on_the_day_it_is_manufactured() {
        // Arrange
        DateOnly     anyDay      = AnyProductionDay();
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Quantity     anyQuantity = Any.Quantity().WithAmount(amount => amount.Positive()).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Lot(anyNumber, anyMaterial, anyQuantity, anyDay, anyDay));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_rejected_lot_cannot_be_released() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? refusal = Record.Exception(anyLot.Release);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_rejected_lot_cannot_return_to_quarantine() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? refusal = Record.Exception(anyLot.Quarantine);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_quarantined_lot_cannot_be_consumed() {
        // Arrange
        DateOnly anyDay = AnyProductionDay();
        Lot      anyQuarantinedLot = Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(anyDay).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyQuarantinedLot.Consume(Kg(10m), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(Kg(100m), anyQuarantinedLot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_from_a_released_lot_lowers_what_remains() {
        // Arrange
        DateOnly anyDay = AnyProductionDay();
        Lot      anyLot = Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(anyDay).Generate();
        anyLot.Release();

        // Act
        anyLot.Consume(new Quantity(30_000m, QuantityUnit.Gram), anyDay);

        // Assert
        Assert.Equal(Kg(70m), anyLot.RemainingQuantity);
        Assert.False(anyLot.IsDepleted);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_on_its_expiry_date() {
        // Arrange
        DateOnly anyDay = AnyProductionDay();
        Lot      anyLot = Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(anyDay).WithShelfLifeDays(30).Generate();
        anyLot.Release();

        // Act
        Exception? refusal = Record.Exception(() => anyLot.Consume(Kg(1m), anyDay.AddDays(30)));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_before_it_was_manufactured() {
        // Arrange
        DateOnly anyDay = AnyProductionDay();
        Lot      anyLot = Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(anyDay).Generate();
        anyLot.Release();

        // Act
        Exception? refusal = Record.Exception(() => anyLot.Consume(Kg(1m), anyDay.AddDays(-1)));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_lot_weighed_in_mass_cannot_be_consumed_by_volume() {
        // Arrange
        DateOnly anyDay = AnyProductionDay();
        Lot      anyLot = Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(anyDay).Generate();
        anyLot.Release();

        // Act
        Exception? refusal = Record.Exception(() => anyLot.Consume(new Quantity(1m, QuantityUnit.Litre), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

}
