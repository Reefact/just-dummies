using JustDummies;
using JustDummies.Xunit;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

[Reproducible]
public sealed class PalletReceiptTests {

    private const decimal EuroPalletTareKg = 25m;

    private static readonly DateTime ReceivedAt = new(2026, 3, 2, 6, 30, 0, DateTimeKind.Utc);

    /// <summary>
    ///     A line that raises no inspection by itself: counted in full, light, and with a long shelf life left
    ///     when it reaches the dock on <see cref="ReceivedAt" />.
    /// </summary>
    private static IAny<ReceiptLine> AnyUnremarkableLine() {
        return Any.Combine(
            Any.Int32().Between(1, 100),
            new AnyReceiptLine()
                .WithUnitWeightKg(weight => weight.LessThanOrEqualTo(1m).WithScale(3))
                .WithBestBefore(bestBefore => bestBefore.After(DateOnly.FromDateTime(ReceivedAt).AddDays(PalletReceipt.MinimumShelfLifeDays + 1))),
            (units, line) => new ReceiptLine(line.Gtin, new Quantity(units), new Quantity(units), line.Lot, line.BestBefore, line.UnitWeightKg));
    }

    private static AnyPalletReceipt AnyPalletReceivedAtTheDock(params ReceiptLine[] lines) {
        return Arbitrary.PalletReceipt()
                        .WithReceivedAt(ReceivedAt)
                        .WithLines(lines);
    }

    [Fact]
    public void A_pallet_counts_the_units_received_on_every_line() {
        // Arrange
        ReceiptLine   anyLineOfTen     = new AnyReceiptLine().WithReceived(new Quantity(10)).Generate();
        ReceiptLine   anyLineOfFifteen = new AnyReceiptLine().WithReceived(new Quantity(15)).Generate();
        PalletReceipt anyPallet        = AnyPalletReceivedAtTheDock(anyLineOfTen, anyLineOfFifteen).Generate();

        // Act
        int totalUnits = anyPallet.TotalUnits;

        // Assert
        Assert.Equal(25, totalUnits);
    }

    [Fact]
    public void A_pallet_of_unremarkable_lines_needs_no_inspection() {
        // Arrange
        List<ReceiptLine> anyLines  = Any.ListOf(AnyUnremarkableLine()).WithCountBetween(1, 8).Generate();
        PalletReceipt     anyPallet = AnyPalletReceivedAtTheDock([.. anyLines]).Generate();

        // Act
        IReadOnlyList<string> reasons = anyPallet.InspectionReasons(EuroPalletTareKg);

        // Assert
        Assert.Empty(reasons);
    }

    [Fact]
    public void A_short_line_holds_the_pallet_for_a_short_delivery() {
        // Arrange
        ReceiptLine   anyUnremarkable = AnyUnremarkableLine().Generate();
        ReceiptLine   anyShortLine    = new ReceiptLine(anyUnremarkable.Gtin, new Quantity(10), new Quantity(8),
                                                        anyUnremarkable.Lot, anyUnremarkable.BestBefore, anyUnremarkable.UnitWeightKg);
        PalletReceipt anyPallet       = AnyPalletReceivedAtTheDock(anyShortLine).Generate();

        // Act
        IReadOnlyList<string> reasons = anyPallet.InspectionReasons(EuroPalletTareKg);

        // Assert
        Assert.Equal(["short delivery"], reasons);
    }

    [Fact]
    public void A_pallet_whose_gross_weight_exceeds_1200_kg_is_held_as_overweight() {
        // Arrange: 1000 units of 1.2 kg is exactly the limit, the tare tips it over.
        ReceiptLine anyUnremarkable = AnyUnremarkableLine().Generate();
        ReceiptLine anyHeavyLine    = new ReceiptLine(anyUnremarkable.Gtin, new Quantity(1000), new Quantity(1000),
                                                      anyUnremarkable.Lot, anyUnremarkable.BestBefore, unitWeightKg: 1.2m);
        PalletReceipt anyPallet = AnyPalletReceivedAtTheDock(anyHeavyLine).Generate();

        // Act
        IReadOnlyList<string> reasons = anyPallet.InspectionReasons(EuroPalletTareKg);

        // Assert
        Assert.Equal(["overweight"], reasons);
    }

    [Fact]
    public void A_line_expiring_within_thirty_days_of_receipt_is_held_for_short_shelf_life() {
        // Arrange
        ReceiptLine anyUnremarkable = AnyUnremarkableLine().Generate();
        ReceiptLine anyExpiringLine = new ReceiptLine(anyUnremarkable.Gtin, anyUnremarkable.Expected, anyUnremarkable.Received,
                                                      anyUnremarkable.Lot, new DateOnly(2026, 3, 31), anyUnremarkable.UnitWeightKg);
        PalletReceipt anyPallet = AnyPalletReceivedAtTheDock(anyExpiringLine).Generate();

        // Act
        IReadOnlyList<string> reasons = anyPallet.InspectionReasons(EuroPalletTareKg);

        // Assert
        Assert.Equal(["short shelf life"], reasons);
    }

    [Fact]
    public void A_received_pallet_put_away_is_recorded_in_its_bin() {
        // Arrange
        PalletReceipt anyPallet = Arbitrary.PalletReceipt().Generate();
        BinLocation   bin       = BinLocation.Parse("C-04-02");

        // Act
        anyPallet.PutAway(bin);

        // Assert
        Assert.Equal((ReceiptStatus.PutAway, bin), (anyPallet.Status, anyPallet.Bin));
    }

    [Fact]
    public void A_quarantined_pallet_cannot_be_put_away() {
        // Arrange
        PalletReceipt anyPallet = Arbitrary.PalletReceipt().Generate();
        BinLocation   anyBin    = new AnyBinLocation().Generate();
        anyPallet.Quarantine("damaged stretch film");

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPallet.PutAway(anyBin));

        // Assert
        Assert.Contains("damaged stretch film", refusal.Message);
    }

    [Fact]
    public void A_pallet_already_in_its_bin_is_not_quarantined_from_the_dock() {
        // Arrange
        PalletReceipt anyPallet = Arbitrary.PalletReceipt().Generate();
        anyPallet.PutAway(new AnyBinLocation().Generate());
        string anyReason = Any.String().Printable().NotBlank().WithMaxLength(80).Generate();

        // Act
        Action quarantine = () => anyPallet.Quarantine(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(quarantine);
    }

    [Fact]
    public void A_supplier_code_out_of_format_is_refused() {
        // Arrange
        Sscc              anySscc  = new AnySscc().Generate();
        List<ReceiptLine> anyLines = Any.ListOf(new AnyReceiptLine()).WithCountBetween(1, 3).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PalletReceipt(anySscc, "SUP-42", ReceivedAt, requiredTemperature: null, anyLines));

        // Assert
        Assert.Equal("supplierCode", refusal.ParamName);
    }

}
