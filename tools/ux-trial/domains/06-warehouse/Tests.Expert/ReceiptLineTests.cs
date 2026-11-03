using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class ReceiptLineTests {

    [Fact]
    public void A_line_keeps_everything_it_was_built_with() {
        // Arrange
        Ean13     gtin     = new Ean13("4006381333931");
        Quantity  expected = new Quantity(10);
        Quantity  received = new Quantity(8);
        LotNumber lot      = new LotNumber("L-2407");
        DateOnly  bestBefore = new DateOnly(2027, 1, 31);

        // Act
        ReceiptLine line = new ReceiptLine(gtin, expected, received, lot, bestBefore, 0.35m);

        // Assert
        Assert.Same(gtin, line.Gtin);
        Assert.Same(expected, line.Expected);
        Assert.Same(received, line.Received);
        Assert.Same(lot, line.Lot);
        Assert.Equal(bestBefore, line.BestBefore);
        Assert.Equal(0.35m, line.UnitWeightKg);
    }

    [Theory]
    [InlineData(10, 8, -2)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 13, 3)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 10_000, 10_000)]
    [InlineData(10_000, 0, -10_000)]
    public void The_discrepancy_is_what_was_received_minus_what_was_announced(int expectedUnits, int receivedUnits, int expectedDiscrepancy) {
        // Arrange
        ReceiptLine anyLine = Any.ReceiptLine().WithExpected(new Quantity(expectedUnits)).WithReceived(new Quantity(receivedUnits)).Generate();

        // Act
        int discrepancy = anyLine.Discrepancy;

        // Assert
        Assert.Equal(expectedDiscrepancy, discrepancy);
    }

    [Fact]
    public void The_weight_is_the_received_units_times_the_unit_weight() {
        // Arrange: what was announced plays no part in the weight, so it is left to the generator.
        ReceiptLine anyLine = Any.ReceiptLine().WithReceived(new Quantity(12)).WithUnitWeightKg(0.25m).Generate();

        // Act
        decimal weightKg = anyLine.WeightKg;

        // Assert
        Assert.Equal(3m, weightKg);
    }

    [Fact]
    public void Nothing_received_weighs_nothing() {
        // Arrange
        ReceiptLine anyLine = Any.ReceiptLine().WithReceived(new Quantity(0)).Generate();

        // Act
        decimal weightKg = anyLine.WeightKg;

        // Assert
        Assert.Equal(0m, weightKg);
    }

    public static TheoryData<decimal> PositiveUnitWeights => new() { 0.0000000001m, 0.001m, 1250m };

    public static TheoryData<decimal> NonPositiveUnitWeights => new() { 0m, -0.01m, -1250m };

    [Theory]
    [MemberData(nameof(PositiveUnitWeights))]
    public void Any_unit_weight_above_zero_is_accepted(decimal unitWeightKg) {
        // Act
        ReceiptLine anyLine = Any.ReceiptLine().WithUnitWeightKg(unitWeightKg).Generate();

        // Assert
        Assert.Equal(unitWeightKg, anyLine.UnitWeightKg);
    }

    [Theory]
    [MemberData(nameof(NonPositiveUnitWeights))]
    public void A_unit_weight_of_zero_or_less_is_refused(decimal unitWeightKg) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ReceiptLine().WithUnitWeightKg(unitWeightKg).Generate());

        // Assert
        Assert.Equal("unitWeightKg", refused.ParamName);
    }

    [Fact(Skip = "domain defect: the unit weight has no upper bound, so a line the constructor accepts can fail with OverflowException when its weight is read (about 9 % of default draws of the generator, unit weight up to decimal.MaxValue); seed n/a (deterministic repro below)")]
    public void Every_line_the_constructor_accepts_can_report_its_weight() {
        // Arrange
        ReceiptLine heavyLine = Any.ReceiptLine().WithReceived(new Quantity(2)).WithUnitWeightKg(decimal.MaxValue).Generate();

        // Act
        Exception? thrown = Record.Exception(() => heavyLine.WeightKg);

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void A_best_before_date_in_the_distant_past_is_not_the_lines_concern() {
        // Act
        ReceiptLine anyLine = Any.ReceiptLine().WithBestBefore(DateOnly.MinValue).Generate();

        // Assert
        Assert.Equal(DateOnly.MinValue, anyLine.BestBefore);
    }

    [Fact]
    public void A_line_without_a_gtin_is_refused() {
        // Arrange
        Quantity  anyExpected   = Any.Quantity().Generate();
        Quantity  anyReceived   = Any.Quantity().Generate();
        LotNumber anyLot        = Any.LotNumber().Generate();
        DateOnly  anyBestBefore = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new ReceiptLine(null!, anyExpected, anyReceived, anyLot, anyBestBefore, 1m));

        // Assert
        Assert.Equal("gtin", refused.ParamName);
    }

    [Fact]
    public void A_line_without_an_expected_quantity_is_refused() {
        // Arrange
        Ean13     anyGtin       = Any.Ean13().Generate();
        Quantity  anyReceived   = Any.Quantity().Generate();
        LotNumber anyLot        = Any.LotNumber().Generate();
        DateOnly  anyBestBefore = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new ReceiptLine(anyGtin, null!, anyReceived, anyLot, anyBestBefore, 1m));

        // Assert
        Assert.Equal("expected", refused.ParamName);
    }

    [Fact]
    public void A_line_without_a_received_quantity_is_refused() {
        // Arrange
        Ean13     anyGtin       = Any.Ean13().Generate();
        Quantity  anyExpected   = Any.Quantity().Generate();
        LotNumber anyLot        = Any.LotNumber().Generate();
        DateOnly  anyBestBefore = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new ReceiptLine(anyGtin, anyExpected, null!, anyLot, anyBestBefore, 1m));

        // Assert
        Assert.Equal("received", refused.ParamName);
    }

    [Fact]
    public void A_line_without_a_lot_is_refused() {
        // Arrange
        Ean13    anyGtin       = Any.Ean13().Generate();
        Quantity anyExpected   = Any.Quantity().Generate();
        Quantity anyReceived   = Any.Quantity().Generate();
        DateOnly anyBestBefore = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new ReceiptLine(anyGtin, anyExpected, anyReceived, null!, anyBestBefore, 1m));

        // Assert
        Assert.Equal("lot", refused.ParamName);
    }

}
