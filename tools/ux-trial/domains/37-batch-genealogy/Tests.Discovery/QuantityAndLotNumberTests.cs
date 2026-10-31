using BatchGenealogy.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

[Reproducible]
public sealed class QuantityAndLotNumberTests {

    [Fact]
    public void One_kilogram_equals_a_thousand_grams() {
        // Arrange
        Quantity oneKilogram = new Quantity(1m, QuantityUnit.Kilogram);

        // Act
        Quantity inGrams = oneKilogram.ConvertTo(QuantityUnit.Gram);

        // Assert
        Assert.Equal(1000m, inGrams.Amount);
        Assert.Equal(new Quantity(1000m, QuantityUnit.Gram), oneKilogram);
    }

    [Fact]
    public void Mass_and_volume_do_not_add_up() {
        // Arrange
        Quantity anyMass   = Any.Quantity().WithUnit(QuantityUnit.Kilogram).Generate();
        Quantity anyVolume = Any.Quantity().WithUnit(QuantityUnit.Litre).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyMass.Add(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Subtracting_more_than_is_held_is_refused() {
        // Arrange
        Quantity held = new Quantity(2m, QuantityUnit.Kilogram);

        // Act
        Exception? refusal = Record.Exception(() => held.Subtract(new Quantity(2001m, QuantityUnit.Gram)));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_sub_lot_number_carries_its_base_lot_and_index() {
        // Arrange
        LotNumber lot = new LotNumber("API-240117");

        // Act
        LotNumber subLot = lot.SubLot(3);

        // Assert
        Assert.Equal("API-240117.03", subLot.Value);
        Assert.Equal("API-240117", subLot.BaseLot);
        Assert.True(subLot.IsSubLot);
    }

    [Fact]
    public void Sub_lot_index_00_is_not_a_sub_lot() {
        // Arrange
        const string subLotZero = "API-240117.00";

        // Act
        Exception? refusal = Record.Exception(() => new LotNumber(subLotZero));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
