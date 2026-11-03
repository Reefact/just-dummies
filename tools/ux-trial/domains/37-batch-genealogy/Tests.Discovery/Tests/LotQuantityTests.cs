using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class LotQuantityTests {

    [Fact]
    public void A_lot_quantity_pairs_a_lot_with_a_positive_quantity() {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyQuantity = Any.Quantity().WithAmount(a => a.Positive()).Generate();

        // Act
        LotQuantity part = new LotQuantity(anyLot, anyQuantity);

        // Assert
        Assert.Same(anyLot, part.Lot);
        Assert.Same(anyQuantity, part.Quantity);
    }

    [Fact]
    public void A_zero_quantity_is_refused_in_whatever_unit_it_is_expressed() {
        // Arrange
        LotNumber anyLot  = Any.LotNumber().Generate();
        Quantity  anyZero = Any.Quantity().WithAmount(0m).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new LotQuantity(anyLot, anyZero));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void The_smallest_positive_amount_is_accepted() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        LotQuantity part = new LotQuantity(anyLot, new Quantity(0.000001m, QuantityUnit.Milligram));

        // Assert
        Assert.False(part.Quantity.IsZero);
    }

    [Fact]
    public void A_missing_lot_or_quantity_is_refused() {
        // Arrange
        LotNumber anyLot      = Any.LotNumber().Generate();
        Quantity  anyQuantity = Any.Quantity().WithAmount(a => a.Positive()).Generate();

        // Act
        Exception? noLot      = Record.Exception(() => new LotQuantity(null!, anyQuantity));
        Exception? noQuantity = Record.Exception(() => new LotQuantity(anyLot, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(noLot);
        Assert.IsType<ArgumentNullException>(noQuantity);
    }

    [Fact]
    public void A_lot_quantity_reads_as_the_quantity_of_the_lot() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        string text = new LotQuantity(anyLot, new Quantity(250m, QuantityUnit.Gram)).ToString();

        // Assert
        Assert.Equal($"250 g of {anyLot.Value}", text);
    }

}
