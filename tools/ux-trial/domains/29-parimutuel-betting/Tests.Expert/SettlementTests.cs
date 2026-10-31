using ParimutuelBetting.Domain;

using static ParimutuelBetting.Tests.Expert.Recipes;

namespace ParimutuelBetting.Tests.Expert;

/// <summary>
/// Sharing the pool. The amounts, the unit stake and every rule an assertion depends on are written
/// down; which horse wins, the race, and the rules a dividend never reaches stay drawn.
/// </summary>
public sealed class SettlementTests {

    private static AnyStakePool AnyPool(Race race, BetType betType, decimal unitStake, Func<AnyPoolRules, AnyPoolRules> rules) {
        return Any.StakePool().WithRace(race).WithBetType(betType).WithUnitStake(unitStake).WithRules(rules);
    }

    [Fact]
    public void The_net_pool_is_shared_among_the_units_on_the_winner() {
        // Arrange
        Race          anyRace     = AnyRaceWithAtLeast(2);
        HorseNumber[] anyHorses   = DistinctRunnersOf(anyRace, 2);
        HorseNumber   anyWinner   = anyHorses[0];
        HorseNumber   anyLoser    = anyHorses[1];
        // The minimum return (1.00 to 1.50 of the stake) lies far below this dividend: it stays drawn.
        StakePool     pool        = AnyPool(anyRace, BetType.Win, 1.00m, rules => rules.WithTakeoutRate(0.15m).WithBreakageStep(0.05m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyWinner, anyLoser], new Stake(Win(anyWinner), 20), new Stake(Win(anyLoser), 80));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(4.25m, dividend.AmountPerUnit);       // 100.00 less 15 % = 85.00, over 20 units
        Assert.Equal(20, dividend.WinningUnits);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void The_dividend_is_rounded_down_to_the_breakage_step_and_the_pool_keeps_the_rest() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(2);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 2);
        StakePool     pool      = AnyPool(anyRace, BetType.Win, 1.00m, rules => rules.WithTakeoutRate(0.15m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, anyHorses, new Stake(Win(anyHorses[0]), 30), new Stake(Win(anyHorses[1]), 70));

        // Assert
        Assert.Equal(2.80m, Assert.Single(settlement.Dividends).AmountPerUnit);   // 85.00 / 30 = 2.833…
        Assert.Equal(1.00m, settlement.Breakage);                                 // 85.00 - 30 x 2.80
    }

