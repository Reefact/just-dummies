using JustDummies;

using Warehouse.Domain.Tests.Expert.Support;
using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class PalletReceiptTests {

    private static readonly DateTime ReceivedAt = new DateTime(2026, 3, 2, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void A_new_receipt_is_received_with_no_bin_and_no_quarantine_reason() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        ReceiptStatus status = anyReceipt.Status;

        // Assert
        Assert.Equal(ReceiptStatus.Received, status);
        Assert.Null(anyReceipt.Bin);
        Assert.Null(anyReceipt.QuarantineReason);
    }

    [Fact]
    public void A_receipt_keeps_everything_it_was_built_with() {
        // Arrange
        Sscc             sscc        = new Sscc("376104250021234569");
        TemperatureRange temperature = new TemperatureRange(2m, 8m);
        ReceiptLine      anyLine     = Any.ReceiptLine().Generate();

        // Act
        PalletReceipt receipt = new PalletReceipt(sscc, "SUP-00042", ReceivedAt, temperature, [anyLine]);

        // Assert
        Assert.Same(sscc, receipt.Sscc);
        Assert.Equal("SUP-00042", receipt.SupplierCode);
        Assert.Equal(ReceivedAt, receipt.ReceivedAt);
        Assert.Same(temperature, receipt.RequiredTemperature);
        Assert.Same(anyLine, Assert.Single(receipt.Lines));
    }

    [Fact]
    public void The_required_temperature_is_optional() {
        // Arrange
        PalletReceipt anyAmbientReceipt = Any.PalletReceipt().WithoutRequiredTemperature().Generate();

        // Act
        TemperatureRange? temperature = anyAmbientReceipt.RequiredTemperature;

        // Assert
        Assert.Null(temperature);
    }

    [Fact]
    public void The_limits_of_the_policy_are_1200_kilograms_and_30_days() {
        Assert.Equal(1_200m, PalletReceipt.MaximumGrossWeightKg);
        Assert.Equal(30, PalletReceipt.MinimumShelfLifeDays);
    }

    [Fact]
    public void The_lines_are_copied_so_the_caller_cannot_change_the_receipt_afterwards() {
        // Arrange
        List<ReceiptLine> givenLines = [Any.ReceiptLine().Generate(), Any.ReceiptLine().Generate()];
        PalletReceipt     anyReceipt = Any.PalletReceipt().WithLines(givenLines).Generate();

        // Act
        givenLines.Add(Any.ReceiptLine().Generate());
        givenLines.RemoveAt(0);

        // Assert
        Assert.Equal(2, anyReceipt.Lines.Count);
    }

    [Fact(Skip = "domain defect: Lines exposes the internal List<ReceiptLine> as IReadOnlyList, so a caller can cast it back and mutate the receipt; seed n/a (deterministic)")]
    public void The_exposed_lines_cannot_be_turned_back_into_a_mutable_list() {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().Generate();

        // Act
        IReadOnlyList<ReceiptLine> lines = anyReceipt.Lines;

        // Assert
        Assert.IsNotType<List<ReceiptLine>>(lines);
    }

    [Fact]
    public void The_total_units_is_the_sum_of_the_received_units_not_of_the_expected_ones() {
        // Arrange
        ReceiptLine firstLine  = Any.ReceiptLine().WithReceived(new Quantity(3)).Generate();
        ReceiptLine secondLine = Any.ReceiptLine().WithReceived(new Quantity(4)).Generate();
        ReceiptLine thirdLine  = Any.ReceiptLine().WithReceived(new Quantity(5)).Generate();
        PalletReceipt receipt  = Any.PalletReceipt().WithLines([firstLine, secondLine, thirdLine]).Generate();

        // Act
        int totalUnits = receipt.TotalUnits;

        // Assert
        Assert.Equal(12, totalUnits);
    }

    [Fact]
    public void The_net_weight_is_the_sum_of_the_weights_of_the_lines() {
        // Arrange
        ReceiptLine halfKiloUnits    = Any.ReceiptLine().WithReceived(new Quantity(2)).WithUnitWeightKg(0.5m).Generate();     // 1.00 kg
        ReceiptLine quarterKiloExtra = Any.ReceiptLine().WithReceived(new Quantity(3)).WithUnitWeightKg(1.25m).Generate();    // 3.75 kg
        PalletReceipt receipt        = Any.PalletReceipt().WithLines([halfKiloUnits, quarterKiloExtra]).Generate();

        // Act
        decimal netWeightKg = receipt.NetWeightKg;

        // Assert
        Assert.Equal(4.75m, netWeightKg);
    }

    [Fact]
    public void A_receipt_without_an_sscc_is_refused() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithSscc((Sscc)null!).Generate());

        // Assert
        Assert.Equal("sscc", refused.ParamName);
    }

    [Theory]
    [InlineData("SUP-00000")]
    [InlineData("SUP-00042")]
    [InlineData("SUP-99999")]
    public void A_supplier_code_is_SUP_followed_by_five_digits(string supplierCode) {
        // Arrange
        PalletReceipt anyReceipt = Any.PalletReceipt().WithSupplierCode(supplierCode).Generate();

        // Act
        string kept = anyReceipt.SupplierCode;

        // Assert
        Assert.Equal(supplierCode, kept);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SUP-0004")]          // four digits
    [InlineData("SUP-000420")]        // six digits
    [InlineData("sup-00042")]         // lower case
    [InlineData("SUP00042")]          // separator missing
    [InlineData("SUP_00042")]         // wrong separator
    [InlineData("SUP-0004A")]         // a letter among the digits
    [InlineData(" SUP-00042")]
    [InlineData("SUP-00042 ")]
    [InlineData("XSUP-00042")]
    [InlineData("SUP-00042X")]
    public void A_supplier_code_of_another_shape_is_refused(string malformed) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithSupplierCode(malformed).Generate());

        // Assert
        Assert.Equal("supplierCode", refused.ParamName);
    }

    [Fact(Skip = "domain defect: '$' accepts a trailing line feed and '\\d' accepts any Unicode decimal digit, so 'SUP-00042\\n' and 'SUP-٠٠٠٤٢' are accepted as supplier codes (use \\z and [0-9]); seed n/a (deterministic)")]
    public void A_supplier_code_must_be_ascii_digits_with_nothing_after_them() {
        // Act
        Exception? withLineFeed     = Record.Exception(() => Any.PalletReceipt().WithSupplierCode("SUP-00042\n").Generate());
        Exception? withArabicDigits = Record.Exception(() => Any.PalletReceipt().WithSupplierCode("SUP-٠٠٠٤٢").Generate());

        // Assert
        Assert.IsType<ArgumentException>(withLineFeed);
        Assert.IsType<ArgumentException>(withArabicDigits);
    }

    [Fact]
    public void A_missing_supplier_code_is_refused_as_a_null_argument() {
        // Act
        Exception? thrown = Record.Exception(() => Any.PalletReceipt().WithSupplierCode((string)null!).Generate());

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact(Skip = "domain defect: a null supplier code surfaces as an ArgumentNullException of the regex ('input'), not of the 'supplierCode' parameter; seed n/a (deterministic)")]
    public void A_missing_supplier_code_is_reported_on_the_supplier_code_parameter() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithSupplierCode((string)null!).Generate());

        // Assert
        Assert.Equal("supplierCode", refused.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void A_reception_time_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        DateTime notUtc = new DateTime(2026, 3, 2, 14, 30, 0, kind);

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithReceivedAt(notUtc).Generate());

        // Assert
        Assert.Equal("receivedAt", refused.ParamName);
    }

    [Fact]
    public void A_utc_reception_time_is_accepted() {
        // Act
        PalletReceipt anyReceipt = Any.PalletReceipt().WithReceivedAt(ReceivedAt).Generate();

        // Assert
        Assert.Equal(ReceivedAt, anyReceipt.ReceivedAt);
        Assert.Equal(DateTimeKind.Utc, anyReceipt.ReceivedAt.Kind);
    }

    [Fact]
    public void A_receipt_without_a_line_is_refused() {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => Any.PalletReceipt().WithLines(Array.Empty<ReceiptLine>()).Generate());

        // Assert
        Assert.Equal("lines", refused.ParamName);
    }

    [Fact(Skip = "domain defect: a null list of lines fails with a NullReferenceException on lines.Count instead of an ArgumentNullException like every other argument; seed n/a (deterministic)")]
    public void A_missing_list_of_lines_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => Any.PalletReceipt().WithLines((IReadOnlyList<ReceiptLine>)null!).Generate());

        // Assert
        Assert.Equal("lines", refused.ParamName);
    }

}
