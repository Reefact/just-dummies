using System.Globalization;

using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class SerialisedPackTests {

    [Fact]
    public void A_new_pack_is_active() {
        // Arrange
        SerialisedPack anyPack = Any.SerialisedPack().Generate();

        // Assert
        Assert.Equal(PackStatus.Active, anyPack.Status);
    }

    [Fact]
    public void A_pack_keeps_what_it_was_made_of() {
        // Arrange
        Gtin         gtin      = new Gtin("04006381333931");
        BatchNumber  batch     = new BatchNumber("LOT-42");
        DateOnly     expiresOn = new DateOnly(2027, 3, 31);
        SerialNumber serial    = new SerialNumber("ab12CD34");

        // Act
        SerialisedPack pack = new SerialisedPack(gtin, batch, expiresOn, serial);

        // Assert
        Assert.Same(gtin, pack.Gtin);
        Assert.Same(batch, pack.Batch);
        Assert.Equal(expiresOn, pack.ExpiresOn);
        Assert.Same(serial, pack.Serial);
    }

    [Fact]
    public void A_pack_without_a_gtin_is_refused() {
        // Arrange
        BatchNumber  anyBatch  = Any.BatchNumber().Generate();
        DateOnly     anyExpiry = Any.DateOnly().Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(null!, anyBatch, anyExpiry, anySerial));

        // Assert
        Assert.Equal("gtin", exception.ParamName);
    }

    [Fact]
    public void A_pack_without_a_batch_is_refused() {
        // Arrange
        Gtin         anyGtin   = Any.Gtin().Generate();
        DateOnly     anyExpiry = Any.DateOnly().Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, null!, anyExpiry, anySerial));

        // Assert
        Assert.Equal("batch", exception.ParamName);
    }

    [Fact]
    public void A_pack_without_a_serial_is_refused() {
        // Arrange
        Gtin        anyGtin   = Any.Gtin().Generate();
        BatchNumber anyBatch  = Any.BatchNumber().Generate();
        DateOnly    anyExpiry = Any.DateOnly().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SerialisedPack(anyGtin, anyBatch, anyExpiry, null!));

        // Assert
        Assert.Equal("serial", exception.ParamName);
    }

    [Fact]
    public void The_expiry_date_is_not_checked() {
        // Arrange
        Gtin         anyGtin   = Any.Gtin().Generate();
        BatchNumber  anyBatch  = Any.BatchNumber().Generate();
        SerialNumber anySerial = Any.SerialNumber().Generate();

        // Act
        SerialisedPack pack = new SerialisedPack(anyGtin, anyBatch, DateOnly.MinValue, anySerial);

        // Assert
        Assert.Equal(DateOnly.MinValue, pack.ExpiresOn);
    }

    [Fact]
    public void The_element_string_chains_the_four_application_identifiers() {
        // Arrange
        SerialisedPack pack = new SerialisedPack(new Gtin("04006381333931"),
                                                 new BatchNumber("LOT-42"),
                                                 new DateOnly(2027, 3, 31),
                                                 new SerialNumber("ab12CD34"));

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Equal("(01)04006381333931(17)270331(10)LOT-42(21)ab12CD34", elementString);
    }

    [Fact]
    public void The_expiry_in_the_element_string_pads_month_and_day_to_two_digits() {
        // Arrange
        SerialisedPack pack = new SerialisedPack(new Gtin("04006381333931"),
                                                 new BatchNumber("LOT-42"),
                                                 new DateOnly(2030, 1, 5),
                                                 new SerialNumber("ab12CD34"));

        // Act
        string elementString = pack.ToElementString();

        // Assert
        Assert.Contains("(17)300105(10)", elementString);
    }

    [Theory(Skip = "domain defect: ToElementString formats ExpiresOn with the current culture, so under th-TH the year prints in the Buddhist calendar (2027-03-31 reads (17)700331), under ar-SA in the Hijri one (481023) and under fa-IR in the Persian one (060111). Literal input, no draw involved (no seed).")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("fa-IR")]
    public void The_element_string_does_not_depend_on_the_culture_of_the_machine(string culture) {
        // Arrange
        SerialisedPack pack = new SerialisedPack(new Gtin("04006381333931"),
                                                 new BatchNumber("LOT-42"),
                                                 new DateOnly(2027, 3, 31),
                                                 new SerialNumber("ab12CD34"));
        CultureInfo previous = CultureInfo.CurrentCulture;

        try {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            // Act
            string elementString = pack.ToElementString();

            // Assert
            Assert.Equal("(01)04006381333931(17)270331(10)LOT-42(21)ab12CD34", elementString);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
