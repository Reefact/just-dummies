using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerTransferTests {

    [Fact]
    public void Transferring_one_of_three_units_records_a_transfer_entry_between_the_two_accounts() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Act
        JournalEntry entry = scene.Ledger.Transfer(new[] { units[0] }, scene.First.AccountId, scene.Second.AccountId, scene.At(1));

        // Assert
        Assert.Equal(EntryKind.Transfer, entry.Kind);
        Assert.Equal(2L, entry.Sequence);
        Assert.Equal(1, entry.Quantity);
        Assert.Same(scene.First, entry.Debit);
        Assert.Same(scene.Second, entry.Credit);
        Assert.Equal(scene.At(1), entry.RecordedAt);
        Assert.Null(entry.Claim);
    }

    [Fact]
    public void Transferring_one_of_three_units_moves_only_that_unit() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Act
        scene.Ledger.Transfer(new[] { units[0] }, scene.First.AccountId, scene.Second.AccountId, scene.At(1));

        // Assert
        Assert.Same(scene.Second, scene.Ledger.HolderOf(units[0]));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[1]));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[2]));
        Assert.Equal(2, scene.Ledger.Balance(scene.First.AccountId));
        Assert.Equal(1, scene.Ledger.Balance(scene.Second.AccountId));
    }

    [Fact]
    public void A_transfer_does_not_change_what_was_issued() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(1));

        Assert.Equal(3, scene.Ledger.IssuedQuantity);
        Assert.Equal(-3, scene.Ledger.Balance(scene.Issuance.AccountId));
        Assert.Equal(0, scene.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Transferring_every_unit_leaves_the_sender_with_none() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Act
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(1));

        // Assert
        Assert.Equal(0, scene.Ledger.Balance(scene.First.AccountId));
        Assert.Empty(scene.Ledger.UnitsHeldBy(scene.First.AccountId));
        Assert.True(units.ToHashSet().SetEquals(scene.Ledger.UnitsHeldBy(scene.Second.AccountId)));
    }

    [Fact]
    public void A_unit_can_go_through_several_hands_and_back() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        // Act
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(1));
        scene.Ledger.Transfer(units, scene.Second.AccountId, scene.Third.AccountId, scene.At(2));
        scene.Ledger.Transfer(units, scene.Third.AccountId, scene.First.AccountId, scene.At(3));

        // Assert
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
        Assert.Equal(1, scene.Ledger.Balance(scene.First.AccountId));
        Assert.Equal(0, scene.Ledger.Balance(scene.Second.AccountId));
        Assert.Equal(0, scene.Ledger.Balance(scene.Third.AccountId));
    }

    // ---- what cannot be transferred -------------------------------------------------------------------------------

    [Fact]
    public void A_unit_held_by_someone_else_cannot_be_transferred() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(units, scene.Second.AccountId, scene.Third.AccountId, scene.At(1)));

        // Assert
        Assert.Contains(scene.First.AccountId, refusal.Message);
        Assert.Contains(scene.Second.AccountId, refusal.Message);
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_transferred() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1);
        CreditSerial neverIssued = scene.NewSerials(1)[0];

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(new[] { neverIssued }, scene.First.AccountId, scene.Second.AccountId, scene.At(1)));

        // Assert
        Assert.Contains(neverIssued.ToString(), refusal.Message);
    }

    [Fact]
    public void A_batch_with_one_unit_the_sender_does_not_hold_moves_none_of_them() {
        // Arrange
        RegistryScene               scene   = RegistryScene.Open();
        IReadOnlyList<CreditSerial> mine    = scene.IssueTo(scene.First, 1);
        IReadOnlyList<CreditSerial> theirs  = scene.IssueTo(scene.Second, 1);

        // Act
        Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(new[] { mine[0], theirs[0] }, scene.First.AccountId, scene.Third.AccountId, scene.At(1)));

        // Assert
        Assert.Same(scene.First, scene.Ledger.HolderOf(mine[0]));
        Assert.Same(scene.Second, scene.Ledger.HolderOf(theirs[0]));
        Assert.Equal(2, scene.Ledger.Journal.Count);
        Assert.True(scene.Ledger.IsConsistent());
    }

    [Fact]
    public void An_account_cannot_transfer_to_itself() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.Throws<ArgumentException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, scene.First.AccountId, scene.At(1)));
        Assert.Single(scene.Ledger.Journal);
    }

    [Fact]
    public void A_transfer_cannot_end_in_a_retirement_account() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.Throws<ArgumentException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, scene.Retirement.AccountId, scene.At(1)));

        Assert.False(scene.Ledger.IsRetired(units[0]));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_transfer_cannot_start_from_the_issuance_account() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Transfer(units, scene.Issuance.AccountId, scene.Second.AccountId, scene.At(1)));
    }

    [Fact]
    public void A_batch_cannot_list_the_same_unit_twice() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        // Act
        Assert.Throws<ArgumentException>(() => scene.Ledger.Transfer(new[] { units[0], units[0] }, scene.First.AccountId, scene.Second.AccountId, scene.At(1)));

        // Assert
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
        Assert.Single(scene.Ledger.Journal);
    }

    [Fact]
    public void A_batch_cannot_be_empty() {
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1);

        Assert.Throws<ArgumentException>(() => scene.Ledger.Transfer(Array.Empty<CreditSerial>(), scene.First.AccountId, scene.Second.AccountId, scene.At(1)));
    }

    [Fact]
    public void A_missing_batch_is_refused() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Transfer(null!, scene.First.AccountId, scene.Second.AccountId, scene.At(1)));
    }

    [Fact]
    public void A_transfer_from_an_account_that_is_not_open_is_refused() {
        // Arrange
        RegistryScene               scene    = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units    = scene.IssueTo(scene.First, 1);
        Account                     stranger = scene.Stranger();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(units, stranger.AccountId, scene.Second.AccountId, scene.At(1)));

        // Assert
        Assert.Contains(stranger.AccountId, refusal.Message);
    }

    [Fact]
    public void A_transfer_to_an_account_that_is_not_open_is_refused() {
        // Arrange
        RegistryScene               scene    = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units    = scene.IssueTo(scene.First, 1);
        Account                     stranger = scene.Stranger();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(units, scene.First.AccountId, stranger.AccountId, scene.At(1)));

        // Assert
        Assert.Contains(stranger.AccountId, refusal.Message);
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    // ---- when -----------------------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_cannot_be_recorded_before_the_last_entry() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 5);

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(4)));

        // Assert
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_transfer_can_be_recorded_the_very_instant_of_the_last_entry() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 5);

        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(5));

        Assert.Same(scene.Second, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_transfer_cannot_be_recorded_before_the_receiver_was_opened() {
        // Arrange
        RegistryScene               scene     = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units     = scene.IssueTo(scene.First, 1);
        Account                     latecomer = scene.OpenHoldingAccountOn(10);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, latecomer.AccountId, scene.At(9)));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_transfer_can_be_recorded_the_instant_the_receiver_was_opened() {
        RegistryScene               scene     = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units     = scene.IssueTo(scene.First, 1);
        Account                     latecomer = scene.OpenHoldingAccountOn(10);

        scene.Ledger.Transfer(units, scene.First.AccountId, latecomer.AccountId, scene.At(10));

        Assert.Same(latecomer, scene.Ledger.HolderOf(units[0]));
    }

}
