using Warehouse.Domain.Tests.Regular.Support;

namespace Warehouse.Domain.Tests.Regular;

public sealed class PalletReceiptInspectionTests {

    private const string ShortDelivery  = "short delivery";
    private const string Overweight     = "overweight";
    private const string ShortShelfLife = "short shelf life";

    [Fact]
    public void A_complete_light_fresh_pallet_raises_no_reason() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, Calm.Line(anyReceived).Generate(), Calm.Line(anyReceived).Generate());
        decimal       anyTare     = Any.Decimal().Between(0m, 50m).Generate();

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(anyTare);

        // Assert
        Assert.Empty(reasons);
    }

    [Fact]
    public void A_short_line_holds_the_pallet_back() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        int           anyExpected = Any.Int32().Between(1, 500).Generate();
        int           anyShort    = Any.Int32().Between(0, anyExpected - 1).Generate();
        ReceiptLine   shortLine   = Calm.Line(anyReceived).WithExpected(new Quantity(anyExpected)).WithReceived(new Quantity(anyShort)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, Calm.Line(anyReceived).Generate(), shortLine);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(0m);

        // Assert
        Assert.Equal([ShortDelivery], reasons);
    }

    [Fact]
    public void An_exact_line_is_not_short() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        Quantity      anyCount    = new AnyQuantity().WithUnits(u => u.Between(0, 500)).Generate();
        ReceiptLine   exactLine   = Calm.Line(anyReceived).WithExpected(anyCount).WithReceived(anyCount).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, exactLine);

        // Assert
        Assert.Empty(receipt.InspectionReasons(0m));
    }

    [Fact]
    public void An_over_delivered_line_is_not_short() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        int           anyExpected = Any.Int32().Between(0, 500).Generate();
        int           anyExcess   = Any.Int32().Between(anyExpected + 1, anyExpected + 500).Generate();
        ReceiptLine   overLine    = Calm.Line(anyReceived).WithExpected(new Quantity(anyExpected)).WithReceived(new Quantity(anyExcess)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, overLine);

        // Assert
        Assert.Empty(receipt.InspectionReasons(0m));
    }

    [Fact]
    public void Several_short_lines_give_the_reason_once() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   firstShort  = Calm.Line(anyReceived).WithExpected(new Quantity(10)).WithReceived(new Quantity(9)).Generate();
        ReceiptLine   secondShort = Calm.Line(anyReceived).WithExpected(new Quantity(10)).WithReceived(new Quantity(1)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, firstShort, secondShort);

        // Assert
        Assert.Equal([ShortDelivery], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void The_gross_weight_of_exactly_1200_kg_is_allowed() {
        // Arrange: 100 units of 11.50 kg = 1150 kg net, and a 50 kg pallet
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   line        = Calm.Line(anyReceived).WithExpected(new Quantity(100)).WithReceived(new Quantity(100)).WithUnitWeightKg(11.5m).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);

        // Assert
        Assert.Empty(receipt.InspectionReasons(50m));
    }

    [Fact]
    public void A_gross_weight_just_above_1200_kg_is_overweight() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   line        = Calm.Line(anyReceived).WithExpected(new Quantity(100)).WithReceived(new Quantity(100)).WithUnitWeightKg(11.5m).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);

        // Assert
        Assert.Equal([Overweight], receipt.InspectionReasons(50.01m));
    }

    [Fact]
    public void Any_tare_that_brings_the_gross_weight_above_the_limit_is_overweight() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   line        = Calm.Line(anyReceived).WithExpected(new Quantity(100)).WithReceived(new Quantity(100)).WithUnitWeightKg(11.5m).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);
        decimal       anyHeavyTare = Any.Decimal().GreaterThan(50m).LessThanOrEqualTo(10_000m).Generate();

        // Assert
        Assert.Equal([Overweight], receipt.InspectionReasons(anyHeavyTare));
    }

    [Fact]
    public void The_net_weight_alone_over_the_limit_is_overweight_even_with_no_tare() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   line        = Calm.Line(anyReceived).WithExpected(new Quantity(1)).WithReceived(new Quantity(1)).WithUnitWeightKg(1_200.01m).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);

        // Assert
        Assert.Equal([Overweight], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void The_weight_of_a_pallet_is_summed_over_all_its_lines() {
        // Arrange: three lines of 400.01 kg
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine[] lines       = [HeavyLine(anyReceived, 400.01m), HeavyLine(anyReceived, 400.01m), HeavyLine(anyReceived, 400.01m)];
        PalletReceipt receipt     = Calm.Receipt(anyReceived, lines);

        // Assert
        Assert.Equal([Overweight], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void Exactly_thirty_days_of_shelf_life_is_enough() {
        // Arrange: received on 15 March at the very end of the day
        DateTime      received = new DateTime(2026, 3, 15, 23, 59, 59, DateTimeKind.Utc);
        ReceiptLine   line     = Calm.Line(received).WithBestBefore(new DateOnly(2026, 4, 14)).Generate();
        PalletReceipt receipt  = Calm.Receipt(received, line);

        // Assert
        Assert.Empty(receipt.InspectionReasons(0m));
    }

    [Fact]
    public void Twenty_nine_days_of_shelf_life_is_short() {
        // Arrange
        DateTime      received = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        ReceiptLine   line     = Calm.Line(received).WithBestBefore(new DateOnly(2026, 4, 13)).Generate();
        PalletReceipt receipt  = Calm.Receipt(received, line);

        // Assert
        Assert.Equal([ShortShelfLife], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void The_shelf_life_is_counted_from_the_date_of_reception_not_its_time() {
        // Arrange: the same best-before date, received at the first and at the last second of the day
        DateOnly bestBefore    = new DateOnly(2026, 4, 14);
        DateTime startOfDay    = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        DateTime endOfDay      = new DateTime(2026, 3, 15, 23, 59, 59, DateTimeKind.Utc);
        ReceiptLine firstLine  = Calm.Line(startOfDay).WithBestBefore(bestBefore).Generate();
        ReceiptLine secondLine = Calm.Line(endOfDay).WithBestBefore(bestBefore).Generate();

        // Assert
        Assert.Empty(Calm.Receipt(startOfDay, firstLine).InspectionReasons(0m));
        Assert.Empty(Calm.Receipt(endOfDay, secondLine).InspectionReasons(0m));
    }

    [Fact]
    public void Any_shelf_life_under_thirty_days_is_short() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        int           anyDays     = Any.Int32().Between(0, PalletReceipt.MinimumShelfLifeDays - 1).Generate();
        ReceiptLine   line        = Calm.Line(anyReceived).WithBestBefore(DateOnly.FromDateTime(anyReceived).AddDays(anyDays)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);

        // Assert
        Assert.Equal([ShortShelfLife], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void A_best_before_date_already_passed_is_short() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        int           anyDaysAgo  = Any.Int32().Between(1, 3_000).Generate();
        ReceiptLine   line        = Calm.Line(anyReceived).WithBestBefore(DateOnly.FromDateTime(anyReceived).AddDays(-anyDaysAgo)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, line);

        // Assert
        Assert.Equal([ShortShelfLife], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void One_short_dated_line_among_fresh_ones_is_enough() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   stale       = Calm.Line(anyReceived).WithBestBefore(DateOnly.FromDateTime(anyReceived).AddDays(10)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, Calm.Line(anyReceived).Generate(), stale, Calm.Line(anyReceived).Generate());

        // Assert
        Assert.Equal([ShortShelfLife], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void Every_reason_is_given_once_in_the_order_short_overweight_shelf_life() {
        // Arrange
        DateTime received  = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc);
        ReceiptLine bad    = Calm.Line(received)
                                 .WithExpected(new Quantity(10))
                                 .WithReceived(new Quantity(5))
                                 .WithUnitWeightKg(300m)
                                 .WithBestBefore(new DateOnly(2026, 3, 20))
                                 .Generate();
        ReceiptLine alsoBad = Calm.Line(received)
                                  .WithExpected(new Quantity(10))
                                  .WithReceived(new Quantity(5))
                                  .WithUnitWeightKg(300m)
                                  .WithBestBefore(new DateOnly(2026, 3, 20))
                                  .Generate();
        PalletReceipt receipt = Calm.Receipt(received, bad, alsoBad);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(25m);

        // Assert
        Assert.Equal([ShortDelivery, Overweight, ShortShelfLife], reasons);
    }

    [Fact]
    public void Two_reasons_keep_the_documented_order() {
        // Arrange: overweight and short shelf life only
        DateTime      received = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc);
        ReceiptLine   line     = HeavyLine(received, 1_300m);
        ReceiptLine   stale    = Calm.Line(received).WithBestBefore(new DateOnly(2026, 3, 16)).Generate();
        PalletReceipt receipt  = Calm.Receipt(received, line, stale);

        // Assert
        Assert.Equal([Overweight, ShortShelfLife], receipt.InspectionReasons(0m));
    }

    [Fact]
    public void Inspecting_does_not_change_the_status() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   shortLine   = Calm.Line(anyReceived).WithExpected(new Quantity(10)).WithReceived(new Quantity(1)).Generate();
        PalletReceipt receipt     = Calm.Receipt(anyReceived, shortLine);

        // Act
        IReadOnlyList<string> reasons = receipt.InspectionReasons(0m);

        // Assert
        Assert.NotEmpty(reasons);
        Assert.Equal(ReceiptStatus.Received, receipt.Status);
        Assert.Null(receipt.QuarantineReason);
    }

    [Fact]
    public void Inspecting_twice_gives_the_same_answer() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        decimal       anyTare    = Any.Decimal().Between(0m, 100m).Generate();

        // Assert
        Assert.Equal(anyReceipt.InspectionReasons(anyTare), anyReceipt.InspectionReasons(anyTare));
    }

    private static ReceiptLine HeavyLine(DateTime receivedAt, decimal weightKg) {
        return Calm.Line(receivedAt).WithExpected(new Quantity(1)).WithReceived(new Quantity(1)).WithUnitWeightKg(weightKg).Generate();
    }

}
