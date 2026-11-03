using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class BatchTests {

    private static readonly DateOnly Made    = new DateOnly(2024, 1, 1);
    private static readonly DateOnly Expires = new DateOnly(2025, 1, 1);

    // ---- construction -------------------------------------------------------------------------------------

    [Fact]
    public void A_new_batch_keeps_what_it_was_given_and_has_no_pack() {
        // Arrange
        Gtin               anyGtin  = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice = new AnyReimbursementPrice().Generate();

        // Act
        Batch batch = new Batch(anyGtin, anyNumber, Made, Expires, 1_000, anyPrice);

        // Assert
        Assert.Same(anyGtin, batch.Gtin);
        Assert.Same(anyNumber, batch.Number);
        Assert.Same(anyPrice, batch.Price);
        Assert.Equal(Made, batch.ManufacturedOn);
        Assert.Equal(Expires, batch.ExpiresOn);
        Assert.Equal(1_000, batch.PlannedQuantity);
        Assert.Empty(batch.Packs);
    }

    [Fact]
    public void A_drawn_batch_has_no_pack_and_expires_after_it_is_made() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Assert
        Assert.Empty(anyBatch.Packs);
        Assert.True(anyBatch.ExpiresOn > anyBatch.ManufacturedOn);
    }

    [Fact]
    public void A_batch_without_a_gtin_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnyBatch().WithGtin((Gtin)null!).Generate());

        // Assert
        Assert.Equal("gtin", refusal.ParamName);
    }

    [Fact]
    public void A_batch_without_a_number_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnyBatch().WithNumber((BatchNumber)null!).Generate());

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void A_batch_without_a_price_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnyBatch().WithPrice((ReimbursementPrice)null!).Generate());

        // Assert
        Assert.Equal("price", refusal.ParamName);
    }

    [Fact]
    public void A_batch_expiring_the_day_it_is_made_is_refused() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Batch(anyGtin, anyNumber, Made, Made, 10, anyPrice));

        // Assert
        Assert.Equal("expiresOn", refusal.ParamName);
    }

    [Fact]
    public void A_batch_expiring_before_it_is_made_is_refused() {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Batch(anyGtin, anyNumber, Made, Made.AddDays(-1), 10, anyPrice));

        // Assert
        Assert.Equal("expiresOn", refusal.ParamName);
    }

    [Fact]
    public void A_batch_expiring_the_day_after_it_is_made_is_accepted() {
        // Act
        Batch batch = new AnyBatch().WithManufacturedOn(Made).WithExpiresOn(Made.AddDays(1)).Generate();

        // Assert
        Assert.Equal(Made.AddDays(1), batch.ExpiresOn);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500_000)]
    public void A_planned_quantity_from_one_to_five_hundred_thousand_is_accepted(int quantity) {
        // Act
        Batch batch = new AnyBatch().WithPlannedQuantity(quantity).Generate();

        // Assert
        Assert.Equal(quantity, batch.PlannedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(500_001)]
    public void A_planned_quantity_out_of_range_is_refused(int quantity) {
        // Arrange
        Gtin               anyGtin   = new AnyGtin().Generate();
        BatchNumber        anyNumber = new AnyBatchNumber().Generate();
        ReimbursementPrice anyPrice  = new AnyReimbursementPrice().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Batch(anyGtin, anyNumber, Made, Expires, quantity, anyPrice));

        // Assert
        Assert.Equal("plannedQuantity", refusal.ParamName);
    }

    // ---- commissioning ------------------------------------------------------------------------------------

    [Fact]
    public void Commissioning_a_serial_registers_an_active_pack_carrying_the_batch_identity() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act
        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
        Assert.Same(anySerial, pack.Serial);
        Assert.Same(anyBatch.Gtin, pack.Gtin);
        Assert.Same(anyBatch.Number, pack.Batch);
        Assert.Equal(Expires, pack.ExpiresOn);
        Assert.Same(pack, Assert.Single(anyBatch.Packs));
    }

    [Fact]
    public void Commissioning_a_null_serial_is_refused() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Commission(null!));
    }

    [Fact]
    public void Commissioning_a_serial_twice_is_refused_and_registers_nothing_more() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(new SerialNumber(anySerial.Value)));

        // Assert
        Assert.Contains("already commissioned", refusal.Message);
        Assert.Single(anyBatch.Packs);
    }

    [Fact]
    public void Serials_differing_only_by_case_are_two_serials() {
        // Arrange
        Batch anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();

        // Act
        anyBatch.Commission(new SerialNumber("ABCDEFGH"));
        anyBatch.Commission(new SerialNumber("abcdefgh"));

        // Assert
        Assert.Equal(2, anyBatch.Packs.Count);
    }

    [Fact]
    public void A_full_batch_refuses_one_more_serial() {
        // Arrange
        Batch                     anyBatch = new AnyBatch().WithPlannedQuantity(q => q.LessThanOrEqualTo(20)).Generate();
        IReadOnlyList<SerialNumber> serials  = Fixtures.DistinctSerials(anyBatch.PlannedQuantity + 1).Generate();

        foreach (SerialNumber serial in serials.Take(anyBatch.PlannedQuantity)) {
            anyBatch.Commission(serial);
        }

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(serials[anyBatch.PlannedQuantity]));

        // Assert
        Assert.Contains("full", refusal.Message);
        Assert.Equal(anyBatch.PlannedQuantity, anyBatch.Packs.Count);
    }

    [Fact]
    public void A_batch_planned_for_one_pack_holds_exactly_one() {
        // Arrange
        Batch                       batch   = new AnyBatch().WithPlannedQuantity(1).Generate();
        IReadOnlyList<SerialNumber> serials = Fixtures.DistinctSerials(2).Generate();

        // Act
        batch.Commission(serials[0]);

        // Assert
        Assert.Throws<InvalidOperationException>(() => batch.Commission(serials[1]));
    }

    [Fact]
    public void A_duplicate_in_a_full_batch_is_reported_as_a_duplicate() {
        // Arrange
        Batch        batch  = new AnyBatch().WithPlannedQuantity(1).Generate();
        SerialNumber serial = new AnySerialNumber().Generate();

        batch.Commission(serial);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => batch.Commission(serial));

        // Assert
        Assert.Contains("already commissioned", refusal.Message);
    }

    [Fact]
    public void Commissioning_all_registers_every_serial_in_order() {
        // Arrange
        Batch                       anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        IReadOnlyList<SerialNumber> serials  = Fixtures.DistinctSerials(5).Generate();

        // Act
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(serials);

        // Assert
        Assert.Equal(serials, packs.Select(pack => pack.Serial));
        Assert.Equal(5, anyBatch.Packs.Count);
        Assert.All(packs, pack => Assert.Equal(PackStatus.Active, pack.Status));
    }

    [Fact]
    public void Commissioning_nothing_registers_nothing() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(Array.Empty<SerialNumber>());

        // Assert
        Assert.Empty(packs);
        Assert.Empty(anyBatch.Packs);
    }

    [Fact]
    public void Commissioning_all_without_a_list_is_refused() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.CommissionAll(null!));
    }

    [Fact]
    public void A_failure_in_the_middle_leaves_the_packs_before_it_registered() {
        // Arrange
        Batch                       anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        IReadOnlyList<SerialNumber> serials  = Fixtures.DistinctSerials(3).Generate();

        SerialNumber[] withARepeat = [serials[0], serials[1], serials[0], serials[2]];

        // Act
        Assert.Throws<InvalidOperationException>(() => anyBatch.CommissionAll(withARepeat));

        // Assert
        Assert.Equal(new[] { serials[0], serials[1] }, anyBatch.Packs.Select(pack => pack.Serial));
    }

    [Fact]
    public void A_batch_too_small_for_the_list_keeps_the_packs_that_fit() {
        // Arrange
        Batch                       batch   = new AnyBatch().WithPlannedQuantity(2).Generate();
        IReadOnlyList<SerialNumber> serials = Fixtures.DistinctSerials(4).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => batch.CommissionAll(serials));

        // Assert
        Assert.Equal(2, batch.Packs.Count);
    }

    // ---- verification -------------------------------------------------------------------------------------

    [Fact]
    public void An_unknown_serial_is_reported_unknown() {
        // Arrange
        Batch        anyBatch      = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anyStranger   = new AnySerialNumber().Generate();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anyStranger, new DateOnly(2024, 6, 1));

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void A_commissioned_pack_before_its_expiry_is_authentic() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, new DateOnly(2024, 6, 1));

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_pack_is_still_authentic_on_the_day_it_expires() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, Expires);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_pack_is_expired_the_day_after_it_expires() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, Expires.AddDays(1));

        // Assert
        Assert.Equal(VerificationOutcome.Expired, outcome);
    }

    [Fact]
    public void A_pack_is_authentic_whatever_the_day_before_its_expiry() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        DateOnly     anyDayBeforeOrOnExpiry = Any.DateOnly().BeforeOrEqualTo(Expires).Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyDayBeforeOrOnExpiry);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_pack_is_expired_whatever_the_day_after_its_expiry() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        DateOnly     anyLaterDay = Any.DateOnly().After(Expires).Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyLaterDay);

        // Assert
        Assert.Equal(VerificationOutcome.Expired, outcome);
    }

    [Fact]
    public void Verifying_an_expired_pack_leaves_it_active() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Act
        anyBatch.Verify(anySerial, Expires.AddDays(30));

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
        Assert.Equal(VerificationOutcome.Expired, anyBatch.Verify(anySerial, Expires.AddDays(30)));
    }

    [Fact]
    public void A_dispensed_pack_is_already_decommissioned() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, new DateOnly(2024, 6, 1));

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, new DateOnly(2024, 6, 2));

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void A_recalled_pack_is_already_decommissioned() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);
        anyBatch.Recall();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, new DateOnly(2024, 6, 1));

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void A_decommissioned_pack_past_its_expiry_is_reported_decommissioned_rather_than_expired() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, new DateOnly(2024, 6, 1));

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, Expires.AddDays(10));

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void Verifying_a_null_serial_is_refused() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Verify(null!, new DateOnly(2024, 6, 1)));
    }

    [Fact]
    public void A_pack_checked_before_its_batch_was_made_is_authentic() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, Made.AddYears(-1));

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    // ---- dispensing ---------------------------------------------------------------------------------------

    [Fact]
    public void Dispensing_an_authentic_pack_marks_it_dispensed() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Act
        anyBatch.Dispense(anySerial, Expires);

        // Assert
        Assert.Equal(PackStatus.Dispensed, pack.Status);
    }

    [Fact]
    public void A_pack_cannot_be_dispensed_twice() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, new DateOnly(2024, 6, 1));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, new DateOnly(2024, 6, 2)));

        // Assert
        Assert.Contains(nameof(VerificationOutcome.AlreadyDecommissioned), refusal.Message);
    }

    [Fact]
    public void An_unknown_serial_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch    = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anyStranger = new AnySerialNumber().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anyStranger, new DateOnly(2024, 6, 1)));

        // Assert
        Assert.Contains(nameof(VerificationOutcome.UnknownSerial), refusal.Message);
    }

    [Fact]
    public void An_expired_pack_cannot_be_dispensed_and_stays_active() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, Expires.AddDays(1)));

        // Assert
        Assert.Contains(nameof(VerificationOutcome.Expired), refusal.Message);
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void A_recalled_pack_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        SerialisedPack pack = anyBatch.Commission(anySerial);
        anyBatch.Recall();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, new DateOnly(2024, 6, 1)));
        Assert.Equal(PackStatus.Recalled, pack.Status);
    }

    [Fact]
    public void Dispensing_one_pack_leaves_the_others_active() {
        // Arrange
        Batch                       anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        IReadOnlyList<SerialNumber> serials  = Fixtures.DistinctSerials(3).Generate();

        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(serials);

        // Act
        anyBatch.Dispense(serials[1], new DateOnly(2024, 6, 1));

        // Assert
        Assert.Equal(PackStatus.Active, packs[0].Status);
        Assert.Equal(PackStatus.Dispensed, packs[1].Status);
        Assert.Equal(PackStatus.Active, packs[2].Status);
    }

    // ---- recall -------------------------------------------------------------------------------------------

    [Fact]
    public void Recalling_marks_every_active_pack_recalled_and_counts_them() {
        // Arrange
        Batch                         anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        IReadOnlyList<SerialisedPack> packs    = anyBatch.CommissionAll(Fixtures.DistinctSerials(4).Generate());

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(4, recalled);
        Assert.All(packs, pack => Assert.Equal(PackStatus.Recalled, pack.Status));
    }

    [Fact]
    public void Recalling_leaves_dispensed_packs_untouched() {
        // Arrange
        Batch                         anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        IReadOnlyList<SerialNumber>   serials  = Fixtures.DistinctSerials(3).Generate();
        IReadOnlyList<SerialisedPack> packs    = anyBatch.CommissionAll(serials);

        anyBatch.Dispense(serials[0], new DateOnly(2024, 6, 1));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(2, recalled);
        Assert.Equal(PackStatus.Dispensed, packs[0].Status);
        Assert.Equal(PackStatus.Recalled, packs[1].Status);
        Assert.Equal(PackStatus.Recalled, packs[2].Status);
    }

    [Fact]
    public void Recalling_an_empty_batch_recalls_nothing() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(0, recalled);
    }

    [Fact]
    public void Recalling_twice_recalls_nothing_the_second_time() {
        // Arrange
        Batch anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();

        anyBatch.CommissionAll(Fixtures.DistinctSerials(3).Generate());
        anyBatch.Recall();

        // Act
        int recalledAgain = anyBatch.Recall();

        // Assert
        Assert.Equal(0, recalledAgain);
    }

    [Fact]
    public void Recalling_includes_packs_that_are_expired_but_still_active() {
        // Arrange
        Batch          anyBatch  = Fixtures.BatchLiving(Made, Expires).Generate();
        SerialNumber   anySerial = new AnySerialNumber().Generate();
        SerialisedPack pack      = anyBatch.Commission(anySerial);

        Assert.Equal(VerificationOutcome.Expired, anyBatch.Verify(anySerial, Expires.AddYears(1)));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(1, recalled);
        Assert.Equal(PackStatus.Recalled, pack.Status);
    }

    // ---- remaining shelf life -----------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-01", 1.00)]
    [InlineData("2023-06-15", 1.00)]
    [InlineData("2025-01-01", 0.00)]
    [InlineData("2026-03-01", 0.00)]
    [InlineData("2024-07-02", 0.50)]
    [InlineData("2024-12-31", 0.00)]
    public void The_remaining_ratio_is_one_before_manufacture_and_zero_from_expiry_on(string todayText, double expected) {
        // Arrange
        Batch    anyBatch = Fixtures.BatchLiving(Made, Expires).Generate();
        DateOnly today    = DateOnly.Parse(todayText, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        decimal ratio = anyBatch.RemainingShelfLifeRatio(today);

        // Assert
        Assert.Equal((decimal)expected, ratio);
    }

    [Theory]
    [InlineData("2024-01-02", 0.88)]   // 7 of 8 days = 0.875
    [InlineData("2024-01-06", 0.38)]   // 3 of 8 days = 0.375
    [InlineData("2024-01-08", 0.13)]   // 1 of 8 days = 0.125
    [InlineData("2024-01-04", 0.63)]   // 5 of 8 days = 0.625
    public void Midpoints_round_away_from_zero(string todayText, double expected) {
        // Arrange
        Batch    eightDayBatch = Fixtures.BatchLiving(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 9)).Generate();
        DateOnly today         = DateOnly.Parse(todayText, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        decimal ratio = eightDayBatch.RemainingShelfLifeRatio(today);

        // Assert
        Assert.Equal((decimal)expected, ratio);
    }

    [Fact]
    public void The_remaining_ratio_of_a_drawn_day_stays_between_zero_and_one() {
        // Arrange
        Batch    anyBatch = new AnyBatch().Generate();
        DateOnly anyDay   = Any.DateOnly().Generate();

        // Act
        decimal ratio = anyBatch.RemainingShelfLifeRatio(anyDay);

        // Assert
        Assert.InRange(ratio, 0m, 1m);
        Assert.Equal(decimal.Round(ratio, 2), ratio);
    }

    // ---- stock value --------------------------------------------------------------------------------------

    [Fact]
    public void An_empty_batch_has_no_stock_value() {
        // Arrange
        Batch anyBatch = new AnyBatch().Generate();

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(0m, value);
    }

    [Fact]
    public void Stock_value_is_the_price_times_the_active_packs() {
        // Arrange
        Batch                         batch   = Fixtures.BatchLiving(Made, Expires).WithPrice(new ReimbursementPrice(12.34m)).Generate();
        IReadOnlyList<SerialNumber>   serials = Fixtures.DistinctSerials(3).Generate();

        batch.CommissionAll(serials);

        // Act
        decimal value = batch.ActiveStockValue();

        // Assert
        Assert.Equal(37.02m, value);
    }

    [Fact]
    public void Dispensed_and_recalled_packs_leave_the_stock_value() {
        // Arrange
        Batch                       batch   = Fixtures.BatchLiving(Made, Expires).WithPrice(new ReimbursementPrice(12.34m)).Generate();
        IReadOnlyList<SerialNumber> serials = Fixtures.DistinctSerials(3).Generate();

        batch.CommissionAll(serials);
        batch.Dispense(serials[0], new DateOnly(2024, 6, 1));

        // Act
        decimal afterDispense = batch.ActiveStockValue();
        batch.Recall();
        decimal afterRecall = batch.ActiveStockValue();

        // Assert
        Assert.Equal(24.68m, afterDispense);
        Assert.Equal(0m, afterRecall);
    }

}
