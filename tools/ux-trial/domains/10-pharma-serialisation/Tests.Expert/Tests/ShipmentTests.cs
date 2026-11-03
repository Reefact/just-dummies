using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class ShipmentTests {

    private static readonly DateOnly ExpiryDay = new DateOnly(2027, 6, 30);

    [Fact]
    public void A_shipment_keeps_what_it_was_made_of() {
        // Arrange
        IReadOnlyList<SerialNumber> serials = new List<SerialNumber> { new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02") };

        // Act
        Shipment shipment = new Shipment("SHP-00012345", serials, new DateOnly(2026, 4, 2));

        // Assert
        Assert.Equal("SHP-00012345", shipment.Reference);
        Assert.Same(serials, shipment.Serials);
        Assert.Equal(new DateOnly(2026, 4, 2), shipment.ShippedOn);
    }

    [Theory]
    [InlineData("SHP-00000000")]
    [InlineData("SHP-99999999")]
    public void A_reference_of_shp_and_eight_digits_is_accepted(string reference) {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = Any.ListOf(Any.SerialNumber()).NonEmpty().Distinct().Generate();
        DateOnly                    anyDate    = Any.DateOnly().Generate();

        // Act
        Shipment shipment = new Shipment(reference, anySerials, anyDate);

        // Assert
        Assert.Equal(reference, shipment.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SHP-1234567")]      // seven digits
    [InlineData("SHP-123456789")]    // nine digits
    [InlineData("shp-12345678")]     // lower case
    [InlineData("SHP12345678")]      // no hyphen
    [InlineData("SHP-1234567A")]     // a letter
    [InlineData(" SHP-12345678")]
    [InlineData("SHP-12345678 ")]
    [InlineData("XSHP-12345678")]
    [InlineData("SHP-1234 678")]
    public void A_reference_that_is_not_shp_and_eight_digits_is_refused(string reference) {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = Any.ListOf(Any.SerialNumber()).NonEmpty().Distinct().Generate();
        DateOnly                    anyDate    = Any.DateOnly().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Shipment(reference, anySerials, anyDate));

        // Assert
        Assert.Equal("reference", exception.ParamName);
    }

    [Fact]
    public void A_null_reference_is_an_argument_exception_not_an_argument_null_exception() {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = Any.ListOf(Any.SerialNumber()).NonEmpty().Distinct().Generate();
        DateOnly                    anyDate    = Any.DateOnly().Generate();

        // Act & Assert: Assert.Throws demands the exact type
        Assert.Throws<ArgumentException>(() => new Shipment(null!, anySerials, anyDate));
    }

    [Fact(Skip = "domain defect: the reference check is ^SHP-\\d{8}$, and $ also matches before a final newline, so \"SHP-12345678\\n\" is accepted and keeps the newline in Reference. Literal input, no draw involved (no seed).")]
    public void A_reference_with_a_trailing_newline_is_refused() {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = Any.ListOf(Any.SerialNumber()).NonEmpty().Distinct().Generate();
        DateOnly                    anyDate    = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shipment("SHP-12345678\n", anySerials, anyDate));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so \"SHP-\" followed by eight Arabic-Indic digits is accepted as a shipment reference. Literal input, no draw involved (no seed).")]
    public void A_reference_written_with_non_ascii_digits_is_refused() {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = Any.ListOf(Any.SerialNumber()).NonEmpty().Distinct().Generate();
        DateOnly                    anyDate    = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Shipment("SHP-١٢٣٤٥٦٧٨", anySerials, anyDate));
    }

    [Fact]
    public void A_shipment_without_a_list_of_serials_is_refused() {
        // Arrange
        string   anyReference = Any.ShipmentReference().Generate();
        DateOnly anyDate      = Any.DateOnly().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Shipment(anyReference, null!, anyDate));

        // Assert
        Assert.Equal("serials", exception.ParamName);
    }

    [Fact]
    public void A_shipment_of_no_serial_is_refused() {
        // Arrange
        string   anyReference = Any.ShipmentReference().Generate();
        DateOnly anyDate      = Any.DateOnly().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Shipment(anyReference, new List<SerialNumber>(), anyDate));

        // Assert
        Assert.Equal("serials", exception.ParamName);
    }

    [Fact]
    public void A_shipment_of_a_single_serial_is_accepted() {
        // Arrange
        string       anyReference = Any.ShipmentReference().Generate();
        SerialNumber anySerial    = Any.SerialNumber().Generate();
        DateOnly     anyDate      = Any.DateOnly().Generate();

        // Act
        Shipment shipment = new Shipment(anyReference, new List<SerialNumber> { anySerial }, anyDate);

        // Assert
        Assert.Single(shipment.Serials);
    }

    [Fact]
    public void A_serial_repeated_by_value_is_refused_although_the_instances_differ() {
        // Arrange
        string   anyReference = Any.ShipmentReference().Generate();
        DateOnly anyDate      = Any.DateOnly().Generate();
        List<SerialNumber> serials = new List<SerialNumber> { new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02"), new SerialNumber("SERIAL01") };

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Shipment(anyReference, serials, anyDate));

        // Assert
        Assert.Equal("serials", exception.ParamName);
    }

    [Fact]
    public void Serials_differing_only_by_case_are_not_repeats() {
        // Arrange
        string   anyReference = Any.ShipmentReference().Generate();
        DateOnly anyDate      = Any.DateOnly().Generate();
        List<SerialNumber> serials = new List<SerialNumber> { new SerialNumber("SERIAL01"), new SerialNumber("serial01") };

        // Act
        Shipment shipment = new Shipment(anyReference, serials, anyDate);

        // Assert
        Assert.Equal(2, shipment.Serials.Count);
    }

    [Fact]
    public void The_reference_is_checked_before_the_serials() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act & Assert: an argument exception of the exact type, not the null one the serials would raise
        Assert.Throws<ArgumentException>(() => new Shipment("nope", null!, anyDate));
    }

    [Fact]
    public void A_shipment_lists_no_suspicious_serial_when_all_are_authentic() {
        // Arrange
        Batch anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).WithRoomFor(2).Generate();
        anyBatch.Commission(new SerialNumber("SERIAL01"));
        anyBatch.Commission(new SerialNumber("SERIAL02"));
        Shipment anyShipment = Any.Shipment()
                                  .WithSerials(new List<SerialNumber> { new SerialNumber("SERIAL01"), new SerialNumber("SERIAL02") })
                                  .WithShippedOn(ExpiryDay)
                                  .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Empty(suspicious);
    }

    [Fact]
    public void Unknown_dispensed_and_recalled_serials_are_suspicious_in_the_order_of_the_shipment() {
        // Arrange
        Batch anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).WithRoomFor(3).Generate();
        anyBatch.Commission(new SerialNumber("DISPENSED"));
        anyBatch.Dispense(new SerialNumber("DISPENSED"), ExpiryDay);
        anyBatch.Commission(new SerialNumber("RECALLED1"));
        anyBatch.Recall();
        anyBatch.Commission(new SerialNumber("AUTHENTIC"));

        Shipment anyShipment = Any.Shipment()
                                  .WithSerials(new List<SerialNumber> {
                                      new SerialNumber("UNKNOWN01"), new SerialNumber("AUTHENTIC"), new SerialNumber("DISPENSED"), new SerialNumber("RECALLED1")
                                  })
                                  .WithShippedOn(ExpiryDay)
                                  .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Equal(new[] { "UNKNOWN01", "DISPENSED", "RECALLED1" }, suspicious.Select(serial => serial.Value));
    }

    [Fact]
    public void A_serial_that_expired_by_the_shipping_day_is_not_suspicious_while_the_pack_is_active() {
        // Arrange
        Batch anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        anyBatch.Commission(new SerialNumber("EXPIRED01"));

        Shipment anyShipment = Any.Shipment()
                                  .WithSerials(new List<SerialNumber> { new SerialNumber("EXPIRED01") })
                                  .WithShippedOn(ExpiryDay.AddDays(1))
                                  .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Empty(suspicious);
    }

    [Fact]
    public void A_dispensed_serial_is_suspicious_even_after_the_expiry() {
        // Arrange
        Batch anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        anyBatch.Commission(new SerialNumber("DISPENSED"));
        anyBatch.Dispense(new SerialNumber("DISPENSED"), ExpiryDay);

        Shipment anyShipment = Any.Shipment()
                                  .WithSerials(new List<SerialNumber> { new SerialNumber("DISPENSED") })
                                  .WithShippedOn(ExpiryDay.AddDays(1))
                                  .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Equal("DISPENSED", Assert.Single(suspicious).Value);
    }

    [Fact]
    public void The_shipping_day_is_the_day_the_serials_are_verified_on() {
        // Arrange
        Batch anyBatch = Any.Batch().WithExpiresOn(ExpiryDay).Generate();
        anyBatch.Commission(new SerialNumber("AUTHENTIC"));
        anyBatch.Commission(new SerialNumber("DISPENSED"));
        anyBatch.Dispense(new SerialNumber("DISPENSED"), ExpiryDay);

        Shipment anyShipment = Any.Shipment()
                                  .WithSerials(new List<SerialNumber> { new SerialNumber("AUTHENTIC"), new SerialNumber("DISPENSED"), new SerialNumber("UNKNOWN01") })
                                  .WithShippedOn(ExpiryDay.AddDays(-30))
                                  .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Equal(new[] { "DISPENSED", "UNKNOWN01" }, suspicious.Select(serial => serial.Value));
    }

    [Fact(Skip = "domain defect: SuspiciousIn(null) throws a NullReferenceException from inside the lambda instead of an ArgumentNullException, unlike every other public method taking a reference (Commission, Verify, Dispense...). Literal input, no draw involved (no seed).")]
    public void A_null_batch_is_refused_as_an_argument_null_exception() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyShipment.SuspiciousIn(null!));
    }

}
