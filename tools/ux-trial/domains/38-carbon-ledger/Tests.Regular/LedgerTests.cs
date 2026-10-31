using System;
using System.Collections.Generic;

using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular;

public sealed class OpeningTheRegistryTests {

    [Fact]
    public void A_ledger_is_founded_on_an_issuance_account_only() {
        // Arrange
        string  anyRegistry = Any.StringMatching("[A-Z]{2,5}").Generate();
        Account anyHolding  = Arbitrary.Account().WithKind(AccountKind.Holding).Generate();

        // Act
        Exception? error = Record.Exception(() => new CarbonRegistryLedger(anyRegistry, anyHolding));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_registry_has_a_single_issuance_account() {
        // Arrange
        RegistryScenario anyRegistry          = RegistryScenario.Open();
        Account          anySecondIssuance    = RegistryScenario.AnAccountOpenedBeforeFirstPosting().WithKind(AccountKind.Issuance).Generate();

        // Act
        Exception? error = Record.Exception(() => anyRegistry.Ledger.OpenAccount(anySecondIssuance));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
    }

    [Fact]
    public void An_account_cannot_be_opened_twice() {
        // Arrange
        RegistryScenario anyRegistry = RegistryScenario.Open();
        Account          anyHolding  = anyRegistry.OpenAccount(AccountKind.Holding);

        // Act
        Exception? error = Record.Exception(() => anyRegistry.Ledger.OpenAccount(anyHolding));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
    }

}

public sealed class IssuanceTests {

    [Fact]
    public void Issuing_units_credits_the_holding_account_and_debits_the_issuance_account() {
        // Arrange
        RegistryScenario   anyRegistry   = RegistryScenario.Open();
        Account            anyHolding    = anyRegistry.OpenAccount(AccountKind.Holding);
        List<CreditSerial> anyThreeUnits = anyRegistry.Units(3);

        // Act
        anyRegistry.Ledger.Issue(anyThreeUnits, anyHolding.AccountId, anyRegistry.NextMoment());

        // Assert
        Assert.Equal(3, anyRegistry.Ledger.Balance(anyHolding.AccountId));
        Assert.Equal(-3, anyRegistry.Ledger.Balance(anyRegistry.IssuanceAccount.AccountId));
        Assert.Equal(3, anyRegistry.Ledger.IssuedQuantity);
    }

