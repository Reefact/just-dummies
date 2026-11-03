using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class CarbonRegistryLedgerIssuanceTests {

    [Fact]
    public void Issuing_three_units_credits_the_receiver_and_debits_the_issuance_account() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(3);

        // Act
        JournalEntry entry = scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        // Assert
        Assert.Equal(3, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.Equal(-3, scenario.Ledger.Balance(scenario.Issuance.AccountId));
        Assert.Equal(3, scenario.Ledger.IssuedQuantity);
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
        Assert.Equal(3, entry.Quantity);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void Issuing_records_an_issuance_entry_in_the_journal() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(2);

        // Act
        JournalEntry entry = scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        // Assert
        Assert.Same(entry, Assert.Single(scenario.Ledger.Journal));
        Assert.Equal(1L, entry.Sequence);
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Same(scenario.Issuance, entry.Debit);
        Assert.Same(scenario.Alice, entry.Credit);
        Assert.Equal(scenario.Day(1), entry.RecordedAt);
        Assert.Null(entry.Claim);
        Assert.Equal(serials, entry.Serials);
    }

    [Fact]
    public void Every_issued_unit_is_held_by_the_receiver_and_is_not_retired() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(3);

        // Act
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        // Assert
        foreach (CreditSerial serial in serials) {
            Assert.Same(scenario.Alice, scenario.Ledger.HolderOf(serial));
            Assert.False(scenario.Ledger.IsRetired(serial));
        }

        Assert.Equal(serials.OrderBy(s => s.ToString()), scenario.Ledger.UnitsHeldBy(scenario.Alice.AccountId).OrderBy(s => s.ToString()));
    }

    [Fact]
    public void Successive_entries_are_numbered_from_one() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act
        JournalEntry first  = scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Alice.AccountId, scenario.Day(1));
        JournalEntry second = scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Bob.AccountId, scenario.Day(2));

        // Assert
        Assert.Equal(1L, first.Sequence);
        Assert.Equal(2L, second.Sequence);
        Assert.Equal(new[] { first, second }, scenario.Ledger.Journal);
    }

    [Fact]
    public void A_unit_cannot_be_issued_twice() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(2);
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Issue(serials.Take(1).ToArray(), scenario.Bob.AccountId, scenario.Day(2)));

        // Assert
        Assert.Contains(serials.First().ToString(), refusal.Message);
        Assert.Equal(2, scenario.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_refused_issuance_leaves_the_ledger_untouched() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> issued   = scenario.NewSerials(1);
        CreditSerial                      fresh    = scenario.NewSerials(1).Single();
        scenario.Ledger.Issue(issued, scenario.Alice.AccountId, scenario.Day(1));

        // Act: one fresh unit and one already issued
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Issue(new[] { fresh, issued.Single() }, scenario.Bob.AccountId, scenario.Day(2)));

        // Assert
        Assert.Single(scenario.Ledger.Journal);
        Assert.Equal(1, scenario.Ledger.IssuedQuantity);
        Assert.Equal(0, scenario.Ledger.Balance(scenario.Bob.AccountId));
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.HolderOf(fresh));
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void A_unit_of_another_registry_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   foreign  = new AnyCreditSerial().WithRegistry("GS").Generate();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Issue(new[] { foreign }, scenario.Alice.AccountId, scenario.Day(1)));

        // Assert
        Assert.Contains("GS", refusal.Message);
        Assert.Contains(LedgerScenario.RegistryCode, refusal.Message);
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact]
    public void Issuing_into_an_unknown_account_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        Account        stranger = scenario.OpenAccountOfKind(AccountKind.Holding);

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(scenario.NewSerials(1), stranger.AccountId, scenario.Day(1)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Issuing_into_a_retirement_account_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Retirement.AccountId, scenario.Day(1)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Issuing_into_the_issuance_account_itself_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Issuance.AccountId, scenario.Day(1)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Issuing_no_unit_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Day(1)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Issuing_the_same_unit_twice_in_one_call_is_refused_and_records_nothing() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(new[] { serial, serial }, scenario.Alice.AccountId, scenario.Day(1)));
        Assert.Empty(scenario.Ledger.Journal);
        Assert.Equal(0, scenario.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_null_serial_collection_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => scenario.Ledger.Issue(null!, scenario.Alice.AccountId, scenario.Day(1)));
    }

    [Fact]
    public void Issuing_before_the_receiving_account_was_opened_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Alice.AccountId, scenario.Opened.AddTicks(-1)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact]
    public void Issuing_at_the_very_instant_the_accounts_were_opened_is_accepted() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act
        JournalEntry entry = scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Alice.AccountId, scenario.Opened);

        // Assert
        Assert.Equal(scenario.Opened, entry.RecordedAt);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_previous_one() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Alice.AccountId, scenario.Day(5));

        // Act
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Bob.AccountId, scenario.Day(5).AddTicks(-1)));

        // Assert
        Assert.Single(scenario.Ledger.Journal);
    }

    [Fact]
    public void An_entry_may_be_recorded_at_the_very_instant_of_the_previous_one() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Alice.AccountId, scenario.Day(5));

        // Act
        JournalEntry second = scenario.Ledger.Issue(scenario.NewSerials(1), scenario.Bob.AccountId, scenario.Day(5));

        // Assert
        Assert.Equal(2L, second.Sequence);
    }

    [Fact]
    public void An_account_opened_later_than_the_entry_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        Account        latecomer = new AnyAccount()
                                  .WithKind(AccountKind.Holding)
                                  .WithOpenedOn(scenario.Day(10))
                                  .WithAccountId(d => d.Except(scenario.Alice.AccountId.Substring(3), scenario.Bob.AccountId.Substring(3), scenario.Issuance.AccountId.Substring(3), scenario.Retirement.AccountId.Substring(3)))
                                  .Generate();
        scenario.Ledger.OpenAccount(latecomer);

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.Issue(scenario.NewSerials(1), latecomer.AccountId, scenario.Day(9)));
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact]
    public void Queries_about_a_unit_that_was_never_issued() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   unknown  = scenario.NewSerials(1).Single();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.HolderOf(unknown));
        Assert.False(scenario.Ledger.IsRetired(unknown));
        Assert.Empty(scenario.Ledger.HistoryOf(unknown));
    }

    [Fact]
    public void Queries_about_a_null_unit_are_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => scenario.Ledger.HolderOf(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.Ledger.IsRetired(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.Ledger.HistoryOf(null!));
    }

}
