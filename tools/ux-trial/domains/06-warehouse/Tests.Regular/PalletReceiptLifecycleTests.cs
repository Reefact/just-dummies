namespace Warehouse.Domain.Tests.Regular;

public sealed class PalletReceiptLifecycleTests {

    [Fact]
    public void Quarantining_a_received_pallet_keeps_the_reason() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        string        anyReason  = Any.String().NotBlank().Generate();

        // Act
        anyReceipt.Quarantine(anyReason);

        // Assert
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal(anyReason, anyReceipt.QuarantineReason);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void Quarantining_again_replaces_the_reason() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        string        first      = Any.String().NotBlank().Generate();
        string        second     = Any.String().NotBlank().DifferentFrom(first).Generate();
        anyReceipt.Quarantine(first);

        // Act
        anyReceipt.Quarantine(second);

        // Assert
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal(second, anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_missing_reason_is_refused_and_leaves_the_pallet_untouched() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyReceipt.Quarantine(null!));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\r\n")]
    public void A_blank_reason_is_refused_and_leaves_the_pallet_untouched(string blank) {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyReceipt.Quarantine(blank));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_blank_reason_does_not_erase_the_reason_already_held() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        string        anyReason  = Any.String().NotBlank().Generate();
        anyReceipt.Quarantine(anyReason);

        // Act
        Assert.Throws<ArgumentException>(() => anyReceipt.Quarantine(" "));

        // Assert
        Assert.Equal(anyReason, anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_pallet_already_put_away_cannot_be_quarantined_from_the_dock() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        BinLocation   anyBin     = new AnyBinLocation().Generate();
        anyReceipt.PutAway(anyBin);

        // Act
        Assert.Throws<InvalidOperationException>(() => anyReceipt.Quarantine(Any.String().NotBlank().Generate()));

        // Assert
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
        Assert.Same(anyBin, anyReceipt.Bin);
    }

    [Fact]
    public void Putting_away_a_received_pallet_stores_it_in_the_bin() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        BinLocation   anyBin     = new AnyBinLocation().Generate();

        // Act
        anyReceipt.PutAway(anyBin);

        // Assert
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(anyBin, anyReceipt.Bin);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_missing_bin_is_refused_and_leaves_the_pallet_received() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyReceipt.PutAway(null!));

        // Assert
        Assert.Equal("bin", refusal.ParamName);
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_quarantined_pallet_cannot_be_put_away_and_the_message_gives_the_reason() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        string        anyReason  = Any.String().NotBlank().Generate();
        anyReceipt.Quarantine(anyReason);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyReceipt.PutAway(new AnyBinLocation().Generate()));

        // Assert
        Assert.Contains(anyReason, refusal.Message);
        Assert.Contains(anyReceipt.Sscc.Value, refusal.Message);
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_pallet_put_away_twice_is_refused_and_stays_in_its_first_bin() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        BinLocation   firstBin   = new AnyBinLocation().Generate();
        BinLocation   otherBin   = new AnyBinLocation().Generate();
        anyReceipt.PutAway(firstBin);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyReceipt.PutAway(otherBin));

        // Assert
        Assert.Contains(firstBin.ToString(), refusal.Message);
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(firstBin, anyReceipt.Bin);
    }

    [Fact]
    public void Nothing_leaves_quarantine() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        anyReceipt.Quarantine(Any.String().NotBlank().Generate());

        // Act
        Assert.Throws<InvalidOperationException>(() => anyReceipt.PutAway(new AnyBinLocation().Generate()));
        anyReceipt.Quarantine(Any.String().NotBlank().Generate());

        // Assert
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
    }

    [Fact]
    public void The_required_temperature_is_not_compared_with_the_bin() {
        // Arrange: a frozen pallet in a bin on the 20th level of an aisle is accepted, as the domain stands
        TemperatureRange frozen  = new TemperatureRange(-30m, -20m);
        PalletReceipt    receipt = new AnyPalletReceipt().WithRequiredTemperature(frozen).Generate();

        // Act
        receipt.PutAway(new AnyBinLocation().Generate());

        // Assert
        Assert.Equal(ReceiptStatus.PutAway, receipt.Status);
        Assert.Same(frozen, receipt.RequiredTemperature);
    }

}
