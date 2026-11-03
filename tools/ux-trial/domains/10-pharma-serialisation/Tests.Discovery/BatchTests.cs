using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class BatchTests {

    private static readonly DateOnly Expiry = new DateOnly(2027, 1, 31);

    // ---- construction ----------------------------------------------------------------------

    [Fact]
    public void A_new_batch_exposes_what_it_was_built_from_and_holds_no_pack() {
        // Arrange
        Gtin               anyGtin    = new AnyGtin().Generate();
        BatchNumber        anyNumber  = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice   = new AnyReimbursementPrice().Generate();
        DateOnly           made       = new DateOnly(2025, 2, 1);
        DateOnly           expires    = new DateOnly(2027, 1, 31);

        // Act
        Batch batch = new Batch(anyGtin, anyNumber, made, expires, 1000, anyPrice);

        // Assert
        Assert.Same(anyGtin, batch.Gtin);
        Assert.Same(anyNumber, batch.Number);
        Assert.Same(anyPrice, batch.Price);
        Assert.Equal(made, batch.ManufacturedOn);
        Assert.Equal(expires, batch.ExpiresOn);
        Assert.Equal(1000, batch.PlannedQuantity);
        Assert.Empty(batch.Packs);
    }

    [Fact]
    public void A_batch_cannot_expire_the_day_it_is_made() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act / Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Batch(anyGtin, anyNumber, anyDay, anyDay, 10, anyPrice));
        Assert.Equal("expiresOn", exception.ParamName);
    }

    [Fact]
    public void A_batch_cannot_expire_before_it_is_made() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();

        // Act / Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Batch(anyGtin, anyNumber, new DateOnly(2025, 2, 2), new DateOnly(2025, 2, 1), 10, anyPrice));
        Assert.Equal("expiresOn", exception.ParamName);
    }

    [Fact]
    public void A_batch_may_expire_the_day_after_it_is_made() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();

        // Act
        Batch batch = new Batch(anyGtin, anyNumber, new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 2), 10, anyPrice);

        // Assert
        Assert.Equal(new DateOnly(2025, 2, 2), batch.ExpiresOn);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500_000)]
    public void A_planned_quantity_from_one_to_five_hundred_thousand_is_accepted(int plannedQuantity) {
        // Act
        Batch batch = new AnyBatch().WithPlannedQuantity(plannedQuantity).Generate();

        // Assert
        Assert.Equal(plannedQuantity, batch.PlannedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(500_001)]
    [InlineData(int.MaxValue)]
    public void A_planned_quantity_outside_one_to_five_hundred_thousand_is_refused(int plannedQuantity) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnyBatch().WithPlannedQuantity(plannedQuantity).Generate());
    }

    [Fact]
    public void A_batch_without_a_GTIN_a_number_or_a_price_is_refused() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();
        DateOnly           made      = new DateOnly(2025, 2, 1);

        // Act / Assert
        Assert.Equal("gtin", Assert.Throws<ArgumentNullException>(() => new Batch(null!, anyNumber, made, Expiry, 10, anyPrice)).ParamName);
        Assert.Equal("number", Assert.Throws<ArgumentNullException>(() => new Batch(anyGtin, null!, made, Expiry, 10, anyPrice)).ParamName);
        Assert.Equal("price", Assert.Throws<ArgumentNullException>(() => new Batch(anyGtin, anyNumber, made, Expiry, 10, null!)).ParamName);
    }

    // ---- commissioning ---------------------------------------------------------------------

    [Fact]
    public void Commissioning_a_serial_registers_an_active_pack_carrying_the_batch_identity() {
        // Arrange
        Batch        anyBatch  = AnyBatchWithRoomFor(1).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act
        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Assert
        Assert.Equal(anyBatch.Gtin, pack.Gtin);
        Assert.Equal(anyBatch.Number, pack.Batch);
        Assert.Equal(anyBatch.ExpiresOn, pack.ExpiresOn);
        Assert.Equal(anySerial, pack.Serial);
        Assert.Equal(PackStatus.Active, pack.Status);
        Assert.Same(pack, Assert.Single(anyBatch.Packs));
    }

    [Fact]
    public void A_serial_cannot_be_commissioned_twice() {
        // Arrange
        Batch        anyBatch  = AnyBatchWithRoomFor(2).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(new SerialNumber(anySerial.Value)));
        Assert.Single(anyBatch.Packs);
    }

    [Fact]
    public void A_full_batch_refuses_one_more_serial() {
        // Arrange
        Batch                      batch   = new AnyBatch().WithPlannedQuantity(2).Generate();
        IReadOnlyList<SerialNumber> serials = AnyDistinctSerials(3);
        batch.Commission(serials[0]);
        batch.Commission(serials[1]);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => batch.Commission(serials[2]));
        Assert.Equal(2, batch.Packs.Count);
    }

    [Fact]
    public void A_batch_can_be_filled_exactly_to_its_planned_quantity() {
        // Arrange
        Batch                       batch   = new AnyBatch().WithPlannedQuantity(3).Generate();
        IReadOnlyList<SerialNumber> serials = AnyDistinctSerials(3);

        // Act
        batch.CommissionAll(serials);

        // Assert
        Assert.Equal(3, batch.Packs.Count);
    }

    [Fact]
    public void Committing_an_already_registered_serial_to_a_full_batch_reports_the_duplicate_first() {
        // Arrange
        Batch        batch     = new AnyBatch().WithPlannedQuantity(1).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        batch.Commission(anySerial);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => batch.Commission(anySerial));

        // Assert
        Assert.Contains("already commissioned", exception.Message);
    }

    [Fact]
    public void A_null_serial_cannot_be_commissioned() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Commission(null!));
    }

    [Fact]
    public void CommissionAll_returns_the_packs_in_the_order_of_the_serials() {
        // Arrange
        Batch                       anyBatch = AnyBatchWithRoomFor(5).Generate();
        IReadOnlyList<SerialNumber> serials  = AnyDistinctSerials(5);

        // Act
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(serials);

        // Assert
        Assert.Equal(serials, packs.Select(pack => pack.Serial));
        Assert.Equal(5, anyBatch.Packs.Count);
    }

    [Fact]
    public void CommissionAll_stops_at_the_first_failure_and_keeps_the_packs_commissioned_before_it() {
        // Arrange
        Batch                       anyBatch = AnyBatchWithRoomFor(5).Generate();
        IReadOnlyList<SerialNumber> serials  = AnyDistinctSerials(3);
        SerialNumber[]              list     = [serials[0], serials[1], serials[0], serials[2]];

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.CommissionAll(list));
        Assert.Equal(2, anyBatch.Packs.Count);
        Assert.DoesNotContain(anyBatch.Packs, pack => pack.Serial.Equals(serials[2]));
    }

    [Fact]
    public void CommissionAll_on_a_batch_too_small_keeps_what_fitted() {
        // Arrange
        Batch                       batch   = new AnyBatch().WithPlannedQuantity(2).Generate();
        IReadOnlyList<SerialNumber> serials = AnyDistinctSerials(4);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => batch.CommissionAll(serials));
        Assert.Equal(2, batch.Packs.Count);
    }

    [Fact]
    public void CommissionAll_refuses_a_null_list() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.CommissionAll(null!));
    }

    // ---- verification ----------------------------------------------------------------------

    [Fact]
    public void An_unregistered_serial_is_unknown() {
        // Arrange
        Batch        anyBatch  = AnyBatchExpiringOn(Expiry).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        DateOnly     anyToday  = Any.DateOnly().Generate();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyToday);

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void A_null_serial_cannot_be_verified() {
        // Arrange
        Batch    anyBatch = new AnyBatch().Generate();
        DateOnly anyToday = Any.DateOnly().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Verify(null!, anyToday));
    }

    [Fact]
    public void An_active_pack_is_authentic_before_its_expiry_date() {
        // Arrange
        Batch          batch    = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack     = batch.Commission(new AnySerialNumber().Generate());
        DateOnly       anyToday = Any.DateOnly().Before(Expiry).Generate();

        // Act
        VerificationOutcome outcome = batch.Verify(pack.Serial, anyToday);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void The_expiry_day_itself_is_still_authentic() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());

        // Act
        VerificationOutcome outcome = batch.Verify(pack.Serial, Expiry);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void The_day_after_expiry_the_pack_is_expired_but_stays_active() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());

        // Act
        VerificationOutcome outcome = batch.Verify(pack.Serial, Expiry.AddDays(1));

        // Assert
        Assert.Equal(VerificationOutcome.Expired, outcome);
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void A_pack_long_after_its_expiry_is_expired() {
        // Arrange
        Batch          batch    = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack     = batch.Commission(new AnySerialNumber().Generate());
        DateOnly       anyLater = Any.DateOnly().After(Expiry).Generate();

        // Act / Assert
        Assert.Equal(VerificationOutcome.Expired, batch.Verify(pack.Serial, anyLater));
    }

    [Fact]
    public void A_date_before_the_batch_was_made_is_still_authentic() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).WithManufacturedOn(new DateOnly(2025, 2, 1)).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());

        // Act / Assert
        Assert.Equal(VerificationOutcome.Authentic, batch.Verify(pack.Serial, new DateOnly(2020, 1, 1)));
    }

    [Fact]
    public void A_dispensed_pack_is_already_decommissioned() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        DateOnly       today = Any.DateOnly().Before(Expiry).Generate();
        batch.Dispense(pack.Serial, today);

        // Act / Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, batch.Verify(pack.Serial, today));
    }

    [Fact]
    public void A_decommissioned_pack_reads_as_decommissioned_even_after_its_expiry() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        batch.Recall();

        // Act / Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, batch.Verify(pack.Serial, Expiry.AddDays(30)));
    }

    // ---- dispensing ------------------------------------------------------------------------

    [Fact]
    public void Dispensing_an_authentic_pack_marks_it_dispensed() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());

        // Act
        batch.Dispense(pack.Serial, Expiry);

        // Assert
        Assert.Equal(PackStatus.Dispensed, pack.Status);
    }

    [Fact]
    public void A_pack_cannot_be_dispensed_twice() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        batch.Dispense(pack.Serial, Expiry);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => batch.Dispense(pack.Serial, Expiry));
        Assert.Equal(PackStatus.Dispensed, pack.Status);
    }

    [Fact]
    public void An_expired_pack_cannot_be_dispensed_and_stays_active() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());

        // Act / Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => batch.Dispense(pack.Serial, Expiry.AddDays(1)));
        Assert.Contains("Expired", exception.Message);
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void An_unknown_serial_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch  = AnyBatchExpiringOn(Expiry).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, Expiry));
    }

    [Fact]
    public void A_recalled_pack_cannot_be_dispensed() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        batch.Recall();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => batch.Dispense(pack.Serial, Expiry));
        Assert.Equal(PackStatus.Recalled, pack.Status);
    }

    [Fact]
    public void A_null_serial_cannot_be_dispensed() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Dispense(null!, Expiry));
    }

    // ---- recall ----------------------------------------------------------------------------

    [Fact]
    public void Recalling_an_empty_batch_recalls_nothing() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act / Assert
        Assert.Equal(0, anyBatch.Recall());
    }

    [Fact]
    public void Recall_marks_every_active_pack_recalled_and_counts_them() {
        // Arrange
        Batch anyBatch = AnyBatchWithRoomFor(4).Generate();
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(AnyDistinctSerials(4));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(4, recalled);
        Assert.All(packs, pack => Assert.Equal(PackStatus.Recalled, pack.Status));
    }

    [Fact]
    public void Recall_leaves_dispensed_packs_untouched() {
        // Arrange
        Batch                         batch = AnyBatchExpiringOn(Expiry).WithPlannedQuantity(10).Generate();
        IReadOnlyList<SerialisedPack> packs = batch.CommissionAll(AnyDistinctSerials(3));
        batch.Dispense(packs[1].Serial, Expiry);

        // Act
        int recalled = batch.Recall();

        // Assert
        Assert.Equal(2, recalled);
        Assert.Equal(PackStatus.Recalled, packs[0].Status);
        Assert.Equal(PackStatus.Dispensed, packs[1].Status);
        Assert.Equal(PackStatus.Recalled, packs[2].Status);
    }

    [Fact]
    public void Recall_also_reaches_packs_that_are_expired_but_still_active() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        Assert.Equal(VerificationOutcome.Expired, batch.Verify(pack.Serial, Expiry.AddDays(400)));

        // Act
        int recalled = batch.Recall();

        // Assert
        Assert.Equal(1, recalled);
        Assert.Equal(PackStatus.Recalled, pack.Status);
    }

    [Fact]
    public void A_second_recall_finds_nothing_left_to_recall() {
        // Arrange
        Batch anyBatch = AnyBatchWithRoomFor(3).Generate();
        anyBatch.CommissionAll(AnyDistinctSerials(3));
        anyBatch.Recall();

        // Act / Assert
        Assert.Equal(0, anyBatch.Recall());
    }

    // ---- remaining shelf life --------------------------------------------------------------

    [Theory]
    [InlineData("2025-01-01", 1.00)]   // on the manufacturing day
    [InlineData("2024-06-15", 1.00)]   // before it
    [InlineData("2025-01-06", 0.50)]   // half-way through ten days
    [InlineData("2025-01-10", 0.10)]
    [InlineData("2025-01-11", 0.00)]   // on the expiry day
    [InlineData("2025-03-01", 0.00)]   // after it
    public void The_remaining_ratio_is_clamped_between_one_and_zero(string today, double expected) {
        // Arrange
        Batch batch = new AnyBatch().WithManufacturedOn(new DateOnly(2025, 1, 1)).WithExpiresOn(new DateOnly(2025, 1, 11)).Generate();

        // Act
        decimal ratio = batch.RemainingShelfLifeRatio(DateOnly.Parse(today));

        // Assert
        Assert.Equal((decimal)expected, ratio);
    }

    [Theory]
    [InlineData(3, 1, "0.33")]
    [InlineData(3, 2, "0.67")]
    [InlineData(8, 1, "0.13")]     // 0.125: the midpoint goes away from zero
    [InlineData(8, 3, "0.38")]     // 0.375
    [InlineData(40, 1, "0.03")]    // 0.025
    [InlineData(200, 1, "0.01")]   // 0.005
    [InlineData(200, 199, "1.00")] // 0.995
    public void The_remaining_ratio_is_rounded_to_two_decimals_with_midpoints_away_from_zero(int totalDays, int daysLeft, string expected) {
        // Arrange
        DateOnly made    = new DateOnly(2025, 1, 1);
        DateOnly expires = made.AddDays(totalDays);
        Batch    batch   = new AnyBatch().WithManufacturedOn(made).WithExpiresOn(expires).Generate();

        // Act
        decimal ratio = batch.RemainingShelfLifeRatio(expires.AddDays(-daysLeft));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), ratio);
    }

    // ---- stock value -----------------------------------------------------------------------

    [Fact]
    public void An_empty_batch_has_no_stock_value() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act / Assert
        Assert.Equal(0m, anyBatch.ActiveStockValue());
    }

    [Fact]
    public void The_stock_value_is_the_number_of_active_packs_times_the_unit_price() {
        // Arrange
        Batch batch = AnyBatchExpiringOn(Expiry).WithPlannedQuantity(10).WithPrice(new ReimbursementPrice(12.50m)).Generate();
        IReadOnlyList<SerialisedPack> packs = batch.CommissionAll(AnyDistinctSerials(3));

        // Act / Assert
        Assert.Equal(37.50m, batch.ActiveStockValue());

        batch.Dispense(packs[0].Serial, Expiry);
        Assert.Equal(25.00m, batch.ActiveStockValue());

        batch.Recall();
        Assert.Equal(0m, batch.ActiveStockValue());
    }

    [Fact]
    public void The_stock_value_keeps_the_cents_on_a_large_batch() {
        // Arrange
        Batch batch = new AnyBatch().WithPlannedQuantity(100).WithPrice(new ReimbursementPrice(99999.99m)).Generate();
        batch.CommissionAll(AnyDistinctSerials(100));

        // Act / Assert
        Assert.Equal(9_999_999m, batch.ActiveStockValue());
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static AnyBatch AnyBatchWithRoomFor(int packs) {
        return new AnyBatch().WithPlannedQuantity(p => p.GreaterThanOrEqualTo(packs));
    }

    private static AnyBatch AnyBatchExpiringOn(DateOnly expiresOn) {
        return new AnyBatch().WithExpiresOn(expiresOn).WithPlannedQuantity(p => p.GreaterThanOrEqualTo(10));
    }

    private static IReadOnlyList<SerialNumber> AnyDistinctSerials(int count) {
        return Any.ListOf(new AnySerialNumber()).WithCount(count).Distinct().Generate();
    }

}
