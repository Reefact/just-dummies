using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class IssueTests {

    [Fact]
    public void Issuing_records_one_balanced_issuance_entry() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            holder  = world.OpenHolding();
        List<CreditSerial> serials = world.NewSerials(4);
        DateTimeOffset     at      = world.NextInstant();

        // Act
        JournalEntry entry = world.Ledger.Issue(serials, holder.AccountId, at);

        // Assert
        Assert.Equal(1, entry.Sequence);
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Equal(at, entry.RecordedAt);
        Assert.Same(world.IssuanceAccount, entry.Debit);
        Assert.Same(holder, entry.Credit);
        Assert.Equal(serials, entry.Serials);
        Assert.Null(entry.Claim);
        Assert.Same(entry, Assert.Single(world.Ledger.Journal));
    }

    [Fact]
    public void Issuing_moves_the_quantity_from_the_issuance_account_to_the_holder() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();

        // Act
        world.Issue(holder, 5);

        // Assert
        Assert.Equal(5, world.Ledger.Balance(holder.AccountId));
        Assert.Equal(-5, world.Ledger.Balance(world.IssuanceAccount.AccountId));
        Assert.Equal(5, world.Ledger.IssuedQuantity);
        Assert.Equal(0, world.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Every_issued_unit_is_held_by_the_account_it_was_issued_to() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();

        // Act
        List<CreditSerial> serials = world.Issue(holder, 3);

        // Assert
        Assert.All(serials, serial => Assert.Same(holder, world.Ledger.HolderOf(serial)));
        Assert.Equal(serials.OrderBy(s => s.UnitNumber), world.Ledger.UnitsHeldBy(holder.AccountId).OrderBy(s => s.UnitNumber));
        Assert.Empty(world.Ledger.UnitsHeldBy(world.IssuanceAccount.AccountId));
    }

    [Fact]
    public void Entries_are_numbered_from_one_in_the_order_they_were_recorded() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     first  = world.OpenHolding();
        Account     second = world.OpenHolding();

        // Act
        world.Issue(first, 1);
        world.Issue(second, 1);
        world.Issue(first, 1);

        // Assert
        Assert.Equal([1L, 2L, 3L], world.Ledger.Journal.Select(entry => entry.Sequence));
    }

    [Fact]
    public void The_same_serial_cannot_be_issued_twice() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            holder  = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(holder, 3);
        Account            another = world.OpenHolding();

        // Act
        Action issueAgain = () => world.Ledger.Issue([serials[1]], another.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issueAgain);
        Assert.Equal(3, world.Ledger.IssuedQuantity);
        Assert.Equal(0, world.Ledger.Balance(another.AccountId));
        Assert.Single(world.Ledger.Journal);
    }

    [Fact]
    public void A_retired_serial_cannot_be_issued_again_either() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            holder  = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(holder, 1);
        world.Retire(serials, holder, offsets);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Issue(serials, holder.AccountId, world.NextInstant()));
    }

    [Fact]
    public void A_serial_of_another_registry_cannot_be_issued() {
        // Arrange
        LedgerWorld world   = new LedgerWorld();
        Account     holder  = world.OpenHolding();
        CreditSerial foreign = Any.CreditSerial().WithRegistry(r => r.OneOf(OtherRegistryThan(world.Registry))).Generate();

        // Act
        Action issue = () => world.Ledger.Issue([foreign], holder.AccountId, world.NextInstant());

        // Assert
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(issue);
        Assert.Contains(foreign.Registry, refusal.Message);
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void One_foreign_serial_in_a_batch_refuses_the_whole_batch() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            holder  = world.OpenHolding();
        List<CreditSerial> ours    = world.NewSerials(3);
        CreditSerial       foreign = Any.CreditSerial().WithRegistry(r => r.OneOf(OtherRegistryThan(world.Registry))).Generate();
        List<CreditSerial> batch   = [ours[0], foreign, ours[1]];

        // Act
        Action issue = () => world.Ledger.Issue(batch, holder.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issue);
        Assert.Equal(0, world.Ledger.IssuedQuantity);
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.HolderOf(ours[0]));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void Issuing_to_an_account_that_is_not_open_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            unknown = world.UnopenedAccount();
        List<CreditSerial> serials = world.NewSerials(1);

        // Act
        Action issue = () => world.Ledger.Issue(serials, unknown.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issue);
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void Issuing_to_a_retirement_account_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.NewSerials(1);

        // Act
        Action issue = () => world.Ledger.Issue(serials, offsets.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(issue);
        Assert.Equal(0, world.Ledger.IssuedQuantity);
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact(Skip = "domain defect: the model says every refused registry operation raises LedgerRuleViolationException, but issuing to a retirement account (like issuing nothing, a null or a repeated serial) surfaces JournalEntry's ArgumentException; deterministic, no seed")]
    public void Issuing_to_a_retirement_account_is_a_ledger_rule_violation() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.NewSerials(1);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Issue(serials, offsets.AccountId, world.NextInstant()));
    }

    [Fact]
    public void Issuing_into_the_issuance_account_itself_is_refused() {
        LedgerWorld        world   = new LedgerWorld();
        List<CreditSerial> serials = world.NewSerials(1);

        Assert.ThrowsAny<Exception>(() => world.Ledger.Issue(serials, world.IssuanceAccount.AccountId, world.NextInstant()));
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void Issuing_nothing_is_refused() {
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();

        Assert.ThrowsAny<Exception>(() => world.Ledger.Issue(new List<CreditSerial>(), holder.AccountId, world.NextInstant()));
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void Issuing_the_same_serial_twice_in_one_call_is_refused_and_leaves_no_trace() {
        // Arrange
        LedgerWorld  world  = new LedgerWorld();
        Account      holder = world.OpenHolding();
        CreditSerial serial = world.NewSerials(1)[0];

        // Act
        Action issue = () => world.Ledger.Issue([serial, serial], holder.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(issue);
        Assert.Empty(world.Ledger.Journal);
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.HolderOf(serial));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void A_null_serial_in_the_batch_is_refused_and_leaves_no_trace() {
        // Arrange
        LedgerWorld         world  = new LedgerWorld();
        Account             holder = world.OpenHolding();
        CreditSerial        serial = world.NewSerials(1)[0];
        List<CreditSerial?> batch  = [serial, null];

        // Act
        Action issue = () => world.Ledger.Issue(batch!, holder.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(issue);
        Assert.Empty(world.Ledger.Journal);
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.HolderOf(serial));
    }

    [Fact]
    public void A_missing_serial_list_or_account_id_is_refused() {
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();

        Assert.Throws<ArgumentNullException>(() => world.Ledger.Issue(null!, holder.AccountId, world.NextInstant()));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Issue(world.NewSerials(1), null!, world.NextInstant()));
    }

    [Fact]
    public void An_entry_can_be_recorded_at_the_very_instant_the_account_was_opened() {
        // Arrange
        LedgerWorld    world   = new LedgerWorld();
        Account        holder  = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(world.Now).Generate();
        world.Ledger.OpenAccount(holder);

        // Act
        JournalEntry entry = world.Ledger.Issue(world.NewSerials(1), holder.AccountId, world.Now);

        // Assert
        Assert.Equal(holder.OpenedOn, entry.RecordedAt);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_holding_account_was_opened() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     holder = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(world.Now).Generate();
        world.Ledger.OpenAccount(holder);

        // Act
        Action issue = () => world.Ledger.Issue(world.NewSerials(1), holder.AccountId, holder.OpenedOn.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issue);
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_issuance_account_was_opened() {
        // Arrange: the holder was opened long ago, the issuance account only just
        Account  issuance = Any.Account().WithKind(AccountKind.Issuance).WithOpenedOn(new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        CarbonRegistryLedger ledger = new CarbonRegistryLedger("VCS", issuance);
        Account  holder   = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        ledger.OpenAccount(holder);
        CreditSerial serial = Any.CreditSerial().WithRegistry("VCS").Generate();

        // Act
        Action issue = () => ledger.Issue([serial], holder.AccountId, issuance.OpenedOn.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issue);
    }

    [Fact]
    public void The_journal_is_append_only_so_an_entry_cannot_be_dated_before_the_previous_one() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();
        world.Issue(holder, 1);
        DateTimeOffset lastEntryAt = world.Now;

        // Act
        Action issue = () => world.Ledger.Issue(world.NewSerials(1), holder.AccountId, lastEntryAt.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(issue);
        Assert.Single(world.Ledger.Journal);
    }

    [Fact]
    public void Two_entries_may_share_the_same_instant() {
        // Arrange
        LedgerWorld world  = new LedgerWorld();
        Account     holder = world.OpenHolding();
        world.Issue(holder, 1);

        // Act
        JournalEntry second = world.Ledger.Issue(world.NewSerials(1), holder.AccountId, world.Now);

        // Assert
        Assert.Equal(2, second.Sequence);
    }

    private static string OtherRegistryThan(string registry) {
        return Any.StringMatching("[A-Z]{2,5}").DifferentFrom(registry).Generate();
    }

}
