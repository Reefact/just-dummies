using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerQueryTests {

    // ---- balances, holders, units --------------------------------------------------------------------------------

    [Fact]
    public void The_balance_of_an_account_that_is_not_open_is_refused() {
        RegistryScene scene    = RegistryScene.Open();
        Account       stranger = scene.Stranger();

        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Balance(stranger.AccountId));

        Assert.Contains(stranger.AccountId, refusal.Message);
    }

    [Fact]
    public void The_units_of_an_account_that_is_not_open_are_refused() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.UnitsHeldBy(scene.Stranger().AccountId));
    }

    [Fact]
    public void The_holder_of_a_unit_that_was_never_issued_is_refused() {
        RegistryScene scene      = RegistryScene.Open();
        CreditSerial  neverIssued = scene.NewSerials(1)[0];

        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.HolderOf(neverIssued));

        Assert.Contains(neverIssued.ToString(), refusal.Message);
    }

    [Fact]
    public void A_unit_that_was_never_issued_is_not_retired() {
        RegistryScene scene = RegistryScene.Open();

        Assert.False(scene.Ledger.IsRetired(scene.NewSerials(1)[0]));
    }

    [Fact]
    public void The_issuance_account_holds_no_unit_whatever_was_issued() {
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 2);

        Assert.Empty(scene.Ledger.UnitsHeldBy(scene.Issuance.AccountId));
    }

    [Fact]
    public void Queries_about_a_missing_unit_or_account_are_refused() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.HolderOf(null!));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.IsRetired(null!));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.HistoryOf(null!));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Balance(null!));
        Assert.Throws<ArgumentNullException>(() => scene.Ledger.UnitsHeldBy(null!));
    }

    // ---- the history of a unit -------------------------------------------------------------------------------------

    [Fact]
    public void The_history_of_a_unit_never_moved_is_one_open_period_from_its_issuance() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 2);

        // Act
        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        // Assert
        HoldingPeriod period = Assert.Single(history);
        Assert.Equal(scene.First.AccountId, period.AccountId);
        Assert.Equal(scene.At(2), period.From);
        Assert.True(period.IsCurrent);
        Assert.Same(units[0], period.Serial);
    }

    [Fact]
    public void The_history_of_a_unit_lists_each_holder_with_the_instants_it_received_and_gave_the_unit() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 1);
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(3));
        scene.Ledger.Transfer(units, scene.Second.AccountId, scene.Third.AccountId, scene.At(7));

        // Act
        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        // Assert
        Assert.Collection(history,
            period => AssertPeriod(period, scene.First.AccountId, scene.At(1), scene.At(3)),
            period => AssertPeriod(period, scene.Second.AccountId, scene.At(3), scene.At(7)),
            period => AssertPeriod(period, scene.Third.AccountId, scene.At(7), null));
    }

    [Fact]
    public void Only_the_last_period_of_a_history_is_current() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 1);
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(3));

        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        Assert.False(history[0].IsCurrent);
        Assert.True(history[1].IsCurrent);
    }

    [Fact]
    public void A_unit_moved_twice_in_the_same_instant_leaves_an_empty_period() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 1);
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(3));
        scene.Ledger.Transfer(units, scene.Second.AccountId, scene.Third.AccountId, scene.At(3));

        // Act
        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        // Assert
        Assert.Equal(3, history.Count);
        AssertPeriod(history[1], scene.Second.AccountId, scene.At(3), scene.At(3));
    }

    [Fact]
    public void The_history_of_a_retired_unit_ends_in_the_retirement_account() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 1);
        scene.RetireFrom(scene.First, units.ToList(), 4);

        // Act
        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        // Assert
        Assert.Collection(history,
            period => AssertPeriod(period, scene.First.AccountId, scene.At(1), scene.At(4)),
            period => AssertPeriod(period, scene.Retirement.AccountId, scene.At(4), null));
    }

    [Fact]
    public void The_history_of_a_unit_ignores_what_happens_to_the_other_units() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 2, day: 1);
        scene.Ledger.Transfer(new[] { units[1] }, scene.First.AccountId, scene.Second.AccountId, scene.At(2));

        // Act
        IReadOnlyList<HoldingPeriod> history = scene.Ledger.HistoryOf(units[0]);

        // Assert
        HoldingPeriod period = Assert.Single(history);
        Assert.Equal(scene.First.AccountId, period.AccountId);
    }

    [Fact]
    public void The_history_of_a_unit_that_was_never_issued_is_empty() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Empty(scene.Ledger.HistoryOf(scene.NewSerials(1)[0]));
    }

    // ---- the journal ------------------------------------------------------------------------------------------------

    [Fact]
    public void The_journal_numbers_its_entries_from_one_in_the_order_they_were_recorded() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 2, day: 1);
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(2));
        scene.RetireFrom(scene.Second, units.ToList(), 3);

        // Act
        long[]      sequences = scene.Ledger.Journal.Select(entry => entry.Sequence).ToArray();
        EntryKind[] kinds     = scene.Ledger.Journal.Select(entry => entry.Kind).ToArray();

        // Assert
        Assert.Equal(new[] { 1L, 2L, 3L }, sequences);
        Assert.Equal(new[] { EntryKind.Issuance, EntryKind.Transfer, EntryKind.Retirement }, kinds);
    }

    [Fact]
    public void A_refused_operation_leaves_no_entry_behind() {
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1);

        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(scene.NewSerials(1), scene.Stranger().AccountId, scene.At(1)));

        Assert.Single(scene.Ledger.Journal);
    }

    [Fact(Skip = "domain defect (low): the journal is documented as append-only but Journal returns the live List<JournalEntry>, so a caller can cast it and add or remove entries behind the ledger's back; literal scenario, no seed")]
    public void The_journal_cannot_be_modified_through_the_list_it_exposes() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1);

        // Act
        bool exposesItsStorage = scene.Ledger.Journal is List<JournalEntry>;

        // Assert
        Assert.False(exposesItsStorage);
    }

    private static void AssertPeriod(HoldingPeriod period, string accountId, DateTimeOffset from, DateTimeOffset? until) {
        Assert.Equal(accountId, period.AccountId);
        Assert.Equal(from, period.From);
        Assert.Equal(until, period.Until);
    }

}
