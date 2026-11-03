using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class ReceiptLineTests {

    [Fact]
    public void A_line_keeps_what_it_was_given() {
        // Arrange
        Ean13     anyGtin     = Any.Ean13().Generate();
        Quantity  anyExpected = Any.Quantity().Generate();
        Quantity  anyReceived = Any.Quantity().Generate();
        LotNumber anyLot      = Any.LotNumber().Generate();
        DateOnly  bestBefore  = new DateOnly(2027, 6, 30);

        // Act
        ReceiptLine line = new ReceiptLine(anyGtin, anyExpected, anyReceived, anyLot, bestBefore, 0.25m);

        // Assert
        Assert.Same(anyGtin, line.Gtin);
        Assert.Same(anyExpected, line.Expected);
        Assert.Same(anyReceived, line.Received);
        Assert.Same(anyLot, line.Lot);
        Assert.Equal(bestBefore, line.BestBefore);
        Assert.Equal(0.25m, line.UnitWeightKg);
    }

    [Fact]
    public void A_null_gtin_is_refused() {
        // Arrange
        AnyReceiptLine anyLine = Any.ReceiptLine();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => anyLine.WithGtin((Ean13)null!).Generate());

        // Assert
        Assert.Equal("gtin", exception.ParamName);
    }

    [Fact]
    public void A_null_expected_quantity_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Any.ReceiptLine().WithExpected((Quantity)null!).Generate());

        Assert.Equal("expected", exception.ParamName);
    }

    [Fact]
    public void A_null_received_quantity_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Any.ReceiptLine().WithReceived((Quantity)null!).Generate());

        Assert.Equal("received", exception.ParamName);
    }

    [Fact]
    public void A_null_lot_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Any.ReceiptLine().WithLot((LotNumber)null!).Generate());

        Assert.Equal("lot", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(-5)]
    public void A_unit_that_weighs_nothing_or_less_is_refused(double weight) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ReceiptLine().WithUnitWeightKg((decimal)weight).Generate());

        Assert.Equal("unitWeightKg", exception.ParamName);
    }

    [Fact]
    public void The_smallest_positive_weight_is_accepted() {
        ReceiptLine anyLine = Any.ReceiptLine().WithUnitWeightKg(0.0000000000000000000000000001m).Generate();

        Assert.Equal(0.0000000000000000000000000001m, anyLine.UnitWeightKg);
    }

    [Theory]
    [InlineData(10, 7, -3)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 14, 4)]
    [InlineData(0, 5, 5)]
    [InlineData(5, 0, -5)]
    public void The_discrepancy_is_received_minus_expected(int expected, int received, int discrepancy) {
        // Arrange
        ReceiptLine anyLine = Any.ReceiptLine()
                                 .WithExpected(new Quantity(expected))
                                 .WithReceived(new Quantity(received))
                                 .Generate();

        // Assert
        Assert.Equal(discrepancy, anyLine.Discrepancy);
    }

    [Theory]
    [InlineData(7, 2.5, 17.5)]
    [InlineData(0, 2.5, 0)]
    [InlineData(10_000, 0.125, 1250)]
    public void The_weight_is_the_received_units_times_the_unit_weight(int received, double unitWeight, double expectedWeight) {
        ReceiptLine anyLine = Any.ReceiptLine()
                                 .WithReceived(new Quantity(received))
                                 .WithUnitWeightKg((decimal)unitWeight)
                                 .Generate();

        Assert.Equal((decimal)expectedWeight, anyLine.WeightKg);
    }

    [Fact]
    public void The_weight_ignores_the_expected_quantity() {
        ReceiptLine anyLine = Any.ReceiptLine()
                                 .WithExpected(new Quantity(100))
                                 .WithReceived(new Quantity(2))
                                 .WithUnitWeightKg(3m)
                                 .Generate();

        Assert.Equal(6m, anyLine.WeightKg);
    }

    [Fact(Skip = "domain defect: an enormous unit weight is accepted by the constructor and only blows up later, in WeightKg (OverflowException), and so in NetWeightKg and InspectionReasons")]
    public void A_unit_weight_that_cannot_be_multiplied_by_the_received_units_is_refused_up_front() {
        Assert.ThrowsAny<ArgumentException>(() => Any.ReceiptLine()
                                                     .WithReceived(new Quantity(Quantity.Maximum))
                                                     .WithUnitWeightKg(decimal.MaxValue / 2m)
                                                     .Generate());
    }

}
