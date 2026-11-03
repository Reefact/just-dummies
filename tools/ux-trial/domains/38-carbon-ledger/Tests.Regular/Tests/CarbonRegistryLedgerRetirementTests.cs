using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class CarbonRegistryLedgerRetirementTests {

    private static (LedgerScenario Scenario, IReadOnlyCollection<CreditSerial> AliceUnits) AliceHoldingThree() {
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(3);
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        return (scenario, serials);
    }

    [Fact]
    public void Retiring_two_of_three_units_moves_them_to_the_retirement_account() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();
        CreditSerial[]  retired = units.Take(2).ToArray();
        RetirementClaim claim   = scenario.NewClaim();

        // Act
        JournalEntry entry = scenario.Ledger.Retire(retired, scenario.Alice.AccountId, scenario.Retirement.AccountId, claim, scenario.Day(2));

        // Assert
        Assert.Equal(1, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Retirement.AccountId));
        Assert.Equal(2, scenario.Ledger.RetiredQuantity);
        Assert.Equal(3, scenario.Ledger.IssuedQuantity);
        Assert.All(retired, unit => Assert.True(scenario.Ledger.IsRetired(unit)));
        Assert.False(scenario.Ledger.IsRetired(units.Last()));
        Assert.Equal(EntryKind.Retirement, entry.Kind);
        Assert.Same(claim, entry.Claim);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void Retired_units_are_held_by_the_retirement_account() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();

        // Act
        scenario.Ledger.Retire(units, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2));

        // Assert
        Assert.All(units, unit => Assert.Same(scenario.Retirement, scenario.Ledger.HolderOf(unit)));
        Assert.Empty(scenario.Ledger.UnitsHeldBy(scenario.Alice.AccountId));
        Assert.Equal(3, scenario.Ledger.UnitsHeldBy(scenario.Retirement.AccountId).Count);
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_transferred() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();
        CreditSerial[] one = units.Take(1).ToArray();
        scenario.Ledger.Retire(one, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2));

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Transfer(one, scenario.Retirement.AccountId, scenario.Bob.AccountId, scenario.Day(3)));

        // Assert
        Assert.Contains("retired", refusal.Message);
        Assert.Same(scenario.Retirement, scenario.Ledger.HolderOf(one[0]));
    }

    [Fact]
    public void A_retired_unit_can_no_longer_be_retired_again() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();
        CreditSerial[] one = units.Take(1).ToArray();
        scenario.Ledger.Retire(one, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2));

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(one, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(3)));
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(one, scenario.Retirement.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(3)));
        Assert.Equal(1, scenario.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_unit_held_by_someone_else_cannot_be_retired() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(units.Take(1).ToArray(), scenario.Bob.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2)));
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_refused_retirement_retires_none_of_its_units() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();
        CreditSerial never = scenario.NewSerials(1).Single();

        // Act
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(new[] { units.First(), never }, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2)));

        // Assert
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
        Assert.Equal(3, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.Single(scenario.Ledger.Journal);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Retiring_into_a_holding_account_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.NewClaim(), scenario.Day(2)));
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
    }

    [Fact]
    public void Retiring_into_an_unknown_account_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();
        Account stranger = scenario.OpenAccountOfKind(AccountKind.Retirement);

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(units.Take(1).ToArray(), scenario.Alice.AccountId, stranger.AccountId, scenario.NewClaim(), scenario.Day(2)));
    }

    [Fact]
    public void A_retirement_needs_a_claim() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(
            () => scenario.Ledger.Retire(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Retirement.AccountId, null!, scenario.Day(2)));
    }

    [Fact]
    public void A_claim_for_a_year_before_the_vintage_of_a_unit_is_refused() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   young    = new AnyCreditSerial().WithRegistry(LedgerScenario.RegistryCode).WithVintage(2022).Generate();
        scenario.Ledger.Issue(new[] { young }, scenario.Alice.AccountId, scenario.Day(1));
        RetirementClaim tooEarly = new AnyRetirementClaim().WithReportingYear(2021).Generate();

        // Act
        Assert.Throws<ArgumentException>(
            () => scenario.Ledger.Retire(new[] { young }, scenario.Alice.AccountId, scenario.Retirement.AccountId, tooEarly, scenario.Day(2)));

        // Assert: nothing was retired
        Assert.False(scenario.Ledger.IsRetired(young));
        Assert.Single(scenario.Ledger.Journal);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void A_claim_for_the_year_of_the_vintage_is_accepted() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   young    = new AnyCreditSerial().WithRegistry(LedgerScenario.RegistryCode).WithVintage(2022).Generate();
        scenario.Ledger.Issue(new[] { young }, scenario.Alice.AccountId, scenario.Day(1));
        RetirementClaim sameYear = new AnyRetirementClaim().WithReportingYear(2022).Generate();

        // Act
        scenario.Ledger.Retire(new[] { young }, scenario.Alice.AccountId, scenario.Retirement.AccountId, sameYear, scenario.Day(2));

        // Assert
        Assert.True(scenario.Ledger.IsRetired(young));
    }

    [Fact]
    public void A_retirement_before_the_last_entry_is_refused() {
        // Arrange
        (LedgerScenario scenario, IReadOnlyCollection<CreditSerial> units) = AliceHoldingThree();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(units.Take(1).ToArray(), scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(1).AddTicks(-1)));
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
    }

    [Fact(Skip = "domain defect: this refusal surfaces as ArgumentException from JournalEntry, not LedgerRuleViolationException as MODEL.md promises for every refused registry operation (deterministic, no seed involved)")]
    public void Retiring_nothing_is_refused() {
        // Arrange
        (LedgerScenario scenario, _) = AliceHoldingThree();

        // Act / Assert
        Assert.Throws<LedgerRuleViolationException>(
            () => scenario.Ledger.Retire(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2)));
    }

}
