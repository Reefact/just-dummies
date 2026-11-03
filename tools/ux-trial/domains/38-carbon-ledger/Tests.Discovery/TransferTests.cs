using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class TransferTests {

    [Fact]
    public void Transferring_records_one_balanced_transfer_entry() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 3);
        DateTimeOffset     at      = world.NextInstant();

        // Act
        JournalEntry entry = world.Ledger.Transfer(serials, alice.AccountId, bob.AccountId, at);

        // Assert
        Assert.Equal(2, entry.Sequence);
        Assert.Equal(EntryKind.Transfer, entry.Kind);
        Assert.Equal(at, entry.RecordedAt);
        Assert.Same(alice, entry.Debit);
        Assert.Same(bob, entry.Credit);
        Assert.Equal(serials, entry.Serials);
        Assert.Null(entry.Claim);
        Assert.Equal(2, world.Ledger.Journal.Count);
    }

    [Fact]
    public void Transferring_changes_the_holder_of_each_unit_and_the_two_balances() {
        // Arrange
        LedgerWorld        world    = new LedgerWorld();
        Account            alice    = world.OpenHolding();
        Account            bob      = world.OpenHolding();
        List<CreditSerial> serials  = world.Issue(alice, 5);
        List<CreditSerial> moved    = serials.Take(2).ToList();
        List<CreditSerial> stayed   = serials.Skip(2).ToList();

        // Act
        world.Transfer(moved, alice, bob);

        // Assert
        Assert.Equal(3, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(2, world.Ledger.Balance(bob.AccountId));
        Assert.All(moved, serial => Assert.Same(bob, world.Ledger.HolderOf(serial)));
        Assert.All(stayed, serial => Assert.Same(alice, world.Ledger.HolderOf(serial)));
        Assert.Equal(5, world.Ledger.IssuedQuantity);
        Assert.Equal(0, world.Ledger.RetiredQuantity);
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void A_unit_can_travel_back_to_a_previous_holder() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        world.Transfer(serials, alice, bob);
        world.Transfer(serials, bob, alice);

        // Assert
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
        Assert.Equal(1, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(0, world.Ledger.Balance(bob.AccountId));
    }

    [Fact]
    public void A_unit_can_only_be_sent_by_the_account_that_holds_it() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            carol   = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 2);

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, bob.AccountId, carol.AccountId, world.NextInstant());

        // Assert
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(transfer);
        Assert.Contains(alice.AccountId, refusal.Message);
        Assert.Contains(bob.AccountId, refusal.Message);
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
        Assert.Single(world.Ledger.Journal);
    }

    [Fact]
    public void One_unit_not_held_by_the_sender_refuses_the_whole_transfer() {
        // Arrange
        LedgerWorld        world     = new LedgerWorld();
        Account            alice     = world.OpenHolding();
        Account            bob       = world.OpenHolding();
        Account            carol     = world.OpenHolding();
        List<CreditSerial> alices    = world.Issue(alice, 2);
        List<CreditSerial> bobs      = world.Issue(bob, 1);
        List<CreditSerial> batch     = [alices[0], bobs[0], alices[1]];

        // Act
        Action transfer = () => world.Ledger.Transfer(batch, alice.AccountId, carol.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(transfer);
        Assert.Equal(2, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(1, world.Ledger.Balance(bob.AccountId));
        Assert.Equal(0, world.Ledger.Balance(carol.AccountId));
        Assert.Same(alice, world.Ledger.HolderOf(alices[0]));
        Assert.Equal(2, world.Ledger.Journal.Count);
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_transferred() {
        // Arrange
        LedgerWorld        world    = new LedgerWorld();
        Account            alice    = world.OpenHolding();
        Account            bob      = world.OpenHolding();
        List<CreditSerial> unissued = world.NewSerials(1);

        // Act
        Action transfer = () => world.Ledger.Transfer(unissued, alice.AccountId, bob.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(transfer);
        Assert.Empty(world.Ledger.Journal);
    }

    [Fact]
    public void A_unit_of_another_registry_cannot_be_transferred() {
        // Arrange
        LedgerWorld  world   = new LedgerWorld();
        Account      alice   = world.OpenHolding();
        Account      bob     = world.OpenHolding();
        world.Issue(alice, 1);
        CreditSerial foreign = Any.CreditSerial().WithRegistry(r => r.OneOf(Any.StringMatching("[A-Z]{2,5}").DifferentFrom(world.Registry).Generate())).Generate();

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Transfer([foreign], alice.AccountId, bob.AccountId, world.NextInstant()));
    }

    [Fact]
    public void The_issuance_account_cannot_be_the_sender_of_a_transfer() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, world.IssuanceAccount.AccountId, alice.AccountId, world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(transfer);
    }

    [Fact]
    public void An_unknown_sender_or_receiver_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            unknown = world.UnopenedAccount();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Transfer(serials, alice.AccountId, unknown.AccountId, world.NextInstant()));
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Transfer(serials, unknown.AccountId, alice.AccountId, world.NextInstant()));
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
    }

    [Fact]
    public void A_transfer_to_oneself_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, alice.AccountId, alice.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(transfer);
        Assert.Single(world.Ledger.Journal);
    }

    [Fact]
    public void A_transfer_into_a_retirement_account_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, alice.AccountId, offsets.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(transfer);
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
        Assert.False(world.Ledger.IsRetired(serials[0]));
    }

    [Fact]
    public void Transferring_nothing_is_refused() {
        LedgerWorld world = new LedgerWorld();
        Account     alice = world.OpenHolding();
        Account     bob   = world.OpenHolding();
        world.Issue(alice, 1);

        Assert.ThrowsAny<Exception>(() => world.Ledger.Transfer(new List<CreditSerial>(), alice.AccountId, bob.AccountId, world.NextInstant()));
    }

    [Fact]
    public void Transferring_the_same_unit_twice_in_one_call_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action transfer = () => world.Ledger.Transfer([serials[0], serials[0]], alice.AccountId, bob.AccountId, world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(transfer);
        Assert.Equal(1, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(0, world.Ledger.Balance(bob.AccountId));
    }

    [Fact]
    public void A_null_argument_is_refused() {
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        Assert.Throws<ArgumentNullException>(() => world.Ledger.Transfer(null!, alice.AccountId, bob.AccountId, world.NextInstant()));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Transfer(serials, null!, bob.AccountId, world.NextInstant()));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Transfer(serials, alice.AccountId, null!, world.NextInstant()));
    }

    [Fact]
    public void A_transfer_cannot_be_dated_before_the_previous_entry() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);
        DateTimeOffset     lastAt  = world.Now;

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, alice.AccountId, bob.AccountId, lastAt.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(transfer);
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
    }

    [Fact]
    public void A_transfer_can_share_the_instant_of_the_previous_entry() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        JournalEntry entry = world.Ledger.Transfer(serials, alice.AccountId, bob.AccountId, world.Now);

        // Assert
        Assert.Equal(world.Now, entry.RecordedAt);
    }

    [Fact]
    public void A_transfer_cannot_be_dated_before_the_receiving_account_was_opened() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);
        DateTimeOffset     later   = world.NextInstant().AddDays(10);
        Account            bob     = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(later).Generate();
        world.Ledger.OpenAccount(bob);

        // Act
        Action transfer = () => world.Ledger.Transfer(serials, alice.AccountId, bob.AccountId, later.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(transfer);
    }

}
