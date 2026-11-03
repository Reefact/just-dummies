using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchDecommissioningTests {

    private static readonly DateOnly ExpiryDay = new DateOnly(2027, 6, 30);

    [Fact]
    public void Dispensing_an_authentic_pack_dispenses_it() {
        // Arrange
        Batch          anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber   anySerial = Any.SerialNumber().Generate();
        SerialisedPack pack      = anyBatch.Commission(anySerial);

        // Act
        anyBatch.Dispense(anySerial, ExpiryDay.AddDays(-1));

        // Assert
        Assert.Equal(PackStatus.Dispensed, pack.Status);
    }

    [Fact]
    public void A_pack_can_still_be_dispensed_on_its_expiry_day() {
        // Arrange
        Batch          anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber   anySerial = Any.SerialNumber().Generate();
        SerialisedPack pack      = anyBatch.Commission(anySerial);

        // Act
        anyBatch.Dispense(anySerial, ExpiryDay);

        // Assert
        Assert.Equal(PackStatus.Dispensed, pack.Status);
    }

    [Fact]
    public void An_expired_pack_cannot_be_dispensed_and_stays_active() {
        // Arrange
        Batch          anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber   anySerial = Any.SerialNumber().Generate();
        SerialisedPack pack      = anyBatch.Commission(anySerial);

        // Act
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, ExpiryDay.AddDays(1)));

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void An_unknown_serial_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch       = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial      = Any.SerialNumber().Generate();
        SerialNumber anyOtherSerial = Any.SerialNumber().DifferentFrom(anySerial).Generate();
        anyBatch.Commission(anySerial);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anyOtherSerial, ExpiryDay));
    }

    [Fact]
    public void A_pack_cannot_be_dispensed_twice() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Dispense(anySerial, ExpiryDay);

        // Act
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, ExpiryDay));
    }

    [Fact]
    public void A_recalled_pack_cannot_be_dispensed() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);
        anyBatch.Recall();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Dispense(anySerial, ExpiryDay));
    }

    [Fact]
    public void Dispensing_one_pack_leaves_the_others_active() {
        // Arrange
        Batch          anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).WithRoomFor(2).Generate();
        SerialisedPack dispensed = anyBatch.Commission(new SerialNumber("SERIAL01"));
        SerialisedPack other     = anyBatch.Commission(new SerialNumber("SERIAL02"));

        // Act
        anyBatch.Dispense(dispensed.Serial, ExpiryDay);

        // Assert
        Assert.Equal(PackStatus.Dispensed, dispensed.Status);
        Assert.Equal(PackStatus.Active, other.Status);
    }

    [Fact]
    public void A_null_serial_cannot_be_dispensed() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Dispense(null!, ExpiryDay));
    }

    [Fact]
    public void Recalling_a_batch_recalls_every_active_pack_and_counts_them() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(3).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Commission(new SerialNumber("SERIAL03"));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(3, recalled);
        Assert.All(anyBatch.Packs, pack => Assert.Equal(PackStatus.Recalled, pack.Status));
    }

    [Fact]
    public void Recalling_leaves_the_dispensed_packs_untouched() {
        // Arrange
        Batch          anyBatch  = Any.Batch().WithExpiresOn(ExpiryDay).WithRoomFor(2).Generate();
        SerialisedPack dispensed = anyBatch.Commission(new SerialNumber("SERIAL01"));
        SerialisedPack active    = anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Dispense(dispensed.Serial, ExpiryDay);

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(1, recalled);
        Assert.Equal(PackStatus.Dispensed, dispensed.Status);
        Assert.Equal(PackStatus.Recalled, active.Status);
    }

    [Fact]
    public void Recalling_reaches_the_packs_that_expired_but_are_still_active() {
        // Arrange
        Batch          anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        SerialisedPack expired  = anyBatch.Commission(Any.SerialNumber().Generate());
        anyBatch.Verify(expired.Serial, ExpiryDay.AddDays(1));

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(1, recalled);
        Assert.Equal(PackStatus.Recalled, expired.Status);
    }

    [Fact]
    public void Recalling_twice_recalls_nothing_the_second_time() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();
        anyBatch.Commission(Any.SerialNumber().Generate());
        anyBatch.Recall();

        // Act
        int recalledAgain = anyBatch.Recall();

        // Assert
        Assert.Equal(0, recalledAgain);
    }

    [Fact]
    public void Recalling_an_empty_batch_recalls_nothing() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act
        int recalled = anyBatch.Recall();

        // Assert
        Assert.Equal(0, recalled);
    }

    [Fact]
    public void Packs_commissioned_after_a_recall_are_active() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(2).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Recall();

        // Act
        SerialisedPack later = anyBatch.Commission(new SerialNumber("SERIAL02"));

        // Assert
        Assert.Equal(PackStatus.Active, later.Status);
        Assert.Equal(1, anyBatch.Recall());
    }

}
