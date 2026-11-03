using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class PalletReceiptLifecycleTests {

    private static readonly BinLocation HighBay = new BinLocation('B', 7, 12);

    [Fact]
    public void Quarantining_a_received_pallet_records_the_reason_and_the_new_status() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        anyReceipt.Quarantine("pallet wrap torn");

        // Assert
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal("pallet wrap torn", anyReceipt.QuarantineReason);
    }

    [Fact]
    public void Quarantining_again_replaces_the_reason_and_stays_quarantined() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("pallet wrap torn");

        // Act
        anyReceipt.Quarantine("temperature log missing");

        // Assert
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal("temperature log missing", anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_missing_quarantine_reason_is_refused_and_the_pallet_is_left_as_it_was() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.Quarantine(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_quarantine_reason_is_refused_and_the_pallet_is_left_as_it_was(string blank) {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.Quarantine(blank));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void Replacing_the_reason_of_a_quarantined_pallet_by_a_blank_one_is_refused_and_keeps_the_first_reason() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("pallet wrap torn");

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.Quarantine(" "));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("pallet wrap torn", anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_pallet_already_put_away_cannot_be_quarantined_and_keeps_its_bin() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.PutAway(HighBay);

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.Quarantine("pallet wrap torn"));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(HighBay, anyReceipt.Bin);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_blank_reason_is_reported_before_the_refusal_of_a_pallet_already_put_away() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.PutAway(HighBay);

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.Quarantine(""));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Putting_away_a_received_pallet_records_its_bin_and_the_new_status() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        anyReceipt.PutAway(HighBay);

        // Assert
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(HighBay, anyReceipt.Bin);
    }

    [Fact]
    public void A_missing_bin_is_refused_and_the_pallet_is_left_as_it_was() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => anyReceipt.PutAway(null!));

        // Assert
        Assert.Equal("bin", refused.ParamName);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_pallet_already_put_away_cannot_be_put_away_again_and_keeps_its_first_bin() {
        // Arrange
        BinLocation   anotherBin = new BinLocation('C', 1, 1);
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.PutAway(HighBay);

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.PutAway(anotherBin));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(HighBay, anyReceipt.Bin);
    }

    [Fact]
    public void A_quarantined_pallet_cannot_be_put_away_and_stays_quarantined_without_a_bin() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("pallet wrap torn");

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.PutAway(HighBay));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal("pallet wrap torn", anyReceipt.QuarantineReason);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_missing_bin_is_reported_before_the_state_check() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("pallet wrap torn");

        // Act
        Exception? thrown = Record.Exception(() => anyReceipt.PutAway(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void The_required_temperature_is_not_checked_against_the_bin_it_is_put_away_in() {
        // Arrange: a deep-frozen pallet in a high bay, with nothing in the model to say it does not belong there.
        PalletReceipt anyFrozenReceipt = Any.PalletReceipt().WithRequiredTemperature(new TemperatureRange(-25m, -18m)).Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyFrozenReceipt.PutAway(HighBay));

        // Assert
        Assert.Null(thrown);
        Assert.Equal(ReceiptStatus.PutAway, anyFrozenReceipt.Status);
    }

}
