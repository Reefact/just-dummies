using System;
using System.Collections.Generic;

using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular;

public sealed class JournalEntryTests {

    private static AnyAccount AnAccount(AccountKind kind) => Arbitrary.Account().WithKind(kind);

    private static IAny<List<CreditSerial>> SomeUnits() => Any.ListOf(Arbitrary.CreditSerial()).WithCountBetween(1, 5).Distinct();

    [Fact]
    public void An_entry_moves_as_many_units_as_it_lists() {
        // Arrange
        Account            anyHolding    = AnAccount(AccountKind.Holding).Generate();
        Account            anyOtherHolding = AnAccount(AccountKind.Holding).Generate();
        List<CreditSerial> anyThreeUnits = Any.ListOf(Arbitrary.CreditSerial()).WithCount(3).Distinct().Generate();
        DateTimeOffset     anyAt         = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new JournalEntry(1, anyAt, EntryKind.Transfer, anyHolding, anyOtherHolding, anyThreeUnits, null);

        // Assert
        Assert.Equal(3, entry.Quantity);
    }

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice() {
        // Arrange
        Account        anyHolding      = AnAccount(AccountKind.Holding).Generate();
        Account        anyOtherHolding = AnAccount(AccountKind.Holding).Generate();
        CreditSerial   anyUnit         = Arbitrary.CreditSerial().Generate();
        DateTimeOffset anyAt           = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(
            () => new JournalEntry(1, anyAt, EntryKind.Transfer, anyHolding, anyOtherHolding, [anyUnit, anyUnit], null));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("serials", argumentError.ParamName);
    }

    [Fact]
    public void A_transfer_cannot_start_from_the_issuance_account() {
        // Arrange
        Account            anyIssuance = AnAccount(AccountKind.Issuance).Generate();
        Account            anyHolding  = AnAccount(AccountKind.Holding).Generate();
        List<CreditSerial> anyUnits    = SomeUnits().Generate();
        DateTimeOffset     anyAt       = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(
            () => new JournalEntry(1, anyAt, EntryKind.Transfer, anyIssuance, anyHolding, anyUnits, null));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("kind", argumentError.ParamName);
    }

    [Fact]
    public void A_retirement_must_state_its_claim() {
        // Arrange
        Account            anyHolding    = AnAccount(AccountKind.Holding).Generate();
        Account            anyRetirement = AnAccount(AccountKind.Retirement).Generate();
        List<CreditSerial> anyUnits      = SomeUnits().Generate();
        DateTimeOffset     anyAt         = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(
            () => new JournalEntry(1, anyAt, EntryKind.Retirement, anyHolding, anyRetirement, anyUnits, null));

        // Assert
        ArgumentNullException argumentError = Assert.IsType<ArgumentNullException>(error);
        Assert.Equal("claim", argumentError.ParamName);
    }

    [Fact]
    public void A_transfer_cannot_carry_a_retirement_claim() {
        // Arrange
        Account            anyHolding      = AnAccount(AccountKind.Holding).Generate();
        Account            anyOtherHolding = AnAccount(AccountKind.Holding).Generate();
        List<CreditSerial> anyUnits        = SomeUnits().Generate();
        RetirementClaim    anyClaim        = Arbitrary.RetirementClaim().Generate();
        DateTimeOffset     anyAt           = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(
            () => new JournalEntry(1, anyAt, EntryKind.Transfer, anyHolding, anyOtherHolding, anyUnits, anyClaim));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("claim", argumentError.ParamName);
    }

    [Fact]
    public void Units_of_a_vintage_cannot_offset_emissions_reported_for_an_earlier_year() {
        // Arrange
        Account         anyHolding      = AnAccount(AccountKind.Holding).Generate();
        Account         anyRetirement   = AnAccount(AccountKind.Retirement).Generate();
        CreditSerial    anyUnitOf2020   = Arbitrary.CreditSerial().WithVintage(2020).Generate();
        RetirementClaim anyClaimFor2019 = Arbitrary.RetirementClaim().WithReportingYear(2019).Generate();
        DateTimeOffset  anyAt           = Any.DateTimeOffset().Generate();

        // Act
        Exception? error = Record.Exception(
            () => new JournalEntry(1, anyAt, EntryKind.Retirement, anyHolding, anyRetirement, [anyUnitOf2020], anyClaimFor2019));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("claim", argumentError.ParamName);
    }

}
