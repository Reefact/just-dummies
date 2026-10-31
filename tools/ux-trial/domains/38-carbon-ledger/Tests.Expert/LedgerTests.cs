using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert;

public sealed class OpeningTests {

    [Fact]
    public void A_ledger_is_founded_on_an_issuance_account_only() {
        // Arrange
        string  anyRegistry = CarbonRecipes.RegistryCode().Generate();
        Account anyHolding  = Any.Account().WithKind(AccountKind.Holding).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger(anyRegistry, anyHolding));

        // Assert
        Assert.Equal("issuanceAccount", error.ParamName);
    }

    [Fact]
    public void A_registry_has_a_single_issuance_account() {
        // Arrange
        RegistryScenario anyRegistry          = new();
        Account          anotherIssuance      = Any.Account().WithKind(AccountKind.Issuance).Generate();

        // Act
        Action open = () => anyRegistry.Ledger.OpenAccount(anotherIssuance);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(open);
    }

    [Fact]
    public void An_account_cannot_be_opened_twice() {
        // Arrange
        RegistryScenario anyRegistry = new();
        Account          anyHolding  = anyRegistry.OpenHolding();

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(() => anyRegistry.Ledger.OpenAccount(anyHolding));

        // Assert
        Assert.Contains("already open", error.Message);
    }

    [Fact]
    public void Posting_to_an_account_the_registry_never_opened_is_refused() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               notOpened     = Any.Account().WithKind(AccountKind.Holding).Generate();
        HashSet<CreditSerial> anyUnits      = anyRegistry.NewUnits(1);

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Issue(anyUnits, notOpened.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains("not open", error.Message);
        Assert.Equal(0, anyRegistry.Ledger.IssuedQuantity);
    }

}

public sealed class IssuanceTests {

    [Fact]
    public void Issuing_three_units_credits_the_holding_account_and_debits_the_issuance_account() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anyHolding  = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyUnits    = anyRegistry.NewUnits(3);

        // Act
        anyRegistry.Ledger.Issue(anyUnits, anyHolding.AccountId, anyRegistry.Later());

