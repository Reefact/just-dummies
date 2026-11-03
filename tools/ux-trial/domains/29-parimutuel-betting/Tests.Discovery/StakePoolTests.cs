namespace ParimutuelBetting.Tests.Discovery;

public sealed class StakePoolTests {

    // The three numbers that shape a payout are what the settlement tests are about; the places paid only matter to a Place pool.
    private static PoolRules RulesOf(decimal takeout, decimal breakageStep, decimal minimumReturn) {
        return Any.PoolRules()
                  .WithTakeoutRate(takeout)
                  .WithBreakageStep(breakageStep)
                  .WithMinimumReturnRatio(minimumReturn)
                  .Generate();
    }

    private static StakePool PoolOf(BetType betType, int fieldSize, decimal unitStake, PoolRules rules, params Stake[] stakes) {
        StakePool pool = new StakePool(Horses.RaceOf(fieldSize), betType, unitStake, rules);
        foreach (Stake stake in stakes) {
            pool.Accept(stake);
        }

        return pool;
    }

    private static Settlement SettleOf(StakePool pool, int[] finishers, params int[] scratched) {
        pool.Close();

        return pool.Settle(new RaceResult(pool.Race, Horses.Of(finishers), Horses.Of(scratched)));
    }

    // --- Opening a pool ----------------------------------------------------------------------------------------

    [Fact]
    public void A_new_pool_is_open_and_empty() {
        // Arrange
        Race      race  = Any.Race().WithFieldSize(10).Generate();
        PoolRules rules = Any.PoolRules().Generate();

        // Act
        StakePool pool = new StakePool(race, BetType.Exacta, 2.50m, rules);

        // Assert
        Assert.Same(race, pool.Race);
        Assert.Same(rules, pool.Rules);
        Assert.Equal(BetType.Exacta, pool.BetType);
        Assert.Equal(2.50m, pool.UnitStake);
        Assert.Equal(PoolStatus.Open, pool.Status);
        Assert.Empty(pool.Stakes);
        Assert.Equal(0m, pool.GrossAmount);
    }

    [Fact]
    public void A_pool_cannot_be_opened_without_a_race_or_rules() {
        Race      anyRace  = Any.Race().WithFieldSize(10).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Throws<ArgumentNullException>(() => new StakePool(null!, BetType.Win, 1m, anyRules));
        Assert.Throws<ArgumentNullException>(() => new StakePool(anyRace, BetType.Win, 1m, null!));
    }

