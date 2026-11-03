using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class RetireTests {

    [Fact]
    public void Retiring_records_one_balanced_retirement_entry_carrying_the_claim() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 3);
        RetirementClaim    claim   = LedgerWorld.ClaimCovering(serials);
        DateTimeOffset     at      = world.NextInstant();

        // Act
        JournalEntry entry = world.Ledger.Retire(serials, alice.AccountId, offsets.AccountId, claim, at);

        // Assert
        Assert.Equal(2, entry.Sequence);
        Assert.Equal(EntryKind.Retirement, entry.Kind);
        Assert.Equal(at, entry.RecordedAt);
        Assert.Same(alice, entry.Debit);
        Assert.Same(offsets, entry.Credit);
        Assert.Equal(serials, entry.Serials);
        Assert.Same(claim, entry.Claim);
    }

    [Fact]
    public void Retiring_moves_units_to_the_retirement_account_and_counts_them_retired() {
        // Arrange
        LedgerWorld        world    = new LedgerWorld();
        Account            alice    = world.OpenHolding();
        Account            offsets  = world.OpenRetirement();
        List<CreditSerial> serials  = world.Issue(alice, 5);
        List<CreditSerial> retired  = serials.Take(2).ToList();
        List<CreditSerial> kept     = serials.Skip(2).ToList();

        // Act
        world.Retire(retired, alice, offsets);

        // Assert
        Assert.Equal(3, world.Ledger.Balance(alice.AccountId));
        Assert.Equal(2, world.Ledger.Balance(offsets.AccountId));
        Assert.Equal(2, world.Ledger.RetiredQuantity);
        Assert.Equal(5, world.Ledger.IssuedQuantity);
        Assert.All(retired, serial => Assert.True(world.Ledger.IsRetired(serial)));
        Assert.All(retired, serial => Assert.Same(offsets, world.Ledger.HolderOf(serial)));
        Assert.All(kept, serial => Assert.False(world.Ledger.IsRetired(serial)));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void Retired_units_are_listed_under_the_retirement_account() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 2);

        // Act
        world.Retire(serials, alice, offsets);

        // Assert
        Assert.Equal(serials.OrderBy(s => s.UnitNumber), world.Ledger.UnitsHeldBy(offsets.AccountId).OrderBy(s => s.UnitNumber));
        Assert.Empty(world.Ledger.UnitsHeldBy(alice.AccountId));
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_transferred() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);
        world.Retire(serials, alice, offsets);

        // Act
        Action transferFromTheRetirementAccount = () => world.Ledger.Transfer(serials, offsets.AccountId, bob.AccountId, world.NextInstant());
        Action transferFromTheFormerHolder      = () => world.Ledger.Transfer(serials, alice.AccountId, bob.AccountId, world.NextInstant());

        // Assert
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(transferFromTheRetirementAccount);
        Assert.Contains("retired", refusal.Message);
        Assert.Throws<LedgerRuleViolationException>(transferFromTheFormerHolder);
        Assert.Same(offsets, world.Ledger.HolderOf(serials[0]));
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_retired_again() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        Account            another = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);
        world.Retire(serials, alice, offsets);

        // Act
        Action retireAgain = () => world.Ledger.Retire(serials, alice.AccountId, another.AccountId, LedgerWorld.ClaimCovering(serials), world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retireAgain);
        Assert.Equal(1, world.Ledger.RetiredQuantity);
        Assert.Equal(0, world.Ledger.Balance(another.AccountId));
    }

    [Fact]
    public void Retirement_of_part_of_a_batch_of_already_retired_units_refuses_the_whole_batch() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 3);
        world.Retire([serials[1]], alice, offsets);

        // Act
        Action retireAll = () => world.Ledger.Retire(serials, alice.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering(serials), world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retireAll);
        Assert.Equal(1, world.Ledger.RetiredQuantity);
        Assert.Equal(2, world.Ledger.Balance(alice.AccountId));
        Assert.False(world.Ledger.IsRetired(serials[0]));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void Only_the_holder_can_retire_a_unit() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action retire = () => world.Ledger.Retire(serials, bob.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering(serials), world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retire);
        Assert.False(world.Ledger.IsRetired(serials[0]));
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_retired() {
        // Arrange
        LedgerWorld        world    = new LedgerWorld();
        Account            alice    = world.OpenHolding();
        Account            offsets  = world.OpenRetirement();
        List<CreditSerial> unissued = world.NewSerials(1);

        // Act
        Action retire = () => world.Ledger.Retire(unissued, alice.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering(unissued), world.NextInstant());

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retire);
        Assert.Equal(0, world.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Retiring_into_a_holding_account_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            bob     = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action retire = () => world.Ledger.Retire(serials, alice.AccountId, bob.AccountId, LedgerWorld.ClaimCovering(serials), world.NextInstant());

        // Assert
        Assert.ThrowsAny<Exception>(retire);
        Assert.False(world.Ledger.IsRetired(serials[0]));
        Assert.Same(alice, world.Ledger.HolderOf(serials[0]));
    }

    [Fact]
    public void Retiring_without_a_claim_is_refused() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);

        // Act
        Action retire = () => world.Ledger.Retire(serials, alice.AccountId, offsets.AccountId, null!, world.NextInstant());

        // Assert
        Assert.Throws<ArgumentNullException>(retire);
        Assert.False(world.Ledger.IsRetired(serials[0]));
    }

    [Fact]
    public void A_claim_reporting_a_year_before_the_vintage_is_refused_and_leaves_the_units_active() {
        // Arrange: a vintage-2030 unit cannot offset emissions reported for 2029
        LedgerWorld  world   = new LedgerWorld();
        Account      alice   = world.OpenHolding();
        Account      offsets = world.OpenRetirement();
        CreditSerial serial  = Any.CreditSerial().WithRegistry(world.Registry).WithVintage(2030).Generate();
        world.Ledger.Issue([serial], alice.AccountId, world.NextInstant());
        RetirementClaim tooEarly = Any.RetirementClaim().WithReportingYear(2029).Generate();

        // Act
        Action retire = () => world.Ledger.Retire([serial], alice.AccountId, offsets.AccountId, tooEarly, world.NextInstant());

        // Assert
        Assert.ThrowsAny<ArgumentException>(retire);
        Assert.False(world.Ledger.IsRetired(serial));
        Assert.Same(alice, world.Ledger.HolderOf(serial));
        Assert.Equal(0, world.Ledger.Balance(offsets.AccountId));
        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact]
    public void A_claim_reporting_the_year_of_the_vintage_is_enough() {
        // Arrange
        LedgerWorld  world   = new LedgerWorld();
        Account      alice   = world.OpenHolding();
        Account      offsets = world.OpenRetirement();
        CreditSerial serial  = Any.CreditSerial().WithRegistry(world.Registry).WithVintage(2030).Generate();
        world.Ledger.Issue([serial], alice.AccountId, world.NextInstant());
        RetirementClaim justInTime = Any.RetirementClaim().WithReportingYear(2030).Generate();

        // Act
        world.Ledger.Retire([serial], alice.AccountId, offsets.AccountId, justInTime, world.NextInstant());

        // Assert
        Assert.True(world.Ledger.IsRetired(serial));
    }

    [Fact]
    public void A_retirement_cannot_be_dated_before_the_previous_entry() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);
        DateTimeOffset     lastAt  = world.Now;

        // Act
        Action retire = () => world.Ledger.Retire(serials, alice.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering(serials), lastAt.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retire);
        Assert.False(world.Ledger.IsRetired(serials[0]));
    }

    [Fact]
    public void A_retirement_cannot_be_dated_before_the_retirement_account_was_opened() {
        // Arrange
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        List<CreditSerial> serials = world.Issue(alice, 1);
        DateTimeOffset     opening = world.NextInstant().AddDays(5);
        Account            offsets = Any.Account().WithKind(AccountKind.Retirement).WithOpenedOn(opening).Generate();
        world.Ledger.OpenAccount(offsets);

        // Act
        Action retire = () => world.Ledger.Retire(serials, alice.AccountId, offsets.AccountId, LedgerWorld.ClaimCovering(serials), opening.AddTicks(-1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(retire);
    }

    [Fact]
    public void Retiring_nothing_is_refused() {
        LedgerWorld world   = new LedgerWorld();
        Account     alice   = world.OpenHolding();
        Account     offsets = world.OpenRetirement();
        world.Issue(alice, 1);

        Assert.ThrowsAny<Exception>(() => world.Ledger.Retire(new List<CreditSerial>(), alice.AccountId, offsets.AccountId, Any.RetirementClaim().Generate(), world.NextInstant()));
    }

    [Fact]
    public void A_null_serial_list_or_account_id_is_refused() {
        LedgerWorld        world   = new LedgerWorld();
        Account            alice   = world.OpenHolding();
        Account            offsets = world.OpenRetirement();
        List<CreditSerial> serials = world.Issue(alice, 1);
        RetirementClaim    claim   = LedgerWorld.ClaimCovering(serials);

        Assert.Throws<ArgumentNullException>(() => world.Ledger.Retire(null!, alice.AccountId, offsets.AccountId, claim, world.NextInstant()));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Retire(serials, null!, offsets.AccountId, claim, world.NextInstant()));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Retire(serials, alice.AccountId, null!, claim, world.NextInstant()));
    }

}
