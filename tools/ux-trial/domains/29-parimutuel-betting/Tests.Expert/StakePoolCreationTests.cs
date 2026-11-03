using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class StakePoolCreationTests {

    public static TheoryData<decimal> UnitStakesInRange => new TheoryData<decimal> { 0.01m, 2.50m, 100.00m };

    public static TheoryData<decimal> UnitStakesOutOfRange => new TheoryData<decimal> { 0m, -0.01m, 100.01m, 1.005m, 0.001m };

    [Fact]
    public void A_new_pool_is_open_with_no_stake_and_nothing_collected() {
        StakePool pool = Any.StakePool().Generate();

        Assert.Equal(PoolStatus.Open, pool.Status);
        Assert.Empty(pool.Stakes);
        Assert.Equal(0m, pool.GrossAmount);
    }

    [Fact]
    public void A_pool_remembers_the_race_the_bet_type_the_unit_stake_and_the_rules_it_was_opened_with() {
        Race      race  = Any.Race().WithFieldSize(8).Generate();
        PoolRules rules = Any.PoolRules().Generate();

        StakePool pool = Any.StakePool().WithRace(race).WithBetType(BetType.Exacta).WithUnitStake(2.50m).WithRules(rules).Generate();

        Assert.Same(race, pool.Race);
        Assert.Equal(BetType.Exacta, pool.BetType);
        Assert.Equal(2.50m, pool.UnitStake);
        Assert.Same(rules, pool.Rules);
    }

    [Theory]
    [MemberData(nameof(UnitStakesInRange))]
    public void A_unit_stake_of_one_cent_to_one_hundred_is_accepted(decimal unitStake) {
        StakePool pool = Any.StakePool().WithUnitStake(unitStake).Generate();

        Assert.Equal(unitStake, pool.UnitStake);
    }

    [Theory]
    [MemberData(nameof(UnitStakesOutOfRange))]
    public void A_unit_stake_that_is_not_a_whole_number_of_cents_in_range_is_refused(decimal unitStake) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.StakePool().WithUnitStake(unitStake).Generate());

        Assert.Equal("unitStake", refusal.ParamName);
    }

    [Fact]
    public void A_pool_needs_a_race_and_rules() {
        Race      anyRace  = Any.Race().Generate();
        PoolRules anyRules = Any.PoolRules().Generate();

        Assert.Throws<ArgumentNullException>(() => new StakePool(null!, BetType.Win, 1.00m, anyRules));
        Assert.Throws<ArgumentNullException>(() => new StakePool(anyRace, BetType.Win, 1.00m, null!));
    }

    [Fact]
    public void A_pool_is_opened_for_a_declared_bet_type() {
        Race      anyRace  = Any.Race().Generate();
        PoolRules anyRules = Any.PoolRules().WithPlacesPaid(1).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new StakePool(anyRace, (BetType)99, 1.00m, anyRules));
    }

    [Theory]
    [InlineData(BetType.Win)]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Exacta)]
    public void A_two_runner_field_is_enough_for_every_bet_type_naming_at_most_two_horses(BetType betType) {
        Race twoRunners = Any.Race().WithFieldSize(2).Generate();

        StakePool pool = Any.StakePool().WithRace(twoRunners).WithBetType(betType).WithRules(r => r.WithPlacesPaid(1)).Generate();

        Assert.Equal(betType, pool.BetType);
    }

    [Fact]
    public void A_trifecta_needs_three_runners() {
        Race twoRunners = Any.Race().WithFieldSize(2).Generate();
        Race threeRunners = Any.Race().WithFieldSize(3).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.StakePool().WithRace(twoRunners).WithBetType(BetType.Trifecta).Generate());
        StakePool         pool    = Any.StakePool().WithRace(threeRunners).WithBetType(BetType.Trifecta).Generate();

        Assert.Equal("betType", refusal.ParamName);
        Assert.Equal(BetType.Trifecta, pool.BetType);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    public void A_place_pool_needs_more_runners_than_the_places_it_pays(int fieldSize, int placesPaid) {
        Race race = Any.Race().WithFieldSize(fieldSize).Generate();

        StakePool pool = Any.StakePool().WithRace(race).WithBetType(BetType.Place).WithRules(r => r.WithPlacesPaid(placesPaid)).Generate();

        Assert.Equal(placesPaid, pool.Rules.PlacesPaid);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    public void A_place_pool_cannot_pay_as_many_places_as_there_are_runners(int fieldSize, int placesPaid) {
        Race race = Any.Race().WithFieldSize(fieldSize).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.StakePool().WithRace(race).WithBetType(BetType.Place).WithRules(r => r.WithPlacesPaid(placesPaid)).Generate());

        Assert.Equal("rules", refusal.ParamName);
    }

    [Fact]
    public void The_places_paid_only_matter_to_a_place_pool() {
        Race twoRunners = Any.Race().WithFieldSize(2).Generate();

        StakePool pool = Any.StakePool().WithRace(twoRunners).WithBetType(BetType.Win).WithRules(r => r.WithPlacesPaid(3)).Generate();

        Assert.Equal(BetType.Win, pool.BetType);
    }

}
