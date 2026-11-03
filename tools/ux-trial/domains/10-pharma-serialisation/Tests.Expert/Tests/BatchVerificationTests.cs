using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchVerificationTests {

    private static readonly DateOnly ExpiryDay = new DateOnly(2027, 6, 30);

    [Fact]
    public void A_commissioned_serial_is_authentic_before_the_expiry() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay.AddDays(-1));

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_commissioned_serial_is_still_authentic_on_the_expiry_day_itself() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, outcome);
    }

    [Fact]
    public void A_commissioned_serial_is_expired_the_day_after_the_expiry() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay.AddDays(1));

        // Assert
        Assert.Equal(VerificationOutcome.Expired, outcome);
    }

    [Fact]
    public void An_expired_pack_stays_active() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        SerialisedPack pack    = anyBatch.Commission(anySerial);

        // Act
        anyBatch.Verify(anySerial, ExpiryDay.AddDays(1));

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void A_serial_the_batch_never_commissioned_is_unknown() {
        // Arrange
        Batch        anyBatch       = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial      = Any.SerialNumber().Generate();
        SerialNumber anyOtherSerial = Any.SerialNumber().DifferentFrom(anySerial).Generate();
        anyBatch.Commission(anySerial);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anyOtherSerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void Any_serial_is_unknown_to_an_empty_batch() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void An_unknown_serial_stays_unknown_after_the_expiry() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay.AddYears(1));

        // Assert
        Assert.Equal(VerificationOutcome.UnknownSerial, outcome);
    }

    [Fact]
    public void A_dispensed_pack_is_already_decommissioned() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, ExpiryDay);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void A_recalled_pack_is_already_decommissioned() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Recall();

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void A_decommissioned_pack_is_reported_as_such_even_after_the_expiry() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, ExpiryDay);

        // Act
        VerificationOutcome outcome = anyBatch.Verify(anySerial, ExpiryDay.AddDays(1));

        // Assert
        Assert.Equal(VerificationOutcome.AlreadyDecommissioned, outcome);
    }

    [Fact]
    public void Verifying_a_serial_changes_nothing() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Verify(anySerial, ExpiryDay);

        // Act
        VerificationOutcome secondOutcome = anyBatch.Verify(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(VerificationOutcome.Authentic, secondOutcome);
    }

    [Fact]
    public void A_null_serial_cannot_be_verified() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Verify(null!, ExpiryDay));
    }

}
