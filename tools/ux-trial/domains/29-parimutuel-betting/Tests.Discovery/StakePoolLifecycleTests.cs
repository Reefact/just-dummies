using System;

using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

using static ParimutuelBetting.Tests.Discovery.Tickets;

namespace ParimutuelBetting.Tests.Discovery;

public sealed class StakePoolLifecycleTests {

    private static Stake AnyStakeFor(StakePool pool) {
        return Any.Stake()
                  .WithCombination(c => c.WithBetType(pool.BetType).WithHorsesAmong(pool.Race.FieldSize))
                  .Generate();
    }

    [Fact]
    public void A_new_pool_is_open() {
        // Arrange
        // Act
        StakePool anyPool = Any.StakePool().Generate();

        // Assert
        Assert.Equal(PoolStatus.Open, anyPool.Status);
    }

    [Fact]
    public void The_gross_amount_is_the_total_units_times_the_unit_stake() {
        // Arrange
        StakePool anyPool = Any.StakePool().WithUnitStake(2.50m).Generate();
        Stake     anyFourUnitStake = Any.Stake().WithUnits(4).WithCombination(c => c.WithBetType(anyPool.BetType).WithHorsesAmong(anyPool.Race.FieldSize)).Generate();
        Stake     anySixUnitStake  = Any.Stake().WithUnits(6).WithCombination(c => c.WithBetType(anyPool.BetType).WithHorsesAmong(anyPool.Race.FieldSize)).Generate();

        // Act
        anyPool.Accept(anyFourUnitStake);
        anyPool.Accept(anySixUnitStake);

        // Assert
        Assert.Equal(25.00m, anyPool.GrossAmount);
    }

    [Fact]
    public void A_closed_pool_refuses_a_stake() {
        // Arrange
        StakePool anyPool  = Any.StakePool().Generate();
        Stake     anyStake = AnyStakeFor(anyPool);
        anyPool.Close();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Accept(anyStake));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Empty(anyPool.Stakes);
    }

    [Fact]
    public void A_pool_refuses_a_stake_of_another_bet_type() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        Stake anyStakeOfAnotherType = Any.Stake()
                                         .WithCombination(c => c.WithBetType(b => b.Except(anyPool.BetType)).WithHorsesAmong(anyPool.Race.FieldSize))
                                         .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Accept(anyStakeOfAnotherType));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_pool_refuses_a_stake_on_a_horse_that_is_not_a_runner() {
        // Arrange
        StakePool anyWinPool = Any.StakePool()
                                  .WithBetType(BetType.Win)
                                  .WithRace(r => r.WithFieldSize(f => f.LessThan(HorseNumber.Maximum)))
                                  .Generate();
        int   firstNonRunner = anyWinPool.Race.FieldSize + 1;
        Stake stakeOnNonRunner = Any.Stake().WithCombination(On(BetType.Win, 1, firstNonRunner).Combination).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyWinPool.Accept(stakeOnNonRunner));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Closing_moves_an_open_pool_to_closed() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();

        // Act
        anyPool.Close();

        // Assert
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_closed_twice() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Close();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Close());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void An_open_pool_cannot_be_settled() {
        // Arrange
        StakePool  anyPool   = Any.StakePool().Generate();
        RaceResult anyResult = Any.RaceResult().Of(anyPool.Race).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Settle(anyResult));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(PoolStatus.Open, anyPool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_settled_with_the_result_of_another_race() {
        // Arrange
        StakePool  anyPool         = Any.StakePool().Generate();
        Race       anyOtherRace    = Any.Race().WithCode(c => c.DifferentFrom(anyPool.Race.Code)).WithFieldSize(anyPool.Race.FieldSize).Generate();
        RaceResult anyOtherResult  = Any.RaceResult().Of(anyOtherRace).Generate();
        anyPool.Close();

        // Act
        Exception? refusal = Record.Exception(() => anyPool.Settle(anyOtherResult));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Equal(PoolStatus.Closed, anyPool.Status);
    }

    [Fact]
    public void Settling_moves_a_closed_pool_to_settled() {
        // Arrange
        StakePool anyPool = Any.StakePool().Generate();
        anyPool.Accept(AnyStakeFor(anyPool));
        anyPool.Close();
        RaceResult anyResult = Any.RaceResult().Of(anyPool.Race).Generate();

        // Act
        anyPool.Settle(anyResult);

        // Assert
        Assert.Equal(PoolStatus.Settled, anyPool.Status);
    }

    [Fact]
    public void A_place_pool_needs_more_runners_than_places_paid() {
        // Arrange
        Race      anyThreeRunnerRace = Any.Race().WithFieldSize(3).Generate();
        PoolRules anyThreePlaceRules = Any.PoolRules().WithPlacesPaid(3).Generate();
        decimal   anyUnitStake       = Any.Decimal().Between(0.01m, 100m).WithScale(2).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new StakePool(anyThreeRunnerRace, BetType.Place, anyUnitStake, anyThreePlaceRules));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