    [Fact]
    public void A_heavy_favourite_is_topped_up_to_the_minimum_return() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(2);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 2);
        // Whatever the breakage step, 1.70 rounds down below the 2.20 minimum: it stays drawn.
        StakePool     pool      = AnyPool(anyRace, BetType.Win, 2.00m, rules => rules.WithTakeoutRate(0.15m).WithMinimumReturnRatio(1.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, anyHorses, new Stake(Win(anyHorses[0]), 100));

        // Assert
        Assert.Equal(2.20m, Assert.Single(settlement.Dividends).AmountPerUnit);   // 170.00 / 100 = 1.70, raised to 2.00 x 1.10
        Assert.Equal(-50.00m, settlement.Breakage);                               // the operator tops up 220.00 - 170.00
    }

    [Fact]
    public void The_takeout_is_rounded_down_to_the_cent() {
        // Arrange
        // The takeout does not depend on the bet type, the result or how the stakes are spread.
        StakePool  pool      = Any.StakePool().WithUnitStake(0.10m).WithRules(rules => rules.WithTakeoutRate(0.175m)).Generate();
        Stake      anyStake  = Any.Stake().WithCombination(combination => combination.For(pool.BetType, pool.Race)).WithUnits(7).Generate();
        RaceResult anyResult = Any.RaceResult().WithRace(pool.Race).WithoutScratchings().SettlingEveryBetType().Generate();
        pool.Accept(anyStake);
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(anyResult);

        // Assert
        Assert.Equal(0.12m, settlement.Deductions);   // 0.70 x 17.5 % = 0.1225
    }

    [Fact]
    public void Stakes_on_a_scratched_horse_are_refunded_before_the_takeout() {
        // Arrange
        Race          anyRace      = AnyRaceWithAtLeast(3);
        HorseNumber[] anyHorses    = DistinctRunnersOf(anyRace, 3);
        HorseNumber   anyWinner    = anyHorses[0];
        HorseNumber   anyLoser     = anyHorses[1];
        HorseNumber   anyScratched = anyHorses[2];
        StakePool     pool         = AnyPool(anyRace, BetType.Win, 0.50m, rules => rules.WithTakeoutRate(0.20m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyWinner, anyLoser], [anyScratched],
                                            new Stake(Win(anyWinner), 10), new Stake(Win(anyLoser), 30), new Stake(Win(anyScratched), 60));

        // Assert
        Assert.Equal(50.00m, settlement.GrossPool);
        Assert.Equal(30.00m, settlement.Refunded);
        Assert.Equal(4.00m, settlement.Deductions);                               // 20 % of the 20.00 left running
        Assert.Equal(1.60m, Assert.Single(settlement.Dividends).AmountPerUnit);   // 16.00 / 10
    }

    [Fact]
    public void A_pool_nobody_won_carries_its_whole_net_pool_over() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(2);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 2);
        StakePool     pool      = AnyPool(anyRace, BetType.Win, 1.00m, rules => rules.WithTakeoutRate(0.20m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, anyHorses, new Stake(Win(anyHorses[1]), 50));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(40.00m, settlement.Carryover);
    }

    [Fact]
    public void A_quinella_pays_whichever_of_its_two_horses_finished_first() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(3);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 3);
        HorseNumber   anyFirst  = anyHorses[0];
        HorseNumber   anySecond = anyHorses[1];
        HorseNumber   anyThird  = anyHorses[2];
        StakePool     pool      = AnyPool(anyRace, BetType.Quinella, 1.00m, rules => rules.WithTakeoutRate(0.10m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyFirst, anySecond, anyThird],
                                            new Stake(Quinella(anySecond, anyFirst), 10), new Stake(Quinella(anyFirst, anyThird), 30));

        // Assert
        Assert.Equal(3.60m, Assert.Single(settlement.Dividends).AmountPerUnit);   // 36.00 / 10
    }

    [Fact]
    public void An_exacta_in_the_reverse_order_does_not_win() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(2);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 2);
        HorseNumber   anyFirst  = anyHorses[0];
        HorseNumber   anySecond = anyHorses[1];
        StakePool     pool      = AnyPool(anyRace, BetType.Exacta, 1.00m, rules => rules.WithTakeoutRate(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyFirst, anySecond], new Stake(Exacta(anySecond, anyFirst), 10));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(9.00m, settlement.Carryover);
    }

    [Fact]
    public void A_trifecta_pays_only_the_first_three_in_their_exact_order() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(3);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 3);
        HorseNumber   anyFirst  = anyHorses[0];
        HorseNumber   anySecond = anyHorses[1];
        HorseNumber   anyThird  = anyHorses[2];
        StakePool     pool      = AnyPool(anyRace, BetType.Trifecta, 1.00m, rules => rules.WithTakeoutRate(0.25m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyFirst, anySecond, anyThird],
                                            new Stake(Trifecta(anyFirst, anySecond, anyThird), 3),
                                            new Stake(Trifecta(anyFirst, anyThird, anySecond), 9));

        // Assert
        Assert.Equal(3.00m, Assert.Single(settlement.Dividends).AmountPerUnit);   // 12.00 less 25 % = 9.00, over the 3 units in order
    }

    [Fact]
    public void A_place_pool_shares_the_net_pool_equally_between_the_paid_places() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(3);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 3);
        HorseNumber   anyFirst  = anyHorses[0];
        HorseNumber   anySecond = anyHorses[1];
        HorseNumber   anyThird  = anyHorses[2];
        StakePool     pool      = AnyPool(anyRace, BetType.Place, 1.00m,
                                          rules => rules.WithPlacesPaid(2).WithTakeoutRate(0.20m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyFirst, anySecond, anyThird],
                                            new Stake(Place(anyFirst), 10), new Stake(Place(anySecond), 20), new Stake(Place(anyThird), 60));

        // Assert: 90.00 less 20 % = 72.00, so 36.00 for each paid place
        Assert.Equal([(10, 3.60m), (20, 1.80m)],
                     settlement.Dividends.Select(dividend => (dividend.WinningUnits, dividend.AmountPerUnit)).OrderBy(pair => pair.WinningUnits));
    }

    [Fact]
    public void A_paid_place_nobody_backed_leaves_its_share_to_the_backed_ones() {
        // Arrange
        Race          anyRace   = AnyRaceWithAtLeast(3);
        HorseNumber[] anyHorses = DistinctRunnersOf(anyRace, 3);
        HorseNumber   anyFirst  = anyHorses[0];
        HorseNumber   anySecond = anyHorses[1];
        HorseNumber   anyThird  = anyHorses[2];
        StakePool     pool      = AnyPool(anyRace, BetType.Place, 1.00m,
                                          rules => rules.WithPlacesPaid(2).WithTakeoutRate(0.20m).WithBreakageStep(0.10m)).Generate();

        // Act
        Settlement settlement = SettleAfter(pool, [anyFirst, anySecond, anyThird],
                                            new Stake(Place(anyFirst), 10), new Stake(Place(anyThird), 40));

        // Assert
        Assert.Equal(4.00m, Assert.Single(settlement.Dividends).AmountPerUnit);   // all of 40.00 over 10 units
    }

}
