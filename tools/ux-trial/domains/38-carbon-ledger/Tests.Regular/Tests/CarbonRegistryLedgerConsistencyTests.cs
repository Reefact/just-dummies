using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class CarbonRegistryLedgerConsistencyTests {

    [Fact]
    public void Balances_always_sum_to_zero_through_a_mix_of_operations() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(6);
        CreditSerial[]                    all      = serials.ToArray();

        // Act
        scenario.Ledger.Issue(all.Take(4).ToArray(), scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Issue(all.Skip(4).ToArray(), scenario.Bob.AccountId, scenario.Day(2));
        scenario.Ledger.Transfer(all.Take(2).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(3));
        scenario.Ledger.Retire(new[] { all[1], all[4] }, scenario.Bob.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(4));

        // Assert
        int sum = scenario.Ledger.Accounts.Sum(account => scenario.Ledger.Balance(account.AccountId));
        Assert.Equal(0, sum);
        Assert.Equal(-6, scenario.Ledger.Balance(scenario.Issuance.AccountId));
        Assert.Equal(6, scenario.Ledger.IssuedQuantity);
        Assert.Equal(2, scenario.Ledger.RetiredQuantity);
        Assert.Equal(4, scenario.Ledger.Journal.Count);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void Each_account_balance_equals_the_number_of_units_it_holds() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial[] all      = scenario.NewSerials(5).ToArray();
        scenario.Ledger.Issue(all, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(all.Take(3).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));
        scenario.Ledger.Retire(all.Take(1).ToArray(), scenario.Bob.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(3));

        // Act / Assert
        foreach (Account account in scenario.Ledger.Accounts.Where(a => a.Kind != AccountKind.Issuance)) {
            Assert.Equal(scenario.Ledger.UnitsHeldBy(account.AccountId).Count, scenario.Ledger.Balance(account.AccountId));
        }
    }

    [Fact]
    public void The_journal_replays_to_the_current_holders() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial[] all      = scenario.NewSerials(4).ToArray();
        scenario.Ledger.Issue(all, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(all.Take(2).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));
        scenario.Ledger.Retire(all.Take(1).ToArray(), scenario.Bob.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(3));

        // Act
        Dictionary<CreditSerial, string> replayed = new Dictionary<CreditSerial, string>();
        foreach (JournalEntry entry in scenario.Ledger.Journal) {
            foreach (CreditSerial serial in entry.Serials) {
                replayed[serial] = entry.Credit.AccountId;
            }
        }

        // Assert
        Assert.Equal(all.Length, replayed.Count);
        Assert.All(all, unit => Assert.Equal(replayed[unit], scenario.Ledger.HolderOf(unit).AccountId));
    }

    [Fact]
    public void A_refused_operation_does_not_alter_the_consistency() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial[] all      = scenario.NewSerials(2).ToArray();
        scenario.Ledger.Issue(all, scenario.Alice.AccountId, scenario.Day(1));

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(all, scenario.Bob.AccountId, scenario.Day(2)));
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Transfer(all, scenario.Bob.AccountId, scenario.Alice.AccountId, scenario.Day(2)));

        // Assert
        Assert.True(scenario.Ledger.IsConsistent());
        Assert.Single(scenario.Ledger.Journal);
    }

}
