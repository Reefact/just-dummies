using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class PalletTypeTests {

    // The numbers are the carrier's EDI codes: they are the contract, so they are spelled out.

    [Theory]
    [InlineData(PalletType.Euro, 1)]
    [InlineData(PalletType.Industrial, 2)]
    [InlineData(PalletType.HalfEuro, 6)]
    public void A_pallet_type_is_numbered_as_the_carriers_edi_messages_number_it(PalletType type, int ediCode) {
        Assert.Equal(ediCode, (int)type);
    }

    [Fact]
    public void There_are_exactly_three_pallet_types() {
        // Act
        PalletType[] types = Enum.GetValues<PalletType>();

        // Assert
        Assert.Equal(3, types.Length);
    }

}
