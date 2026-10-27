using JustDummies;
using JustDummies.Xunit;
using Serialisation.Domain;
using Xunit;

namespace Serialisation.Tests;

[Reproducible]
public sealed class PackAndShipmentTests {

    [Fact]
    public void The_check_digit_of_a_known_gtin_body_is_appended() {
        // Act
        Gtin gtin = Gtin.FromBody("0950600013435");

        // Assert
        Assert.Equal("09506000134352", gtin.Value);
    }

    [Fact]
    public void A_gtin_with_a_wrong_check_digit_is_refused() {
        // Act
        Action build = () => new Gtin("09506000134353");

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void The_element_string_carries_the_expiry_as_yymmdd_under_ai_17() {
        // Arrange
        SerialisedPack anyPackExpiringEndOf2026 = Arbitrary.SerialisedPack().WithExpiresOn(new DateOnly(2026, 12, 31)).Generate();

        // Act
        string elementString = anyPackExpiringEndOf2026.ToElementString();

        // Assert
        Assert.Contains("(17)261231(10)", elementString);
    }

    [Fact]
    public void The_element_string_lists_the_four_application_identifiers_in_order() {
        // Arrange
        SerialisedPack pack = new SerialisedPack(new Gtin("09506000134352"), new BatchNumber("LOT-A1"), new DateOnly(2027, 6, 30), new SerialNumber("12345678AB"));

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Equal("(01)09506000134352(17)270630(10)LOT-A1(21)12345678AB", elementString);
    }

    [Fact]
    public void A_shipment_refuses_the_same_serial_twice() {
        // Arrange
        SerialNumber anySerial    = Arbitrary.SerialNumber().Generate();
        string       anyReference = Any.String().StartingWith("SHP-").Numeric().WithLength(12).Generate();
        DateOnly     anyShippedOn = Any.DateOnly().Generate();

        // Act
        Action build = () => new Shipment(anyReference, [anySerial, anySerial], anyShippedOn);

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_shipment_flags_the_serials_its_source_batch_never_commissioned() {
        // Arrange
        Batch              anyBatch   = Arbitrary.Batch().WithPlannedQuantity(q => q.GreaterThanOrEqualTo(2)).Generate();
        List<SerialNumber> anySerials = Any.ListOf(Arbitrary.SerialNumber()).WithCount(3).Distinct().Generate();
        anyBatch.Commission(anySerials[0]);
        anyBatch.Commission(anySerials[1]);
        SerialNumber forged = anySerials[2];
        Shipment anyShipment = Arbitrary.Shipment()
                                        .WithSerials([anySerials[0], forged])
                                        .WithShippedOn(Any.DateOnly().Between(anyBatch.ManufacturedOn, anyBatch.ExpiresOn).Generate())
                                        .Generate();

        // Act
        IReadOnlyList<SerialNumber> suspicious = anyShipment.SuspiciousIn(anyBatch);

        // Assert
        Assert.Equal([forged], suspicious);
    }

}
