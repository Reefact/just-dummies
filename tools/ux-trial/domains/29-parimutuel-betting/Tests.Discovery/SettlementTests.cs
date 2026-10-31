using System.Collections.Generic;

using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

using static ParimutuelBetting.Tests.Discovery.Tickets;

namespace ParimutuelBetting.Tests.Discovery;

public sealed class SettlementTests {

    private static StakePool AnyClosedPool(BetType betType, decimal unitStake, AnyPoolRules rules, IEnumerable<Stake> stakes) {
        // The race only has to field every horse the stakes name (horse 8 at most in this class).
        StakePool anyPool = Any.StakePool()
                               .WithRace(r => r.WithFieldSize(f => f.GreaterThanOrEqualTo(8)))
                               .WithBetType(betType)
                               .WithUnitStake(unitStake)
                               .WithRules(rules.Generate())
                               .Generate();
        foreach (Stake stake in stakes) {
            anyPool.Accept(stake);
        }

        anyPool.Close();

        return anyPool;
    }

    [Fact]
    public void The_net_pool_is_shared_among_the_winning_units() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.15m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.00m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, [On(BetType.Win, 10, 3), On(BetType.Win, 30, 5)]);

        // Act
        Settlement settlement = anyWinPool.Settle(new RaceResult(anyWinPool.Race, Horses(3, 5), NobodyScratched));

        // Assert
        Assert.Equal(40.00m, settlement.GrossPool);
        Assert.Equal(6.00m, settlement.Deductions);
        Assert.Equal(34.00m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(new Combination(BetType.Win, Horses(3)), dividend.Combination);
        Assert.Equal(3.40m, dividend.AmountPerUnit);
        Assert.Equal(10, dividend.WinningUnits);
    }

    [Fact]
    public void The_dividend_is_rounded_down_to_the_breakage_step_and_the_remainder_is_breakage() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.15m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.00m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, [On(BetType.Win, 10, 3), On(BetType.Win, 30, 5)]);

        // Act
        Settlement settlement = anyWinPool.Settle(new RaceResult(anyWinPool.Race, Horses(5, 3), NobodyScratched));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(1.10m, dividend.AmountPerUnit);   // 34.00 / 30 = 1.133..., down to 0.10
        Assert.Equal(33.00m, settlement.TotalPayout);
        Assert.Equal(1.00m, settlement.Breakage);
    }

    [Fact]
    public void A_dividend_below_the_minimum_return_is_topped_up_at_the_operators_expense() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.20m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.05m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, [On(BetType.Win, 100, 3), On(BetType.Win, 1, 5)]);

        // Act
        Settlement settlement = anyWinPool.Settle(new RaceResult(anyWinPool.Race, Horses(3), NobodyScratched));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(1.05m, dividend.AmountPerUnit);   // 80.80 / 100 = 0.80 after breakage, raised to 1.05
        Assert.Equal(-24.20m, settlement.Breakage);
    }

    [Fact]
    public void When_nobody_backed_the_winner_the_whole_net_pool_is_carried_over() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.15m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, [On(BetType.Win, 10, 3)]);

        // Act
        Settlement settlement = anyWinPool.Settle(new RaceResult(anyWinPool.Race, Horses(7, 3), NobodyScratched));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(8.50m, settlement.Carryover);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void Stakes_naming_a_scratched_horse_are_refunded_before_the_takeout() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.10m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.00m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, [On(BetType.Win, 10, 3), On(BetType.Win, 20, 5), On(BetType.Win, 5, 4)]);

        // Act
        Settlement settlement = anyWinPool.Settle(new RaceResult(anyWinPool.Race, Horses(3, 5), scratched: Horses(4)));

        // Assert
        Assert.Equal(35.00m, settlement.GrossPool);
        Assert.Equal(5.00m, settlement.Refunded);
        Assert.Equal(3.00m, settlement.Deductions);
        Assert.Equal(2.70m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_shares_the_net_pool_equally_among_the_placed_horses() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(3);
        StakePool anyPlacePool = AnyClosedPool(BetType.Place, 1.00m, anyRules,
                                               [On(BetType.Place, 10, 1), On(BetType.Place, 20, 2), On(BetType.Place, 30, 3), On(BetType.Place, 40, 6)]);

        // Act
        Settlement settlement = anyPlacePool.Settle(new RaceResult(anyPlacePool.Race, Horses(2, 3, 1, 6), NobodyScratched));

        // Assert: 100.00 shared in three, each third divided by that horse's units.
        Assert.Collection(settlement.Dividends,
                          second => Assert.Equal(1.66m, second.AmountPerUnit),
                          third => Assert.Equal(1.11m, third.AmountPerUnit),
                          first => Assert.Equal(3.33m, first.AmountPerUnit));
    }

    [Fact]
    public void A_place_pool_shares_only_among_the_placed_horses_that_were_backed() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(2);
        StakePool anyPlacePool = AnyClosedPool(BetType.Place, 1.00m, anyRules, [On(BetType.Place, 10, 1), On(BetType.Place, 40, 6)]);

        // Act
        Settlement settlement = anyPlacePool.Settle(new RaceResult(anyPlacePool.Race, Horses(1, 5, 6), NobodyScratched));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(new Combination(BetType.Place, Horses(1)), dividend.Combination);
        Assert.Equal(5.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_quinella_wins_whatever_the_order_its_two_horses_finish_in() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.10m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.00m);
        StakePool anyQuinellaPool = AnyClosedPool(BetType.Quinella, 1.00m, anyRules, [On(BetType.Quinella, 10, 5, 3), On(BetType.Quinella, 10, 1, 2)]);

        // Act
        Settlement settlement = anyQuinellaPool.Settle(new RaceResult(anyQuinellaPool.Race, Horses(3, 5, 1), NobodyScratched));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(new Combination(BetType.Quinella, Horses(3, 5)), dividend.Combination);
        Assert.Equal(1.80m, dividend.AmountPerUnit);
    }

    [Fact]
    public void An_exacta_in_the_reverse_order_does_not_win() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.10m);
        StakePool anyExactaPool = AnyClosedPool(BetType.Exacta, 1.00m, anyRules, [On(BetType.Exacta, 10, 5, 3)]);

        // Act
        Settlement settlement = anyExactaPool.Settle(new RaceResult(anyExactaPool.Race, Horses(3, 5), NobodyScratched));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(9.00m, settlement.Carryover);
    }

    [Fact]
    public void A_trifecta_pays_the_first_three_in_exact_order() {
        // Arrange
        AnyPoolRules anyRules = Any.PoolRules().WithTakeoutRate(0.25m).WithBreakageStep(0.05m).WithMinimumReturnRatio(1.00m);
        StakePool anyTrifectaPool = AnyClosedPool(BetType.Trifecta, 2.00m, anyRules, [On(BetType.Trifecta, 4, 1, 2, 3), On(BetType.Trifecta, 6, 3, 2, 1)]);

        // Act
        Settlement settlement = anyTrifectaPool.Settle(new RaceResult(anyTrifectaPool.Race, Horses(1, 2, 3, 4), NobodyScratched));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(new Combination(BetType.Trifecta, Horses(1, 2, 3)), dividend.Combination);
        Assert.Equal(3.75m, dividend.AmountPerUnit);
        Assert.Equal(15.00m, settlement.TotalPayout);
    }

}
