using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchCommissioningTests {

    [Fact]
    public void A_commissioned_pack_carries_the_batch_gtin_number_and_expiry_and_the_serial() {
        // Arrange
        Gtin         gtin      = new Gtin("04006381333931");
        BatchNumber  number    = new BatchNumber("LOT-42");
        DateOnly     expiresOn = new DateOnly(2027, 3, 31);
        SerialNumber serial    = new SerialNumber("SERIAL01");

        Batch batch = Any.Batch().WithGtin(gtin).WithNumber(number).WithExpiresOn(expiresOn).Generate();

        // Act
        SerialisedPack pack = batch.Commission(serial);

        // Assert
        Assert.Same(gtin, pack.Gtin);
        Assert.Same(number, pack.Batch);
        Assert.Equal(expiresOn, pack.ExpiresOn);
        Assert.Same(serial, pack.Serial);
    }

    [Fact]
    public void A_commissioned_pack_is_active() {
        // Arrange
        Batch        anyBatch  = Any.Batch().Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void A_commissioned_pack_is_registered_in_the_batch() {
        // Arrange
        Batch        anyBatch  = Any.Batch().Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        SerialisedPack pack = anyBatch.Commission(anySerial);

        // Assert
        Assert.Same(pack, Assert.Single(anyBatch.Packs));
    }

    [Fact]
    public void Several_serials_commissioned_one_after_the_other_are_all_registered() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(3).Generate();

        // Act
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        anyBatch.Commission(new SerialNumber("SERIAL03"));

        // Assert
        Assert.Equal(3, anyBatch.Packs.Count);
    }

    [Fact]
    public void A_null_serial_cannot_be_commissioned() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.Commission(null!));
    }

    [Fact]
    public void A_serial_already_commissioned_cannot_be_commissioned_again() {
        // Arrange
        Batch        anyBatch  = Any.Batch().WithRoomFor(2).Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();
        anyBatch.Commission(anySerial);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(new SerialNumber(anySerial.Value)));

        // Assert
        Assert.Contains("already commissioned", exception.Message);
        Assert.Single(anyBatch.Packs);
    }

    [Fact]
    public void A_serial_is_a_duplicate_by_value_not_by_instance() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(2).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(new SerialNumber("SERIAL01")));
    }

    [Fact]
    public void Serials_differing_only_by_case_are_not_duplicates() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(2).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));

        // Act
        anyBatch.Commission(new SerialNumber("serial01"));

        // Assert
        Assert.Equal(2, anyBatch.Packs.Count);
    }

    [Fact]
    public void A_full_batch_cannot_commission_another_serial() {
        // Arrange
        Batch anyFullBatch = Any.Batch().WithPlannedQuantity(2).Generate();
        anyFullBatch.Commission(new SerialNumber("SERIAL01"));
        anyFullBatch.Commission(new SerialNumber("SERIAL02"));

        // Act
        Assert.Throws<InvalidOperationException>(() => anyFullBatch.Commission(new SerialNumber("SERIAL03")));

        // Assert
        Assert.Equal(2, anyFullBatch.Packs.Count);
    }

    [Fact]
    public void A_batch_planned_for_one_pack_takes_exactly_one() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPlannedQuantity(1).Generate();

        // Act
        SerialisedPack pack = anyBatch.Commission(new SerialNumber("SERIAL01"));

        // Assert
        Assert.Same(pack, Assert.Single(anyBatch.Packs));
        Assert.Throws<InvalidOperationException>(() => anyBatch.Commission(new SerialNumber("SERIAL02")));
    }

    [Fact]
    public void A_duplicate_is_reported_before_a_full_batch() {
        // Arrange
        Batch anyFullBatch = Any.Batch().WithPlannedQuantity(1).Generate();
        anyFullBatch.Commission(new SerialNumber("SERIAL01"));

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyFullBatch.Commission(new SerialNumber("SERIAL01")));

        // Assert
        Assert.Contains("already commissioned", exception.Message);
    }

    [Fact]
    public void Commissioning_all_returns_the_packs_in_the_order_of_the_serials() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(3).Generate();
        List<SerialNumber> serials = new List<SerialNumber> {
            new SerialNumber("SERIAL03"), new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02")
        };

        // Act
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(serials);

        // Assert
        Assert.Equal(serials, packs.Select(pack => pack.Serial));
        Assert.Equal(3, anyBatch.Packs.Count);
    }

    [Fact]
    public void Commissioning_no_serial_at_all_returns_no_pack() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act
        IReadOnlyList<SerialisedPack> packs = anyBatch.CommissionAll(new List<SerialNumber>());

        // Assert
        Assert.Empty(packs);
        Assert.Empty(anyBatch.Packs);
    }

    [Fact]
    public void A_null_list_cannot_be_commissioned() {
        // Arrange
        Batch anyBatch = Any.Batch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyBatch.CommissionAll(null!));
    }

    [Fact]
    public void The_first_failure_stops_the_commissioning_and_keeps_the_packs_before_it() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(4).Generate();
        List<SerialNumber> serials = new List<SerialNumber> {
            new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02"), new SerialNumber("SERIAL01"), new SerialNumber("SERIAL03")
        };

        // Act
        Assert.Throws<InvalidOperationException>(() => anyBatch.CommissionAll(serials));

        // Assert
        Assert.Equal(new[] { "SERIAL01", "SERIAL02" }, anyBatch.Packs.Select(pack => pack.Serial.Value).OrderBy(value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void A_batch_that_fills_up_midway_keeps_the_packs_it_took() {
        // Arrange
        Batch anyBatch = Any.Batch().WithPlannedQuantity(2).Generate();
        List<SerialNumber> serials = new List<SerialNumber> {
            new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02"), new SerialNumber("SERIAL03")
        };

        // Act
        Assert.Throws<InvalidOperationException>(() => anyBatch.CommissionAll(serials));

        // Assert
        Assert.Equal(2, anyBatch.Packs.Count);
    }

    [Fact]
    public void A_null_serial_in_the_list_stops_the_commissioning_after_the_packs_before_it() {
        // Arrange
        Batch anyBatch = Any.Batch().WithRoomFor(3).Generate();
        List<SerialNumber> serials = new List<SerialNumber> { new SerialNumber("SERIAL01"), null!, new SerialNumber("SERIAL02") };

        // Act
        Assert.Throws<ArgumentNullException>(() => anyBatch.CommissionAll(serials));

        // Assert
        Assert.Single(anyBatch.Packs);
    }

}
