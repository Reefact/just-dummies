using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

using static ParimutuelBetting.Tests.Regular.TestHorses;

namespace ParimutuelBetting.Tests.Regular;

public sealed class StakePoolTests {

    private static readonly PoolRules NoTakeout = new PoolRules(0m, 0.01m, 1m, 1);

    // ---- Opening a pool ------------------------------------------------------------------

    [Fact]
    public void A_new_pool_is_open_and_empty() {
        // Arrange
        Race      race  = Arbitrary.Race().Generate();
        PoolRules rules = Arbitrary.PoolRules().Generate();

        // Act
        StakePool pool = new StakePool(race, BetType.Exacta, 2.50m, rules);

        // Assert
        Assert.Equal(PoolStatus.Open, pool.Status);
        Assert.Empty(pool.Stakes);
        Assert.Equal(0m, pool.GrossAmount);
        Assert.Same(race, pool.Race);
        Assert.Equal(BetType.Exacta, pool.BetType);
        Assert.Equal(2.50m, pool.UnitStake);
        Assert.Same(rules, pool.Rules);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("100.00")]
    public void A_unit_stake_of_a_cent_to_a_hundred_is_accepted(string stake) {
        // Arrange
        decimal unitStake = decimal.Parse(stake, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        StakePool pool = Arbitrary.StakePool().WithUnitStake(unitStake).Generate();

        // Assert
        Assert.Equal(unitStake, pool.UnitStake);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1.00")]
    [InlineData("100.01")]
    [InlineData("0.001")]
    [InlineData("2.505")]
    public void A_unit_stake_that_is_not_a_whole_number_of_cents_up_to_a_hundred_is_refused(string stake) {
        // Arrange
        decimal unitStake = decimal.Parse(stake, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.StakePool().WithUnitStake(unitStake).Generate());
    }

    [Theory]
    [InlineData(BetType.Win, 2)]
    [InlineData(BetType.Place, 3)]
    [InlineData(BetType.Quinella, 2)]
    [InlineData(BetType.Exacta, 2)]
    [InlineData(BetType.Trifecta, 3)]
    public void A_pool_is_accepted_when_the_field_holds_at_least_the_arity(BetType betType, int fieldSize) {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(fieldSize).Generate();
        PoolRules rules = Arbitrary.PoolRules().WithPlacesPaid(1).Generate();

        // Act
        StakePool pool = new StakePool(race, betType, 1m, rules);

        // Assert
        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Fact]
    public void A_trifecta_pool_needs_at_least_three_runners() {
        // Arrange
        Race      twoRunners = Arbitrary.Race().WithFieldSize(2).Generate();
        PoolRules anyRules   = Arbitrary.PoolRules().WithPlacesPaid(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StakePool(twoRunners, BetType.Trifecta, 1m, anyRules));
    }

    [Fact]
    public void A_place_pool_paying_three_places_needs_a_fourth_runner() {
        // Arrange
        Race      threeRunners = Arbitrary.Race().WithFieldSize(3).Generate();
        Race      fourRunners  = Arbitrary.Race().WithFieldSize(4).Generate();
        PoolRules threePlaces  = Arbitrary.PoolRules().WithPlacesPaid(3).Generate();

        // Act
        StakePool pool = new StakePool(fourRunners, BetType.Place, 1m, threePlaces);

        // Assert
        Assert.Equal(PoolStatus.Open, pool.Status);
        Assert.Throws<ArgumentException>(() => new StakePool(threeRunners, BetType.Place, 1m, threePlaces));
    }

    [Fact]
    public void A_place_pool_paying_one_place_is_enough_with_two_runners() {
        // Arrange
        Race      twoRunners = Arbitrary.Race().WithFieldSize(2).Generate();
        PoolRules onePlace   = Arbitrary.PoolRules().WithPlacesPaid(1).Generate();
        PoolRules twoPlaces  = Arbitrary.PoolRules().WithPlacesPaid(2).Generate();

        // Act
        StakePool pool = new StakePool(twoRunners, BetType.Place, 1m, onePlace);

        // Assert
        Assert.Equal(PoolStatus.Open, pool.Status);
        Assert.Throws<ArgumentException>(() => new StakePool(twoRunners, BetType.Place, 1m, twoPlaces));
    }

    [Fact]
    public void The_paid_places_only_matter_to_a_place_pool() {
        // Arrange
        Race      twoRunners = Arbitrary.Race().WithFieldSize(2).Generate();
        PoolRules threePlaces = Arbitrary.PoolRules().WithPlacesPaid(3).Generate();

        // Act
        StakePool pool = new StakePool(twoRunners, BetType.Win, 1m, threePlaces);

        // Assert
        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Fact]
    public void A_pool_needs_a_race_and_rules() {
        // Arrange
        Race      anyRace  = Arbitrary.Race().Generate();
        PoolRules anyRules = Arbitrary.PoolRules().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StakePool(null!, BetType.Win, 1m, anyRules));
        Assert.Throws<ArgumentNullException>(() => new StakePool(anyRace, BetType.Win, 1m, null!));
    }

    [Fact]
    public void An_undefined_bet_type_is_refused() {
        // Arrange
        Race      anyRace  = Arbitrary.Race().Generate();
        PoolRules anyRules = Arbitrary.PoolRules().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new StakePool(anyRace, (BetType)99, 1m, anyRules));
    }

