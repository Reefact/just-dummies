namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class LotTests {

    private static Lot AnyReleasedLot(AnyLot recipe) {
        Lot lot = recipe.Generate();
        lot.Release();

        return lot;
    }

    // A date on which the lot may be used: on or after manufacture, before expiry.
    private static DateOnly AnyDayInShelfLife(Lot lot) {
        return Any.DateOnly().Between(lot.ManufacturedOn, lot.ExpiresOn.AddDays(-1)).Generate();
    }

    [Fact]
    public void A_new_lot_waits_in_quarantine_for_quality_release() {
        // Arrange
        AnyLot anyLot = Any.Lot();

        // Act
        Lot lot = anyLot.Generate();

        // Assert
        Assert.Equal(LotStatus.Quarantined, lot.Status);
    }

    [Fact]
    public void A_released_lot_cannot_be_released_a_second_time() {
        // Arrange
        Lot anyReleasedLot = AnyReleasedLot(Any.Lot());

        // Act
        Exception? error = Record.Exception(anyReleasedLot.Release);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_rejected_lot_cannot_go_back_to_quarantine() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act
        Exception? error = Record.Exception(anyLot.Quarantine);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_lot_still_in_quarantine_cannot_be_consumed() {
        // Arrange
        Lot      anyQuarantinedLot = Any.Lot().WithInitialQuantity(Kg(100m)).Generate();
        DateOnly anyDay            = AnyDayInShelfLife(anyQuarantinedLot);

        // Act
        Exception? error = Record.Exception(() => anyQuarantinedLot.Consume(Kg(10m), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Consuming_part_of_a_released_lot_leaves_the_rest_available() {
        // Arrange
        Lot      anyLotOf500Kg = AnyReleasedLot(Any.Lot().WithInitialQuantity(Kg(500m)));
        DateOnly anyDay        = AnyDayInShelfLife(anyLotOf500Kg);

        // Act
        anyLotOf500Kg.Consume(G(120_000m), anyDay);

        // Assert
        Assert.Equal(Kg(380m), anyLotOf500Kg.RemainingQuantity);
        Assert.False(anyLotOf500Kg.IsDepleted);
    }

    [Fact]
    public void Consuming_everything_a_lot_holds_depletes_it() {
        // Arrange
        Lot      anyLotOf2L = AnyReleasedLot(Any.Lot().WithInitialQuantity(L(2m)));
        DateOnly anyDay     = AnyDayInShelfLife(anyLotOf2L);

        // Act
        anyLotOf2L.Consume(new Quantity(2_000m, QuantityUnit.Millilitre), anyDay);

        // Assert
        Assert.True(anyLotOf2L.IsDepleted);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_on_its_expiry_date() {
        // Arrange
        DateOnly expiry           = new(2027, 3, 31);
        Lot      anyExpiringLot   = AnyReleasedLot(Any.Lot().WithInitialQuantity(Kg(100m)).WithExpiresOn(expiry));

        // Act
        Exception? error = Record.Exception(() => anyExpiringLot.Consume(Kg(1m), expiry));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(Kg(100m), anyExpiringLot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_before_the_day_it_was_manufactured() {
        // Arrange
        DateOnly manufacturedOn = new(2025, 6, 2);
        Lot      anyLot         = AnyReleasedLot(Any.Lot().WithInitialQuantity(Kg(100m)).WithManufacturedOn(manufacturedOn));

        // Act
        Exception? error = Record.Exception(() => anyLot.Consume(Kg(1m), new DateOnly(2025, 6, 1)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_volume_cannot_be_drawn_from_a_lot_measured_by_mass() {
        // Arrange
        Lot      anyLotWeighed = AnyReleasedLot(Any.Lot().WithInitialQuantity(quantity => quantity.Mass()));
        Quantity anyVolume     = Any.Quantity().Volume().NonZero().Generate();
        DateOnly anyDay        = AnyDayInShelfLife(anyLotWeighed);

        // Act
        Exception? error = Record.Exception(() => anyLotWeighed.Consume(anyVolume, anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_lot_cannot_give_more_than_it_still_holds() {
        // Arrange
        Lot      anyLotOf100Kg = AnyReleasedLot(Any.Lot().WithInitialQuantity(Kg(100m)));
        DateOnly anyDay        = AnyDayInShelfLife(anyLotOf100Kg);
        anyLotOf100Kg.Consume(Kg(60m), anyDay);

        // Act
        Exception? error = Record.Exception(() => anyLotOf100Kg.Consume(Kg(40.001m), anyDay));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(Kg(40m), anyLotOf100Kg.RemainingQuantity);
    }

}
