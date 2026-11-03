using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerIssueTests {

    [Fact]
    public void Issuing_three_units_records_one_issuance_entry_from_the_issuance_account_to_the_receiver() {
        // Arrange
        RegistryScene               scene   = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units   = scene.NewSerials(3);

        // Act
        JournalEntry entry = scene.Ledger.Issue(units, scene.First.AccountId, scene.At(1));

        // Assert
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Equal(1L, entry.Sequence);
        Assert.Equal(3, entry.Quantity);
        Assert.Same(scene.Issuance, entry.Debit);
        Assert.Same(scene.First, entry.Credit);
        Assert.Equal(scene.At(1), entry.RecordedAt);
        Assert.Null(entry.Claim);
        Assert.Equal(units, entry.Serials);
        Assert.Same(entry, Assert.Single(scene.Ledger.Journal));
    }

    [Fact]
    public void Issuing_three_units_credits_the_receiver_and_debits_the_issuance_account_by_three() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();

        // Act
        scene.IssueTo(scene.First, 3);

        // Assert
        Assert.Equal(3, scene.Ledger.Balance(scene.First.AccountId));
        Assert.Equal(-3, scene.Ledger.Balance(scene.Issuance.AccountId));
        Assert.Equal(3, scene.Ledger.IssuedQuantity);
        Assert.Equal(0, scene.Ledger.Balance(scene.Second.AccountId));
    }

    [Fact]
    public void Every_issued_unit_is_held_by_the_receiver() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();

        // Act
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Assert
        Assert.All(units, unit => Assert.Same(scene.First, scene.Ledger.HolderOf(unit)));
        Assert.True(units.ToHashSet().SetEquals(scene.Ledger.UnitsHeldBy(scene.First.AccountId)));
    }

    [Fact]
    public void An_issued_unit_is_not_retired() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.False(scene.Ledger.IsRetired(units[0]));
        Assert.Equal(0, scene.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_second_issuance_is_the_second_entry_and_adds_to_the_issued_quantity() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 2);

        // Act
        JournalEntry second = scene.Ledger.Issue(scene.NewSerials(3), scene.Second.AccountId, scene.At(2));

        // Assert
        Assert.Equal(2L, second.Sequence);
        Assert.Equal(5, scene.Ledger.IssuedQuantity);
        Assert.Equal(-5, scene.Ledger.Balance(scene.Issuance.AccountId));
    }

    // ---- what cannot be issued -----------------------------------------------------------------------------------

    [Fact]
    public void A_unit_of_another_registry_cannot_be_issued() {
        // Arrange
        RegistryScene scene  = RegistryScene.Open();
        CreditSerial  foreign = Any.CreditSerial().WithRegistry(registry => registry.DifferentFrom(scene.Registry)).Generate();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Issue(new[] { foreign }, scene.First.AccountId, scene.At(0)));

        // Assert
        Assert.Contains(foreign.ToString(), refusal.Message);
        Assert.Contains(scene.Registry, refusal.Message);
        Assert.Empty(scene.Ledger.Journal);
    }

    [Fact]
    public void A_unit_cannot_be_issued_twice() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Issue(units, scene.Second.AccountId, scene.At(1)));

        // Assert
        Assert.Contains(units[0].ToString(), refusal.Message);
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
        Assert.Equal(1, scene.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_retired_unit_cannot_be_issued_again() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        scene.RetireFrom(scene.First, units.ToList(), 1);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(units, scene.Second.AccountId, scene.At(2)));
    }

    [Fact]
    public void A_batch_with_one_unit_already_issued_issues_none_of_them() {
        // Arrange
        RegistryScene               scene    = RegistryScene.Open();
        IReadOnlyList<CreditSerial> issued   = scene.IssueTo(scene.First, 1);
        IReadOnlyList<CreditSerial> fresh    = scene.NewSerials(1);

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(new[] { fresh[0], issued[0] }, scene.Second.AccountId, scene.At(1)));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.HolderOf(fresh[0]));
        Assert.Equal(1, scene.Ledger.IssuedQuantity);
        Assert.Single(scene.Ledger.Journal);
        Assert.True(scene.Ledger.IsConsistent());
    }

    [Fact]
    public void A_batch_cannot_list_the_same_unit_twice() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();
        CreditSerial  unit  = scene.NewSerials(1)[0];

        // Act
        Assert.Throws<ArgumentException>(() => scene.Ledger.Issue(new[] { unit, unit }, scene.First.AccountId, scene.At(0)));

        // Assert
        Assert.Empty(scene.Ledger.Journal);
        Assert.Equal(0, scene.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_batch_cannot_be_empty() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentException>(() => scene.Ledger.Issue(Array.Empty<CreditSerial>(), scene.First.AccountId, scene.At(0)));
    }

    [Fact]
    public void A_batch_cannot_contain_a_missing_unit() {
        RegistryScene scene = RegistryScene.Open();
        CreditSerial  unit  = scene.NewSerials(1)[0];

        Assert.Throws<ArgumentException>(() => scene.Ledger.Issue(new CreditSerial?[] { unit, null }!, scene.First.AccountId, scene.At(0)));
        Assert.Empty(scene.Ledger.Journal);
    }

    [Fact]
    public void A_missing_batch_is_refused() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Issue(null!, scene.First.AccountId, scene.At(0)));
    }

    [Fact]
    public void Units_cannot_be_issued_into_an_account_that_is_not_open() {
        // Arrange
        RegistryScene scene    = RegistryScene.Open();
        Account       stranger = scene.Stranger();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Issue(scene.NewSerials(1), stranger.AccountId, scene.At(0)));

        // Assert
        Assert.Contains(stranger.AccountId, refusal.Message);
    }

    [Fact]
    public void Units_cannot_be_issued_into_a_retirement_account() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentException>(() => scene.Ledger.Issue(scene.NewSerials(1), scene.Retirement.AccountId, scene.At(0)));
        Assert.Empty(scene.Ledger.Journal);
    }

    [Fact]
    public void Units_cannot_be_issued_back_into_the_issuance_account() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentException>(() => scene.Ledger.Issue(scene.NewSerials(1), scene.Issuance.AccountId, scene.At(0)));
        Assert.Empty(scene.Ledger.Journal);
    }

    // ---- when -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Units_can_be_issued_the_very_instant_the_accounts_were_opened() {
        RegistryScene scene = RegistryScene.Open();

        JournalEntry entry = scene.Ledger.Issue(scene.NewSerials(1), scene.First.AccountId, scene.OpenedAt);

        Assert.Equal(scene.OpenedAt, entry.RecordedAt);
    }

    [Fact]
    public void Units_cannot_be_issued_before_the_accounts_were_opened() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(scene.NewSerials(1), scene.First.AccountId, scene.At(-1)));

        // Assert
        Assert.Empty(scene.Ledger.Journal);
    }

    [Fact]
    public void Units_cannot_be_issued_into_an_account_before_it_was_opened_even_if_the_issuance_account_was_already() {
        // Arrange
        RegistryScene scene     = RegistryScene.Open();
        Account       latecomer = scene.OpenHoldingAccountOn(10);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(scene.NewSerials(1), latecomer.AccountId, scene.At(9)));
        Assert.Empty(scene.Ledger.Journal);
    }

    [Fact]
    public void Units_can_be_issued_into_an_account_the_instant_it_was_opened() {
        RegistryScene scene     = RegistryScene.Open();
        Account       latecomer = scene.OpenHoldingAccountOn(10);

        scene.Ledger.Issue(scene.NewSerials(1), latecomer.AccountId, scene.At(10));

        Assert.Equal(1, scene.Ledger.Balance(latecomer.AccountId));
    }

    [Fact]
    public void Units_cannot_be_issued_before_the_issuance_account_was_opened() {
        // Arrange
        DateTimeOffset issuanceOpening = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        Account        issuance        = Any.Account().WithKind(AccountKind.Issuance).WithOpenedOn(issuanceOpening).Generate();
        Account        holding         = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(issuanceOpening.AddDays(-10)).WithHolder(holder => holder.WithCountryCode(country => country.DifferentFrom(issuance.Holder.CountryCode))).Generate();
        CarbonRegistryLedger ledger    = new CarbonRegistryLedger("VCS", issuance);
        ledger.OpenAccount(holding);
        CreditSerial unit = Any.CreditSerial().WithRegistry("VCS").Generate();

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => ledger.Issue(new[] { unit }, holding.AccountId, issuanceOpening.AddDays(-5)));
        Assert.Empty(ledger.Journal);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_previous_one() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1, day: 5);

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(scene.NewSerials(1), scene.First.AccountId, scene.At(4)));

        // Assert
        Assert.Single(scene.Ledger.Journal);
    }

    [Fact]
    public void An_entry_can_be_recorded_the_very_instant_of_the_previous_one() {
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1, day: 5);

        JournalEntry second = scene.Ledger.Issue(scene.NewSerials(1), scene.First.AccountId, scene.At(5));

        Assert.Equal(2L, second.Sequence);
    }

}
