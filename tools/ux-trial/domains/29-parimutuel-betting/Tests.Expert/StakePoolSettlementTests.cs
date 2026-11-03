using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

/// <summary>
///     The race has eight runners; its code never matters to the sharing of a pool, so it is drawn.
///     Every amount, unit count and rule below is spelled out: they are what each test computes.
/// </summary>
public sealed class StakePoolSettlementTests {

    private static Race EightRunners() {
        return Any.Race().WithFieldSize(8).Generate();
    }

    public static TheoryData<decimal, decimal> DividendsByBreakageStep => new TheoryData<decimal, decimal> {
        { 0.01m, 14.28m },
        { 0.05m, 14.25m },
        { 0.10m, 14.20m },
        { 0.25m, 14.25m },
        { 0.50m, 14.00m },
        { 1.00m, 14.00m },
    };

    [Fact]
    public void The_net_pool_is_shared_among_the_units_backing_the_winner_and_rounded_down_to_the_cent() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Win, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Win, 10, 3),
                                            Tickets.Ticket(BetType.Win, 30, 5),
                                            Tickets.Ticket(BetType.Win, 60, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 5, 3, 7 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Win, 5), dividend.Combination);
        Assert.Equal(3.33m, dividend.AmountPerUnit);
        Assert.Equal(30, dividend.WinningUnits);
    }

    [Fact]
    public void What_rounding_down_does_not_pay_out_is_kept_as_breakage() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Win, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Win, 10, 3),
                                            Tickets.Ticket(BetType.Win, 30, 5),
                                            Tickets.Ticket(BetType.Win, 60, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 5, 3, 7 }));

        Assert.Equal(100m, settlement.GrossPool);
        Assert.Equal(99.90m, settlement.TotalPayout);
        Assert.Equal(0.10m, settlement.Breakage);
        Assert.Equal(0m, settlement.Carryover);
    }

    [Theory]
    [MemberData(nameof(DividendsByBreakageStep))]
    public void The_dividend_is_rounded_down_to_a_multiple_of_the_breakage_step(decimal breakageStep, decimal expectedDividend) {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0m, breakageStep: breakageStep, minimumReturnRatio: 1.00m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 1.00m, rules,
                                             Tickets.Ticket(BetType.Win, 7, 3),
                                             Tickets.Ticket(BetType.Win, 93, 5));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 3, 5 }));

        Assert.Equal(expectedDividend, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void The_takeout_is_rounded_down_to_the_cent_even_on_a_half_cent() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.15m, breakageStep: 0.01m, minimumReturnRatio: 1.00m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 0.50m, rules,
                                             Tickets.Ticket(BetType.Win, 1, 1),
                                             Tickets.Ticket(BetType.Win, 200, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 1, 4 }));

        Assert.Equal(100.50m, settlement.GrossPool);
        Assert.Equal(15.07m, settlement.Deductions);
        Assert.Equal(85.43m, settlement.NetPool);
    }

    [Fact]
    public void The_takeout_is_taken_on_the_pool_left_after_refunds() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.10m, breakageStep: 0.01m, minimumReturnRatio: 1.00m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 1.00m, rules,
                                             Tickets.Ticket(BetType.Win, 10, 6),
                                             Tickets.Ticket(BetType.Win, 40, 1),
                                             Tickets.Ticket(BetType.Win, 50, 2));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 1, 2 }, 6));

        Assert.Equal(10m, settlement.Refunded);
        Assert.Equal(9.00m, settlement.Deductions);
        Assert.Equal(81.00m, settlement.NetPool);
    }

    [Fact]
    public void A_zero_takeout_deducts_nothing() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Win, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Win, 12, 1));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 1 }));

        Assert.Equal(0m, settlement.Deductions);
    }

    [Fact]
    public void A_dividend_that_falls_short_of_the_minimum_return_is_raised_to_it() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.20m, breakageStep: 0.01m, minimumReturnRatio: 1.10m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 2.00m, rules, Tickets.Ticket(BetType.Win, 100, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4 }));

        Assert.Equal(160m, settlement.NetPool);
        Assert.Equal(2.20m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void Topping_up_to_the_minimum_return_makes_the_pool_pay_more_than_it_holds() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.20m, breakageStep: 0.01m, minimumReturnRatio: 1.10m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 2.00m, rules, Tickets.Ticket(BetType.Win, 100, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4 }));

        Assert.Equal(220m, settlement.TotalPayout);
        Assert.Equal(-60m, settlement.Breakage);
    }

    [Fact]
    public void A_minimum_return_of_one_gives_the_stake_back_at_least() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.20m, breakageStep: 0.01m, minimumReturnRatio: 1.00m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 2.00m, rules, Tickets.Ticket(BetType.Win, 100, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4 }));

        Assert.Equal(2.00m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void The_minimum_return_is_rounded_to_the_nearest_cent() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.40m, breakageStep: 0.01m, minimumReturnRatio: 1.15m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 0.33m, rules, Tickets.Ticket(BetType.Win, 1, 2));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2 }));

        Assert.Equal(0.38m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void A_dividend_above_the_minimum_return_is_left_as_computed() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0m, breakageStep: 0.01m, minimumReturnRatio: 1.50m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 1.00m, rules,
                                             Tickets.Ticket(BetType.Win, 10, 3),
                                             Tickets.Ticket(BetType.Win, 30, 5),
                                             Tickets.Ticket(BetType.Win, 60, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 5 }));

        Assert.Equal(3.33m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void When_nobody_backed_the_winner_the_whole_net_pool_is_carried_over() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0.10m, breakageStep: 0.01m, minimumReturnRatio: 1.00m, placesPaid: 1);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Win, 1.00m, rules,
                                             Tickets.Ticket(BetType.Win, 40, 3),
                                             Tickets.Ticket(BetType.Win, 60, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 5, 3, 7 }));

        Assert.Empty(settlement.Dividends);
        Assert.Equal(10m, settlement.Deductions);
        Assert.Equal(90m, settlement.Carryover);
        Assert.Equal(0m, settlement.TotalPayout);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void A_winner_whose_backers_were_all_refunded_is_not_a_backed_winner() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Quinella, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Quinella, 10, 2, 6),
                                            Tickets.Ticket(BetType.Quinella, 5, 3, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 3, 2 }, 6));

        Assert.Empty(settlement.Dividends);
        Assert.Equal(10m, settlement.Refunded);
        Assert.Equal(5m, settlement.Carryover);
    }

    [Fact]
    public void A_stake_naming_a_scratched_horse_is_refunded_in_whatever_position_it_names_it() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Trifecta, 2.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Trifecta, 1, 6, 1, 2),
                                            Tickets.Ticket(BetType.Trifecta, 2, 1, 6, 2),
                                            Tickets.Ticket(BetType.Trifecta, 3, 1, 2, 6),
                                            Tickets.Ticket(BetType.Trifecta, 4, 1, 2, 3));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 1, 2, 3 }, 6));

        Assert.Equal(20m, settlement.GrossPool);
        Assert.Equal(12m, settlement.Refunded);
    }

    [Fact]
    public void Only_the_combination_finishing_in_the_exact_order_wins_an_exacta() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Exacta, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Exacta, 10, 2, 1),
                                            Tickets.Ticket(BetType.Exacta, 30, 1, 2));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2, 1 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Exacta, 2, 1), dividend.Combination);
        Assert.Equal(10, dividend.WinningUnits);
        Assert.Equal(4.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_quinella_is_won_by_the_two_first_horses_in_either_order_and_stakes_on_either_order_add_up() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Quinella, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Quinella, 10, 2, 1),
                                            Tickets.Ticket(BetType.Quinella, 30, 1, 2),
                                            Tickets.Ticket(BetType.Quinella, 60, 3, 4));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2, 1, 3 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Quinella, 1, 2), dividend.Combination);
        Assert.Equal(40, dividend.WinningUnits);
        Assert.Equal(2.50m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_trifecta_is_won_by_the_first_three_in_the_exact_order() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Trifecta, 1.00m, Tickets.PlainRules(),
                                            Tickets.Ticket(BetType.Trifecta, 5, 4, 2, 7),
                                            Tickets.Ticket(BetType.Trifecta, 95, 4, 2, 1));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4, 2, 7, 1 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Trifecta, 4, 2, 7), dividend.Combination);
        Assert.Equal(20.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_pays_each_of_the_places_paid_with_an_equal_share_of_the_net_pool() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Place, 1.00m, Tickets.PlainRules(placesPaid: 2),
                                            Tickets.Ticket(BetType.Place, 10, 4),
                                            Tickets.Ticket(BetType.Place, 40, 7),
                                            Tickets.Ticket(BetType.Place, 50, 1));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4, 7, 1 }));

        Dictionary<Combination, Dividend> byHorse = settlement.Dividends.ToDictionary(dividend => dividend.Combination);
        Assert.Equal(2, byHorse.Count);
        Assert.Equal(5.00m, byHorse[Tickets.Bet(BetType.Place, 4)].AmountPerUnit);
        Assert.Equal(1.25m, byHorse[Tickets.Bet(BetType.Place, 7)].AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_gives_the_whole_net_pool_to_the_only_paid_place_that_was_backed() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Place, 1.00m, Tickets.PlainRules(placesPaid: 2),
                                            Tickets.Ticket(BetType.Place, 40, 7),
                                            Tickets.Ticket(BetType.Place, 60, 1));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4, 7, 1 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Place, 7), dividend.Combination);
        Assert.Equal(2.50m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_with_a_single_place_paid_settles_like_a_win_pool() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Place, 1.00m, Tickets.PlainRules(placesPaid: 1),
                                            Tickets.Ticket(BetType.Place, 25, 4),
                                            Tickets.Ticket(BetType.Place, 75, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4, 7 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Tickets.Bet(BetType.Place, 4), dividend.Combination);
        Assert.Equal(4.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_pays_as_many_places_as_there_are_finishers_when_the_result_names_fewer() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Place, 1.00m, Tickets.PlainRules(placesPaid: 3),
                                            Tickets.Ticket(BetType.Place, 20, 4),
                                            Tickets.Ticket(BetType.Place, 80, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4 }));

        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(5.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_applies_the_minimum_return_to_each_paid_place() {
        Race      race  = EightRunners();
        PoolRules rules = new PoolRules(takeoutRate: 0m, breakageStep: 0.01m, minimumReturnRatio: 1.50m, placesPaid: 2);
        StakePool pool  = Tickets.ClosedPool(race, BetType.Place, 1.00m, rules,
                                             Tickets.Ticket(BetType.Place, 90, 4),
                                             Tickets.Ticket(BetType.Place, 10, 7));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 4, 7 }));

        Dictionary<Combination, Dividend> byHorse = settlement.Dividends.ToDictionary(dividend => dividend.Combination);
        Assert.Equal(1.50m, byHorse[Tickets.Bet(BetType.Place, 4)].AmountPerUnit);
        Assert.Equal(5.00m, byHorse[Tickets.Bet(BetType.Place, 7)].AmountPerUnit);
    }

    [Fact]
    public void A_pool_with_no_stake_settles_to_nothing() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Win, 1.00m, Any.PoolRules().Generate());

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2 }));

        Assert.Equal(0m, settlement.GrossPool);
        Assert.Empty(settlement.Dividends);
        Assert.Equal(0m, settlement.Carryover);
    }

    [Fact]
    public void A_pool_whose_every_stake_names_a_scratched_horse_gives_everything_back() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Exacta, 5.00m, Any.PoolRules().Generate(),
                                            Tickets.Ticket(BetType.Exacta, 3, 6, 1),
                                            Tickets.Ticket(BetType.Exacta, 2, 1, 6));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2, 3 }, 6));

        Assert.Equal(25m, settlement.GrossPool);
        Assert.Equal(25m, settlement.Refunded);
        Assert.Equal(0m, settlement.NetPool);
        Assert.Equal(0m, settlement.Carryover);
    }

    [Fact]
    public void The_money_refunded_is_the_same_whatever_the_rules_of_the_pool() {
        Race      race = EightRunners();
        StakePool pool = Tickets.ClosedPool(race, BetType.Win, 4.00m, Any.PoolRules().WithPlacesPaid(1).Generate(),
                                            Tickets.Ticket(BetType.Win, 3, 6),
                                            Tickets.Ticket(BetType.Win, 5, 2));

        Settlement settlement = pool.Settle(Tickets.Result(race, new[] { 2 }, 6));

        Assert.Equal(12m, settlement.Refunded);
    }

}
