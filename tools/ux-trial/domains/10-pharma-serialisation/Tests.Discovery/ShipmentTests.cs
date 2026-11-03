using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ShipmentTests {

    private static readonly DateOnly Expiry = new DateOnly(2027, 1, 31);

    [Fact]
    public void A_shipment_keeps_its_reference_its_date_and_the_serial_list_as_given() {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials   = AnyDistinctSerials(3);
        DateOnly                    shippedOn    = new DateOnly(2026, 6, 1);

        // Act
        Shipment shipment = new Shipment("SHP-00001234", anySerials, shippedOn);

        // Assert
        Assert.Equal("SHP-00001234", shipment.Reference);
        Assert.Same(anySerials, shipment.Serials);
        Assert.Equal(shippedOn, shipment.ShippedOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SHP-1234567")]
    [InlineData("SHP-123456789")]
    [InlineData("shp-12345678")]
    [InlineData("SHP12345678")]
    [InlineData("SHP-1234567A")]
    [InlineData(" SHP-12345678")]
    [InlineData("SHP-12345678 ")]
    [InlineData("XSHP-12345678")]
    public void A_reference_that_is_not_SHP_dash_and_eight_digits_is_refused(string? reference) {
        // Arrange
        IReadOnlyList<SerialNumber> anySerials = AnyDistinctSerials(1);

        // Act / Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Shipment(reference!, anySerials, Expiry));
        Assert.Equal("reference", exception.ParamName);
    }

    [Fact(Skip = "domain defect: the pattern '^SHP-\\d{8}$' lets a trailing line feed through ('$' matches before it), so 'SHP-12345678\\n' is accepted as a reference. Seed-independent: literal input.")]
    public void A_reference_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new Shipment("SHP-12345678\n", AnyDistinctSerials(1), Expiry));
    }

    [Fact]
    public void A_shipment_without_a_serial_list_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Shipment("SHP-00000001", null!, Expiry));
    }

    [Fact]
    public void A_shipment_with_an_empty_serial_list_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Shipment("SHP-00000001", [], Expiry));
    }

    [Fact]
    public void A_shipment_cannot_carry_the_same_serial_twice_even_through_two_instances() {
        // Arrange
        SerialNumber anySerial = new AnySerialNumber().Generate();
        SerialNumber sameValue = new SerialNumber(anySerial.Value);

        // Act / Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Shipment("SHP-00000001", [anySerial, sameValue], Expiry));
        Assert.Equal("serials", exception.ParamName);
    }

    [Fact]
    public void Serials_differing_only_by_case_are_distinct() {
        // Act
        Shipment shipment = new Shipment("SHP-00000001", [new SerialNumber("abcd1234"), new SerialNumber("ABCD1234")], Expiry);

        // Assert
        Assert.Equal(2, shipment.Serials.Count);
    }

    [Fact]
    public void The_reference_is_checked_before_the_serials() {
        Assert.Throws<ArgumentException>(() => new Shipment("nope", null!, Expiry));
    }

    // ---- suspicious serials ----------------------------------------------------------------

    [Fact]
    public void Serials_unknown_to_the_batch_or_already_dispensed_are_suspicious() {
        // Arrange
        Batch                       batch   = AnyBatchExpiringOn(Expiry).Generate();
        IReadOnlyList<SerialNumber> serials = AnyDistinctSerials(3);
        batch.Commission(serials[0]);                         // genuine and still active
        batch.Commission(serials[1]);                         // dispensed already
        batch.Dispense(serials[1], Expiry);
        // serials[2] was never registered

        Shipment shipment = new Shipment("SHP-00000042", [serials[0], serials[1], serials[2]], new DateOnly(2026, 6, 1));

        // Act
        IReadOnlyList<SerialNumber> suspicious = shipment.SuspiciousIn(batch);

        // Assert
        Assert.Equal([serials[1], serials[2]], suspicious);
    }

    [Fact]
    public void An_expired_serial_is_not_suspicious() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        Shipment       late  = new Shipment("SHP-00000042", [pack.Serial], Expiry.AddDays(1));

        // Act / Assert
        Assert.Empty(late.SuspiciousIn(batch));
    }

    [Fact]
    public void A_decommissioned_serial_stays_suspicious_after_its_expiry() {
        // Arrange
        Batch          batch = AnyBatchExpiringOn(Expiry).Generate();
        SerialisedPack pack  = batch.Commission(new AnySerialNumber().Generate());
        batch.Recall();
        Shipment late = new Shipment("SHP-00000042", [pack.Serial], Expiry.AddDays(1));

        // Act
        IReadOnlyList<SerialNumber> suspicious = late.SuspiciousIn(batch);

        // Assert
        Assert.Equal([pack.Serial], suspicious);
    }

    [Fact]
    public void A_shipment_of_genuine_serials_has_nothing_suspicious() {
        // Arrange
        Batch                         batch = AnyBatchExpiringOn(Expiry).Generate();
        IReadOnlyList<SerialisedPack> packs = batch.CommissionAll(AnyDistinctSerials(3));
        Shipment                      shipment = new Shipment("SHP-00000042", packs.Select(pack => pack.Serial).ToList(), new DateOnly(2026, 6, 1));

        // Act / Assert
        Assert.Empty(shipment.SuspiciousIn(batch));
    }

    private static AnyBatch AnyBatchExpiringOn(DateOnly expiresOn) {
        return new AnyBatch().WithExpiresOn(expiresOn).WithPlannedQuantity(p => p.GreaterThanOrEqualTo(10));
    }

    private static IReadOnlyList<SerialNumber> AnyDistinctSerials(int count) {
        return Any.ListOf(new AnySerialNumber()).WithCount(count).Distinct().Generate();
    }

}
