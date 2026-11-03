namespace Warehouse.Domain.Tests.Regular;

public sealed class EnumTests {

    // The numbers are the contract: they are what a stored or exchanged pallet type is read back from.
    [Theory]
    [InlineData(PalletType.Euro, 1)]
    [InlineData(PalletType.Industrial, 2)]
    [InlineData(PalletType.HalfEuro, 6)]
    public void A_pallet_type_keeps_its_published_number(PalletType type, int number) {
        Assert.Equal(number, (int)type);
    }

    [Fact]
    public void There_are_exactly_three_pallet_types() {
        Assert.Equal([PalletType.Euro, PalletType.Industrial, PalletType.HalfEuro], Enum.GetValues<PalletType>());
    }

    [Fact]
    public void There_are_exactly_three_storage_zones() {
        Assert.Equal([StorageZone.Ambient, StorageZone.Chilled, StorageZone.Frozen], Enum.GetValues<StorageZone>());
    }

    [Fact]
    public void There_are_exactly_three_receipt_statuses() {
        Assert.Equal([ReceiptStatus.Received, ReceiptStatus.Quarantined, ReceiptStatus.PutAway], Enum.GetValues<ReceiptStatus>());
    }

    [Fact]
    public void Any_pallet_type_is_one_of_the_declared_ones() {
        // Act
        PalletType anyType = Any.Enum<PalletType>().Generate();

        // Assert
        Assert.True(Enum.IsDefined(anyType));
    }

}
