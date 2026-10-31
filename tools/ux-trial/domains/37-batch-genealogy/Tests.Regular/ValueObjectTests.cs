using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace GenealogyTests;

public sealed class LotNumberTests {

    [Fact]
    public void A_sub_lot_number_keeps_the_base_lot_and_carries_a_two_digit_index() {
        // Arrange
        LotNumber lot = new("API-240117");

        // Act
        LotNumber subLot = lot.SubLot(7);

        // Assert
        Assert.Equal("API-240117.07", subLot.Value);
        Assert.Equal("API-240117", subLot.BaseLot);
        Assert.Equal(7, subLot.SubLotIndex);
    }

    [Fact]
    public void Sub_lot_zero_does_not_exist() {
        // Arrange
        const string subLotZero = "API-240117.00";

        // Act
        Exception? error = Record.Exception(() => new LotNumber(subLotZero));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}

public sealed class QuantityTests {

    [Fact]
    public void One_kilogram_equals_a_thousand_grams() {
        // Arrange
        Quantity oneKilogram = new(1m, QuantityUnit.Kilogram);

        // Act
        bool equal = oneKilogram.Equals(new Quantity(1000m, QuantityUnit.Gram));

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void Mass_and_volume_do_not_add_up() {
        // Arrange
        Quantity anyMass   = Arbitrary.Quantity().WithUnit(QuantityUnit.Kilogram).Generate();
        Quantity anyVolume = Arbitrary.Quantity().WithUnit(QuantityUnit.Litre).Generate();

        // Act
        Exception? error = Record.Exception(() => anyMass.Add(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Subtracting_converts_the_subtrahend_into_the_unit_of_the_minuend() {
        // Arrange
        Quantity twoKilograms = new(2m, QuantityUnit.Kilogram);

        // Act
        Quantity remaining = twoKilograms.Subtract(new Quantity(500m, QuantityUnit.Gram));

        // Assert
        Assert.Equal(1.5m, remaining.Amount);
        Assert.Equal(QuantityUnit.Kilogram, remaining.Unit);
    }

    [Fact]
    public void A_quantity_cannot_become_negative() {
        // Arrange
        Quantity oneKilogram = new(1m, QuantityUnit.Kilogram);

        // Act
        Exception? error = Record.Exception(() => oneKilogram.Subtract(new Quantity(1001m, QuantityUnit.Gram)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}
