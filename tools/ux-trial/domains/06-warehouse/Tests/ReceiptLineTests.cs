using JustDummies;
using JustDummies.Xunit;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

[Reproducible]
public sealed class ReceiptLineTests {

    [Fact]
    public void A_line_counted_three_units_under_its_notice_is_three_units_short() {
        // Arrange
        ReceiptLine anyShortLine = new AnyReceiptLine()
                                   .WithExpected(new Quantity(100))
                                   .WithReceived(new Quantity(97))
                                   .Generate();

        // Act
        int discrepancy = anyShortLine.Discrepancy;

        // Assert
        Assert.Equal(-3, discrepancy);
    }

    [Fact]
    public void A_line_weighs_its_received_units_times_the_unit_weight() {
        // Arrange
        ReceiptLine anyLine = new AnyReceiptLine()
                              .WithReceived(new Quantity(12))
                              .WithUnitWeightKg(2.5m)
                              .Generate();

        // Act
        decimal weightKg = anyLine.WeightKg;

        // Assert
        Assert.Equal(30m, weightKg);
    }

    [Fact]
    public void A_unit_weight_of_zero_is_refused() {
        // Arrange
        Ean13     anyGtin       = new AnyEan13().Generate();
        Quantity  anyExpected   = new AnyQuantity().Generate();
        Quantity  anyReceived   = new AnyQuantity().Generate();
        LotNumber anyLot        = new AnyLotNumber().Generate();
        DateOnly  anyBestBefore = Any.DateOnly().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReceiptLine(anyGtin, anyExpected, anyReceived, anyLot, anyBestBefore, unitWeightKg: 0m));

        // Assert
        Assert.Equal("unitWeightKg", refusal.ParamName);
    }

}
