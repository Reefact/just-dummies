using JustDummies;
using JustDummies.Xunit;
using Serialisation.Domain;
using Xunit;

namespace Serialisation.Tests;

[Reproducible]
public sealed class BatchTests {

    private static List<SerialNumber> AnyDistinctSerials(int count) {
        return Any.ListOf(Arbitrary.SerialNumber()).WithCount(count).Distinct().Generate();
    }

    private static DateOnly AnyDayWithinShelfLife(Batch batch) {
        return Any.DateOnly().Between(batch.ManufacturedOn, batch.ExpiresOn).Generate();
    }

    [Fact]
    public void A_commissioned_pack_verifies_as_authentic_within_its_shelf_life() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        DateOnly anyToday = AnyDayWithinShelfLife(anyBatch);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyToday);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void Commissioning_a_serial_twice_in_one_batch_is_refused() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().WithPlannedQuantity(q => q.GreaterThanOrEqualTo(2)).Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        Action commissionAgain = () => anyBatch.Commission(anySerial);

        // Assert
        Assert.Throws<InvalidOperationException>(commissionAgain);
    }

    [Fact]
    public void A_batch_refuses_more_packs_than_planned() {
        // Arrange
        Batch              anyBatchOfTwo = Arbitrary.Batch().WithPlannedQuantity(2).Generate();
        List<SerialNumber> anySerials    = AnyDistinctSerials(3);
        anyBatchOfTwo.Commission(anySerials[0]);
        anyBatchOfTwo.Commission(anySerials[1]);

        // Act
        Action commissionAThird = () => anyBatchOfTwo.Commission(anySerials[2]);

        // Assert
        Assert.Throws<InvalidOperationException>(commissionAThird);
    }

    [Fact]
    public void A_serial_never_commissioned_in_the_batch_is_unknown() {
        // Arrange
        Batch              anyBatch   = Arbitrary.Batch().Generate();
        List<SerialNumber> anySerials = AnyDistinctSerials(2);
        anyBatch.Commission(anySerials[0]);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerials[1], AnyDayWithinShelfLife(anyBatch));

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void A_pack_is_still_authentic_on_its_expiry_date() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyBatch.ExpiresOn);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_pack_verified_after_its_expiry_date_is_expired() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        DateOnly anyDayAfterExpiry = Any.DateOnly().After(anyBatch.ExpiresOn).BeforeOrEqualTo(anyBatch.ExpiresOn.AddYears(2)).Generate();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyDayAfterExpiry);

        // Assert
        Assert.Equal(VerificationOutcome.Expired, outcome);
    }

    [Fact]
    public void A_dispensed_pack_cannot_be_dispensed_again() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        DateOnly anyToday = AnyDayWithinShelfLife(anyBatch);
        anyBatch.Dispense(anySerial, anyToday);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, anyToday);

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, anyToday));
    }

    [Fact]
    public void An_expired_pack_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch  = Arbitrary.Batch().Generate();
        SerialNumber anySerial = Arbitrary.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        Action dispenseTheDayAfterExpiry = () => anyBatch.Dispense(anySerial, anyBatch.ExpiresOn.AddDays(1));

        // Assert
        Assert.Throws<InvalidOperationException>(dispenseTheDayAfterExpiry);
    }

    [Fact]
    public void A_recall_decommissions_only_the_packs_still_active() {
        // Arrange
        Batch              anyBatch   = Arbitrary.Batch().WithPlannedQuantity(q => q.GreaterThanOrEqualTo(3)).Generate();
        List<SerialNumber> anySerials = AnyDistinctSerials(3);
        anyBatch.CommissionAll(anySerials);
        anyBatch.Dispense(anySerials[0], AnyDayWithinShelfLife(anyBatch));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(2, recalled);
        Assert.Equal(1, anyBatch.Packs.Count(pack => pack.Status == PackStatus.Dispensed));
    }

    [Fact]
    public void Half_the_shelf_life_left_reads_as_one_half() {
        // Arrange
        Batch anyTenDayBatch = Arbitrary.Batch()
                                        .WithManufacturedOn(new DateOnly(2025, 1, 1))
                                        .WithExpiresOn(new DateOnly(2025, 1, 11))
                                        .Generate();

        // Act
        decimal ratio = anyTenDayBatch.RemainingShelfLifeRatio(new DateOnly(2025, 1, 6));

        // Assert
        Assert.Equal(0.5m, ratio);
    }

    [Fact]
    public void The_active_stock_value_counts_only_active_packs_at_the_unit_price() {
        // Arrange
        Batch anyBatch = Arbitrary.Batch()
                                  .WithPrice(new ReimbursementPrice(12.50m))
                                  .WithPlannedQuantity(q => q.GreaterThanOrEqualTo(4))
                                  .Generate();
        List<SerialNumber> anySerials = AnyDistinctSerials(4);
        anyBatch.CommissionAll(anySerials);
        anyBatch.Dispense(anySerials[0], AnyDayWithinShelfLife(anyBatch));

        // Act
        decimal value = anyBatch.ActiveStockValue();

        // Assert
        Assert.Equal(37.50m, value);
    }

    [Fact]
    public void A_batch_cannot_expire_on_the_day_it_is_made() {
        // Arrange
        DateOnly sameDay = new DateOnly(2025, 3, 14);

        // Act
        Action build = () => Arbitrary.Batch().WithManufacturedOn(sameDay).WithExpiresOn(sameDay).Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

}
