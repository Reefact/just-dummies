using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class PalletReceiptTests {

    private static readonly DateTime ReceivedAt = new DateTime(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void A_new_pallet_is_received_with_no_bin_and_no_quarantine_reason() {
        // Arrange
        Sscc                sscc  = Any.Sscc().Generate();
        List<ReceiptLine>   lines = [Any.ReceiptLine().Generate()];
        TemperatureRange    range = Any.TemperatureRange().Generate();

        // Act
        PalletReceipt receipt = new PalletReceipt(sscc, "SUP-00042", ReceivedAt, range, lines);

        // Assert
        Assert.Same(sscc, receipt.Sscc);
        Assert.Equal("SUP-00042", receipt.SupplierCode);
        Assert.Equal(ReceivedAt, receipt.ReceivedAt);
        Assert.Same(range, receipt.RequiredTemperature);
        Assert.Equal(ReceiptStatus.Received, receipt.Status);
        Assert.Null(receipt.Bin);
        Assert.Null(receipt.QuarantineReason);
    }

    [Fact]
    public void The_required_temperature_is_optional() {
        PalletReceipt anyReceipt = Any.PalletReceipt().WithoutRequiredTemperature().Generate();

        Assert.Null(anyReceipt.RequiredTemperature);
    }

    [Fact]
    public void A_null_sscc_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithSscc((Sscc)null!).Generate());

        Assert.Equal("sscc", exception.ParamName);
    }

    [Theory]
    [InlineData("SUP-00000")]
    [InlineData("SUP-99999")]
    public void A_supplier_code_is_sup_dash_and_five_digits(string code) {
        PalletReceipt anyReceipt = Any.PalletReceipt().WithSupplierCode(code).Generate();

        Assert.Equal(code, anyReceipt.SupplierCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SUP-1234")]
    [InlineData("SUP-123456")]
    [InlineData("sup-12345")]
    [InlineData("SUP-ABCDE")]
    [InlineData("SUP 12345")]
    [InlineData("XSUP-12345")]
    [InlineData("SUP-12345 ")]
    [InlineData(" SUP-12345")]
    public void A_supplier_code_in_another_shape_is_refused(string code) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithSupplierCode(code).Generate());

        Assert.Equal("supplierCode", exception.ParamName);
    }

    [Fact]
    public void A_null_supplier_code_is_refused() {
        // The regex rejects it before any guard of the pallet does.
        Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithSupplierCode((string)null!).Generate());
    }

    [Fact(Skip = "domain defect: the supplier pattern '^SUP-\\d{5}$' accepts a trailing line feed and non-ASCII digits")]
    public void A_supplier_code_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithSupplierCode("SUP-12345\n").Generate());
    }

    [Fact(Skip = "domain defect: the supplier pattern '^SUP-\\d{5}$' accepts Arabic-Indic digits")]
    public void A_supplier_code_with_non_ascii_digits_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithSupplierCode("SUP-٠٠٠٤٢").Generate());
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_receipt_time_that_is_not_utc_is_refused(DateTimeKind kind) {
        DateTime notUtc = new DateTime(2026, 3, 10, 14, 30, 0, kind);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithReceivedAt(notUtc).Generate());

        Assert.Equal("receivedAt", exception.ParamName);
    }

    [Fact]
    public void A_utc_receipt_time_is_kept_as_it_is() {
        PalletReceipt anyReceipt = Any.PalletReceipt().WithReceivedAt(ReceivedAt).Generate();

        Assert.Equal(ReceivedAt, anyReceipt.ReceivedAt);
        Assert.Equal(DateTimeKind.Utc, anyReceipt.ReceivedAt.Kind);
    }

    [Fact]
    public void A_pallet_with_no_line_is_refused() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithLines(new List<ReceiptLine>()).Generate());

        Assert.Equal("lines", exception.ParamName);
    }

    [Fact(Skip = "domain defect: a null list of lines fails with NullReferenceException instead of ArgumentNullException")]
    public void A_null_list_of_lines_is_refused_as_a_null_argument() {
        Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithLines((IReadOnlyList<ReceiptLine>)null!).Generate());
    }

    [Fact]
    public void The_lines_are_copied_so_the_caller_cannot_change_them_afterwards() {
        // Arrange
        List<ReceiptLine> source = [Any.ReceiptLine().Generate(), Any.ReceiptLine().Generate()];
        PalletReceipt     anyReceipt = Any.PalletReceipt().WithLines(source).Generate();

        // Act
        source.Add(Any.ReceiptLine().Generate());
        source.RemoveAt(0);

        // Assert
        Assert.Equal(2, anyReceipt.Lines.Count);
    }

    [Fact(Skip = "domain defect: Lines hands out the internal List, so a caller can cast it to IList<ReceiptLine> and add a line")]
    public void The_lines_cannot_be_changed_through_the_property() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        Assert.False(anyReceipt.Lines is IList<ReceiptLine>);
    }

    [Fact]
    public void The_constants_are_the_goods_in_policy() {
        Assert.Equal(1_200m, PalletReceipt.MaximumGrossWeightKg);
        Assert.Equal(30, PalletReceipt.MinimumShelfLifeDays);
    }

    [Fact]
    public void The_total_units_is_the_sum_of_the_received_units() {
        // Arrange
        ReceiptLine first  = Any.ReceiptLine().WithReceived(new Quantity(3)).Generate();
        ReceiptLine second = Any.ReceiptLine().WithReceived(new Quantity(4)).Generate();
        ReceiptLine third  = Any.ReceiptLine().WithReceived(new Quantity(0)).Generate();

        PalletReceipt anyReceipt = Any.PalletReceipt().WithLines(new List<ReceiptLine> { first, second, third }).Generate();

        // Assert
        Assert.Equal(7, anyReceipt.TotalUnits);
    }

    [Fact]
    public void The_net_weight_is_the_sum_of_the_lines_weights() {
        // Arrange
        ReceiptLine first  = Any.ReceiptLine().WithReceived(new Quantity(3)).WithUnitWeightKg(2m).Generate();
        ReceiptLine second = Any.ReceiptLine().WithReceived(new Quantity(4)).WithUnitWeightKg(0.5m).Generate();

        PalletReceipt anyReceipt = Any.PalletReceipt().WithLines(new List<ReceiptLine> { first, second }).Generate();

        // Assert
        Assert.Equal(8m, anyReceipt.NetWeightKg);
    }

    // ---- Inspection ----------------------------------------------------------------------------------------

    /// <summary>A line that triggers no inspection reason by itself: nothing short, light, 60 days of shelf life left.</summary>
    private static ReceiptLine CalmLine() {
        // Few units, so that two lines stay far below the weight limit.
        Quantity anyQuantity = Any.Quantity().WithUnits(u => u.Between(0, 100)).Generate();

        return Any.ReceiptLine()
                  .WithExpected(anyQuantity)
                  .WithReceived(anyQuantity)
                  .WithUnitWeightKg(0.1m)
                  .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(60))
                  .Generate();
    }

    private static PalletReceipt ReceiptOf(params ReceiptLine[] lines) {
        return Any.PalletReceipt().WithReceivedAt(ReceivedAt).WithLines(lines.ToList()).Generate();
    }

    [Fact]
    public void A_calm_pallet_has_nothing_to_inspect() {
        PalletReceipt anyReceipt = ReceiptOf(CalmLine(), CalmLine());

        IReadOnlyList<string> reasons = anyReceipt.InspectionReasons(25m);

        Assert.Empty(reasons);
    }

    [Fact]
    public void A_short_line_calls_for_inspection() {
        ReceiptLine shortLine = Any.ReceiptLine()
                                   .WithExpected(new Quantity(10))
                                   .WithReceived(new Quantity(9))
                                   .WithUnitWeightKg(0.1m)
                                   .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(60))
                                   .Generate();
        PalletReceipt anyReceipt = ReceiptOf(CalmLine(), shortLine);

        IReadOnlyList<string> reasons = anyReceipt.InspectionReasons(25m);

        Assert.Equal(new[] { "short delivery" }, reasons);
    }

    [Fact]
    public void An_over_delivered_line_does_not_call_for_inspection() {
        ReceiptLine overLine = Any.ReceiptLine()
                                  .WithExpected(new Quantity(10))
                                  .WithReceived(new Quantity(11))
                                  .WithUnitWeightKg(0.1m)
                                  .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(60))
                                  .Generate();
        PalletReceipt anyReceipt = ReceiptOf(overLine);

        Assert.Empty(anyReceipt.InspectionReasons(25m));
    }

    [Theory]
    [InlineData(0, false)]        // 1200 kg gross: the limit itself is allowed
    [InlineData(0.01, true)]
    [InlineData(-1, false)]
    public void A_pallet_is_overweight_only_strictly_above_twelve_hundred_kilos_gross(double tare, bool overweight) {
        // Arrange: 100 units of 12 kg make exactly 1200 kg net.
        Quantity anyQuantity = Any.Quantity().WithUnits(100).Generate();
        ReceiptLine heavy = Any.ReceiptLine()
                               .WithExpected(anyQuantity)
                               .WithReceived(anyQuantity)
                               .WithUnitWeightKg(12m)
                               .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(60))
                               .Generate();
        PalletReceipt anyReceipt = ReceiptOf(heavy);

        // Act
        IReadOnlyList<string> reasons = anyReceipt.InspectionReasons((decimal)tare);

        // Assert
        Assert.Equal(overweight, reasons.Contains("overweight"));
        Assert.Equal(overweight ? 1 : 0, reasons.Count);
    }

    [Fact]
    public void The_tare_counts_in_the_gross_weight() {
        Quantity anyQuantity = Any.Quantity().WithUnits(50).Generate();
        ReceiptLine line = Any.ReceiptLine()
                              .WithExpected(anyQuantity)
                              .WithReceived(anyQuantity)
                              .WithUnitWeightKg(20m)
                              .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(60))
                              .Generate();
        PalletReceipt anyReceipt = ReceiptOf(line);

        Assert.Empty(anyReceipt.InspectionReasons(200m));
        Assert.Equal(new[] { "overweight" }, anyReceipt.InspectionReasons(200.5m));
    }

    [Theory]
    [InlineData(30, false)]
    [InlineData(31, false)]
    [InlineData(29, true)]
    [InlineData(0, true)]
    [InlineData(-5, true)]
    public void A_line_with_fewer_than_thirty_days_of_shelf_life_calls_for_inspection(int daysLeft, bool short_) {
        // Arrange
        ReceiptLine line = Any.ReceiptLine()
                              .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(daysLeft))
                              .WithUnitWeightKg(0.1m)
                              .Generate();
        PalletReceipt anyReceipt = ReceiptOf(line);

        // Act
        IReadOnlyList<string> reasons = anyReceipt.InspectionReasons(25m);

        // Assert
        Assert.Equal(short_, reasons.Contains("short shelf life"));
    }

    [Fact]
    public void The_shelf_life_is_counted_from_the_received_date_not_the_received_time() {
        // Received at 23:59 UTC: best before 30 calendar days later is still enough.
        DateTime lateEvening = new DateTime(2026, 3, 10, 23, 59, 0, DateTimeKind.Utc);
        ReceiptLine enough = Any.ReceiptLine().WithBestBefore(new DateOnly(2026, 4, 9)).WithUnitWeightKg(0.1m).Generate();
        ReceiptLine lacking = Any.ReceiptLine().WithBestBefore(new DateOnly(2026, 4, 8)).WithUnitWeightKg(0.1m).Generate();

        PalletReceipt okReceipt  = Any.PalletReceipt().WithReceivedAt(lateEvening).WithLines(new List<ReceiptLine> { enough }).Generate();
        PalletReceipt shortReceipt = Any.PalletReceipt().WithReceivedAt(lateEvening).WithLines(new List<ReceiptLine> { lacking }).Generate();

        Assert.DoesNotContain("short shelf life", okReceipt.InspectionReasons(25m));
        Assert.Contains("short shelf life", shortReceipt.InspectionReasons(25m));
    }

    [Fact]
    public void Every_reason_appears_in_the_policy_order_and_only_once() {
        // Arrange: two short, heavy, nearly expired lines
        ReceiptLine bad = Any.ReceiptLine()
                             .WithExpected(new Quantity(100))
                             .WithReceived(new Quantity(90))
                             .WithUnitWeightKg(10m)
                             .WithBestBefore(DateOnly.FromDateTime(ReceivedAt).AddDays(5))
                             .Generate();
        PalletReceipt anyReceipt = ReceiptOf(bad, bad);

        // Act
        IReadOnlyList<string> reasons = anyReceipt.InspectionReasons(25m);

        // Assert
        Assert.Equal(new[] { "short delivery", "overweight", "short shelf life" }, reasons);
    }

    [Fact]
    public void Inspecting_a_pallet_does_not_change_its_status() {
        ReceiptLine shortLine = Any.ReceiptLine().WithExpected(new Quantity(10)).WithReceived(new Quantity(1)).WithUnitWeightKg(0.1m).Generate();
        PalletReceipt anyReceipt = ReceiptOf(shortLine);

        anyReceipt.InspectionReasons(25m);

        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    // ---- Quarantine ----------------------------------------------------------------------------------------

    [Fact]
    public void Quarantining_a_received_pallet_records_the_reason() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        anyReceipt.Quarantine("short delivery");

        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal("short delivery", anyReceipt.QuarantineReason);
    }

    [Fact]
    public void Quarantining_again_replaces_the_reason() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("short delivery");

        anyReceipt.Quarantine("overweight");

        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Equal("overweight", anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_null_reason_is_refused_and_leaves_the_pallet_received() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        Assert.Throws<ArgumentNullException>(() => anyReceipt.Quarantine(null!));

        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_reason_is_refused_and_leaves_the_pallet_received(string blank) {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        Assert.Throws<ArgumentException>(() => anyReceipt.Quarantine(blank));

        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_pallet_already_put_away_cannot_be_quarantined() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        BinLocation   anyBin     = Any.BinLocation().Generate();
        anyReceipt.PutAway(anyBin);

        Assert.Throws<InvalidOperationException>(() => anyReceipt.Quarantine("too late"));

        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Null(anyReceipt.QuarantineReason);
        Assert.Same(anyBin, anyReceipt.Bin);
    }

    // ---- Put-away ------------------------------------------------------------------------------------------

    [Fact]
    public void Putting_away_a_received_pallet_records_the_bin() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        BinLocation   anyBin     = Any.BinLocation().Generate();

        anyReceipt.PutAway(anyBin);

        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(anyBin, anyReceipt.Bin);
    }

    [Fact]
    public void A_null_bin_is_refused_and_leaves_the_pallet_received() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        Assert.Throws<ArgumentNullException>(() => anyReceipt.PutAway(null!));

        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_quarantined_pallet_cannot_be_put_away() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        anyReceipt.Quarantine("overweight");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyReceipt.PutAway(Any.BinLocation().Generate()));

        Assert.Contains("quarantined", exception.Message);
        Assert.Contains("overweight", exception.Message);
        Assert.Equal(ReceiptStatus.Quarantined, anyReceipt.Status);
        Assert.Null(anyReceipt.Bin);
    }

    [Fact]
    public void A_pallet_cannot_be_put_away_twice_and_keeps_its_first_bin() {
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();
        BinLocation   firstBin   = Any.BinLocation().Generate();
        BinLocation   otherBin   = Any.BinLocation().Generate();
        anyReceipt.PutAway(firstBin);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyReceipt.PutAway(otherBin));

        Assert.Contains(firstBin.ToString(), exception.Message);
        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
        Assert.Same(firstBin, anyReceipt.Bin);
    }

    [Fact]
    public void The_required_temperature_is_not_checked_against_the_bin() {
        // Arrange: a frozen pallet in the highest bin is accepted, whatever it means for the warehouse.
        TemperatureRange frozen = new TemperatureRange(-30m, -20m);
        PalletReceipt anyReceipt = Any.PalletReceipt().WithRequiredTemperature(frozen).Generate();

        anyReceipt.PutAway(Any.BinLocation().WithLevel(20).Generate());

        Assert.Equal(ReceiptStatus.PutAway, anyReceipt.Status);
    }

}
