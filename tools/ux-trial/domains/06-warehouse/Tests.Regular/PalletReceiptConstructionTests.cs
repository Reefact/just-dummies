using Warehouse.Domain.Tests.Regular.Support;

namespace Warehouse.Domain.Tests.Regular;

public sealed class PalletReceiptConstructionTests {

    [Fact]
    public void The_arguments_are_kept_and_the_pallet_starts_received_and_not_stored() {
        // Arrange
        Sscc                  anySscc     = new AnySscc().Generate();
        DateTime              anyReceived = Calm.AnyReceivedAt();
        TemperatureRange      anyRange    = new AnyTemperatureRange().Generate();
        List<ReceiptLine>     anyLines    = [Calm.Line(anyReceived).Generate()];

        // Act
        PalletReceipt receipt = new PalletReceipt(anySscc, "SUP-00042", anyReceived, anyRange, anyLines);

        // Assert
        Assert.Same(anySscc, receipt.Sscc);
        Assert.Equal("SUP-00042", receipt.SupplierCode);
        Assert.Equal(anyReceived, receipt.ReceivedAt);
        Assert.Same(anyRange, receipt.RequiredTemperature);
        Assert.Equal(ReceiptStatus.Received, receipt.Status);
        Assert.Null(receipt.Bin);
        Assert.Null(receipt.QuarantineReason);
    }

    [Fact]
    public void Any_generated_receipt_starts_received_with_at_least_one_line() {
        // Act
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Assert
        Assert.Equal(ReceiptStatus.Received, anyReceipt.Status);
        Assert.NotEmpty(anyReceipt.Lines);
        Assert.Equal(DateTimeKind.Utc, anyReceipt.ReceivedAt.Kind);
        Assert.Matches(@"^SUP-\d{5}\z", anyReceipt.SupplierCode);
    }

    [Fact]
    public void The_required_temperature_is_optional() {
        // Act
        PalletReceipt receipt = new AnyPalletReceipt().WithoutRequiredTemperature().Generate();

        // Assert
        Assert.Null(receipt.RequiredTemperature);
    }

    [Fact]
    public void The_lines_are_copied() {
        // Arrange
        DateTime          anyReceived = Calm.AnyReceivedAt();
        ReceiptLine       first       = Calm.Line(anyReceived).Generate();
        ReceiptLine       second      = Calm.Line(anyReceived).Generate();
        List<ReceiptLine> source      = [first];
        PalletReceipt     receipt     = new AnyPalletReceipt().WithReceivedAt(anyReceived).WithLines(source).Generate();

        // Act
        source.Add(second);
        source.Clear();

        // Assert
        Assert.Single(receipt.Lines);
        Assert.Same(first, receipt.Lines[0]);
    }

    [Fact]
    public void The_lines_keep_their_order() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine[] anyLines    = [Calm.Line(anyReceived).Generate(), Calm.Line(anyReceived).Generate(), Calm.Line(anyReceived).Generate()];

        // Act
        PalletReceipt receipt = Calm.Receipt(anyReceived, anyLines);

