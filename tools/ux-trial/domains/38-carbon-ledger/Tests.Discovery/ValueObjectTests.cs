using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class ValueObjectTests {

    [Fact]
    public void A_canonical_serial_parses_into_its_four_parts() {
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
    public void A_serial_is_written_with_zero_padded_project_and_unit_numbers() {
        // Arrange
        CreditSerial serial = new("GS", 7, 2021, 42);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("GS-0007-2021-000000000042", text);
    }

    [Fact]
    public void Two_serials_with_the_same_parts_are_the_same_unit() {
        // Arrange
        CreditSerial first  = new("VCS", 1234, 2019, 42);
        CreditSerial second = new("VCS", 1234, 2019, 42);

        // Act
        bool same = first.Equals(second);

        // Assert
        Assert.True(same);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_holders_legal_name_is_stored_trimmed() {
        // Arrange
        Holder anyHolder = Any.Holder().Generate();

        // Act
        Holder holder = new(anyHolder.HolderId, "  Acme Forestry  ", anyHolder.CountryCode);

        // Assert
        Assert.Equal("Acme Forestry", holder.LegalName);
    }

    [Fact]
    public void An_account_id_not_prefixed_with_its_holders_country_is_refused() {
        // Arrange
        Holder         anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();
        AccountKind    anyKind         = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpenedOn     = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new Account("DE-000123", anyFrenchHolder, anyKind, anyOpenedOn));

        // Assert
        Assert.Equal("accountId", error.ParamName);
    }

    [Fact]
    public void A_holding_period_cannot_end_before_it_starts() {
        // Arrange
        CreditSerial   anySerial    = Any.CreditSerial().Generate();
        Account        anyAccount   = Any.Account().Generate();
        DateTimeOffset anyFrom      = Any.DateTimeOffset().After(DateTimeOffset.MinValue.AddDays(1)).Generate();
        DateTimeOffset dayBeforeFrom = anyFrom.AddDays(-1);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new HoldingPeriod(anySerial, anyAccount.AccountId, anyFrom, dayBeforeFrom));

        // Assert
        Assert.Equal("until", error.ParamName);
    }

}
