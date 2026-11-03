using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Entities;

public sealed class LotTests {

    // Lots are drawn through Any.Lot(), which keeps the shelf life within the limit. What a test is
    // about (a status, a date, a quantity) is pinned; everything else stays arbitrary.

    private static readonly DateOnly Manufacture = new DateOnly(2026, 3, 10);

    private static AnyLot AnyLotManufacturedOnTheTenthOfMarch() {
        return Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(365));
    }

    private static Lot AnyReleasedLot(decimal kg) {
        Lot lot = AnyLotManufacturedOnTheTenthOfMarch().WithInitialQuantity(new Quantity(kg, QuantityUnit.Kilogram)).Generate();
        lot.Release();

        return lot;
    }

    // ---- creation ----------------------------------------------------------------------------------

    [Fact]
    public void A_new_lot_is_quarantined_with_all_of_its_initial_quantity_remaining() {
        // Arrange
        Quantity initial = new Quantity(250m, QuantityUnit.Kilogram);

        // Act
        Lot lot = Any.Lot().WithInitialQuantity(initial).Generate();

        // Assert
        Assert.Equal(LotStatus.Quarantined, lot.Status);
        Assert.Equal(initial, lot.InitialQuantity);
        Assert.Equal(initial, lot.RemainingQuantity);
        Assert.False(lot.IsDepleted);
    }

    [Fact]
    public void A_lot_keeps_the_identity_material_and_dates_it_was_created_with() {
        // Arrange
        LotNumber    number   = new LotNumber("API-240117");
        MaterialCode material = new MaterialCode("MAT-01234");

        // Act
        Lot lot = Any.Lot().WithNumber(number).WithMaterial(material).WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(30)).Generate();

        // Assert
        Assert.Same(number, lot.Number);
        Assert.Same(material, lot.Material);
        Assert.Equal(Manufacture, lot.ManufacturedOn);
        Assert.Equal(Manufacture.AddDays(30), lot.ExpiresOn);
    }

    [Theory]
    [InlineData(QuantityUnit.Milligram)]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Millilitre)]
    [InlineData(QuantityUnit.Litre)]
    public void A_lot_cannot_be_created_empty_in_any_unit(QuantityUnit unit) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Lot().WithInitialQuantity(new Quantity(0m, unit)).Generate());
    }

    [Fact]
    public void A_lot_created_for_a_volume_is_measured_in_that_volume() {
        // Act
        Lot lot = Any.Lot().WithInitialQuantity(new Quantity(40m, QuantityUnit.Litre)).Generate();

        // Assert
        Assert.False(lot.InitialQuantity.IsMass);
    }

    [Fact]
    public void A_lot_expires_strictly_after_it_is_manufactured() {
        // Act / Assert: the same day, then the day before
        Assert.Throws<ArgumentException>(() => Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture).Generate());
        Assert.Throws<ArgumentException>(() => new Lot(Any.LotNumber().Generate(), Any.MaterialCode().Generate(), new Quantity(1m, QuantityUnit.Kilogram), Manufacture, Manufacture.AddDays(-1)));
    }

    [Fact]
    public void A_lot_that_expires_the_day_after_it_is_manufactured_is_accepted() {
        // Act
        Lot lot = Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(1)).Generate();

        // Assert
        Assert.Equal(Manufacture.AddDays(1), lot.ExpiresOn);
    }

    [Fact]
    public void The_longest_shelf_life_is_3653_days() {
        Assert.Equal(3653, Lot.MaximumShelfLifeDays);
    }

    [Fact]
    public void A_shelf_life_of_exactly_the_maximum_is_accepted() {
        // Act
        Lot lot = Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(Lot.MaximumShelfLifeDays)).Generate();

        // Assert
        Assert.Equal(Manufacture.AddDays(3653), lot.ExpiresOn);
    }

    [Fact]
    public void A_shelf_life_of_one_day_more_than_the_maximum_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(Lot.MaximumShelfLifeDays + 1)).Generate());
    }

    [Fact]
    public void A_missing_number_material_or_quantity_is_refused() {
        // Arrange
        LotNumber    anyNumber   = Any.LotNumber().Generate();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Quantity     anyQuantity = Any.Quantity().WithAmount(amount => amount.Positive()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Lot(null!, anyMaterial, anyQuantity, Manufacture, Manufacture.AddDays(1)));
        Assert.Throws<ArgumentNullException>(() => new Lot(anyNumber, null!, anyQuantity, Manufacture, Manufacture.AddDays(1)));
        Assert.Throws<ArgumentNullException>(() => new Lot(anyNumber, anyMaterial, null!, Manufacture, Manufacture.AddDays(1)));
    }

    [Fact]
    public void A_lot_whose_dates_span_the_whole_calendar_is_refused_by_the_shelf_life_limit() {
        Assert.Throws<ArgumentException>(() => Any.Lot().WithManufacturedOn(DateOnly.MinValue).WithExpiresOn(DateOnly.MaxValue).Generate());
    }

    // ---- expiry ------------------------------------------------------------------------------------

    [Fact]
    public void A_lot_is_not_expired_the_day_before_its_expiry_date_and_is_from_that_date_on() {
        // Arrange
        Lot lot = Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(100)).Generate();

        // Assert
        Assert.False(lot.IsExpiredOn(Manufacture.AddDays(99)));
        Assert.True(lot.IsExpiredOn(Manufacture.AddDays(100)));
        Assert.True(lot.IsExpiredOn(Manufacture.AddDays(101)));
    }

    [Fact]
    public void A_lot_is_not_expired_before_it_was_manufactured() {
        // Arrange
        Lot lot = Any.Lot().WithManufacturedOn(Manufacture).WithExpiresOn(Manufacture.AddDays(100)).Generate();

        // Assert
        Assert.False(lot.IsExpiredOn(Manufacture.AddDays(-1)));
    }

    // ---- quality status ----------------------------------------------------------------------------

    [Fact]
    public void A_quarantined_lot_can_be_released() {
        // Arrange
        Lot lot = Any.Lot().Generate();

        // Act
        lot.Release();

        // Assert
        Assert.Equal(LotStatus.Released, lot.Status);
    }

    [Fact]
    public void A_lot_cannot_be_released_twice() {
        // Arrange
        Lot lot = Any.Lot().Generate();
        lot.Release();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Release());
        Assert.Equal(LotStatus.Released, lot.Status);
    }

    [Fact]
    public void A_rejected_lot_cannot_be_released() {
        // Arrange
        Lot lot = Any.Lot().Generate();
        lot.Reject();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Release());
        Assert.Equal(LotStatus.Rejected, lot.Status);
    }

    [Fact]
    public void A_released_lot_can_go_back_to_quarantine_pending_an_investigation() {
        // Arrange
        Lot lot = Any.Lot().Generate();
        lot.Release();

        // Act
        lot.Quarantine();

        // Assert
        Assert.Equal(LotStatus.Quarantined, lot.Status);
    }

    [Fact]
    public void Quarantining_a_quarantined_lot_changes_nothing() {
        // Arrange
        Lot lot = Any.Lot().Generate();

        // Act
        lot.Quarantine();

        // Assert
        Assert.Equal(LotStatus.Quarantined, lot.Status);
    }

    [Fact]
    public void A_quarantined_lot_can_be_released_again_after_an_investigation() {
        // Arrange
        Lot lot = Any.Lot().Generate();
        lot.Release();
        lot.Quarantine();

        // Act
        lot.Release();

        // Assert
        Assert.Equal(LotStatus.Released, lot.Status);
    }

    [Fact]
    public void A_rejected_lot_cannot_return_to_quarantine() {
        // Arrange
        Lot lot = Any.Lot().Generate();
        lot.Reject();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Quarantine());
        Assert.Equal(LotStatus.Rejected, lot.Status);
    }

    [Theory]
    [InlineData(LotStatus.Quarantined)]
    [InlineData(LotStatus.Released)]
    [InlineData(LotStatus.Rejected)]
    public void A_lot_can_be_rejected_from_any_status_and_it_is_final(LotStatus from) {
        // Arrange
        Lot lot = Any.Lot().Generate();
        if (from == LotStatus.Released) { lot.Release(); }
        if (from == LotStatus.Rejected) { lot.Reject(); }

        // Act
        lot.Reject();

        // Assert
        Assert.Equal(LotStatus.Rejected, lot.Status);
    }

    // ---- consumption -------------------------------------------------------------------------------

    [Fact]
    public void Consuming_a_released_lot_withdraws_the_quantity_from_what_remains() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);

        // Act
        lot.Consume(new Quantity(30m, QuantityUnit.Kilogram), Manufacture.AddDays(10));

        // Assert
        Assert.Equal(new Quantity(70m, QuantityUnit.Kilogram), lot.RemainingQuantity);
        Assert.Equal(new Quantity(100m, QuantityUnit.Kilogram), lot.InitialQuantity);
        Assert.False(lot.IsDepleted);
    }

    [Fact]
    public void Consuming_in_another_unit_of_the_same_dimension_is_converted() {
        // Arrange
        Lot lot = AnyReleasedLot(1m);

        // Act
        lot.Consume(new Quantity(250m, QuantityUnit.Gram), Manufacture.AddDays(10));

        // Assert
        Assert.Equal(new Quantity(750m, QuantityUnit.Gram), lot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_everything_that_remains_depletes_the_lot() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);
        lot.Consume(new Quantity(40m, QuantityUnit.Kilogram), Manufacture.AddDays(10));

        // Act
        lot.Consume(new Quantity(60m, QuantityUnit.Kilogram), Manufacture.AddDays(11));

        // Assert
        Assert.True(lot.IsDepleted);
        Assert.True(lot.RemainingQuantity.IsZero);
    }

    [Fact]
    public void Consuming_more_than_what_remains_is_refused_and_leaves_the_lot_untouched() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);
        lot.Consume(new Quantity(40m, QuantityUnit.Kilogram), Manufacture.AddDays(10));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(60.001m, QuantityUnit.Kilogram), Manufacture.AddDays(11)));
        Assert.Equal(new Quantity(60m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void A_volume_cannot_be_consumed_from_a_lot_measured_in_mass() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Litre), Manufacture.AddDays(10)));
        Assert.Equal(new Quantity(100m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Theory]
    [InlineData(LotStatus.Quarantined)]
    [InlineData(LotStatus.Rejected)]
    public void Only_a_released_lot_can_be_consumed(LotStatus status) {
        // Arrange
        Lot lot = AnyLotManufacturedOnTheTenthOfMarch().WithInitialQuantity(new Quantity(100m, QuantityUnit.Kilogram)).Generate();
        if (status == LotStatus.Rejected) { lot.Reject(); }

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), Manufacture.AddDays(10)));
        Assert.Equal(new Quantity(100m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_sent_back_to_quarantine_cannot_be_consumed_until_released_again() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);
        lot.Quarantine();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), Manufacture.AddDays(10)));
        lot.Release();
        lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), Manufacture.AddDays(10));
        Assert.Equal(new Quantity(99m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_can_be_consumed_on_the_day_it_is_manufactured_but_not_the_day_before() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), Manufacture.AddDays(-1)));
        lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), Manufacture);
        Assert.Equal(new Quantity(99m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void A_lot_can_be_consumed_the_day_before_it_expires_but_not_on_its_expiry_date() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);
        DateOnly expiry = lot.ExpiresOn;

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), expiry));
        Assert.Throws<InvalidOperationException>(() => lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), expiry.AddDays(1)));
        lot.Consume(new Quantity(1m, QuantityUnit.Kilogram), expiry.AddDays(-1));
        Assert.Equal(new Quantity(99m, QuantityUnit.Kilogram), lot.RemainingQuantity);
    }

    [Fact]
    public void Consuming_nothing_is_refused() {
        // Arrange
        Lot lot = AnyReleasedLot(100m);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => lot.Consume(null!, Manufacture.AddDays(10)));
    }

    [Fact]
    public void Consuming_a_lot_measured_in_volume_works_in_volume_units() {
        // Arrange
        Lot lot = AnyLotManufacturedOnTheTenthOfMarch().WithInitialQuantity(new Quantity(2m, QuantityUnit.Litre)).Generate();
        lot.Release();

        // Act
        lot.Consume(new Quantity(500m, QuantityUnit.Millilitre), Manufacture.AddDays(1));

        // Assert
        Assert.Equal(new Quantity(1.5m, QuantityUnit.Litre), lot.RemainingQuantity);
    }

}
