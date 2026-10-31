using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert;

public sealed class CreditSerialTests {

    [Fact]
    public void Parse_reads_registry_project_vintage_and_unit_from_the_canonical_form() {
        // Arrange
        const string text = "VCS-1234-2019-000000000042";

        // Act
        CreditSerial serial = CreditSerial.Parse(text);

        // Assert
        Assert.Equal("VCS", serial.Registry);
        Assert.Equal(1234, serial.ProjectId);
        Assert.Equal(2019, serial.Vintage);
        Assert.Equal(42L, serial.UnitNumber);
    }

    [Fact]
    public void A_serial_prints_its_unit_number_on_twelve_digits() {
        // Arrange
        CreditSerial serial = new("GS", 77, 2021, 5);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("GS-0077-2021-000000000005", text);
    }

    [Theory]
    [InlineData("VCS-1234-2019-42")]
    [InlineData("vcs-1234-2019-000000000042")]
    [InlineData("VCS-1234-19-000000000042")]
    public void Parse_refuses_a_text_that_is_not_a_serial(string text) {
        // Act
        FormatException error = Assert.Throws<FormatException>(() => CreditSerial.Parse(text));

        // Assert
        Assert.Contains(text, error.Message);
    }

    [Fact]
    public void Two_serials_naming_the_same_unit_are_the_same_credit() {
        // Arrange
        CreditSerial printed = new("VCS", 1234, 2019, 42);
        CreditSerial parsed  = CreditSerial.Parse("VCS-1234-2019-000000000042");

        // Act
        bool equal = printed.Equals(parsed);

        // Assert
        Assert.True(equal);
        Assert.Equal(printed.GetHashCode(), parsed.GetHashCode());
    }

}

public sealed class HolderAndAccountTests {

    [Fact]
    public void A_holder_legal_name_is_stored_trimmed() {
        // Arrange
        AnyHolder anyHolder = Any.Holder();

        // Act
        Holder holder = anyHolder.WithLegalName("  Acme Forestry Ltd  ").Generate();

        // Assert
        Assert.Equal("Acme Forestry Ltd", holder.LegalName);
    }

    [Fact]
    public void An_account_id_must_carry_its_holder_country_prefix() {
        // Arrange
        Holder         anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();
        DateTimeOffset anyOpenedOn     = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new Account("DE-000123", anyFrenchHolder, AccountKind.Holding, anyOpenedOn));

        // Assert
        Assert.Equal("accountId", error.ParamName);
        Assert.Contains("'FR'", error.Message);
    }

    [Fact]
    public void An_account_prefixed_with_its_holder_country_is_opened() {
        // Arrange
        Holder         anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();
        AccountKind    anyKind         = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpenedOn     = Any.DateTimeOffset().Generate();

        // Act
        Account account = new("FR-000123", anyFrenchHolder, anyKind, anyOpenedOn);

        // Assert
        Assert.Equal("FR-000123", account.AccountId);
    }

}
