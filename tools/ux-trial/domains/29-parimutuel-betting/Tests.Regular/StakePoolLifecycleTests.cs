using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class StakePoolLifecycleTests {

    [Fact]
    public void The_gross_amount_is_every_unit_taken_at_the_unit_stake() {
        // Arrange
        StakePool anyWinPool = Arbitrary.StakePool().WithBetType(BetType.Win).WithUnitStake(2.50m).Generate();
        anyWinPool.Accept(Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesBetween(1, 4)).WithUnits(10).Generate());
        anyWinPool.Accept(Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesBetween(1, 4)).WithUnits(3).Generate());

        // Act
        decimal gross = anyWinPool.GrossAmount;

        // Assert
        Assert.Equal(32.50m, gross);
    }

    [Fact]
    public void A_closed_pool_no_longer_accepts_stakes() {
        // Arrange
        StakePool anyWinPool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        Stake     anyStake   = Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesBetween(1, 4)).Generate();
        anyWinPool.Close();

        // Act
        Action accept = () => anyWinPool.Accept(anyStake);

        // Assert
        Assert.Throws<InvalidOperationException>(accept);
    }

    [Fact]
    public void A_pool_refuses_a_stake_of_another_bet_type() {
        // Arrange
        StakePool anyExactaPool = Arbitrary.StakePool().WithBetType(BetType.Exacta).Generate();
        Stake     anyQuinella   = Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Quinella).WithHorsesBetween(1, 4)).Generate();

        // Act
        Action accept = () => anyExactaPool.Accept(anyQuinella);

        // Assert
        Assert.Throws<ArgumentException>(accept);
    }

    [Fact]
    public void A_pool_refuses_a_stake_on_a_horse_that_is_not_declared() {
        // Arrange
        StakePool anyWinPool = Arbitrary.StakePool()
                                        .WithBetType(BetType.Win)
                                        .WithRace(r => r.WithFieldSize(8))
                                        .Generate();
        Stake anyStakeOnHorseNine = Arbitrary.Stake()
                                             .WithCombination(new Combination(BetType.Win, [new HorseNumber(9)]))
                                             .Generate();

        // Act
        Action accept = () => anyWinPool.Accept(anyStakeOnHorseNine);

        // Assert
        Assert.Throws<ArgumentException>(accept);
    }

    [Fact]
    public void A_pool_cannot_be_closed_twice() {
        // Arrange
        StakePool anyPool = Arbitrary.StakePool().Generate();
        anyPool.Close();

        // Act
        Action closeAgain = anyPool.Close;

        // Assert
        Assert.Throws<InvalidOperationException>(closeAgain);
    }

    [Fact]
    public void An_open_pool_cannot_be_settled() {
        // Arrange
        StakePool  anyWinPool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        RaceResult anyResult  = new RaceResult(anyWinPool.Race, [new HorseNumber(1)], []);

        // Act
        Action settle = () => anyWinPool.Settle(anyResult);

        // Assert
        Assert.Throws<InvalidOperationException>(settle);
    }

    [Fact]
    public void A_pool_cannot_be_settled_with_the_result_of_another_race() {
        // Arrange
        StakePool anyWinPool = Arbitrary.StakePool()
                                        .WithBetType(BetType.Win)
                                        .WithRace(new Race("LCH-R4", 10))
                                        .Generate();
        anyWinPool.Close();
        Race       otherRace          = new Race("LCH-R5", 10);
        RaceResult resultOfOtherRace  = new RaceResult(otherRace, [new HorseNumber(1)], []);

        // Act
        Action settle = () => anyWinPool.Settle(resultOfOtherRace);

        // Assert
        Assert.Throws<ArgumentException>(settle);
    }

    [Fact]
    public void A_settled_pool_is_marked_settled() {
        // Arrange
        StakePool anyWinPool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
        anyWinPool.Accept(Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesBetween(1, 4)).Generate());
        anyWinPool.Close();
        RaceResult anyResult = new RaceResult(anyWinPool.Race, [new HorseNumber(1)], []);

        // Act
        anyWinPool.Settle(anyResult);

        // Assert
        Assert.Equal(PoolStatus.Settled, anyWinPool.Status);
    }

}
