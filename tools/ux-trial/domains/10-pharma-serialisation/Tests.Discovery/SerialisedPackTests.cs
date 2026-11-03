using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class SerialisedPackTests {

    [Fact]
    public void A_new_pack_keeps_what_it_was_built_from_and_starts_active() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        DateOnly     expiresOn = new DateOnly(2027, 3, 31);

        // Act
        SerialisedPack pack = new SerialisedPack(anyGtin, anyBatch, expiresOn, anySerial);

        // Assert
        Assert.Same(anyGtin, pack.Gtin);
        Assert.Same(anyBatch, pack.Batch);
        Assert.Same(anySerial, pack.Serial);
        Assert.Equal(expiresOn, pack.ExpiresOn);
        Assert.Equal(PackStatus.Active, pack.Status);
    }

    [Fact]
    public void The_element_string_chains_the_four_application_identifiers() {
        // Arrange: the string is exactly what is asserted, so every part is a literal
        SerialisedPack pack = new SerialisedPack(new Gtin("04006381333931"), new BatchNumber("LOT-7"), new DateOnly(2027, 3, 31), new SerialNumber("ABC12345xyz"));

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Equal("(01)04006381333931(17)270331(10)LOT-7(21)ABC12345xyz", elementString);
    }

    [Fact]
    public void The_expiry_is_printed_as_a_two_digit_year_month_and_day() {
        // Arrange
        SerialisedPack pack = new SerialisedPack(new Gtin("04006381333931"), new BatchNumber("B"), new DateOnly(2005, 1, 9), new SerialNumber("ABC12345"));

        // Assert
        Assert.Contains("(17)050109(", pack.ToElementString());
    }

    [Fact]
    public void A_pack_without_a_GTIN_is_refused() {
        // Arrange
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act / Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(null!, anyBatch, new DateOnly(2027, 3, 31), anySerial));
        Assert.Equal("gtin", exception.ParamName);
    }

    [Fact]
    public void A_pack_without_a_batch_is_refused() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act / Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, null!, new DateOnly(2027, 3, 31), anySerial));
        Assert.Equal("batch", exception.ParamName);
    }

    [Fact]
    public void A_pack_without_a_serial_is_refused() {
        // Arrange
        Gtin        anyGtin  = new AnyGtin().Generate();
        BatchNumber anyBatch = new AnyBatchNumber().Generate();

        // Act / Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, anyBatch, new DateOnly(2027, 3, 31), null!));
        Assert.Equal("serial", exception.ParamName);
    }

}