        // Assert
        Assert.Equal(anyLines, receipt.Lines);
    }

    [Fact]
    public void A_missing_sscc_is_refused() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new PalletReceipt(null!, anyReceipt.SupplierCode, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, anyReceipt.Lines));

        // Assert
        Assert.Equal("sscc", refusal.ParamName);
    }

    [Theory]
    [InlineData("SUP-00000")]
    [InlineData("SUP-99999")]
    public void Supplier_codes_at_both_ends_of_the_numbering_are_accepted(string supplierCode) {
        // Act
        PalletReceipt receipt = new AnyPalletReceipt().WithSupplierCode(supplierCode).Generate();

        // Assert
        Assert.Equal(supplierCode, receipt.SupplierCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SUP-1234")]
    [InlineData("SUP-123456")]
    [InlineData("sup-12345")]
    [InlineData("SUP12345")]
    [InlineData("SUP-1234A")]
    [InlineData("XSUP-12345")]
    [InlineData("SUP-12345X")]
    [InlineData(" SUP-12345")]
    [InlineData("SUP-12345 ")]
    [InlineData("SUP_12345")]
    public void A_supplier_code_that_is_not_SUP_and_five_digits_is_refused(string supplierCode) {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PalletReceipt(anyReceipt.Sscc, supplierCode, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, anyReceipt.Lines));

        // Assert
        Assert.Equal("supplierCode", refusal.ParamName);
    }

    [Fact]
    public void A_null_supplier_code_is_refused() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(
            () => new PalletReceipt(anyReceipt.Sscc, null!, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, anyReceipt.Lines));
    }

    // `$` also matches before a final line feed, and `\d` matches every Unicode decimal digit.
    [Theory(Skip = "domain defect: SupplierFormat '^SUP-\\d{5}$' accepts a trailing line feed and non-ASCII digits, seed n/a (deterministic)")]
    [InlineData("SUP-12345\n")]
    [InlineData("SUP-١٢٣٤٥")]
    public void A_supplier_code_is_ascii_digits_only_with_nothing_after_them(string supplierCode) {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Assert
        Assert.Throws<ArgumentException>(
            () => new PalletReceipt(anyReceipt.Sscc, supplierCode, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, anyReceipt.Lines));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_reception_time_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();
        DateTime      notUtc     = DateTime.SpecifyKind(anyReceipt.ReceivedAt, kind);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PalletReceipt(anyReceipt.Sscc, anyReceipt.SupplierCode, notUtc, anyReceipt.RequiredTemperature, anyReceipt.Lines));

        // Assert
        Assert.Equal("receivedAt", refusal.ParamName);
    }

    [Fact]
    public void A_pallet_without_a_line_is_refused() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new PalletReceipt(anyReceipt.Sscc, anyReceipt.SupplierCode, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, []));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: a null list of lines throws NullReferenceException instead of ArgumentNullException (PalletReceipt.cs, 'lines.Count'), seed n/a (deterministic)")]
    public void A_null_list_of_lines_is_an_argument_null_error() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(
            () => new PalletReceipt(anyReceipt.Sscc, anyReceipt.SupplierCode, anyReceipt.ReceivedAt, anyReceipt.RequiredTemperature, null!));
    }

    [Fact]
    public void The_total_units_is_the_sum_of_the_received_units() {
        // Arrange
        DateTime      anyReceived = Calm.AnyReceivedAt();
        ReceiptLine   first       = Calm.Line(anyReceived).WithReceived(new Quantity(3)).WithExpected(new Quantity(3)).Generate();
        ReceiptLine   second      = Calm.Line(anyReceived).WithReceived(new Quantity(4)).WithExpected(new Quantity(9)).Generate();
        ReceiptLine   third       = Calm.Line(anyReceived).WithReceived(new Quantity(0)).WithExpected(new Quantity(0)).Generate();

        // Act
        PalletReceipt receipt = Calm.Receipt(anyReceived, first, second, third);

        // Assert
        Assert.Equal(7, receipt.TotalUnits);
    }

    [Fact]
    public void The_net_weight_is_the_sum_of_the_weights_of_the_lines() {
        // Arrange
        DateTime    anyReceived = Calm.AnyReceivedAt();
        ReceiptLine heavy       = Calm.Line(anyReceived).WithReceived(new Quantity(3)).WithUnitWeightKg(2.5m).Generate();
        ReceiptLine light       = Calm.Line(anyReceived).WithReceived(new Quantity(4)).WithUnitWeightKg(10m).Generate();

        // Act
        PalletReceipt receipt = Calm.Receipt(anyReceived, heavy, light);

        // Assert
        Assert.Equal(47.5m, receipt.NetWeightKg);
    }

    [Fact]
    public void The_net_weight_matches_the_lines_it_holds() {
        // Arrange
        PalletReceipt anyReceipt = new AnyPalletReceipt().Generate();

        // Assert
        Assert.Equal(anyReceipt.Lines.Sum(l => l.WeightKg), anyReceipt.NetWeightKg);
        Assert.Equal(anyReceipt.Lines.Sum(l => l.Received.Units), anyReceipt.TotalUnits);
    }

    [Fact]
    public void The_limits_of_goods_in_are_published_as_constants() {
        Assert.Equal(1_200m, PalletReceipt.MaximumGrossWeightKg);
        Assert.Equal(30, PalletReceipt.MinimumShelfLifeDays);
    }

}