    [Fact]
    public void A_pool_cannot_be_opened_for_an_unknown_bet_type() {
        Race      anyRace  = Any.Race().WithFieldSize(10).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new StakePool(anyRace, (BetType)17, 1m, anyRules));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("100.00")]
    public void The_smallest_and_the_largest_unit_stake_are_accepted(string unitStake) {
        Race      anyRace  = Any.Race().WithFieldSize(10).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();
        BetType   anyType  = Any.Enum<BetType>().Generate();

        StakePool pool = new StakePool(anyRace, anyType, decimal.Parse(unitStake, System.Globalization.CultureInfo.InvariantCulture), anyRules);

        Assert.Equal(decimal.Parse(unitStake, System.Globalization.CultureInfo.InvariantCulture), pool.UnitStake);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1.00")]
    [InlineData("0.005")]
    [InlineData("1.234")]
    [InlineData("100.01")]
    public void A_unit_stake_that_is_not_a_whole_number_of_cents_up_to_one_hundred_is_refused(string unitStake) {
        Race      anyRace  = Any.Race().WithFieldSize(10).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new StakePool(anyRace, BetType.Win, decimal.Parse(unitStake, System.Globalization.CultureInfo.InvariantCulture), anyRules));
    }

    [Fact]
    public void A_two_runner_field_cannot_open_a_trifecta_pool() {
        Race      race     = Any.Race().WithFieldSize(2).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Throws<ArgumentException>(() => new StakePool(race, BetType.Trifecta, 1m, anyRules));
    }

    [Fact]
    public void A_two_runner_field_can_open_a_win_a_quinella_and_an_exacta_pool() {
        Race      race     = Any.Race().WithFieldSize(2).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Equal(PoolStatus.Open, new StakePool(race, BetType.Win, 1m, anyRules).Status);
        Assert.Equal(PoolStatus.Open, new StakePool(race, BetType.Quinella, 1m, anyRules).Status);
        Assert.Equal(PoolStatus.Open, new StakePool(race, BetType.Exacta, 1m, anyRules).Status);
    }

    [Fact]
    public void A_three_runner_field_can_open_a_trifecta_pool() {
        Race      race     = Any.Race().WithFieldSize(3).Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Equal(PoolStatus.Open, new StakePool(race, BetType.Trifecta, 1m, anyRules).Status);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void A_place_pool_can_open_when_the_field_has_more_runners_than_places_paid(int placesPaid, int fieldSize) {
        Race      race  = Any.Race().WithFieldSize(fieldSize).Generate();
        PoolRules rules = Any.PoolRules().WithPlacesPaid(placesPaid).Generate();

        StakePool pool = new StakePool(race, BetType.Place, 1m, rules);

        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void A_place_pool_cannot_open_when_every_runner_would_be_paid(int placesPaid, int fieldSize) {
        Race      race  = Any.Race().WithFieldSize(fieldSize).Generate();
        PoolRules rules = Any.PoolRules().WithPlacesPaid(placesPaid).Generate();

        Assert.Throws<ArgumentException>(() => new StakePool(race, BetType.Place, 1m, rules));
    }

    [Fact]
    public void The_places_paid_do_not_restrict_a_pool_that_is_not_a_place_pool() {
        Race      race  = Any.Race().WithFieldSize(2).Generate();
        PoolRules rules = Any.PoolRules().WithPlacesPaid(3).Generate();

        StakePool pool = new StakePool(race, BetType.Win, 1m, rules);

        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    // --- Accepting stakes --------------------------------------------------------------------------------------

    [Fact]
    public void An_open_pool_accepts_a_stake_on_its_bet_type_and_counts_its_money() {
        // Arrange
        StakePool pool = PoolOf(BetType.Win, 10, 0.25m, Any.PoolRules().Generate());

        // Act
        pool.Accept(Horses.Stake(BetType.Win, 3, 4));
        pool.Accept(Horses.Stake(BetType.Win, 4, 9));

        // Assert
        Assert.Equal(2, pool.Stakes.Count);
        Assert.Equal(1.75m, pool.GrossAmount);
    }

    [Fact]
    public void A_pool_accepts_a_stake_on_any_runner_whatever_the_field() {
        StakePool anyPool  = Any.StakePool().Generate();
        Stake     anyStake = Horses.AnyStakeFor(anyPool);

        anyPool.Accept(anyStake);

        Assert.Same(anyStake, Assert.Single(anyPool.Stakes));
    }

    [Fact]
    public void A_stake_cannot_enter_a_pool_of_another_bet_type() {
        StakePool pool = PoolOf(BetType.Win, 10, 1m, Any.PoolRules().Generate());

        Assert.Throws<ArgumentException>(() => pool.Accept(Horses.Stake(BetType.Place, 1, 3)));
        Assert.Throws<ArgumentException>(() => pool.Accept(Horses.Stake(BetType.Exacta, 1, 3, 4)));
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void A_stake_cannot_name_a_horse_that_is_not_a_runner() {
        StakePool pool = PoolOf(BetType.Exacta, 8, 1m, Any.PoolRules().Generate());

        Assert.Throws<ArgumentException>(() => pool.Accept(Horses.Stake(BetType.Exacta, 1, 3, 9)));
        Assert.Throws<ArgumentException>(() => pool.Accept(Horses.Stake(BetType.Exacta, 1, 24, 2)));
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void The_last_runner_of_the_field_can_be_backed() {
        StakePool pool = PoolOf(BetType.Win, 8, 1m, Any.PoolRules().Generate());

        pool.Accept(Horses.Stake(BetType.Win, 1, 8));

        Assert.Single(pool.Stakes);
    }

    [Fact]
    public void A_missing_stake_is_refused() {
        StakePool anyPool = Any.StakePool().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPool.Accept(null!));
    }

    [Fact]
    public void A_closed_pool_no_longer_accepts_stakes() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        Stake     anyStake = Horses.AnyStakeFor(anyPool);
        anyPool.Close();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyPool.Accept(anyStake));
        Assert.Empty(anyPool.Stakes);
    }

    [Fact]
    public void A_settled_pool_no_longer_accepts_stakes() {
        // Arrange
        StakePool pool     = PoolOf(BetType.Win, 8, 1m, Any.PoolRules().Generate());
        Stake     anyStake = Horses.AnyStakeFor(pool);
        SettleOf(pool, new[] { 1, 2, 3 });

        // Assert
        Assert.Throws<InvalidOperationException>(() => pool.Accept(anyStake));
    }

    // --- Life cycle --------------------------------------------------------------------------------------------

    [Fact]
    public void Closing_an_open_pool_closes_it() {
        StakePool anyPool = Any.StakePool().Generate();

        anyPool.Close();

        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_closed_twice() {
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Close();

        Assert.Throws<InvalidOperationException>(() => anyPool.Close());
    }

    [Fact]
    public void A_settled_pool_cannot_be_closed() {
        StakePool pool = PoolOf(BetType.Win, 8, 1m, Any.PoolRules().Generate());
        SettleOf(pool, new[] { 1 });

        Assert.Throws<InvalidOperationException>(() => pool.Close());
    }

    [Fact]
    public void An_open_pool_cannot_be_settled() {
        StakePool pool = PoolOf(BetType.Win, 8, 1m, Any.PoolRules().Generate());
        RaceResult result = new RaceResult(pool.Race, Horses.Of(1), Horses.Of());

        Assert.Throws<InvalidOperationException>(() => pool.Settle(result));
        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_settled_twice() {
        StakePool pool = PoolOf(BetType.Win, 8, 1m, Any.PoolRules().Generate());
        SettleOf(pool, new[] { 1 });

        Assert.Equal(PoolStatus.Settled, pool.Status);
        Assert.Throws<InvalidOperationException>(() => pool.Settle(new RaceResult(pool.Race, Horses.Of(1), Horses.Of())));
    }

    [Fact]
    public void A_pool_cannot_be_settled_with_the_result_of_another_race() {
        // Arrange
        Race      poolRace  = Any.Race().WithCode("LCH-R4").WithFieldSize(8).Generate();
        Race      otherRace = Any.Race().WithCode("LCH-R5").WithFieldSize(8).Generate();
        StakePool pool      = new StakePool(poolRace, BetType.Win, 1m, Any.PoolRules().Generate());
        pool.Close();

        // Assert
        Assert.Throws<ArgumentException>(() => pool.Settle(new RaceResult(otherRace, Horses.Of(1), Horses.Of())));
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_missing_result_is_refused() {
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Close();

        Assert.Throws<ArgumentNullException>(() => anyPool.Settle(null!));
    }

    [Fact]
    public void A_result_that_cannot_name_the_winning_combination_leaves_the_pool_closed() {
        // Arrange: an exacta needs two finishers
        StakePool pool = PoolOf(BetType.Exacta, 8, 1m, Any.PoolRules().Generate(), Horses.Stake(BetType.Exacta, 5, 1, 2));
        pool.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pool.Settle(new RaceResult(pool.Race, Horses.Of(1), Horses.Of(3))));
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    // --- Settling ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_net_pool_of_a_win_bet_is_shared_among_the_units_backing_the_winner() {
        // Arrange: 100 units of 2.00 = 200.00, takeout 10 % = 20.00, net 180.00 on 10 winning units
        StakePool pool = PoolOf(BetType.Win, 8, 2.00m, RulesOf(0.10m, 0.10m, 1.00m),
                                Horses.Stake(BetType.Win, 10, 3),
                                Horses.Stake(BetType.Win, 30, 5),
                                Horses.Stake(BetType.Win, 60, 7));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 5, 7 });

        // Assert
        Assert.Equal(PoolStatus.Settled, pool.Status);
        Assert.Equal(200.00m, settlement.GrossPool);
        Assert.Equal(0m, settlement.Refunded);
        Assert.Equal(20.00m, settlement.Deductions);
        Assert.Equal(180.00m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Win, 3), dividend.Combination);
        Assert.Equal(18.00m, dividend.AmountPerUnit);
        Assert.Equal(10, dividend.WinningUnits);
        Assert.Equal(0m, settlement.Carryover);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void Several_tickets_on_the_winner_add_their_units() {
        // Arrange: net 90.00 on 4 + 3 winning units
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0.10m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Win, 4, 2),
                                Horses.Stake(BetType.Win, 93, 6),
                                Horses.Stake(BetType.Win, 3, 2));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 2 });

        // Assert: 90.00 / 7 = 12.857… rounded down to the cent
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(7, dividend.WinningUnits);
        Assert.Equal(12.85m, dividend.AmountPerUnit);
    }

    [Fact]
    public void The_dividend_per_unit_is_rounded_down_to_the_breakage_step_and_the_difference_is_breakage() {
        // Arrange: net 90.00 on 7 winning units, 12.857… per unit
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0.10m, 0.05m, 1.00m),
                                Horses.Stake(BetType.Win, 7, 2),
                                Horses.Stake(BetType.Win, 93, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 2 });

        // Assert
        Assert.Equal(12.85m, Assert.Single(settlement.Dividends).AmountPerUnit);
        Assert.Equal(89.95m, settlement.TotalPayout);
        Assert.Equal(0.05m, settlement.Breakage);
    }

    [Fact]
    public void A_breakage_step_of_one_unit_rounds_the_dividend_down_to_whole_units() {
        // Arrange: net 90.00 on 7 winning units, 12.857… per unit
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0.10m, 1.00m, 1.00m),
                                Horses.Stake(BetType.Win, 7, 2),
                                Horses.Stake(BetType.Win, 93, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 2 });

        // Assert
        Assert.Equal(12.00m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void A_breakage_step_that_is_not_a_round_number_rounds_to_its_own_multiples() {
        // Arrange: net 100.00 on 7 units, 14.285… per unit, multiples of 0.07
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0m, 0.07m, 1.00m),
                                Horses.Stake(BetType.Win, 7, 2),
                                Horses.Stake(BetType.Win, 93, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 2 });

        // Assert: 204 x 0.07 = 14.28
        Assert.Equal(14.28m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void The_dividend_is_raised_to_the_minimum_return_when_the_pool_cannot_honour_it() {
        // Arrange: net 160.00 on 100 winning units is 1.60 per unit, under 1.10 x 2.00
        StakePool pool = PoolOf(BetType.Win, 8, 2.00m, RulesOf(0.20m, 0.10m, 1.10m),
                                Horses.Stake(BetType.Win, 100, 1));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 1 });

        // Assert
        Assert.Equal(2.20m, Assert.Single(settlement.Dividends).AmountPerUnit);
        Assert.Equal(220.00m, settlement.TotalPayout);
        Assert.Equal(-60.00m, settlement.Breakage);
    }

    [Fact]
    public void The_minimum_return_rounds_half_a_cent_up() {
        // Arrange: minimum 1.50 x 0.33 = 0.495, i.e. 0.50; the 39.60 collected would pay 0.396 on 100 units
        StakePool pool = PoolOf(BetType.Win, 8, 0.33m, RulesOf(0m, 0.01m, 1.50m),
                                Horses.Stake(BetType.Win, 100, 1),
                                Horses.Stake(BetType.Win, 20, 2));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 1 });

        // Assert: 0.396 rounds down to 0.39, under the minimum
        Assert.Equal(0.50m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void The_takeout_is_rounded_down_to_the_cent() {
        // Arrange: 3 333 units of 0.01 = 33.33, 7 % of it is 2.3331
        StakePool pool = PoolOf(BetType.Win, 8, 0.01m, RulesOf(0.07m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Win, 3_333, 1));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 1 });

        // Assert
        Assert.Equal(33.33m, settlement.GrossPool);
        Assert.Equal(2.33m, settlement.Deductions);
        Assert.Equal(31.00m, settlement.NetPool);
    }

    [Fact]
    public void A_zero_takeout_deducts_nothing() {
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Win, 50, 1));

        Settlement settlement = SettleOf(pool, new[] { 1 });

        Assert.Equal(0m, settlement.Deductions);
        Assert.Equal(50.00m, settlement.NetPool);
    }

    [Fact]
    public void When_nobody_backed_the_winner_the_whole_net_pool_is_carried_over() {
        // Arrange: 50.00 gross, 16 % takeout is 8.00, net 42.00
        StakePool pool = PoolOf(BetType.Win, 8, 5.00m, RulesOf(0.16m, 0.10m, 1.00m),
                                Horses.Stake(BetType.Win, 4, 2),
                                Horses.Stake(BetType.Win, 6, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 2, 6 });

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(8.00m, settlement.Deductions);
        Assert.Equal(42.00m, settlement.Carryover);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void A_pool_without_stakes_settles_to_nothing() {
        StakePool pool = PoolOf(BetType.Win, 8, 5.00m, Any.PoolRules().Generate());

        Settlement settlement = SettleOf(pool, new[] { 3 });

        Assert.Equal(0m, settlement.GrossPool);
        Assert.Equal(0m, settlement.NetPool);
        Assert.Equal(0m, settlement.Carryover);
        Assert.Empty(settlement.Dividends);
    }

    [Fact]
    public void Stakes_on_a_scratched_horse_are_refunded_before_the_takeout() {
        // Arrange: 100 units of 1.00; 20 on the scratched horse are refunded, takeout is 10 % of the remaining 80.00
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0.10m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Win, 20, 4),
                                Horses.Stake(BetType.Win, 30, 2),
                                Horses.Stake(BetType.Win, 50, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 6, 2 }, 4);

        // Assert
        Assert.Equal(100.00m, settlement.GrossPool);
        Assert.Equal(20.00m, settlement.Refunded);
        Assert.Equal(8.00m, settlement.Deductions);
        Assert.Equal(72.00m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Win, 6), dividend.Combination);
        Assert.Equal(1.44m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_pool_entirely_backing_scratched_horses_is_entirely_refunded() {
        // Arrange
        StakePool pool = PoolOf(BetType.Win, 8, 1.00m, RulesOf(0.10m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Win, 20, 4),
                                Horses.Stake(BetType.Win, 5, 5));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 1 }, 4, 5);

        // Assert
        Assert.Equal(25.00m, settlement.Refunded);
        Assert.Equal(0m, settlement.Deductions);
        Assert.Equal(0m, settlement.NetPool);
        Assert.Empty(settlement.Dividends);
        Assert.Equal(0m, settlement.Carryover);
    }

    [Fact]
    public void A_quinella_ticket_naming_a_scratched_horse_is_refunded_whichever_side_it_is_on() {
        // Arrange
        StakePool pool = PoolOf(BetType.Quinella, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Quinella, 10, 1, 4),
                                Horses.Stake(BetType.Quinella, 5, 4, 7),
                                Horses.Stake(BetType.Quinella, 30, 2, 3));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 2, 3 }, 4);

        // Assert
        Assert.Equal(15.00m, settlement.Refunded);
        Assert.Equal(30.00m, settlement.NetPool);
        Assert.Equal(1.00m, Assert.Single(settlement.Dividends).AmountPerUnit);
    }

    [Fact]
    public void A_quinella_pays_the_first_two_in_either_order_and_adds_the_tickets_written_both_ways() {
        // Arrange: 5/3 (10 units) and 3/5 (5 units) are the same combination
        StakePool pool = PoolOf(BetType.Quinella, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Quinella, 10, 5, 3),
                                Horses.Stake(BetType.Quinella, 5, 3, 5),
                                Horses.Stake(BetType.Quinella, 85, 1, 2));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 5, 1 });

        // Assert: 100.00 / 15 = 6.666… -> 6.66
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Quinella, 3, 5), dividend.Combination);
        Assert.Equal(15, dividend.WinningUnits);
        Assert.Equal(6.66m, dividend.AmountPerUnit);
        Assert.Equal(0.10m, settlement.Breakage);
    }

    [Fact]
    public void An_exacta_pays_only_the_first_two_in_the_exact_order() {
        // Arrange
        StakePool pool = PoolOf(BetType.Exacta, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Exacta, 10, 5, 3),
                                Horses.Stake(BetType.Exacta, 10, 3, 5));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 5 });

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Exacta, 3, 5), dividend.Combination);
        Assert.Equal(2.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void An_exacta_nobody_called_in_order_is_carried_over() {
        // Arrange
        StakePool pool = PoolOf(BetType.Exacta, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Exacta, 10, 5, 3));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 5 });

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(10.00m, settlement.Carryover);
    }

    [Fact]
    public void A_trifecta_pays_the_first_three_in_the_exact_order() {
        // Arrange
        StakePool pool = PoolOf(BetType.Trifecta, 8, 1.00m, RulesOf(0m, 0.01m, 1.00m),
                                Horses.Stake(BetType.Trifecta, 4, 1, 2, 3),
                                Horses.Stake(BetType.Trifecta, 6, 3, 2, 1));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 1, 2, 3, 4 });

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Trifecta, 1, 2, 3), dividend.Combination);
        Assert.Equal(2.50m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_pays_each_of_the_paid_places_an_equal_share_of_the_net_pool() {
        // Arrange: 120.00 gross, 10 % takeout, net 108.00 split in two shares of 54.00
        StakePool pool = new StakePool(Horses.RaceOf(8), BetType.Place, 2.00m, Any.PoolRules().WithTakeoutRate(0.10m).WithBreakageStep(0.10m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(2).Generate());
        pool.Accept(Horses.Stake(BetType.Place, 10, 3));
        pool.Accept(Horses.Stake(BetType.Place, 20, 5));
        pool.Accept(Horses.Stake(BetType.Place, 30, 7));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 5, 3, 7 });

        // Assert
        Assert.Equal(108.00m, settlement.NetPool);
        Assert.Equal(2, settlement.Dividends.Count);
        Assert.Equal(Horses.Combination(BetType.Place, 5), settlement.Dividends[0].Combination);
        Assert.Equal(2.70m, settlement.Dividends[0].AmountPerUnit);
        Assert.Equal(Horses.Combination(BetType.Place, 3), settlement.Dividends[1].Combination);
        Assert.Equal(5.40m, settlement.Dividends[1].AmountPerUnit);
        Assert.Equal(108.00m, settlement.TotalPayout);
    }

    [Fact]
    public void A_place_pool_gives_the_whole_net_pool_to_the_one_paid_place_that_was_backed() {
        // Arrange: places 5 and 3 are paid, nobody backed 5
        StakePool pool = new StakePool(Horses.RaceOf(8), BetType.Place, 1.00m, Any.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(2).Generate());
        pool.Accept(Horses.Stake(BetType.Place, 10, 3));
        pool.Accept(Horses.Stake(BetType.Place, 10, 7));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 5, 3, 7 });

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Horses.Combination(BetType.Place, 3), dividend.Combination);
        Assert.Equal(2.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_pays_no_more_places_than_there_are_finishers() {
        // Arrange: three places are paid but only two horses finished
        StakePool pool = new StakePool(Horses.RaceOf(8), BetType.Place, 1.00m, Any.PoolRules().WithTakeoutRate(0m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(3).Generate());
        pool.Accept(Horses.Stake(BetType.Place, 10, 3));
        pool.Accept(Horses.Stake(BetType.Place, 10, 4));
        pool.Accept(Horses.Stake(BetType.Place, 20, 6));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 3, 4 });

        // Assert
        Assert.Equal(2, settlement.Dividends.Count);
        Assert.Equal(2.00m, settlement.Dividends[0].AmountPerUnit);
        Assert.Equal(2.00m, settlement.Dividends[1].AmountPerUnit);
        Assert.Equal(40.00m, settlement.TotalPayout);
    }

    [Fact]
    public void A_place_pool_where_no_paid_place_was_backed_carries_the_net_pool_over() {
        // Arrange
        StakePool pool = new StakePool(Horses.RaceOf(8), BetType.Place, 1.00m, Any.PoolRules().WithTakeoutRate(0.10m).WithBreakageStep(0.01m).WithMinimumReturnRatio(1.00m).WithPlacesPaid(1).Generate());
        pool.Accept(Horses.Stake(BetType.Place, 10, 3));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 5, 3 });

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(9.00m, settlement.Carryover);
    }

    [Fact]
    public void A_settlement_always_accounts_for_every_unit_collected() {
        // Arrange
        StakePool pool = PoolOf(BetType.Win, 8, 2.00m, RulesOf(0.10m, 0.10m, 1.00m),
                                Horses.Stake(BetType.Win, 10, 3),
                                Horses.Stake(BetType.Win, 30, 5),
                                Horses.Stake(BetType.Win, 60, 7));

        // Act
        Settlement settlement = SettleOf(pool, new[] { 5 });

        // Assert
        Assert.Equal(settlement.GrossPool, settlement.Refunded + settlement.Deductions + settlement.TotalPayout + settlement.Breakage + settlement.Carryover);
    }

}
