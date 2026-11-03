using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class LotTests {

    private static DateOnly AnyManufacturingDay() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)).Generate();
    }

    private static Lot AnyReleasedLot(Quantity quantity) {
        Lot lot = Any.Lot().WithInitialQuantity(quantity).Generate();
        lot.Release();

        return lot;
    }

    // ---- construction ------------------------------------------------------------------------------------

    [Fact]
    public void A_new_lot_is_quarantined_and_entirely_available() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        DateOnly     anyDay      = AnyManufacturingDay();

        // Act
        Lot lot = new Lot(anyNumber, anyMaterial, new Quantity(40m, QuantityUnit.Kilogram), anyDay, anyDay.AddDays(365));

        // Assert
        Assert.Same(anyNumber, lot.Number);
        Assert.Same(anyMaterial, lot.Material);
        Assert.Equal(LotStatus.Quarantined, lot.Status);
        Assert.Equal(new Quantity(40m, QuantityUnit.Kilogram), lot.InitialQuantity);
        Assert.Equal(lot.InitialQuantity, lot.RemainingQuantity);
        Assert.False(lot.IsDepleted);
        Assert.Equal(anyDay, lot.ManufacturedOn);
        Assert.Equal(anyDay.AddDays(365), lot.ExpiresOn);
    }

    [Fact]
    public void A_lot_cannot_be_created_empty() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Quantity     anyZero     = Any.Quantity().WithAmount(0m).Generate();
        DateOnly     anyDay      = AnyManufacturingDay();

        // Act
        Exception? thrown = Record.Exception(() => new Lot(anyNumber, anyMaterial, anyZero, anyDay, anyDay.AddDays(30)));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_lot_must_expire_after_it_is_manufactured() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();

        // Act
        Exception? sameDay   = Record.Exception(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Gram), anyDay, anyDay));
        Exception? earlier   = Record.Exception(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Gram), anyDay, anyDay.AddDays(-1)));
        Exception? nextDay   = Record.Exception(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Gram), anyDay, anyDay.AddDays(1)));

        // Assert
        Assert.IsType<ArgumentException>(sameDay);
        Assert.IsType<ArgumentException>(earlier);
        Assert.Null(nextDay);
    }

    [Fact]
    public void A_shelf_life_of_3653_days_is_the_longest_accepted() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();

        // Act
        Exception? longest = Record.Exception(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Gram), anyDay, anyDay.AddDays(3653)));
        Exception? tooLong = Record.Exception(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Gram), anyDay, anyDay.AddDays(3654)));

        // Assert
        Assert.Null(longest);
        Assert.IsType<ArgumentException>(tooLong);
    }

    [Fact]
    public void A_missing_number_material_or_quantity_is_refused() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Quantity     anyQuantity = Any.Quantity().WithAmount(a => a.Positive()).Generate();
        DateOnly     anyDay      = AnyManufacturingDay();

        // Act
        Exception? noNumber   = Record.Exception(() => new Lot(null!, anyMaterial, anyQuantity, anyDay, anyDay.AddDays(5)));
        Exception? noMaterial = Record.Exception(() => new Lot(anyNumber, null!, anyQuantity, anyDay, anyDay.AddDays(5)));
        Exception? noQuantity = Record.Exception(() => new Lot(anyNumber, anyMaterial, null!, anyDay, anyDay.AddDays(5)));

        // Assert
        Assert.IsType<ArgumentNullException>(noNumber);
        Assert.IsType<ArgumentNullException>(noMaterial);
        Assert.IsType<ArgumentNullException>(noQuantity);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(31, true)]
    [InlineData(-5, false)]
    public void A_lot_is_expired_from_its_expiry_date_included(int daysAfterManufacture, bool expected) {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(30)).Generate();

        // Act
        bool expired = lot.IsExpiredOn(anyDay.AddDays(daysAfterManufacture));

        // Assert
        Assert.Equal(expected, expired);
    }

    // ---- quality status ----------------------------------------------------------------------------------

    [Fact]
    public void Releasing_a_quarantined_lot_makes_it_released() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act
        anyLot.Release();

        // Assert
        Assert.Equal(LotStatus.Released, anyLot.Status);
    }

    [Fact]
    public void A_released_lot_cannot_be_released_again() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Release();

        // Act
        Exception? thrown = Record.Exception(() => anyLot.Release());

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(LotStatus.Released, anyLot.Status);
    }

    [Fact]
    public void A_rejected_lot_cannot_be_released() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? thrown = Record.Exception(() => anyLot.Release());

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_released_lot_can_be_sent_back_to_quarantine() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Release();

        // Act
        anyLot.Quarantine();

        // Assert
        Assert.Equal(LotStatus.Quarantined, anyLot.Status);
    }

    [Fact]
    public void Quarantining_a_quarantined_lot_changes_nothing() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act
        anyLot.Quarantine();

        // Assert
        Assert.Equal(LotStatus.Quarantined, anyLot.Status);
    }

    [Fact]
    public void A_quarantined_lot_can_be_released_again() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Release();
        anyLot.Quarantine();

        // Act
        anyLot.Release();

        // Assert
        Assert.Equal(LotStatus.Released, anyLot.Status);
    }

    [Fact]
    public void A_rejected_lot_cannot_return_to_quarantine() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? thrown = Record.Exception(() => anyLot.Quarantine());

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_lot_can_be_rejected_from_any_status_and_stays_rejected() {
        // Arrange
        Lot fromQuarantine = Any.Lot().Generate();
        Lot fromReleased   = Any.Lot().Generate();
        fromReleased.Release();
        Lot alreadyRejected = Any.Lot().Generate();
        alreadyRejected.Reject();

        // Act
        fromQuarantine.Reject();
        fromReleased.Reject();
        alreadyRejected.Reject();

        // Assert
        Assert.Equal(LotStatus.Rejected, fromQuarantine.Status);
        Assert.Equal(LotStatus.Rejected, fromReleased.Status);
        Assert.Equal(LotStatus.Rejected, alreadyRejected.Status);
    }

    // ---- consumption -------------------------------------------------------------------------------------

    [Fact]
    public void Consuming_part_of_a_released_lot_reduces_what_remains() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        // Act
        lot.Consume(new Quantity(400m, QuantityUnit.Gram), anyDay.AddDays(10));

        // Assert
        Assert.Equal(new Quantity(0.6m, QuantityUnit.Kilogram), lot.RemainingQuantity);
        Assert.Equal(QuantityUnit.Kilogram, lot.RemainingQuantity.Unit);
        Assert.Equal(new Quantity(1m, QuantityUnit.Kilogram), lot.InitialQuantity);
        Assert.False(lot.IsDepleted);
    }

    [Fact]
    public void Consuming_everything_depletes_the_lot() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(2m, QuantityUnit.Litre)).Generate();
        lot.Release();

        // Act
        lot.Consume(new Quantity(2000m, QuantityUnit.Millilitre), anyDay);

        // Assert
        Assert.True(lot.IsDepleted);
        Assert.True(lot.RemainingQuantity.IsZero);
    }

    [Fact]
    public void A_depleted_lot_cannot_give_anything_more() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(5m, QuantityUnit.Gram)).Generate();
        lot.Release();
        lot.Consume(new Quantity(5m, QuantityUnit.Gram), anyDay);

        // Act
        Exception? thrown = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Milligram), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void Consuming_more_than_what_remains_is_refused_and_changes_nothing() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        // Act
        Exception? thrown = Record.Exception(() => lot.Consume(new Quantity(1001m, QuantityUnit.Gram), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(new Quantity(1m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void Only_a_released_lot_can_be_consumed() {
        // Arrange
        DateOnly anyDay      = AnyManufacturingDay();
        Quantity anyQuantity = new Quantity(10m, QuantityUnit.Gram);
        Lot      quarantined = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        Lot      rejected    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        rejected.Reject();

        // Act
        Exception? fromQuarantine = Record.Exception(() => quarantined.Consume(anyQuantity, anyDay));
        Exception? fromRejected   = Record.Exception(() => rejected.Consume(anyQuantity, anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(fromQuarantine);
        Assert.IsType<InvalidOperationException>(fromRejected);
        Assert.Equal(new Quantity(1m, QuantityUnit.Kilogram), quarantined.RemainingQuantity);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_before_it_was_manufactured() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        // Act
        Exception? dayBefore = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Gram), anyDay.AddDays(-1)));
        Exception? sameDay   = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Gram), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(dayBefore);
        Assert.Null(sameDay);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_from_its_expiry_date_on() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        // Act
        Exception? lastDay   = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Gram), anyDay.AddDays(99)));
        Exception? expiryDay = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Gram), anyDay.AddDays(100)));

        // Assert
        Assert.Null(lastDay);
        Assert.IsType<InvalidOperationException>(expiryDay);
    }

    [Fact]
    public void A_mass_lot_cannot_give_a_volume() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(1m, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        // Act
        Exception? thrown = Record.Exception(() => lot.Consume(new Quantity(1m, QuantityUnit.Millilitre), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(new Quantity(1m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_nothing_is_refused() {
        // Arrange
        Lot anyLot = AnyReleasedLot(new Quantity(1m, QuantityUnit.Kilogram));

        // Act
        Exception? thrown = Record.Exception(() => anyLot.Consume(null!, anyLot.ManufacturedOn));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Successive_withdrawals_add_up() {
        // Arrange
        DateOnly anyDay = AnyManufacturingDay();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(100)).WithInitialQuantity(new Quantity(10m, QuantityUnit.Gram)).Generate();
        lot.Release();

        // Act
        lot.Consume(new Quantity(2.5m, QuantityUnit.Gram), anyDay);
        lot.Consume(new Quantity(2500m, QuantityUnit.Milligram), anyDay.AddDays(1));
        lot.Consume(new Quantity(0.005m, QuantityUnit.Kilogram), anyDay.AddDays(2));

        // Assert
        Assert.True(lot.IsDepleted);
    }

}
