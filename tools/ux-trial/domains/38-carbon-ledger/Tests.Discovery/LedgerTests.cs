using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class OpeningTests {

    [Fact]
    public void A_ledger_is_founded_only_on_an_issuance_account() {
        // Arrange
        string  anyRegistry = Any.String().Alpha().InUpperCase().WithLengthBetween(2, 5).Generate();
        Account anyHolding  = Any.Account().WithKind(AccountKind.Holding).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger(anyRegistry, anyHolding));

        // Assert
        Assert.Equal("issuanceAccount", error.ParamName);
    }

    [Fact]
    public void A_registry_refuses_a_second_issuance_account() {
        // Arrange
        RegistryScenario scenario      = RegistryScenario.Open();
        Account          anyOtherIssuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        // Act
        void Open() => scenario.Ledger.OpenAccount(anyOtherIssuance);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Open);
    }

    [Fact]
    public void A_registry_refuses_to_open_the_same_account_id_twice() {
        // Arrange
        RegistryScenario scenario = RegistryScenario.Open();
        Account          anySameId = Any.Account()
                                        .WithHolder(scenario.Seller.Holder)
                                        .WithKind(AccountKind.Holding)
                                        .Generate();
        Account          duplicate = new(scenario.Seller.AccountId, anySameId.Holder, anySameId.Kind, anySameId.OpenedOn);

        // Act
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(() => scenario.Ledger.OpenAccount(duplicate));

        // Assert
        Assert.Contains(scenario.Seller.AccountId, error.Message);
    }

}

public sealed class IssuanceTests {

    [Fact]
    public void Issued_units_are_held_by_the_receiving_account_and_drawn_from_the_issuance_account() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(3);

        // Act
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(1));

        // Assert
        Assert.Equal(3, scenario.Ledger.Balance(scenario.Seller.AccountId));
        Assert.Equal(-3, scenario.Ledger.Balance(scenario.Issuance.AccountId));
        Assert.Equal(3, scenario.Ledger.IssuedQuantity);
        Assert.All(anySerials, s => Assert.Same(scenario.Seller, scenario.Ledger.HolderOf(s)));
    }

    [Fact]
    public void A_unit_of_another_registry_cannot_be_issued() {
        // Arrange
        RegistryScenario scenario        = RegistryScenario.Open();
        CreditSerial     anyForeignSerial = Any.CreditSerial()
                                               .WithRegistry(r => r.DifferentFrom(scenario.Registry))
                                               .Generate();

        // Act
        void Issue() => scenario.Ledger.Issue([anyForeignSerial], scenario.Seller.AccountId, scenario.At(1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Issue);
        Assert.Empty(scenario.Ledger.Journal);
    }

    [Fact]
    public void A_unit_cannot_be_issued_twice() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));

        // Act
        void IssueAgain() => scenario.Ledger.Issue([anySerial], scenario.Buyer.AccountId, scenario.At(2));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(IssueAgain);
        Assert.Same(scenario.Seller, scenario.Ledger.HolderOf(anySerial));
    }

    [Fact]
    public void Units_cannot_be_issued_into_an_account_the_registry_never_opened() {
        // Arrange
        RegistryScenario scenario     = RegistryScenario.Open();
        CreditSerial     anySerial    = scenario.AnySerial().Generate();
        Account          anyUnopened  = Any.Account().WithKind(AccountKind.Holding).Generate();

        // Act
        void Issue() => scenario.Ledger.Issue([anySerial], anyUnopened.AccountId, scenario.At(1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Issue);
    }

}

public sealed class TransferTests {

    [Fact]
    public void A_transfer_moves_the_units_and_keeps_the_ledger_balanced() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(3);
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(1));

        // Act
        scenario.Ledger.Transfer([anySerials[0]], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(2));

        // Assert
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Seller.AccountId));
        Assert.Equal(1, scenario.Ledger.Balance(scenario.Buyer.AccountId));
        Assert.Same(scenario.Buyer, scenario.Ledger.HolderOf(anySerials[0]));
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void An_account_cannot_transfer_a_unit_it_does_not_hold() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));

        // Act
        void Transfer() => scenario.Ledger.Transfer([anySerial], scenario.Buyer.AccountId, scenario.Seller.AccountId, scenario.At(2));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Transfer);
        Assert.Same(scenario.Seller, scenario.Ledger.HolderOf(anySerial));
    }

    [Fact]
    public void A_unit_never_issued_cannot_be_transferred() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();

        // Act
        void Transfer() => scenario.Ledger.Transfer([anySerial], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(1));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Transfer);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_last_one() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(2);
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(5));

        // Act
        void Backdated() => scenario.Ledger.Transfer([anySerials[0]], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(4));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(Backdated);
        Assert.Single(scenario.Ledger.Journal);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_its_receiving_account_was_opened() {
        // Arrange
        RegistryScenario scenario    = RegistryScenario.Open();
        CreditSerial     anySerial   = scenario.AnySerial().Generate();
        Account          lateAccount = Any.Account()
                                          .WithKind(AccountKind.Holding)
                                          .WithOpenedOn(scenario.At(10))
                                          .Generate();
        scenario.Ledger.OpenAccount(lateAccount);
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));

        // Act
        void TooEarly() => scenario.Ledger.Transfer([anySerial], scenario.Seller.AccountId, lateAccount.AccountId, scenario.At(9));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(TooEarly);
    }

    [Fact]
    public void A_transfer_cannot_credit_a_retirement_account() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));

        // Act
        void Transfer() => scenario.Ledger.Transfer([anySerial], scenario.Seller.AccountId, scenario.Retirement.AccountId, scenario.At(2));

        // Assert
        Assert.Throws<ArgumentException>(Transfer);
        Assert.False(scenario.Ledger.IsRetired(anySerial));
    }

}

