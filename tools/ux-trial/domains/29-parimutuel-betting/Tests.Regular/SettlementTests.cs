using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class SettlementTests {

    private static HorseNumber[] Horses(params int[] numbers) => numbers.Select(n => new HorseNumber(n)).ToArray();

    private static Stake StakeOn(BetType betType, int units, params int[] horses) => new Stake(new Combination(betType, Horses(horses)), units);

    private static Stake AnyLosingStake(BetType betType, int units, int lowest, int highest) =>
        Arbitrary.Stake()
                 .WithCombination(c => c.WithBetType(betType).WithHorsesBetween(lowest, highest))
                 .WithUnits(units)
                 .Generate();

    private static StakePool AnyClosedPool(BetType betType, decimal unitStake, AnyPoolRules rules, params Stake[] stakes) {
        StakePool pool = Arbitrary.StakePool()
                                  .WithBetType(betType)
                                  .WithUnitStake(unitStake)
                                  .WithRace(r => r.WithFieldSize(f => f.GreaterThanOrEqualTo(10)))
                                  .WithRules(rules.Generate())
                                  .Generate();
        foreach (Stake stake in stakes) {
            pool.Accept(stake);
        }

        pool.Close();

        return pool;
    }

    [Fact]
    public void The_net_pool_is_shared_among_the_winning_units_after_the_takeout() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0.15m).WithBreakageStep(0.10m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 2.00m, anyRules,
                                             StakeOn(BetType.Win, 100, 3),
                                             AnyLosingStake(BetType.Win, 300, 4, 10));
        RaceResult result = new RaceResult(anyWinPool.Race, Horses(3, 7), []);

        // Act
        Settlement settlement = anyWinPool.Settle(result);

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(6.80m, dividend.AmountPerUnit);
    }

    [Fact]
    public void The_takeout_is_rounded_down_to_the_cent() {
        // Arrange
        AnyPoolRules anyRules   = Arbitrary.PoolRules().WithTakeoutRate(0.1525m);
        StakePool    anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, AnyLosingStake(BetType.Win, 7, 1, 10));
        RaceResult   anyResult  = new RaceResult(anyWinPool.Race, Horses(1), []);

        // Act
        Settlement settlement = anyWinPool.Settle(anyResult);

        // Assert
        Assert.Equal(1.06m, settlement.Deductions);
    }

    [Fact]
    public void The_dividend_is_rounded_down_to_the_breakage_step_and_the_rest_is_kept_as_breakage() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.10m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules,
                                             StakeOn(BetType.Win, 3, 2),
                                             AnyLosingStake(BetType.Win, 7, 3, 10));
        RaceResult result = new RaceResult(anyWinPool.Race, Horses(2), []);

        // Act
        Settlement settlement = anyWinPool.Settle(result);

        // Assert
        Assert.Equal(3.30m, settlement.Dividends[0].AmountPerUnit);
        Assert.Equal(0.10m, settlement.Breakage);
    }

    [Fact]
    public void A_heavily_backed_winner_is_topped_up_to_the_minimum_return() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0.20m).WithMinimumReturnRatio(1.10m);
        StakePool anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules,
                                             StakeOn(BetType.Win, 950, 1),
                                             AnyLosingStake(BetType.Win, 50, 2, 10));
        RaceResult result = new RaceResult(anyWinPool.Race, Horses(1), []);

        // Act
        Settlement settlement = anyWinPool.Settle(result);

        // Assert
        Assert.Equal(1.10m, settlement.Dividends[0].AmountPerUnit);
        Assert.Equal(-245.00m, settlement.Breakage);
    }

    [Fact]
    public void When_nobody_backed_the_winner_the_whole_net_pool_is_carried_over() {
        // Arrange
        AnyPoolRules anyRules   = Arbitrary.PoolRules().WithTakeoutRate(0.10m);
        StakePool    anyWinPool = AnyClosedPool(BetType.Win, 1.00m, anyRules, AnyLosingStake(BetType.Win, 40, 2, 10));
        RaceResult   result     = new RaceResult(anyWinPool.Race, Horses(1), []);

        // Act
        Settlement settlement = anyWinPool.Settle(result);

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(36.00m, settlement.Carryover);
    }

    [Fact]
    public void Stakes_naming_a_scratched_horse_are_refunded_and_escape_the_takeout() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0.10m);
        StakePool anyExactaPool = AnyClosedPool(BetType.Exacta, 2.00m, anyRules,
                                                StakeOn(BetType.Exacta, 15, 4, 6),
                                                StakeOn(BetType.Exacta, 5, 1, 2));
        RaceResult result = new RaceResult(anyExactaPool.Race, Horses(1, 2), Horses(6));

        // Act
        Settlement settlement = anyExactaPool.Settle(result);

        // Assert
        Assert.Equal(30.00m, settlement.Refunded);
        Assert.Equal(1.00m, settlement.Deductions);
    }

    [Fact]
    public void A_quinella_wins_whichever_of_its_two_horses_finishes_first() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m);
        StakePool anyQuinellaPool = AnyClosedPool(BetType.Quinella, 1.00m, anyRules,
                                                  StakeOn(BetType.Quinella, 4, 3, 5),
                                                  AnyLosingStake(BetType.Quinella, 16, 6, 10));
        RaceResult result = new RaceResult(anyQuinellaPool.Race, Horses(5, 3), []);

        // Act
        Settlement settlement = anyQuinellaPool.Settle(result);

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(5.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void An_exacta_in_the_wrong_order_does_not_win() {
        // Arrange
        AnyPoolRules anyRules        = Arbitrary.PoolRules();
        StakePool    anyExactaPool   = AnyClosedPool(BetType.Exacta, 1.00m, anyRules, StakeOn(BetType.Exacta, 10, 3, 5));
        RaceResult   result          = new RaceResult(anyExactaPool.Race, Horses(5, 3), []);

        // Act
        Settlement settlement = anyExactaPool.Settle(result);

        // Assert
        Assert.Empty(settlement.Dividends);
    }

    [Fact]
    public void A_place_pool_shares_the_net_pool_equally_among_the_backed_placed_horses() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(3);
        StakePool anyPlacePool = AnyClosedPool(BetType.Place, 1.00m, anyRules,
                                               StakeOn(BetType.Place, 10, 4),
                                               StakeOn(BetType.Place, 20, 7),
                                               StakeOn(BetType.Place, 70, 9));
        RaceResult result = new RaceResult(anyPlacePool.Race, Horses(4, 7, 1, 9), []);

        // Act
        Settlement settlement = anyPlacePool.Settle(result);

        // Assert
        Assert.Equal([5.00m, 2.50m], settlement.Dividends.Select(dividend => dividend.AmountPerUnit));
    }

    [Fact]
    public void The_trifecta_pays_only_the_exact_first_three() {
        // Arrange
        AnyPoolRules anyRules = Arbitrary.PoolRules().WithTakeoutRate(0.25m).WithBreakageStep(0.05m);
        StakePool anyTrifectaPool = AnyClosedPool(BetType.Trifecta, 0.50m, anyRules,
                                                  StakeOn(BetType.Trifecta, 2, 8, 1, 6),
                                                  StakeOn(BetType.Trifecta, 6, 1, 8, 6),
                                                  AnyLosingStake(BetType.Trifecta, 92, 2, 5));
        RaceResult result = new RaceResult(anyTrifectaPool.Race, Horses(8, 1, 6, 2), []);

        // Act
        Settlement settlement = anyTrifectaPool.Settle(result);

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(new Combination(BetType.Trifecta, Horses(8, 1, 6)), dividend.Combination);
        Assert.Equal(18.75m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_settlement_that_declares_dividends_carries_nothing_over() {
        // Arrange
        Dividend anyDividend = new Dividend(Arbitrary.Combination().Generate(), 4.20m, 10);

        // Act
        Action create = () => new Settlement(100m, 0m, 10m, [anyDividend], 5m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}
