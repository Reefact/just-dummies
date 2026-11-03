using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerRetireTests {

    [Fact]
    public void Retiring_two_of_three_units_records_a_retirement_entry_carrying_the_claim() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);
        CreditSerial[]              two   = units.Take(2).ToArray();
        RetirementClaim             claim = scene.ClaimFor(two);

        // Act
        JournalEntry entry = scene.Ledger.Retire(two, scene.First.AccountId, scene.Retirement.AccountId, claim, scene.At(1));

        // Assert
        Assert.Equal(EntryKind.Retirement, entry.Kind);
        Assert.Equal(2L, entry.Sequence);
        Assert.Equal(2, entry.Quantity);
        Assert.Same(scene.First, entry.Debit);
        Assert.Same(scene.Retirement, entry.Credit);
        Assert.Same(claim, entry.Claim);
        Assert.Equal(scene.At(1), entry.RecordedAt);
    }

    [Fact]
    public void Retiring_two_of_three_units_retires_those_two_and_keeps_the_third_active() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Act
        scene.RetireFrom(scene.First, units.Take(2).ToArray(), 1);

        // Assert
        Assert.True(scene.Ledger.IsRetired(units[0]));
        Assert.True(scene.Ledger.IsRetired(units[1]));
        Assert.False(scene.Ledger.IsRetired(units[2]));
        Assert.Equal(2, scene.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Retired_units_sit_in_the_retirement_account_and_leave_the_sender() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        // Act
        scene.RetireFrom(scene.First, units.Take(2).ToArray(), 1);

        // Assert
        Assert.Same(scene.Retirement, scene.Ledger.HolderOf(units[0]));
        Assert.Equal(2, scene.Ledger.Balance(scene.Retirement.AccountId));
        Assert.Equal(1, scene.Ledger.Balance(scene.First.AccountId));
        Assert.True(units.Take(2).ToHashSet().SetEquals(scene.Ledger.UnitsHeldBy(scene.Retirement.AccountId)));
    }

    [Fact]
    public void Retiring_units_does_not_change_what_was_issued() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 3);

        scene.RetireFrom(scene.First, units.Take(2).ToArray(), 1);

        Assert.Equal(3, scene.Ledger.IssuedQuantity);
        Assert.Equal(-3, scene.Ledger.Balance(scene.Issuance.AccountId));
    }

    [Fact]
    public void Units_can_be_retired_after_having_been_transferred() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(1));

        // Act
        scene.RetireFrom(scene.Second, units.ToList(), 2);

        // Assert
        Assert.True(scene.Ledger.IsRetired(units[0]));
    }

    // ---- locked forever -------------------------------------------------------------------------------------------

    [Fact]
    public void A_retired_unit_cannot_be_transferred() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        scene.RetireFrom(scene.First, units.ToList(), 1);

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Transfer(units, scene.Retirement.AccountId, scene.Second.AccountId, scene.At(2)));

        // Assert
        Assert.Contains("retired", refusal.Message);
        Assert.Same(scene.Retirement, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_retired_unit_cannot_be_transferred_even_by_an_account_that_once_held_it() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        scene.RetireFrom(scene.First, units.ToList(), 1);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, scene.Second.AccountId, scene.At(2)));
    }

    [Fact]
    public void A_retired_unit_cannot_be_retired_again() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        scene.RetireFrom(scene.First, units.ToList(), 1);

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Retire(units, scene.Retirement.AccountId, scene.Retirement.AccountId, scene.ClaimFor(units), scene.At(2)));

        // Assert
        Assert.Contains("retired", refusal.Message);
        Assert.Equal(1, scene.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_batch_with_one_retired_unit_retires_none_of_the_others() {
        // Arrange
        RegistryScene               scene   = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units   = scene.IssueTo(scene.First, 2);
        scene.RetireFrom(scene.First, new[] { units[0] }, 1);

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.RetireFrom(scene.First, units.ToList(), 2));

        // Assert
        Assert.False(scene.Ledger.IsRetired(units[1]));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[1]));
        Assert.Equal(1, scene.Ledger.RetiredQuantity);
        Assert.True(scene.Ledger.IsConsistent());
    }

    // ---- what cannot be retired -----------------------------------------------------------------------------------

    [Fact]
    public void A_unit_held_by_someone_else_cannot_be_retired() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.Throws<LedgerRuleViolationException>(() => scene.RetireFrom(scene.Second, units.ToList(), 1));

        Assert.False(scene.Ledger.IsRetired(units[0]));
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_retired() {
        RegistryScene scene = RegistryScene.Open();
        scene.IssueTo(scene.First, 1);
        IReadOnlyList<CreditSerial> neverIssued = scene.NewSerials(1);

        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(() => scene.RetireFrom(scene.First, neverIssued.ToList(), 1));

        Assert.Contains(neverIssued[0].ToString(), refusal.Message);
    }

    [Fact]
    public void Units_cannot_be_retired_into_a_holding_account() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        // Act
        Assert.Throws<ArgumentException>(
            () => scene.Ledger.Retire(units, scene.First.AccountId, scene.Second.AccountId, scene.ClaimFor(units), scene.At(1)));

        // Assert
        Assert.False(scene.Ledger.IsRetired(units[0]));
        Assert.Same(scene.First, scene.Ledger.HolderOf(units[0]));
    }

    [Fact]
    public void A_retirement_must_state_its_claim() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Retire(units, scene.First.AccountId, scene.Retirement.AccountId, null!, scene.At(1)));
    }

    [Fact]
    public void A_missing_batch_is_refused() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.Retire(null!, scene.First.AccountId, scene.Retirement.AccountId, Any.RetirementClaim().Generate(), scene.At(1)));
    }

    [Fact]
    public void A_retirement_to_an_account_that_is_not_open_is_refused() {
        // Arrange
        RegistryScene               scene    = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units    = scene.IssueTo(scene.First, 1);
        Account                     stranger = scene.Stranger();

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Retire(units, scene.First.AccountId, stranger.AccountId, scene.ClaimFor(units), scene.At(1)));
    }

    // ---- the claim --------------------------------------------------------------------------------------------------

    [Fact]
    public void Units_cannot_offset_emissions_of_a_year_before_their_vintage() {
        // Arrange
        RegistryScene   scene = RegistryScene.Open();
        CreditSerial    unit  = Any.CreditSerial().WithRegistry(scene.Registry).WithVintage(2022).Generate();
        scene.Ledger.Issue(new[] { unit }, scene.First.AccountId, scene.At(0));
        RetirementClaim claim2021 = Any.RetirementClaim().WithReportingYear(2021).Generate();

        // Act
        Assert.Throws<ArgumentException>(() => scene.Ledger.Retire(new[] { unit }, scene.First.AccountId, scene.Retirement.AccountId, claim2021, scene.At(1)));

        // Assert
        Assert.False(scene.Ledger.IsRetired(unit));
        Assert.Same(scene.First, scene.Ledger.HolderOf(unit));
        Assert.Single(scene.Ledger.Journal);
    }

    [Fact]
    public void Units_can_offset_emissions_of_the_year_of_their_vintage() {
        // Arrange
        RegistryScene   scene = RegistryScene.Open();
        CreditSerial    unit  = Any.CreditSerial().WithRegistry(scene.Registry).WithVintage(2022).Generate();
        scene.Ledger.Issue(new[] { unit }, scene.First.AccountId, scene.At(0));
        RetirementClaim claim2022 = Any.RetirementClaim().WithReportingYear(2022).Generate();

        // Act
        scene.Ledger.Retire(new[] { unit }, scene.First.AccountId, scene.Retirement.AccountId, claim2022, scene.At(1));

        // Assert
        Assert.True(scene.Ledger.IsRetired(unit));
    }

    // ---- when -----------------------------------------------------------------------------------------------------

    [Fact]
    public void A_retirement_cannot_be_recorded_before_the_last_entry() {
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1, day: 5);

        Assert.Throws<LedgerRuleViolationException>(() => scene.RetireFrom(scene.First, units.ToList(), 4));

        Assert.False(scene.Ledger.IsRetired(units[0]));
    }

    [Fact]
    public void A_retirement_cannot_be_recorded_before_the_retirement_account_was_opened() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 1);
        Account lateRetirement = scene.Stranger();
        lateRetirement = new Account(lateRetirement.AccountId, lateRetirement.Holder, AccountKind.Retirement, scene.At(10));
        scene.Ledger.OpenAccount(lateRetirement);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scene.Ledger.Retire(units, scene.First.AccountId, lateRetirement.AccountId, scene.ClaimFor(units), scene.At(9)));
    }

}
