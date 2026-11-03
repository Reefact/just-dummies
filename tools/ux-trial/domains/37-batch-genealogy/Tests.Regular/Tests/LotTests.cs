namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class LotTests {

    private static readonly DateOnly _anyDay = new DateOnly(2024, 3, 1);

    // --- creation -------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_lot_is_quarantined_with_its_whole_quantity_remaining() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        DateOnly     anyDay      = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)).Generate();

        // Act
        Lot lot = new Lot(anyNumber, anyMaterial, Kg.Of(40m), anyDay, anyDay.AddDays(365));

        // Assert
        Assert.Same(anyNumber, lot.Number);
        Assert.Same(anyMaterial, lot.Material);
        Assert.Equal(LotStatus.Quarantined, lot.Status);
        Assert.Equal(Kg.Of(40m), lot.InitialQuantity);
        Assert.Equal(Kg.Of(40m), lot.RemainingQuantity);
        Assert.False(lot.IsDepleted);
        Assert.Equal(anyDay, lot.ManufacturedOn);
        Assert.Equal(anyDay.AddDays(365), lot.ExpiresOn);
    }

    [Fact]
    public void A_drawn_lot_is_valid_and_quarantined() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Assert
        Assert.Equal(LotStatus.Quarantined, anyLot.Status);
        Assert.True(anyLot.ExpiresOn > anyLot.ManufacturedOn);
        Assert.Equal(anyLot.InitialQuantity, anyLot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_needs_a_positive_quantity() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        QuantityUnit anyUnit     = Any.Enum<QuantityUnit>().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Lot(anyNumber, anyMaterial, new Quantity(0m, anyUnit), _anyDay, _anyDay.AddDays(10)));
    }

    [Fact]
    public void A_lot_cannot_expire_the_day_it_is_manufactured() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Lot(anyLot.Number, anyLot.Material, anyLot.InitialQuantity, anyLot.ManufacturedOn, anyLot.ManufacturedOn));
    }

    [Fact]
    public void A_lot_cannot_expire_before_it_is_manufactured() {
        // Arrange
        Lot anyLot  = Any.Lot().Generate();
        int anyDays = Any.Int32().Between(1, 3_000).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Lot(anyLot.Number, anyLot.Material, anyLot.InitialQuantity, anyLot.ManufacturedOn, anyLot.ManufacturedOn.AddDays(-anyDays)));
    }

    [Fact]
    public void The_longest_shelf_life_is_accepted() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act
        Lot lot = new Lot(anyLot.Number, anyLot.Material, anyLot.InitialQuantity, _anyDay, _anyDay.AddDays(Lot.MaximumShelfLifeDays));

        // Assert
        Assert.Equal(_anyDay.AddDays(3653), lot.ExpiresOn);
    }

    [Fact]
    public void A_shelf_life_one_day_beyond_the_maximum_is_refused() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Lot(anyLot.Number, anyLot.Material, anyLot.InitialQuantity, _anyDay, _anyDay.AddDays(3654)));
    }

    [Fact]
    public void A_lot_needs_all_its_parts() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Lot(null!, anyLot.Material, anyLot.InitialQuantity, anyLot.ManufacturedOn, anyLot.ExpiresOn));
        Assert.Throws<ArgumentNullException>(() => new Lot(anyLot.Number, null!, anyLot.InitialQuantity, anyLot.ManufacturedOn, anyLot.ExpiresOn));
        Assert.Throws<ArgumentNullException>(() => new Lot(anyLot.Number, anyLot.Material, null!, anyLot.ManufacturedOn, anyLot.ExpiresOn));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    public void A_lot_is_expired_from_its_expiry_date_on(int daysFromExpiry, bool expected) {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)).Generate();
        Lot      lot    = Any.Lot().WithManufacturedOn(anyDay).WithExpiresOn(anyDay.AddDays(200)).Generate();

        // Act
        bool expired = lot.IsExpiredOn(lot.ExpiresOn.AddDays(daysFromExpiry));

        // Assert
        Assert.Equal(expected, expired);
    }

    // --- quality status -----------------------------------------------------------------------------------------

    [Fact]
    public void A_quarantined_lot_can_be_released() {
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

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Release());
    }

    [Fact]
    public void A_rejected_lot_cannot_be_released() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Release());
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_released_lot_can_go_back_to_quarantine_pending_investigation() {
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
    public void A_rejected_lot_cannot_return_to_quarantine() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Reject();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Quarantine());
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    [Fact]
    public void A_released_lot_can_be_rejected_and_stays_so() {
        // Arrange
        Lot anyLot = Any.Lot().Generate();
        anyLot.Release();

        // Act
        anyLot.Reject();
        anyLot.Reject();

        // Assert
        Assert.Equal(LotStatus.Rejected, anyLot.Status);
    }

    // --- consumption ------------------------------------------------------------------------------------------------

    [Fact]
    public void Consuming_withdraws_the_quantity_from_what_remains() {
        // Arrange
        Lot anyLot = ReleasedLot(100m);

        // Act
        anyLot.Consume(Kg.Of(30m), anyLot.ManufacturedOn.AddDays(1));

        // Assert
        Assert.Equal(Kg.Of(70m), anyLot.RemainingQuantity);
        Assert.Equal(Kg.Of(100m), anyLot.InitialQuantity);
        Assert.False(anyLot.IsDepleted);
    }

    [Fact]
    public void Consuming_in_another_unit_of_the_same_dimension_converts() {
        // Arrange
        Lot anyLot = ReleasedLot(1m);

        // Act
        anyLot.Consume(new Quantity(250m, QuantityUnit.Gram), anyLot.ManufacturedOn.AddDays(1));

        // Assert
        Assert.Equal(Kg.Of(0.75m), anyLot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_everything_depletes_the_lot() {
        // Arrange
        Lot anyLot = ReleasedLot(100m);

        // Act
        anyLot.Consume(new Quantity(100_000m, QuantityUnit.Gram), anyLot.ManufacturedOn);

        // Assert
        Assert.True(anyLot.IsDepleted);
        Assert.True(anyLot.RemainingQuantity.IsZero);
    }

    [Fact]
    public void Consumption_adds_up_until_nothing_is_left() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);
        anyLot.Consume(Kg.Of(4m), anyLot.ManufacturedOn);
        anyLot.Consume(Kg.Of(6m), anyLot.ManufacturedOn);

        // Act / Assert
        Assert.True(anyLot.IsDepleted);
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(0.001m), anyLot.ManufacturedOn));
    }

    [Fact]
    public void More_than_what_remains_cannot_be_consumed_and_nothing_is_withdrawn() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);
        anyLot.Consume(Kg.Of(4m), anyLot.ManufacturedOn);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(6.001m), anyLot.ManufacturedOn));
        Assert.Equal(Kg.Of(6m), anyLot.RemainingQuantity);
    }

    [Fact]
    public void A_quarantined_lot_cannot_be_consumed() {
        // Arrange
        Lot anyLot = Any.Lot().WithInitialQuantity(Kg.Of(10m)).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(1m), anyLot.ManufacturedOn));
    }

    [Fact]
    public void A_rejected_lot_cannot_be_consumed() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);
        anyLot.Reject();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(1m), anyLot.ManufacturedOn));
    }

    [Fact]
    public void A_lot_cannot_be_consumed_before_it_was_manufactured() {
        // Arrange
        Lot anyLot  = ReleasedLot(10m);
        int anyDays = Any.Int32().Between(1, 1_000).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(1m), anyLot.ManufacturedOn.AddDays(-anyDays)));
    }

    [Fact]
    public void A_lot_can_be_consumed_the_day_it_was_manufactured() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);

        // Act
        anyLot.Consume(Kg.Of(1m), anyLot.ManufacturedOn);

        // Assert
        Assert.Equal(Kg.Of(9m), anyLot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_can_be_consumed_the_day_before_it_expires() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);

        // Act
        anyLot.Consume(Kg.Of(1m), anyLot.ExpiresOn.AddDays(-1));

        // Assert
        Assert.Equal(Kg.Of(9m), anyLot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_cannot_be_consumed_on_its_expiry_date() {
        // Arrange
        Lot anyLot = ReleasedLot(10m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(1m), anyLot.ExpiresOn));
    }

    [Fact]
    public void A_lot_cannot_be_consumed_after_it_expired() {
        // Arrange
        Lot anyLot  = ReleasedLot(10m);
        int anyDays = Any.Int32().Between(1, 1_000).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(Kg.Of(1m), anyLot.ExpiresOn.AddDays(anyDays)));
    }

    [Fact]
    public void A_volume_cannot_be_drawn_from_a_lot_measured_in_mass() {
        // Arrange
        Lot          anyLot    = ReleasedLot(10m);
        QuantityUnit anyVolume = Any.OneOf(QuantityUnit.Millilitre, QuantityUnit.Litre).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(new Quantity(1m, anyVolume), anyLot.ManufacturedOn));
        Assert.Equal(Kg.Of(10m), anyLot.RemainingQuantity);
    }

    [Fact]
    public void A_mass_cannot_be_drawn_from_a_lot_measured_in_volume() {
        // Arrange
        Lot anyLot = Any.Lot().WithInitialQuantity(new Quantity(10m, QuantityUnit.Litre)).Generate();
        anyLot.Release();
        QuantityUnit anyMass = Any.OneOf(QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLot.Consume(new Quantity(1m, anyMass), anyLot.ManufacturedOn));
    }

    private static Lot ReleasedLot(decimal kilograms) {
        Lot lot = Any.Lot().WithInitialQuantity(Kg.Of(kilograms)).Generate();
        lot.Release();

        return lot;
    }

}