    [Fact]
    public void A_registry_issues_only_its_own_units() {
        // Arrange
        RegistryScenario anyRegistry     = RegistryScenario.Open();
        Account          anyHolding      = anyRegistry.OpenAccount(AccountKind.Holding);
        CreditSerial     anyForeignUnit  = Arbitrary.CreditSerial().WithRegistry(r => r.DifferentFrom(anyRegistry.Registry)).Generate();

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Issue([anyForeignUnit], anyHolding.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
    }

    [Fact]
    public void A_unit_is_issued_only_once() {
        // Arrange
        RegistryScenario anyRegistry     = RegistryScenario.Open();
        CreditSerial     anyUnit         = anyRegistry.Unit().Generate();
        anyRegistry.HoldingAccountWith([anyUnit]);
        Account          anyOtherHolding = anyRegistry.OpenAccount(AccountKind.Holding);

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Issue([anyUnit], anyOtherHolding.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Equal(1, anyRegistry.Ledger.IssuedQuantity);
    }

    [Fact]
    public void Units_cannot_be_issued_into_an_account_the_registry_does_not_know() {
        // Arrange
        RegistryScenario   anyRegistry   = RegistryScenario.Open();
        Account            anyStranger   = RegistryScenario.AnAccountOpenedBeforeFirstPosting().WithKind(AccountKind.Holding).Generate();
        List<CreditSerial> anyUnits      = anyRegistry.SomeUnits();

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Issue(anyUnits, anyStranger.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Empty(anyRegistry.Ledger.Journal);
    }

}

public sealed class TransferTests {

    [Fact]
    public void A_transfer_hands_the_units_to_the_receiving_account() {
        // Arrange
        RegistryScenario   anyRegistry  = RegistryScenario.Open();
        List<CreditSerial> anyTwoUnits  = anyRegistry.Units(2);
        Account            anySender    = anyRegistry.HoldingAccountWith(anyTwoUnits);
        Account            anyReceiver  = anyRegistry.OpenAccount(AccountKind.Holding);

        // Act
        anyRegistry.Ledger.Transfer([anyTwoUnits[0]], anySender.AccountId, anyReceiver.AccountId, anyRegistry.NextMoment());

        // Assert
        Assert.Same(anyReceiver, anyRegistry.Ledger.HolderOf(anyTwoUnits[0]));
        Assert.Same(anySender, anyRegistry.Ledger.HolderOf(anyTwoUnits[1]));
        Assert.Equal(1, anyRegistry.Ledger.Balance(anySender.AccountId));
        Assert.Equal(1, anyRegistry.Ledger.Balance(anyReceiver.AccountId));
    }

    [Fact]
    public void An_account_cannot_transfer_a_unit_it_does_not_hold() {
        // Arrange
        RegistryScenario   anyRegistry  = RegistryScenario.Open();
        List<CreditSerial> anyUnits     = anyRegistry.SomeUnits();
        Account            anyHolder    = anyRegistry.HoldingAccountWith(anyUnits);
        Account            anyImpostor  = anyRegistry.OpenAccount(AccountKind.Holding);
        Account            anyReceiver  = anyRegistry.OpenAccount(AccountKind.Holding);

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Transfer(anyUnits, anyImpostor.AccountId, anyReceiver.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Equal(anyUnits, anyRegistry.Ledger.UnitsHeldBy(anyHolder.AccountId));
    }

    [Fact]
    public void A_unit_that_was_never_issued_cannot_be_transferred() {
        // Arrange
        RegistryScenario anyRegistry   = RegistryScenario.Open();
        Account          anySender     = anyRegistry.OpenAccount(AccountKind.Holding);
        Account          anyReceiver   = anyRegistry.OpenAccount(AccountKind.Holding);
        CreditSerial     anyUnissued   = anyRegistry.Unit().Generate();

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Transfer([anyUnissued], anySender.AccountId, anyReceiver.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
    }

    [Fact]
    public void The_journal_refuses_an_entry_dated_before_the_last_one() {
        // Arrange
        RegistryScenario   anyRegistry = RegistryScenario.Open();
        List<CreditSerial> anyUnits    = anyRegistry.SomeUnits();
        Account            anySender   = anyRegistry.HoldingAccountWith(anyUnits);
        Account            anyReceiver = anyRegistry.OpenAccount(AccountKind.Holding);
        DateTimeOffset     lastEntry   = anyRegistry.Ledger.Journal[^1].RecordedAt;

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Transfer(anyUnits, anySender.AccountId, anyReceiver.AccountId, lastEntry.AddSeconds(-1)));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Single(anyRegistry.Ledger.Journal);
    }

    [Fact]
    public void An_entry_cannot_be_recorded_before_the_receiving_account_was_opened() {
        // Arrange
        RegistryScenario   anyRegistry  = RegistryScenario.Open();
        List<CreditSerial> anyUnits     = anyRegistry.SomeUnits();
        Account            anySender    = anyRegistry.HoldingAccountWith(anyUnits);
        DateTimeOffset     lastEntry    = anyRegistry.Ledger.Journal[^1].RecordedAt;
        Account            anyLateComer = Arbitrary.Account().WithKind(AccountKind.Holding).WithOpenedOn(lastEntry.AddDays(10)).Generate();
        anyRegistry.Ledger.OpenAccount(anyLateComer);

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Transfer(anyUnits, anySender.AccountId, anyLateComer.AccountId, lastEntry.AddDays(9)));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
    }

}

public sealed class RetirementTests {

    [Fact]
    public void A_retired_unit_is_locked_in_the_retirement_account() {
        // Arrange
        RegistryScenario   anyRegistry   = RegistryScenario.Open();
        List<CreditSerial> anyThreeUnits = anyRegistry.Units(3);
        Account            anyHolding    = anyRegistry.HoldingAccountWith(anyThreeUnits);
        Account            anyRetirement = anyRegistry.OpenAccount(AccountKind.Retirement);
        RetirementClaim    anyClaim      = RegistryScenario.ClaimFor(anyThreeUnits);

        // Act
        anyRegistry.Ledger.Retire(anyThreeUnits, anyHolding.AccountId, anyRetirement.AccountId, anyClaim, anyRegistry.NextMoment());

        // Assert
        Assert.All(anyThreeUnits, unit => Assert.True(anyRegistry.Ledger.IsRetired(unit)));
        Assert.Equal(3, anyRegistry.Ledger.RetiredQuantity);
        Assert.Equal(3, anyRegistry.Ledger.Balance(anyRetirement.AccountId));
        Assert.Equal(0, anyRegistry.Ledger.Balance(anyHolding.AccountId));
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_transferred() {
        // Arrange
        RegistryScenario anyRegistry   = RegistryScenario.Open();
        CreditSerial     anyUnit       = anyRegistry.Unit().Generate();
        Account          anyHolding    = anyRegistry.HoldingAccountWith([anyUnit]);
        Account          anyRetirement = anyRegistry.OpenAccount(AccountKind.Retirement);
        Account          anyBuyer      = anyRegistry.OpenAccount(AccountKind.Holding);
        anyRegistry.Ledger.Retire([anyUnit], anyHolding.AccountId, anyRetirement.AccountId, RegistryScenario.ClaimFor([anyUnit]), anyRegistry.NextMoment());

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Transfer([anyUnit], anyRetirement.AccountId, anyBuyer.AccountId, anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Same(anyRetirement, anyRegistry.Ledger.HolderOf(anyUnit));
    }

    [Fact]
    public void A_unit_cannot_be_retired_twice() {
        // Arrange
        RegistryScenario anyRegistry        = RegistryScenario.Open();
        CreditSerial     anyUnit            = anyRegistry.Unit().Generate();
        Account          anyHolding         = anyRegistry.HoldingAccountWith([anyUnit]);
        Account          anyRetirement      = anyRegistry.OpenAccount(AccountKind.Retirement);
        Account          anyOtherRetirement = anyRegistry.OpenAccount(AccountKind.Retirement);
        anyRegistry.Ledger.Retire([anyUnit], anyHolding.AccountId, anyRetirement.AccountId, RegistryScenario.ClaimFor([anyUnit]), anyRegistry.NextMoment());

        // Act
        Exception? error = Record.Exception(
            () => anyRegistry.Ledger.Retire([anyUnit], anyRetirement.AccountId, anyOtherRetirement.AccountId, RegistryScenario.ClaimFor([anyUnit]), anyRegistry.NextMoment()));

        // Assert
        Assert.IsType<LedgerRuleViolationException>(error);
        Assert.Equal(1, anyRegistry.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_partial_retirement_leaves_the_other_units_transferable() {
        // Arrange
        RegistryScenario   anyRegistry   = RegistryScenario.Open();
        List<CreditSerial> anyTwoUnits   = anyRegistry.Units(2);
        Account            anyHolding    = anyRegistry.HoldingAccountWith(anyTwoUnits);
        Account            anyRetirement = anyRegistry.OpenAccount(AccountKind.Retirement);
        Account            anyBuyer      = anyRegistry.OpenAccount(AccountKind.Holding);
        anyRegistry.Ledger.Retire([anyTwoUnits[0]], anyHolding.AccountId, anyRetirement.AccountId, RegistryScenario.ClaimFor([anyTwoUnits[0]]), anyRegistry.NextMoment());

        // Act
        anyRegistry.Ledger.Transfer([anyTwoUnits[1]], anyHolding.AccountId, anyBuyer.AccountId, anyRegistry.NextMoment());

        // Assert
        Assert.Same(anyBuyer, anyRegistry.Ledger.HolderOf(anyTwoUnits[1]));
        Assert.False(anyRegistry.Ledger.IsRetired(anyTwoUnits[1]));
    }

}

public sealed class DerivedStateTests {

    [Fact]
    public void The_history_of_a_unit_is_rebuilt_from_the_journal() {
        // Arrange
        RegistryScenario anyRegistry = RegistryScenario.Open();
        CreditSerial     anyUnit     = anyRegistry.Unit().Generate();
        Account          anyFirst    = anyRegistry.OpenAccount(AccountKind.Holding);
        Account          anySecond   = anyRegistry.OpenAccount(AccountKind.Holding);
        DateTimeOffset   issuedAt    = new DateTimeOffset(2023, 3, 1, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset   soldAt      = new DateTimeOffset(2024, 7, 15, 14, 30, 0, TimeSpan.Zero);
        anyRegistry.Ledger.Issue([anyUnit], anyFirst.AccountId, issuedAt);
        anyRegistry.Ledger.Transfer([anyUnit], anyFirst.AccountId, anySecond.AccountId, soldAt);

        // Act
        IReadOnlyList<HoldingPeriod> history = anyRegistry.Ledger.HistoryOf(anyUnit);

        // Assert
        Assert.Collection(history,
            first => {
                Assert.Equal(anyFirst.AccountId, first.AccountId);
                Assert.Equal(issuedAt, first.From);
                Assert.Equal(soldAt, first.Until);
            },
            second => {
                Assert.Equal(anySecond.AccountId, second.AccountId);
                Assert.Equal(soldAt, second.From);
                Assert.True(second.IsCurrent);
            });
    }

    [Fact]
    public void A_unit_that_was_never_issued_has_no_history() {
        // Arrange
        RegistryScenario anyRegistry = RegistryScenario.Open();
        anyRegistry.HoldingAccountWith(anyRegistry.SomeUnits());
        CreditSerial     anyOtherUnit = anyRegistry.Unit().Generate();

        // Act
        IReadOnlyList<HoldingPeriod> history = anyRegistry.Ledger.HistoryOf(anyOtherUnit);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public void Issuing_transferring_and_retiring_keep_the_ledger_consistent() {
        // Arrange
        RegistryScenario   anyRegistry   = RegistryScenario.Open();
        List<CreditSerial> anyFourUnits  = anyRegistry.Units(4);
        Account            anyHolding    = anyRegistry.HoldingAccountWith(anyFourUnits);
        Account            anyBuyer      = anyRegistry.OpenAccount(AccountKind.Holding);
        Account            anyRetirement = anyRegistry.OpenAccount(AccountKind.Retirement);
        anyRegistry.Ledger.Transfer(anyFourUnits[..3], anyHolding.AccountId, anyBuyer.AccountId, anyRegistry.NextMoment());
        anyRegistry.Ledger.Retire(anyFourUnits[..2], anyBuyer.AccountId, anyRetirement.AccountId, RegistryScenario.ClaimFor(anyFourUnits[..2]), anyRegistry.NextMoment());

        // Act
        bool consistent = anyRegistry.Ledger.IsConsistent();

        // Assert
        Assert.True(consistent);
        Assert.Equal(1, anyRegistry.Ledger.Balance(anyHolding.AccountId));
        Assert.Equal(1, anyRegistry.Ledger.Balance(anyBuyer.AccountId));
        Assert.Equal(2, anyRegistry.Ledger.Balance(anyRetirement.AccountId));
        Assert.Equal(-4, anyRegistry.Ledger.Balance(anyRegistry.IssuanceAccount.AccountId));
    }

}
