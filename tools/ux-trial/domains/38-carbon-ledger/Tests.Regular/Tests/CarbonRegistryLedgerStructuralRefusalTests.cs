using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

/// <summary>
///     What is observable today whatever exception type a structurally invalid post ends in: it is refused,
///     and the ledger is left exactly as it was. (The exception type itself is pinned by the skipped
///     sibling tests, which follow MODEL.md.)
/// </summary>
public sealed class CarbonRegistryLedgerStructuralRefusalTests {

    private static (LedgerScenario Scenario, CreditSerial[] AliceUnits) AliceHoldingTwo() {
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial[] serials  = scenario.NewSerials(2).ToArray();
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));

        return (scenario, serials);
    }

    private static void AssertUntouched(LedgerScenario scenario, int journalCount) {
        Assert.Equal(journalCount, scenario.Ledger.Journal.Count);
        Assert.True(scenario.Ledger.IsConsistent());
    }

    [Fact]
    public void Issuing_no_unit_is_refused_and_records_nothing() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Issue(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Day(1)));
        AssertUntouched(scenario, 0);
    }

    [Fact]
    public void Issuing_into_an_account_that_is_not_a_holding_account_is_refused_and_records_nothing() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial[] serials  = scenario.NewSerials(1).ToArray();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Issue(serials, scenario.Retirement.AccountId, scenario.Day(1)));
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Issue(serials, scenario.Issuance.AccountId, scenario.Day(1)));
        AssertUntouched(scenario, 0);
        Assert.Equal(0, scenario.Ledger.IssuedQuantity);
    }

    [Fact]
    public void Issuing_the_same_unit_twice_in_one_call_is_refused_and_records_nothing() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Issue(new[] { serial, serial }, scenario.Alice.AccountId, scenario.Day(1)));
        AssertUntouched(scenario, 0);
        Assert.Equal(0, scenario.Ledger.IssuedQuantity);
    }

    [Fact]
    public void Transferring_to_the_sender_or_into_a_retirement_account_is_refused_and_moves_nothing() {
        // Arrange
        (LedgerScenario scenario, CreditSerial[] units) = AliceHoldingTwo();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Transfer(units, scenario.Alice.AccountId, scenario.Alice.AccountId, scenario.Day(2)));
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Transfer(units, scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.Day(2)));
        AssertUntouched(scenario, 1);
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Alice.AccountId));
        Assert.All(units, unit => Assert.False(scenario.Ledger.IsRetired(unit)));
    }

    [Fact]
    public void Transferring_or_retiring_nothing_is_refused_and_records_nothing() {
        // Arrange
        (LedgerScenario scenario, _) = AliceHoldingTwo();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Transfer(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2)));
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Retire(Array.Empty<CreditSerial>(), scenario.Alice.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(2)));
        AssertUntouched(scenario, 1);
    }

    [Fact]
    public void Retiring_into_a_holding_account_is_refused_and_retires_nothing() {
        // Arrange
        (LedgerScenario scenario, CreditSerial[] units) = AliceHoldingTwo();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Retire(units, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.NewClaim(), scenario.Day(2)));
        AssertUntouched(scenario, 1);
        Assert.Equal(0, scenario.Ledger.RetiredQuantity);
    }

    [Fact]
    public void A_null_serial_in_a_post_is_refused_and_records_nothing() {
        // Arrange
        (LedgerScenario scenario, CreditSerial[] units) = AliceHoldingTwo();
        CreditSerial fresh = scenario.NewSerials(1).Single();

        // Act / Assert
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Issue(new CreditSerial[] { fresh, null! }, scenario.Bob.AccountId, scenario.Day(2)));
        Assert.ThrowsAny<Exception>(() => scenario.Ledger.Transfer(new CreditSerial[] { units[0], null! }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2)));
        AssertUntouched(scenario, 1);
        Assert.Equal(2, scenario.Ledger.Balance(scenario.Alice.AccountId));
    }

}