        // Assert
        Assert.Equal(3, anyRegistry.Ledger.Balance(anyHolding.AccountId));
        Assert.Equal(-3, anyRegistry.Ledger.Balance(anyRegistry.Issuance.AccountId));
        Assert.Equal(3, anyRegistry.Ledger.IssuedQuantity);
        Assert.True(anyRegistry.Ledger.IsConsistent());
    }

    [Fact]
    public void An_issuance_is_journalled_as_one_balanced_entry() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anyHolding  = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyUnits    = anyRegistry.NewUnits(2);

        // Act
        JournalEntry entry = anyRegistry.Ledger.Issue(anyUnits, anyHolding.AccountId, anyRegistry.Later());

        // Assert
        Assert.Equal(1L, entry.Sequence);
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Same(anyRegistry.Issuance, entry.Debit);
        Assert.Same(anyHolding, entry.Credit);
        Assert.Equal(2, entry.Quantity);
        Assert.Single(anyRegistry.Ledger.Journal);
    }

    [Fact]
    public void A_unit_cannot_be_issued_twice() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anyHolding  = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyIssued   = anyRegistry.IssueTo(anyHolding, 2);

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Issue(anyIssued, anyHolding.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains("already been issued", error.Message);
        Assert.Equal(2, anyRegistry.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_registry_issues_only_its_own_units() {
        // Arrange
        RegistryScenario anyRegistry   = new();
        Account          anyHolding    = anyRegistry.OpenHolding();
        CreditSerial     anyForeignUnit = Any.CreditSerial().WithRegistry(code => code.DifferentFrom(anyRegistry.Registry)).Generate();

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Issue([anyForeignUnit], anyHolding.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains("belongs to registry", error.Message);
        Assert.Empty(anyRegistry.Ledger.Journal);
    }

}

public sealed class TransferTests {

    [Fact]
    public void A_transfer_hands_the_units_to_the_buyer_and_keeps_the_ledger_balanced() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anySeller   = anyRegistry.OpenHolding();
        Account               anyBuyer    = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyUnits    = anyRegistry.IssueTo(anySeller, 5);

        // Act
        anyRegistry.Ledger.Transfer(anyUnits, anySeller.AccountId, anyBuyer.AccountId, anyRegistry.Later());

        // Assert
        Assert.All(anyUnits, unit => Assert.Same(anyBuyer, anyRegistry.Ledger.HolderOf(unit)));
        Assert.Equal(0, anyRegistry.Ledger.Balance(anySeller.AccountId));
        Assert.Equal(5, anyRegistry.Ledger.Balance(anyBuyer.AccountId));
        Assert.True(anyRegistry.Ledger.IsConsistent());
    }

    [Fact]
    public void A_partial_transfer_leaves_the_other_units_with_the_seller() {
        // Arrange
        RegistryScenario      anyRegistry  = new();
        Account               anySeller    = anyRegistry.OpenHolding();
        Account               anyBuyer     = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyIssued    = anyRegistry.IssueTo(anySeller, 5);
        List<CreditSerial>    anySold      = Any.ListOf(Any.ElementOf(anyIssued.ToList())).WithCount(2).Distinct().Generate();

        // Act
        anyRegistry.Ledger.Transfer(anySold, anySeller.AccountId, anyBuyer.AccountId, anyRegistry.Later());

        // Assert
        Assert.Equal(3, anyRegistry.Ledger.Balance(anySeller.AccountId));
        Assert.Equal(anyIssued.Except(anySold).ToHashSet(), anyRegistry.Ledger.UnitsHeldBy(anySeller.AccountId).ToHashSet());
        Assert.Equal(anySold.ToHashSet(), anyRegistry.Ledger.UnitsHeldBy(anyBuyer.AccountId).ToHashSet());
    }

    [Fact]
    public void A_seller_cannot_transfer_a_unit_another_account_holds() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               anySeller     = anyRegistry.OpenHolding();
        Account               anyOtherOwner = anyRegistry.OpenHolding();
        Account               anyBuyer      = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyNotHis     = anyRegistry.IssueTo(anyOtherOwner, 1);

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Transfer(anyNotHis, anySeller.AccountId, anyBuyer.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains($"held by {anyOtherOwner.AccountId}", error.Message);
        Assert.Equal(1, anyRegistry.Ledger.Balance(anyOtherOwner.AccountId));
    }

    [Fact]
    public void A_unit_never_issued_cannot_be_transferred() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anySeller   = anyRegistry.OpenHolding();
        Account               anyBuyer    = anyRegistry.OpenHolding();
        HashSet<CreditSerial> anyUnissued = anyRegistry.NewUnits(1);

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Transfer(anyUnissued, anySeller.AccountId, anyBuyer.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains("never been issued", error.Message);
    }

}

public sealed class RetirementTests {

    [Fact]
    public void A_retired_unit_can_no_longer_be_transferred() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               anyHolder     = anyRegistry.OpenHolding();
        Account               anyBuyer      = anyRegistry.OpenHolding();
        Account               anyRetirement = anyRegistry.OpenRetirement();
        HashSet<CreditSerial> anyUnits      = anyRegistry.IssueTo(anyHolder, 1);
        anyRegistry.Ledger.Retire(anyUnits, anyHolder.AccountId, anyRetirement.AccountId, anyRegistry.ClaimFor(anyUnits), anyRegistry.Later());

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Transfer(anyUnits, anyRetirement.AccountId, anyBuyer.AccountId, anyRegistry.Later()));

        // Assert
        Assert.Contains("retired", error.Message);
    }

    [Fact]
    public void A_unit_cannot_offset_emissions_twice() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               anyHolder     = anyRegistry.OpenHolding();
        Account               anyRetirement = anyRegistry.OpenRetirement();
        HashSet<CreditSerial> anyUnits      = anyRegistry.IssueTo(anyHolder, 2);
        anyRegistry.Ledger.Retire(anyUnits, anyHolder.AccountId, anyRetirement.AccountId, anyRegistry.ClaimFor(anyUnits), anyRegistry.Later());

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Retire(anyUnits, anyHolder.AccountId, anyRetirement.AccountId, anyRegistry.ClaimFor(anyUnits), anyRegistry.Later()));

        // Assert
        Assert.Contains("retired and can no longer be used", error.Message);
        Assert.Equal(2, anyRegistry.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Retiring_units_moves_them_to_the_retirement_account_and_counts_them_as_retired() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               anyHolder     = anyRegistry.OpenHolding();
        Account               anyRetirement = anyRegistry.OpenRetirement();
        HashSet<CreditSerial> anyUnits      = anyRegistry.IssueTo(anyHolder, 4);
        RetirementClaim       anyClaim      = anyRegistry.ClaimFor(anyUnits);

        // Act
        JournalEntry entry = anyRegistry.Ledger.Retire(anyUnits, anyHolder.AccountId, anyRetirement.AccountId, anyClaim, anyRegistry.Later());

        // Assert
        Assert.Same(anyClaim, entry.Claim);
        Assert.All(anyUnits, unit => Assert.True(anyRegistry.Ledger.IsRetired(unit)));
        Assert.Equal(4, anyRegistry.Ledger.Balance(anyRetirement.AccountId));
        Assert.Equal(4, anyRegistry.Ledger.RetiredQuantity);
        Assert.Equal(4, anyRegistry.Ledger.IssuedQuantity);
        Assert.True(anyRegistry.Ledger.IsConsistent());
    }

}

