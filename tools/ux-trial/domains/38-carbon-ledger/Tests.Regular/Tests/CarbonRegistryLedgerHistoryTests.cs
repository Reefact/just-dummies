using CarbonLedger.Domain;
using CarbonLedger.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class CarbonRegistryLedgerHistoryTests {

    [Fact]
    public void An_issued_unit_has_a_single_open_period_with_its_first_holder() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();
        scenario.Ledger.Issue(new[] { serial }, scenario.Alice.AccountId, scenario.Day(1));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(serial);

        // Assert
        HoldingPeriod period = Assert.Single(history);
        Assert.Equal(serial, period.Serial);
        Assert.Equal(scenario.Alice.AccountId, period.AccountId);
        Assert.Equal(scenario.Day(1), period.From);
        Assert.True(period.IsCurrent);
    }

    [Fact]
    public void A_transferred_unit_has_a_closed_period_then_an_open_one() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();
        scenario.Ledger.Issue(new[] { serial }, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(new[] { serial }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(4));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(serial);

        // Assert
        Assert.Equal(2, history.Count);
        Assert.Equal(scenario.Alice.AccountId, history[0].AccountId);
        Assert.Equal(scenario.Day(1), history[0].From);
        Assert.Equal(scenario.Day(4), history[0].Until);
        Assert.Equal(scenario.Bob.AccountId, history[1].AccountId);
        Assert.Equal(scenario.Day(4), history[1].From);
        Assert.True(history[1].IsCurrent);
    }

    [Fact]
    public void A_retired_unit_ends_its_history_in_the_retirement_account() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();
        scenario.Ledger.Issue(new[] { serial }, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(new[] { serial }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));
        scenario.Ledger.Retire(new[] { serial }, scenario.Bob.AccountId, scenario.Retirement.AccountId, scenario.NewClaim(), scenario.Day(3));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(serial);

        // Assert
        Assert.Equal(new[] { scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Retirement.AccountId }, history.Select(p => p.AccountId));
        Assert.Equal(new DateTimeOffset?[] { scenario.Day(2), scenario.Day(3), null }, history.Select(p => p.Until));
        Assert.True(history[2].IsCurrent);
    }

    [Fact]
    public void The_history_of_a_unit_ignores_the_entries_of_the_other_units() {
        // Arrange
        LedgerScenario                    scenario = new LedgerScenario();
        IReadOnlyCollection<CreditSerial> serials  = scenario.NewSerials(2);
        CreditSerial                      watched  = serials.First();
        CreditSerial                      other    = serials.Last();
        scenario.Ledger.Issue(serials, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(new[] { other }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(2));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(watched);

        // Assert
        HoldingPeriod period = Assert.Single(history);
        Assert.Equal(scenario.Alice.AccountId, period.AccountId);
        Assert.True(period.IsCurrent);
    }

    [Fact]
    public void A_unit_moved_twice_at_the_same_instant_keeps_zero_length_periods() {
        // Arrange
        LedgerScenario scenario = new LedgerScenario();
        CreditSerial   serial   = scenario.NewSerials(1).Single();
        scenario.Ledger.Issue(new[] { serial }, scenario.Alice.AccountId, scenario.Day(1));
        scenario.Ledger.Transfer(new[] { serial }, scenario.Alice.AccountId, scenario.Bob.AccountId, scenario.Day(1));

        // Act
        IReadOnlyList<HoldingPeriod> history = scenario.Ledger.HistoryOf(serial);

        // Assert
        Assert.Equal(2, history.Count);
        Assert.Equal(history[0].From, history[0].Until);
    }

}
