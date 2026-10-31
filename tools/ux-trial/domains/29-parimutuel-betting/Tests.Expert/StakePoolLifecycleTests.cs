using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Expert;

/// <summary>Opening, taking stakes, closing at the off: everything before the money is shared.</summary>
public sealed class StakePoolLifecycleTests {

    /// <summary>A stake the pool accepts: its bet type, runners of its race.</summary>
    private static AnyStake AnyStakeFor(StakePool pool) {
        return Any.Stake().WithCombination(combination => combination.For(pool.BetType, pool.Race));
    }

    [Fact]
    public void The_gross_amount_is_every_unit_taken_times_the_unit_stake() {
        // Arrange
        StakePool pool = Any.StakePool().WithUnitStake(2.50m).Generate();

        // Act
        pool.Accept(AnyStakeFor(pool).WithUnits(3).Generate());
        pool.Accept(AnyStakeFor(pool).WithUnits(5).Generate());

        // Assert
        Assert.Equal(20.00m, pool.GrossAmount);
    }

    [Fact]
    public void A_closed_pool_no_longer_accepts_stakes() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Close();
        Stake anyStake = AnyStakeFor(anyPool).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Accept(anyStake));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Empty(anyPool.Stakes);
    }

    [Fact]
    public void A_pool_only_accepts_stakes_of_its_own_bet_type() {
        // Arrange
        StakePool anyPool         = Any.StakePool().Generate();
        BetType   anyOtherBetType = Any.Enum<BetType>().DifferentFrom(anyPool.BetType).Generate();
        Stake     anyForeignStake = Any.Stake().WithCombination(combination => combination.For(anyOtherBetType, anyPool.Race)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Accept(anyForeignStake));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Empty(anyPool.Stakes);
    }

    [Fact]
    public void A_pool_refuses_a_stake_naming_a_horse_that_is_not_on_the_card() {
        // Arrange
        StakePool   anyPool      = Any.StakePool().WithBetType(BetType.Win)
                                      .WithRace(race => race.WithFieldSize(size => size.LessThan(HorseNumber.Maximum)))
                                      .Generate();
        HorseNumber anyNonRunner = Any.HorseNumber().WithValue(value => value.GreaterThan(anyPool.Race.FieldSize)).Generate();
        Stake       anyStake     = Any.Stake().WithCombination(Recipes.Win(anyNonRunner)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Accept(anyStake));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Closing_a_pool_twice_is_refused() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Close();

        // Act
        Exception? refusal = Record.Exception(anyPool.Close);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void A_pool_still_open_cannot_be_settled() {
        // Arrange
        StakePool  anyPool   = Any.StakePool().Generate();
        RaceResult anyResult = Any.RaceResult().WithRace(anyPool.Race).SettlingEveryBetType().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Settle(anyResult));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(PoolStatus.Open, anyPool.Status);
    }

    [Fact]
    public void A_pool_is_settled_only_against_the_result_of_its_own_race() {
        // Arrange
        StakePool  anyPool            = Any.StakePool().Generate();
        Race       anyOtherRace       = Any.Race().WithCode(code => code.DifferentFrom(anyPool.Race.Code)).Generate();
        RaceResult anyOtherRaceResult = Any.RaceResult().WithRace(anyOtherRace).Generate();
        anyPool.Close();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Settle(anyOtherRaceResult));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void A_settled_pool_cannot_be_settled_again() {
        // Arrange
        StakePool  anyPool   = Any.StakePool().Generate();
        RaceResult anyResult = Any.RaceResult().WithRace(anyPool.Race).SettlingEveryBetType().Generate();
        anyPool.Accept(AnyStakeFor(anyPool).Generate());
        anyPool.Close();
        anyPool.Settle(anyResult);

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Settle(anyResult));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(PoolStatus.Settled, anyPool.Status);
    }

    [Fact]
    public void A_trifecta_pool_cannot_open_on_a_two_horse_race() {
        // Arrange
        Race      anyTwoHorseRace = Any.Race().WithFieldSize(2).Generate();
        decimal   anyUnitStake    = Recipes.AnyUnitStake().Generate();
        PoolRules anyRules        = Any.PoolRules().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new StakePool(anyTwoHorseRace, BetType.Trifecta, anyUnitStake, anyRules));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_place_pool_needs_more_runners_than_the_places_it_pays() {
        // Arrange
        Race      threeHorseRace         = Any.Race().WithFieldSize(3).Generate();
        PoolRules anyRulesPayingThree    = Any.PoolRules().WithPlacesPaid(3).Generate();
        decimal   anyUnitStake           = Recipes.AnyUnitStake().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new StakePool(threeHorseRace, BetType.Place, anyUnitStake, anyRulesPayingThree));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