public sealed class JournalTests {

    [Fact]
    public void The_journal_refuses_an_entry_dated_before_the_last_one() {
        // Arrange
        RegistryScenario      anyRegistry = new();
        Account               anyHolding  = anyRegistry.OpenHolding();
        JournalEntry          anyLast     = anyRegistry.Ledger.Issue(anyRegistry.NewUnits(1), anyHolding.AccountId, anyRegistry.Later());
        DateTimeOffset        anyEarlier  = Any.DateTimeOffset().Before(anyLast.RecordedAt).AfterOrEqualTo(anyHolding.OpenedOn).Generate();

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Issue(anyRegistry.NewUnits(1), anyHolding.AccountId, anyEarlier));

        // Assert
        Assert.Contains("append-only", error.Message);
        Assert.Single(anyRegistry.Ledger.Journal);
    }

    [Fact]
    public void An_entry_cannot_predate_the_opening_of_its_accounts() {
        // Arrange
        RegistryScenario anyRegistry        = new();
        Account          anyHolding         = anyRegistry.OpenHolding();
        DateTimeOffset   anyBeforeOpening   = Any.DateTimeOffset().Before(anyHolding.OpenedOn).After(anyHolding.OpenedOn.AddYears(-5)).Generate();

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(
            () => anyRegistry.Ledger.Issue(anyRegistry.NewUnits(1), anyHolding.AccountId, anyBeforeOpening));

        // Assert
        Assert.Contains("before both its accounts were opened", error.Message);
    }

    [Fact]
    public void The_history_of_a_unit_is_rebuilt_from_the_journal() {
        // Arrange
        RegistryScenario anyRegistry   = new(startsAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Account          anyFirstOwner = anyRegistry.OpenHolding();
        Account          anyRetirement = anyRegistry.OpenRetirement();
        CreditSerial     anyUnit       = anyRegistry.Serial.WithVintage(2023).Generate();
        RetirementClaim  anyClaim      = Any.RetirementClaim().WithReportingYear(2024).Generate();
        DateTimeOffset   issuedAt      = new(2024, 2, 1, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset   retiredAt     = new(2024, 6, 30, 17, 0, 0, TimeSpan.Zero);
        anyRegistry.Ledger.Issue([anyUnit], anyFirstOwner.AccountId, issuedAt);
        anyRegistry.Ledger.Retire([anyUnit], anyFirstOwner.AccountId, anyRetirement.AccountId, anyClaim, retiredAt);

        // Act
        IReadOnlyList<HoldingPeriod> history = anyRegistry.Ledger.HistoryOf(anyUnit);

        // Assert
        Assert.Collection(
            history,
            held => {
                Assert.Equal(anyFirstOwner.AccountId, held.AccountId);
                Assert.Equal(issuedAt, held.From);
                Assert.Equal(retiredAt, held.Until);
            },
            retired => {
                Assert.Equal(anyRetirement.AccountId, retired.AccountId);
                Assert.Equal(retiredAt, retired.From);
                Assert.True(retired.IsCurrent);
            });
    }

    [Fact]
    public void Conservation_holds_across_issuances_transfers_and_retirements() {
        // Arrange
        RegistryScenario      anyRegistry   = new();
        Account               anyProject    = anyRegistry.OpenHolding();
        Account               anyBroker     = anyRegistry.OpenHolding();
        Account               anyRetirement = anyRegistry.OpenRetirement();
        HashSet<CreditSerial> anyIssued     = anyRegistry.IssueTo(anyProject, 6);
        List<CreditSerial>    anyBrokered   = anyIssued.Take(4).ToList();
        List<CreditSerial>    anyRetired    = anyBrokered.Take(1).ToList();

        // Act
        anyRegistry.Ledger.Transfer(anyBrokered, anyProject.AccountId, anyBroker.AccountId, anyRegistry.Later());
        anyRegistry.Ledger.Retire(anyRetired, anyBroker.AccountId, anyRetirement.AccountId, anyRegistry.ClaimFor(anyRetired), anyRegistry.Later());

        // Assert
        Assert.Equal(2, anyRegistry.Ledger.Balance(anyProject.AccountId));
        Assert.Equal(3, anyRegistry.Ledger.Balance(anyBroker.AccountId));
        Assert.Equal(1, anyRegistry.Ledger.Balance(anyRetirement.AccountId));
        Assert.Equal(-6, anyRegistry.Ledger.Balance(anyRegistry.Issuance.AccountId));
        Assert.True(anyRegistry.Ledger.IsConsistent());
    }

}