    // ---- Accepting stakes ----------------------------------------------------------------

    [Fact]
    public void An_open_pool_accepts_a_stake_of_its_bet_type_on_its_runners() {
        // Arrange
        StakePool pool  = Arbitrary.StakePool().WithBetType(BetType.Exacta).Generate();
        Stake     stake = Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Exacta).WithHorsesAmong(pool.Race.FieldSize)).Generate();

        // Act
        pool.Accept(stake);

        // Assert
        Assert.Same(stake, Assert.Single(pool.Stakes));
    }

    [Fact]
    public void The_gross_amount_is_the_total_units_times_the_unit_stake() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(6).Generate();
        StakePool pool = new StakePool(race, BetType.Win, 2.50m, NoTakeout);

        // Act
        pool.Accept(new Stake(Of(BetType.Win, 3), 3));
        pool.Accept(new Stake(Of(BetType.Win, 5), 4));
        pool.Accept(new Stake(Of(BetType.Win, 3), 1));

        // Assert
        Assert.Equal(3, pool.Stakes.Count);
        Assert.Equal(20.00m, pool.GrossAmount);
    }

    [Fact]
    public void A_stake_of_another_bet_type_is_refused() {
        // Arrange
        StakePool pool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        Stake     exactaStake = Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Exacta).WithHorsesAmong(pool.Race.FieldSize)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pool.Accept(exactaStake));
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void A_stake_on_a_horse_beyond_the_field_is_refused() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Quinella, 1m, NoTakeout);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pool.Accept(new Stake(Of(BetType.Quinella, 3, 9), 1)));
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void A_stake_on_the_last_runner_is_accepted() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Quinella, 1m, NoTakeout);

        // Act
        pool.Accept(new Stake(Of(BetType.Quinella, 3, 8), 1));

        // Assert
        Assert.Single(pool.Stakes);
    }

    [Fact]
    public void A_pool_does_not_accept_a_missing_stake() {
        // Arrange
        StakePool anyPool = Arbitrary.StakePool().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPool.Accept(null!));
    }

    [Fact]
    public void A_closed_pool_accepts_no_more_stakes() {
        // Arrange
        StakePool pool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        Stake     stake = Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesAmong(pool.Race.FieldSize)).Generate();
        pool.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pool.Accept(stake));
        Assert.Empty(pool.Stakes);
    }

    // ---- Life cycle ----------------------------------------------------------------------

    [Fact]
    public void Closing_an_open_pool_closes_it() {
        // Arrange
        StakePool anyPool = Arbitrary.StakePool().Generate();

        // Act
        anyPool.Close();

        // Assert
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_closed_twice() {
        // Arrange
        StakePool anyPool = Arbitrary.StakePool().Generate();
        anyPool.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyPool.Close());
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void An_open_pool_cannot_be_settled() {
        // Arrange
        StakePool  pool   = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        RaceResult result = Arbitrary.RaceResult().WithRace(pool.Race).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pool.Settle(result));
        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Fact]
    public void Settling_closes_the_accounts_of_the_pool() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(6).Generate();
        StakePool pool = new StakePool(race, BetType.Win, 1m, NoTakeout);
        pool.Accept(new Stake(Of(BetType.Win, 3), 1));
        pool.Close();

        // Act
        pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(PoolStatus.Settled, pool.Status);
    }

    [Fact]
    public void A_settled_pool_cannot_be_settled_closed_or_fed_again() {
        // Arrange
        Race       race   = Arbitrary.Race().WithFieldSize(6).Generate();
        StakePool  pool   = new StakePool(race, BetType.Win, 1m, NoTakeout);
        RaceResult result = new RaceResult(race, Horses(3), Array.Empty<HorseNumber>());
        pool.Close();
        pool.Settle(result);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pool.Settle(result));
        Assert.Throws<InvalidOperationException>(() => pool.Close());
        Assert.Throws<InvalidOperationException>(() => pool.Accept(new Stake(Of(BetType.Win, 3), 1)));
    }

    [Fact]
    public void A_result_of_another_race_cannot_settle_a_pool() {
        // Arrange
        StakePool  pool        = Arbitrary.StakePool().Generate();
        RaceResult otherResult = Arbitrary.RaceResult().WithRace(r => r.WithCode("ZZZ-R1")).Generate();
        pool.Close();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pool.Settle(otherResult));
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_pool_needs_a_result_to_settle() {
        // Arrange
        StakePool anyPool = Arbitrary.StakePool().Generate();
        anyPool.Close();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPool.Settle(null!));
    }

    // ---- Settlement ----------------------------------------------------------------------

    [Fact]
    public void A_win_pool_pays_the_net_pool_to_the_backers_of_the_winner_rounded_down_to_the_breakage_step() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.10m, 0.10m, 1.00m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 3), 7));
        pool.Accept(new Stake(Of(BetType.Win, 5), 43));
        pool.Accept(new Stake(Of(BetType.Win, 7), 50));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3, 5, 7), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(100m, settlement.GrossPool);
        Assert.Equal(0m, settlement.Refunded);
        Assert.Equal(10m, settlement.Deductions);
        Assert.Equal(90m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Of(BetType.Win, 3), dividend.Combination);
        Assert.Equal(12.80m, dividend.AmountPerUnit);
        Assert.Equal(7, dividend.WinningUnits);
        Assert.Equal(0m, settlement.Carryover);
        Assert.Equal(0.40m, settlement.Breakage);
    }

    [Fact]
    public void The_takeout_is_rounded_down_to_the_cent() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.175m, 0.01m, 1.00m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 0.10m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 2), 111));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(2), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(11.10m, settlement.GrossPool);
        Assert.Equal(1.94m, settlement.Deductions);
        Assert.Equal(9.16m, settlement.NetPool);
    }

    [Fact]
    public void The_minimum_return_tops_up_a_dividend_the_pool_cannot_fund() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.25m, 0.10m, 1.20m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 3), 90));
        pool.Accept(new Stake(Of(BetType.Win, 5), 10));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(75m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(1.20m, dividend.AmountPerUnit);
        Assert.Equal(-33m, settlement.Breakage);
    }

    [Fact]
    public void The_minimum_return_is_rounded_to_the_cent_when_the_unit_stake_makes_it_fractional() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.49m, 0.01m, 1.05m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 0.10m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 3), 100));
        pool.Accept(new Stake(Of(BetType.Win, 5), 100));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(0.11m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_stake_on_a_scratched_horse_is_refunded_before_the_takeout() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.10m, 0.01m, 1.00m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 3), 10));
        pool.Accept(new Stake(Of(BetType.Win, 5), 20));
        pool.Accept(new Stake(Of(BetType.Win, 7), 30));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3, 7), Horses(5)));

        // Assert
        Assert.Equal(60m, settlement.GrossPool);
        Assert.Equal(20m, settlement.Refunded);
        Assert.Equal(4m, settlement.Deductions);
        Assert.Equal(36m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(3.60m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_stake_is_refunded_when_a_scratched_horse_is_any_of_its_horses() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool  = new StakePool(race, BetType.Trifecta, 1m, NoTakeout);
        pool.Accept(new Stake(Of(BetType.Trifecta, 1, 2, 3), 5));
        pool.Accept(new Stake(Of(BetType.Trifecta, 4, 6, 1), 7));
        pool.Accept(new Stake(Of(BetType.Trifecta, 4, 5, 6), 11));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(1, 2, 3), Horses(6)));

        // Assert
        Assert.Equal(18m, settlement.Refunded);
        Assert.Equal(5m, settlement.NetPool);
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(1.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void When_nobody_backed_the_winner_the_whole_net_pool_is_carried_over() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.10m, 0.01m, 1.00m, 1);
        StakePool pool  = new StakePool(race, BetType.Win, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Win, 5), 20));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(18m, settlement.NetPool);
        Assert.Equal(18m, settlement.Carryover);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void A_pool_without_any_stake_settles_to_nothing() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Win, 1m, NoTakeout);
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(0m, settlement.GrossPool);
        Assert.Equal(0m, settlement.NetPool);
        Assert.Empty(settlement.Dividends);
        Assert.Equal(0m, settlement.Carryover);
    }

    [Fact]
    public void A_pool_whose_every_stake_is_refunded_carries_nothing_over() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Win, 1m, new PoolRules(0.10m, 0.01m, 1m, 1));
        pool.Accept(new Stake(Of(BetType.Win, 5), 20));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Horses(5)));

        // Assert
        Assert.Equal(20m, settlement.Refunded);
        Assert.Equal(0m, settlement.Deductions);
        Assert.Equal(0m, settlement.Carryover);
        Assert.Empty(settlement.Dividends);
    }

    [Fact]
    public void A_quinella_is_won_by_the_first_two_in_either_order() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Quinella, 1m, NoTakeout);
        pool.Accept(new Stake(Of(BetType.Quinella, 3, 5), 4));
        pool.Accept(new Stake(Of(BetType.Quinella, 5, 3), 6));
        pool.Accept(new Stake(Of(BetType.Quinella, 3, 7), 10));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(5, 3, 7), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Of(BetType.Quinella, 3, 5), dividend.Combination);
        Assert.Equal(10, dividend.WinningUnits);
        Assert.Equal(2.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void An_exacta_is_won_by_the_first_two_in_the_exact_order_only() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Exacta, 1m, NoTakeout);
        pool.Accept(new Stake(Of(BetType.Exacta, 5, 3), 4));
        pool.Accept(new Stake(Of(BetType.Exacta, 3, 5), 6));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(5, 3, 7), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Of(BetType.Exacta, 5, 3), dividend.Combination);
        Assert.Equal(4, dividend.WinningUnits);
        Assert.Equal(2.50m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_trifecta_needs_the_first_three_in_the_exact_order() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Trifecta, 1m, NoTakeout);
        pool.Accept(new Stake(Of(BetType.Trifecta, 5, 3, 7), 5));
        pool.Accept(new Stake(Of(BetType.Trifecta, 3, 5, 7), 15));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(5, 3, 7, 1), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Of(BetType.Trifecta, 5, 3, 7), dividend.Combination);
        Assert.Equal(4.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void An_exacta_cannot_be_settled_by_a_result_with_a_single_finisher_and_stays_closed() {
        // Arrange
        Race      race = Arbitrary.Race().WithFieldSize(8).Generate();
        StakePool pool = new StakePool(race, BetType.Exacta, 1m, NoTakeout);
        pool.Close();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pool.Settle(new RaceResult(race, Horses(5), Array.Empty<HorseNumber>())));
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_place_pool_splits_the_net_pool_between_the_paid_places_that_were_backed() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0m, 0.05m, 1.00m, 2);
        StakePool pool  = new StakePool(race, BetType.Place, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Place, 3), 10));
        pool.Accept(new Stake(Of(BetType.Place, 5), 30));
        pool.Accept(new Stake(Of(BetType.Place, 7), 60));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3, 5, 7), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Equal(2, settlement.Dividends.Count);
        Dividend first  = settlement.Dividends.Single(dividend => dividend.Combination.Equals(Of(BetType.Place, 3)));
        Dividend second = settlement.Dividends.Single(dividend => dividend.Combination.Equals(Of(BetType.Place, 5)));
        Assert.Equal(5.00m, first.AmountPerUnit);
        Assert.Equal(1.65m, second.AmountPerUnit);
        Assert.Equal(100m - first.Payout - second.Payout, settlement.Breakage);
    }

    [Fact]
    public void A_place_pool_gives_the_whole_net_pool_to_the_only_backed_place() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0m, 0.01m, 1.00m, 3);
        StakePool pool  = new StakePool(race, BetType.Place, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Place, 3), 10));
        pool.Accept(new Stake(Of(BetType.Place, 7), 90));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3, 5), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(Of(BetType.Place, 3), dividend.Combination);
        Assert.Equal(10.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_pays_only_as_many_places_as_the_result_names() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0m, 0.01m, 1.00m, 3);
        StakePool pool  = new StakePool(race, BetType.Place, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Place, 3), 10));
        pool.Accept(new Stake(Of(BetType.Place, 7), 10));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3), Array.Empty<HorseNumber>()));

        // Assert
        Dividend dividend = Assert.Single(settlement.Dividends);
        Assert.Equal(2.00m, dividend.AmountPerUnit);
    }

    [Fact]
    public void A_place_pool_with_no_backer_in_the_paid_places_carries_the_net_pool_over() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0.20m, 0.01m, 1.00m, 2);
        StakePool pool  = new StakePool(race, BetType.Place, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Place, 7), 50));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(3, 5, 7), Array.Empty<HorseNumber>()));

        // Assert
        Assert.Empty(settlement.Dividends);
        Assert.Equal(40m, settlement.Carryover);
    }

    [Fact]
    public void A_place_stake_on_a_horse_finishing_beyond_the_paid_places_loses() {
        // Arrange
        Race      race  = Arbitrary.Race().WithFieldSize(8).Generate();
        PoolRules rules = new PoolRules(0m, 0.01m, 1.00m, 2);
        StakePool pool  = new StakePool(race, BetType.Place, 1m, rules);
        pool.Accept(new Stake(Of(BetType.Place, 2), 10));
        pool.Accept(new Stake(Of(BetType.Place, 4), 10));
        pool.Accept(new Stake(Of(BetType.Place, 6), 20));
        pool.Close();

        // Act
        Settlement settlement = pool.Settle(new RaceResult(race, Horses(4, 6, 2), Horses(8, 1)));

        // Assert
        Assert.Equal(0m, settlement.Refunded);
        Assert.Equal(2, settlement.Dividends.Count);
        Assert.DoesNotContain(settlement.Dividends, dividend => dividend.Combination.Equals(Of(BetType.Place, 2)));
    }

}
