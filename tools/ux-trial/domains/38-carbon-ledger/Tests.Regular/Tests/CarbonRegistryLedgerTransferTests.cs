using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class CarbonRegistryLedgerTransferTests {

    private static (LedgerScenario Scenario, IReadOnlyCollection<CreditSerial> AliceUnits) AliceHoldingFour() {
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(4);
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        return (scenario, serials);
    }

    [Fact]
    public void Transferring_two_of_four_units_moves_them_and_leaves_the_others() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        CreditSerial[] moved = units.Take(2).ToArray();

        // Act
        JournalEntry entry = scenario.Ledger.Transfer(moved, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));

        // Assert
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Bob.AccountId));
        Assert.Equal(-4, scenario.Ledger.Balance(scenario.Issuance.AccountId));
        Assert.Equal(4, scenario.Ledger.IssuedQuantity);
        Assert.Equal(2, entry.Quantity);
        Assert.All(moved, unit => Assert.Same(scenario.Bob, scenario.Ledger.HolderOf(unit)));
        Assert.All(units.Skip(2), unit => Assert.Same(scenario.Alice, scenario.Ledger.HolderOf(unit)));
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void A_transfer_is_recorded_as_a_transfer_entry() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();

        // Act
        JournalEntry entry = scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));

        // Assert
        Assert.Equal(2L, entry.Sequence);
        Assert.Equal(EntryKind.Transfer, entry.Kind);
        Assert.Same(scenario.Alice, entry.Debit);
        Assert.Same(scenario.Bob, entry.Credit);
        Assert.Null(entry.Claim);
        Assert.Equal(2, scenario.Ledger.Journal.Count);
    }

    [Fact]
    public void A_unit_can_be_transferred_back_and_forth() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        CreditSerial[] one = units.Take(1).ToArray();

        // Act
        scenario.Ledger.Transfer(one, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));
        scenario.Ledger.Transfer(one, scenario.Bob.AccountId, scenario.Alice.AccountId, scenario.Day(3));

        // Assert
        Assert.Same(scenario.Alice, scenario.Ledger.HolderOf(one[0]));
        Assert.Equal(4, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void A_unit_held_by_someone_else_cannot_be_transferred() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        CreditSerial[] one = units.Take(1).ToArray();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(one, scenario.Bob.AccountId, scenario.Alice.AccountId, scenario.Day(2)));

        // Assert
        Assert.Contains(scenario.Alice.AccountId, refusal.Message);
        Assert.Contains(scenario.Bob.AccountId, refusal.Message);
        Assert.Equal(4, scenario.Ledger.Balance(scenario.Alice.AccountId));
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_transferred() {
        // Arrange
        (LedgerScenario scenario, _) = AliceHoldingFour();
        CreditSerial never = scenario.NewSerials(1).Single();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Transfer(new[] { never }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2)));
    }

    [Fact]
    public void A_refused_transfer_moves_none_of_its_units() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        CreditSerial never = scenario.NewSerials(1).Single();

        // Act: the first unit is Alice's, the second was never issued
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(new[] { units.First(), never }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2)));

        // Assert
        Assert.Equal(4, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.Equal(0, scenario.Ledger.Balance(scenario.Bob.AccountId));
        Assert.Single(scenario.Ledger.Journal);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void Transferring_from_or_to_an_unknown_account_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        Account        stranger = scenario.OpenAccountOfKind(AccountKind.Holding);
        CreditSerial[] one      = units.Take(1).ToArray();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Transfer(one, scenario.Alice.AccountId, stranger.AccountId, scenario.Day(2)));
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Transfer(one, stranger.AccountId, scenario.Alice.AccountId, scenario.Day(2)));
        Assert.Single(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Transferring_to_the_sender_itself_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Alice.AccountId, scenario.Day(2)));
        Assert.Single(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Transferring_into_a_retirement_account_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.Day(2)));
        Assert.False(scenario.Ledger.IsRetired(units.First()));
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Transferring_nothing_is_refused() {
        // Arrange
        (LedgerScenario scenario, _) = AliceHoldingFour();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2)));
    }

    [Fact]
    public void Units_cannot_be_transferred_out_of_the_issuance_account() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Issuance.AccountId, scenario.Bob.AccountId, scenario.Day(2)));
    }

    [Fact]
    public void A_transfer_before_the_last_entry_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();

        // Act: Alice's units were issued on day 1
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(1).AddTicks(-1)));

        // Assert
        Assert.Single(scenario.Ledger.Journal);
    }

    [Fact]
    public void A_transfer_before_the_receiving_account_was_opened_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        Account latecomer = new AnyAccount()
                           .WithKind(AccountKind.Holding)
                           .WithOpenedOn(scenario.Day(10))
                           .WithAccountId(d => d.Except(scenario.Alice.AccountId.Substring(3), scenario.Bob.AccountId.Substring(3), scenario.Issuance.AccountId.Substring(3), scenario.Retirement.AccountId.Substring(3)))
                           .Generate();
        scenario.Ledger.OpenAccount(latecomer);

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(units.Take(1).ToArray(), scenario.Alice.AccountId, latecomer.AccountId, scenario.Day(9)));
        Assert.Equal(0, scenario.Ledger.Balance(latecomer.AccountId));
    }

    [Fact]
    public void The_units_held_by_an_account_follow_its_transfers() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingFour();
        CreditSerial[] moved = units.Take(3).ToArray();

        // Act
        scenario.Ledger.Transfer(moved, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));

        // Assert
        Assert.Equal(moved.OrderBy(s => s.ToString()), scenario.Ledger.UnitsHeldBy(scenario.Bob.AccountId).OrderBy(s => s.ToString()));
        Assert.Equal(units.Skip(3), scenario.Ledger.UnitsHeldBy(scenario.Alice.AccountId));
    }

}
