using System;

using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void A_credit_serial_prints_in_the_registry_canonical_form() {
        // Arrange
        CreditSerial serial = new CreditSerial("VCS", 1234, 2019, 42);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("VCS-1234-2019-000000000042", text);
    }

    [Fact]
    public void Parsing_the_canonical_form_gives_back_the_same_unit() {
        // Arrange
        const string text = "GS-004711-2022-000000001337";

        // Act
        CreditSerial serial = CreditSerial.Parse(text);

        // Assert
        Assert.Equal(new CreditSerial("GS", 4711, 2022, 1337), serial);
    }

    [Fact]
    public void A_text_that_is_not_a_serial_cannot_be_parsed() {
        // Arrange
        const string text = "VCS-1234-19-42";

        // Act
        Exception? error = Record.Exception(() => CreditSerial.Parse(text));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void An_account_must_be_numbered_in_its_holder_country() {
        // Arrange
        Holder         anyFrenchHolder = Arbitrary.Holder().WithCountryCode("FR").Generate();
        DateTimeOffset anyOpenedOn     = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(() => new Account("DE-000123", anyFrenchHolder, AccountKind.Holding, anyOpenedOn));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("accountId", argumentError.ParamName);
    }

    [Fact]
    public void A_holder_legal_name_is_kept_without_surrounding_spaces() {
        // Arrange
        Holder anyHolder = Arbitrary.Holder().WithLegalName("  Acme Offsets SA  ").Generate();

        // Act
        string legalName = anyHolder.LegalName;

        // Assert
        Assert.Equal("Acme Offsets SA", legalName);
    }

    [Fact]
    public void A_retirement_claim_needs_a_beneficiary() {
        // Arrange
        AnyRetirementClaim claimWithBlankBeneficiary = Arbitrary.RetirementClaim().WithBeneficiary("   ");

        // Act
        Exception? error = Record.Exception(() => claimWithBlankBeneficiary.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("beneficiary", argumentError.ParamName);
    }

    [Fact]
    public void A_holding_period_cannot_end_before_it_starts() {
        // Arrange
        CreditSerial   anySerial    = Arbitrary.CreditSerial().Generate();
        Account        anyAccount   = Arbitrary.Account().Generate();
        DateTimeOffset from         = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset dayBefore    = from.AddDays(-1);

        // Act
        Exception? error = Record.Exception(() => new HoldingPeriod(anySerial, anyAccount.AccountId, from, dayBefore));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
