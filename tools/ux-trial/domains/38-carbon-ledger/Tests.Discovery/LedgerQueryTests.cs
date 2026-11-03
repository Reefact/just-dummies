using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class LedgerQueryTests {

    [Fact]
    public void The_history_of_a_unit_lists_each_holder_with_the_instants_it_held_the_unit() {
        // Arrange
        LedgerWorld          world   = new LedgerWorld();
        Account              alice   = world.OpenHolding();
        Account              bob     = world.OpenHolding();
        Account              offsets = world.OpenRetirement();
        CreditSerial         serial  = world.NewSerials(1)[0];
        DateTimeOffset       issued  = new DateTimeOffset(2040, 1, 10, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset       sent    = new DateTimeOffset(2040, 2, 20, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset       retired = new DateTimeOffset(2040, 3, 30, 8, 0, 0, TimeSpan.Zero);

        // Act
        world.Ledger.Issue([serial], alice.AccountId, issued);
        world.Ledger.Transfer([serial], alice.AccountId, bob.AccountId, sent);
        world.Ledger.Retire([serial], bob.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering([serial]), retired);
        IReadOnlyList<HoldingPeriod> history = world.Ledger.HistoryOf(serial);

        // Assert
        Assert.Equal(3, history.Count);

        Assert.Equal(alice.AccountId, history[0].AccountId);
        Assert.Equal(issued, history[0].From);
        Assert.Equal(sent, history[0].Until);

        Assert.Equal(bob.AccountId, history[1].AccountId);
        Assert.Equal(sent, history[1].From);
        Assert.Equal(retired, history[1].Until);

        Assert.Equal(offsets.AccountId, history[2].AccountId);
        Assert.Equal(retired, history[2].From);
        Assert.Null(history[2].Until);
        Assert.True(history[2].IsCurrent);

        Assert.All(history, period => Assert.Equal(serial, period.Serial));
    }

    [Fact]
    public void A_unit_never_moved_has_a_single_open_period() {
        // Arrange
        LedgerWorld  world  = new LedgerWorld();
        Account      alice  = world.OpenHolding();
        CreditSerial serial = world.NewSerials(1)[0];
        DateTimeOffset issuedAt = world.NextInstant();
        world.Ledger.Issue([serial], alice.AccountId, issuedAt);

        // Act
        IReadOnlyList<HoldingPeriod> history = world.Ledger.HistoryOf(serial);

        // Assert
        HoldingPeriod only = Assert.Single(history);
        Assert.Equal(alice.AccountId, only.AccountId);
        Assert.Equal(issuedAt, only.From);
        Assert.True(only.IsCurrent);
    }

    [Fact]
    public void The_history_of_a_unit_ignores_the_entries_about_other_units() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 3);
        world.Transfer([serials[0]], alice, bob);
        world.Transfer([serials[1]], alice, bob);

        // Act
        IReadOnlyList<HoldingPeriod> history = world.Ledger.HistoryOf(serials[2]);

        // Assert
        Assert.Single(history);
        Assert.Equal(alice.AccountId, history[0].AccountId);
    }

    [Fact]
    public void A_unit_moved_in_a_batch_gets_the_same_history_as_one_moved_alone() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 3);
        world.Transfer(serials, alice, bob);

        // Assert
        Assert.All(serials, serial => Assert.Equal([alice.AccountId, bob.AccountId], world.Ledger.HistoryOf(serial).Select(period => period.AccountId)));
    }

    [Fact]
    public void A_unit_that_returns_to_a_holder_has_two_periods_with_that_holder() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);
        world.Transfer(serials, alice, bob);
        world.Transfer(serials, bob, alice);

        // Act
        IReadOnlyList<HoldingPeriod> history = world.Ledger.HistoryOf(serials[0]);

        // Assert
        Assert.Equal([alice.AccountId, bob.AccountId, alice.AccountId], history.Select(period => period.AccountId));
        Assert.Equal([false, false, true], history.Select(period => period.IsCurrent));
    }

    [Fact]
    public void Consecutive_periods_of_a_unit_touch_end_to_start() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);
        world.Transfer(serials, alice, bob);
        world.Transfer(serials, bob, alice);

        // Act
        IReadOnlyList<HoldingPeriod> history = world.Ledger.HistoryOf(serials[0]);

        // Assert
        Assert.Equal(history[0].Until, history[1].From);
        Assert.Equal(history[1].Until, history[2].From);
    }

    [Fact]
    public void The_history_of_a_unit_never_issued_is_empty() {
        LedgerWorld world = new LedgerWorld();

        Assert.Empty(world.Ledger.HistoryOf(world.NewSerials(1)[0]));
    }

    [Fact]
    public void The_history_of_a_null_serial_is_refused() {
        LedgerWorld world = new LedgerWorld();

        Assert.Throws<ArgumentNullException>(() => world.Ledger.HistoryOf(null!));
    }

    [Fact]
    public void The_holder_of_a_unit_never_issued_is_unknown() {
        // Arrange
        LedgerWorld  world  = new LedgerWorld();
        CreditSerial serial = world.NewSerials(1)[0];

        // Act
        Action ask = () => world.Ledger.HolderOf(serial);

        // Assert
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(ask);
        Assert.Contains("never been issued", refusal.Message);
        Assert.Throws<ArgumentNullException>(() => world.Ledger.HolderOf(null!));
    }

    [Fact]
    public void A_unit_never_issued_is_not_retired() {
        LedgerWorld world = new LedgerWorld();

        Assert.False(world.Ledger.IsRetired(world.NewSerials(1)[0]));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.IsRetired(null!));
    }

    [Fact]
    public void Balances_of_all_the_accounts_always_sum_to_zero() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 6);
        world.Transfer(serials.Take(4).ToList(), alice, bob);
        world.Retire(serials.Take(2).ToList(), bob, offsets);

        // Act
        int sum = world.Ledger.Accounts.Sum(account => world.Ledger.Balance(account.AccountId));

        // Assert
        Assert.Equal(0, sum);
        Assert.Equal(-6, world.Ledger.Balance(world.IssuanceAccount.AccountId));
    }

    [Fact]
    public void The_ledger_stays_consistent_through_issuing_transferring_and_retiring() {
        // Arrange
        LedgerWorld world = new LedgerWorld();
        Account     alice = world.OpenHolding();
        Account     bob   = world.OpenHolding();
        Account     carol = world.OpenHolding();
        Account     offsets = world.OpenRetirement();

        // Act, checking after each step
        List<CreditSerial> first = world.Issue(alice, 4);
        Assert.True(world.Ledger.IsConsistent());

        List<CreditSerial> second = world.Issue(bob, 3);
        Assert.True(world.Ledger.IsConsistent());

        world.Transfer(first.Take(2).ToList(), alice, carol);
        Assert.True(world.Ledger.IsConsistent());

        world.Transfer(second, bob, alice);
        Assert.True(world.Ledger.IsConsistent());

        world.Retire(first.Skip(2).ToList(), alice, offsets);
        Assert.True(world.Ledger.IsConsistent());

        world.Retire(second.Take(1).ToList(), alice, offsets);
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void A_refused_operation_never_leaves_the_ledger_inconsistent() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 3);
        int                journalSize = world.Ledger.Journal.Count;

        // Act: a series of refusals, each a different reason
        Assert.ThrowsAny<Exception>(() => world.Ledger.Transfer(serials, bob.AccountId, alice.AccountId, world.NextInstant()));
        Assert.ThrowsAny<Exception>(() => world.Ledger.Transfer(serials, alice.AccountId, offsets.AccountId, world.NextInstant()));
        Assert.ThrowsAny<Exception>(() => world.Ledger.Retire(serials, alice.AccountId, bob.AccountId, LedgerWorld.ClaimCovering(serials), world.NextInstant()));
        Assert.ThrowsAny<Exception>(() => world.Ledger.Issue(serials, alice.AccountId, world.NextInstant()));

        // Assert
        Assert.Equal(journalSize, world.Ledger.Journal.Count);
        Assert.Equal(3, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(0, world.Ledger.Balance(bob.AccountId));
        Assert.Equal(0, world.Ledger.Balance(offsets.AccountId));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void The_journal_lists_the_entries_in_the_order_they_were_recorded() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 2);

        // Act
        world.Transfer(serials, alice, bob);
        world.Retire(serials, bob, offsets);

        // Assert
        Assert.Equal([EntryKind.Issuance, EntryKind.Transfer, EntryKind.Retirement], world.Ledger.Journal.Select(entry => entry.Kind));
        Assert.Equal([1L, 2L, 3L], world.Ledger.Journal.Select(entry => entry.Sequence));
        Assert.True(world.Ledger.Journal.Zip(world.Ledger.Journal.Skip(1), (earlier, later) => earlier.RecordedAt <= later.RecordedAt).All(ordered => ordered));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDE")]
    public void A_ledger_works_with_a_registry_code_of_two_letters_and_one_of_five(string registry) {
        // Arrange
        LedgerWorld world  = new LedgerWorld(registry);
        Account     holder = world.OpenHolding();

        // Act
        List<CreditSerial> serials = world.Issue(holder, 2);

        // Assert
        Assert.Equal(2, world.Ledger.IssuedQuantity);
        Assert.All(serials, serial => Assert.Equal(registry, serial.Registry));
    }

}
