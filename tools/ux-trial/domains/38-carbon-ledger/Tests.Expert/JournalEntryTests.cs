using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert;

public sealed class JournalEntryTests {

    [Fact]
    public void An_entry_moves_as_many_units_as_it_lists() {
        // Arrange
        (Account anySeller, Account anyBuyer) = TwoHoldingAccounts();
        HashSet<CreditSerial>                 anyUnits = Any.SetOf(Any.CreditSerial()).WithCount(4).Generate();

        DateTimeOffset anyRecordedAt = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new(1, anyRecordedAt, EntryKind.Transfer, anySeller, anyBuyer, anyUnits, null);

        // Assert
        Assert.Equal(4, entry.Quantity);
    }

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice() {
        // Arrange
        (Account anySeller, Account anyBuyer) = TwoHoldingAccounts();
        CreditSerial                          anyUnit = Any.CreditSerial().Generate();
        CreditSerial[]                        twice   = [anyUnit, anyUnit];

        DateTimeOffset anyRecordedAt = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyRecordedAt, EntryKind.Transfer, anySeller, anyBuyer, twice, null));

        // Assert
        Assert.Equal("serials", error.ParamName);
    }

    [Fact]
    public void A_transfer_cannot_carry_a_retirement_claim() {
        // Arrange
        (Account anySeller, Account anyBuyer) = TwoHoldingAccounts();
        HashSet<CreditSerial>                 anyUnits = Any.SetOf(Any.CreditSerial()).NonEmpty().WithMaxCount(5).Generate();
        RetirementClaim                       anyClaim = Any.RetirementClaim().ValidFor(anyUnits).Generate();

        DateTimeOffset anyRecordedAt = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyRecordedAt, EntryKind.Transfer, anySeller, anyBuyer, anyUnits, anyClaim));

        // Assert
        Assert.Equal("claim", error.ParamName);
    }

    [Fact]
    public void A_retirement_cannot_offset_emissions_reported_before_the_vintage_of_its_units() {
        // Arrange
        Account         anyHolding    = Any.Account().WithKind(AccountKind.Holding).Generate();
        Account         anyRetirement = Any.Account().WithKind(AccountKind.Retirement).Generate();
        CreditSerial    vintage2022   = Any.CreditSerial().WithVintage(2022).Generate();
        RetirementClaim claimFor2021  = Any.RetirementClaim().WithReportingYear(2021).Generate();

        DateTimeOffset anyRecordedAt = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyRecordedAt, EntryKind.Retirement, anyHolding, anyRetirement, [vintage2022], claimFor2021));

        // Assert
        Assert.Contains("vintage 2022", error.Message);
        Assert.Contains("2021", error.Message);
    }

    [Fact]
    public void A_transfer_moves_units_between_holding_accounts_only() {
        // Arrange
        Account               anyHolding    = Any.Account().WithKind(AccountKind.Holding).Generate();
        Account               anyRetirement = Any.Account().WithKind(AccountKind.Retirement).Generate();
        HashSet<CreditSerial> anyUnits      = Any.SetOf(Any.CreditSerial()).NonEmpty().WithMaxCount(5).Generate();

        DateTimeOffset anyRecordedAt = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyRecordedAt, EntryKind.Transfer, anyHolding, anyRetirement, anyUnits, null));

        // Assert
        Assert.Equal("kind", error.ParamName);
    }

    // Two holding accounts are distinct by id: Account has reference equality, so the comparer says what "same" means.
    private static (Account, Account) TwoHoldingAccounts() {
        List<Account> accounts = Any.ListOf(Any.Account().WithKind(AccountKind.Holding))
                                    .WithCount(2)
                                    .Distinct(CarbonRecipes.SameAccountId)
                                    .Generate();

        return (accounts[0], accounts[1]);
    }

}
