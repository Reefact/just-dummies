using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class JournalEntryTests {

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice() {
        // Arrange
        Account        anyFrom   = Any.Account().WithKind(AccountKind.Holding).Generate();
        Account        anyTo     = Any.Account().WithKind(AccountKind.Holding).Generate();
        CreditSerial   anySerial = Any.CreditSerial().Generate();
        DateTimeOffset anyAt     = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyAt, EntryKind.Transfer, anyFrom, anyTo, [anySerial, anySerial], null));

        // Assert
        Assert.Equal("serials", error.ParamName);
    }

    [Fact]
    public void Only_a_retirement_carries_a_claim() {
        // Arrange
        Account            anyFrom    = Any.Account().WithKind(AccountKind.Holding).Generate();
        Account            anyTo      = Any.Account().WithKind(AccountKind.Holding).Generate();
        CreditSerial       anySerial  = Any.CreditSerial().Generate();
        RetirementClaim    anyClaim   = Any.RetirementClaim().Generate();
        DateTimeOffset     anyAt      = Any.DateTimeOffset().Generate();
        long               anySequence = Any.Int64().Positive().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new JournalEntry(anySequence, anyAt, EntryKind.Transfer, anyFrom, anyTo, [anySerial], anyClaim));

        // Assert
        Assert.Equal("claim", error.ParamName);
    }

    [Fact]
    public void An_entry_moves_as_many_units_as_it_lists() {
        // Arrange
        Account             anyIssuance = Any.Account().WithKind(AccountKind.Issuance).Generate();
        Account             anyHolding  = Any.Account().WithKind(AccountKind.Holding).Generate();
        List<CreditSerial>  anySerials  = Any.ListOf(Any.CreditSerial()).WithCount(4).Distinct().Generate();
        DateTimeOffset      anyAt       = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new(1, anyAt, EntryKind.Issuance, anyIssuance, anyHolding, anySerials, null);

        // Assert
        Assert.Equal(4, entry.Quantity);
    }

}