public sealed class RetirementTests {

    [Fact]
    public void A_retired_unit_is_locked_in_the_retirement_account() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(2);
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(1));

        // Act
        scenario.Ledger.Retire([anySerials[0]], scenario.Seller.AccountId, scenario.Retirement.AccountId,
                               scenario.AnyClaimFor([anySerials[0]]), scenario.At(2));

        // Assert
        Assert.True(scenario.Ledger.IsRetired(anySerials[0]));
        Assert.False(scenario.Ledger.IsRetired(anySerials[1]));
        Assert.Equal(1, scenario.Ledger.RetiredQuantity);
        Assert.Equal(1, scenario.Ledger.Balance(scenario.Retirement.AccountId));
        Assert.Equal(2, scenario.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_transferred() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));
        scenario.Ledger.Retire([anySerial], scenario.Seller.AccountId, scenario.Retirement.AccountId,
                               scenario.AnyClaimFor([anySerial]), scenario.At(2));

        // Act
        void Transfer() => scenario.Ledger.Transfer([anySerial], scenario.Retirement.AccountId, scenario.Buyer.AccountId, scenario.At(3));

        // Assert
        LedgerRuleViolationException error = Assert.Throws<LedgerRuleViolationException>(Transfer);
        Assert.Contains("retired", error.Message);
    }

    [Fact]
    public void A_unit_cannot_be_retired_twice() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));
        scenario.Ledger.Retire([anySerial], scenario.Seller.AccountId, scenario.Retirement.AccountId,
                               scenario.AnyClaimFor([anySerial]), scenario.At(2));

        // Act
        void RetireAgain() => scenario.Ledger.Retire([anySerial], scenario.Retirement.AccountId, scenario.Retirement.AccountId,
                                                     scenario.AnyClaimFor([anySerial]), scenario.At(3));

        // Assert
        Assert.Throws<LedgerRuleViolationException>(RetireAgain);
        Assert.Equal(1, scenario.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Units_cannot_offset_emissions_reported_before_their_vintage() {
        // Arrange
        RegistryScenario scenario   = RegistryScenario.Open();
        CreditSerial     serial2021 = scenario.AnySerial().WithVintage(2021).Generate();
        RetirementClaim  claim2020  = Any.RetirementClaim().WithReportingYear(2020).Generate();
        scenario.Ledger.Issue([serial2021], scenario.Seller.AccountId, scenario.At(1));

        // Act
        void Retire() => scenario.Ledger.Retire([serial2021], scenario.Seller.AccountId, scenario.Retirement.AccountId, claim2020, scenario.At(2));

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(Retire);
        Assert.Equal("claim", error.ParamName);
        Assert.False(scenario.Ledger.IsRetired(serial2021));
    }

    [Fact]
    public void The_retirement_entry_records_the_claim_it_was_made_for() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        RetirementClaim  claim     = scenario.AnyClaimFor([anySerial]);
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));

        // Act
        JournalEntry entry = scenario.Ledger.Retire([anySerial], scenario.Seller.AccountId, scenario.Retirement.AccountId, claim, scenario.At(2));

        // Assert
        Assert.Equal(EntryKind.Retirement, entry.Kind);
        Assert.Same(claim, entry.Claim);
        Assert.Equal(2L, entry.Sequence);
    }

}

public sealed class HistoryTests {

    [Fact]
    public void A_units_history_lists_each_holder_in_turn_and_leaves_the_last_period_open() {
        // Arrange
        RegistryScenario scenario  = RegistryScenario.Open();
        CreditSerial     anySerial = scenario.AnySerial().Generate();
        scenario.Ledger.Issue([anySerial], scenario.Seller.AccountId, scenario.At(1));
        scenario.Ledger.Transfer([anySerial], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(2));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(anySerial);

        // Assert
        Assert.Collection(history,
            first => {
                Assert.Equal(scenario.Seller.AccountId, first.AccountId);
                Assert.Equal(scenario.At(1), first.From);
                Assert.Equal(scenario.At(2), first.Until);
            },
            second => {
                Assert.Equal(scenario.Buyer.AccountId, second.AccountId);
                Assert.Equal(scenario.At(2), second.From);
                Assert.True(second.IsCurrent);
            });
    }

    [Fact]
    public void Only_the_units_an_account_currently_holds_are_listed_for_it() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(3);
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(1));
        scenario.Ledger.Transfer([anySerials[1]], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(2));

        // Act
        IReadOnlyList<CreditSerial> held = scenario.Ledger.UnitsHeldBy(scenario.Seller.AccountId);

        // Assert
        Assert.Equal([anySerials[0], anySerials[2]], held.OrderBy(anySerials.IndexOf));
    }

    [Fact]
    public void The_derived_state_stays_consistent_with_the_journal_through_issue_transfer_and_retirement() {
        // Arrange
        RegistryScenario   scenario   = RegistryScenario.Open();
        List<CreditSerial> anySerials = scenario.AnyDistinctSerials(4);
        scenario.Ledger.Issue(anySerials, scenario.Seller.AccountId, scenario.At(1));
        scenario.Ledger.Transfer(anySerials[..2], scenario.Seller.AccountId, scenario.Buyer.AccountId, scenario.At(2));
        scenario.Ledger.Retire([anySerials[0]], scenario.Buyer.AccountId, scenario.Retirement.AccountId,
                               scenario.AnyClaimFor([anySerials[0]]), scenario.At(3));

        // Act
        bool consistent = scenario.Ledger.IsConsistent();

        // Assert
        Assert.True(consistent);
        Assert.Equal([1L, 2L, 3L], scenario.Ledger.Journal.Select(e => e.Sequence));
    }

}
