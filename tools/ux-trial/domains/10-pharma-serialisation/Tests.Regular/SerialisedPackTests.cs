using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class SerialisedPackTests {

    [Fact]
    public void A_new_pack_is_active_and_keeps_what_it_was_given() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        DateOnly     anyExpiry = Any.DateOnly().Generate();

        // Act
        SerialisedPack pack = new SerialisedPack(anyGtin, anyBatch, anyExpiry, anySerial);

        // Assert
        Assert.Equal(PackStatus.Active, pack.Status);
        Assert.Same(anyGtin, pack.Gtin);
        Assert.Same(anyBatch, pack.Batch);
        Assert.Same(anySerial, pack.Serial);
        Assert.Equal(anyExpiry, pack.ExpiresOn);
    }

    [Fact]
    public void A_pack_without_a_gtin_is_refused() {
        // Arrange
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(null!, anyBatch, new DateOnly(2027, 3, 31), anySerial));

        // Assert
        Assert.Equal("gtin", refusal.ParamName);
    }

    [Fact]
    public void A_pack_without_a_batch_is_refused() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, null!, new DateOnly(2027, 3, 31), anySerial));

        // Assert
        Assert.Equal("batch", refusal.ParamName);
    }

    [Fact]
    public void A_pack_without_a_serial_is_refused() {
        // Arrange
        Gtin        anyGtin  = new AnyGtin().Generate();
        BatchNumber anyBatch = new AnyBatchNumber().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, anyBatch, new DateOnly(2027, 3, 31), null!));

        // Assert
        Assert.Equal("serial", refusal.ParamName);
    }

    [Fact]
    public void Any_expiry_date_is_accepted_even_the_oldest() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        // Act
        SerialisedPack pack = new SerialisedPack(anyGtin, anyBatch, DateOnly.MinValue, anySerial);

        // Assert
        Assert.Equal(DateOnly.MinValue, pack.ExpiresOn);
    }

    [Fact]
    public void The_element_string_lists_gtin_expiry_batch_and_serial_with_their_application_identifiers() {
        // Arrange
        Gtin         gtin   = new Gtin("04012345678901");
        BatchNumber  batch  = new BatchNumber("LOT-2024-07");
        SerialNumber serial = new SerialNumber("Ab12Cd34Ef56");

        SerialisedPack pack = new SerialisedPack(gtin, batch, new DateOnly(2027, 3, 31), serial);

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Equal("(01)04012345678901(17)270331(10)LOT-2024-07(21)Ab12Cd34Ef56", elementString);
    }

    [Theory]
    [InlineData(2005, 1, 9, "050109")]
    [InlineData(2099, 12, 31, "991231")]
    [InlineData(2000, 2, 29, "000229")]
    public void The_expiry_is_printed_as_two_digit_year_month_day(int year, int month, int day, string expected) {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        BatchNumber  anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();

        SerialisedPack pack = new SerialisedPack(anyGtin, anyBatch, new DateOnly(year, month, day), anySerial);

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Contains("(17)" + expected + "(10)", elementString);
    }

    [Fact]
    public void The_element_string_carries_the_values_of_the_pack_in_order() {
        // Arrange
        Gtin           anyGtin   = new AnyGtin().Generate();
        BatchNumber    anyBatch  = new AnyBatchNumber().Generate();
        SerialNumber   anySerial = new AnySerialNumber().Generate();
        SerialisedPack pack      = new SerialisedPack(anyGtin, anyBatch, Fixtures.DatesOfTheModernEra().Generate(), anySerial);

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.StartsWith("(01)" + anyGtin.Value + "(17)", elementString);
        Assert.EndsWith("(10)" + anyBatch.Value + "(21)" + anySerial.Value, elementString);
    }

}
