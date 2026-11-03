using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

/// <summary>The scaffolded generators are code we own: they must only ever draw values the domain accepts.</summary>
public sealed class GeneratorsTests {

    private const int Draws = 200;

    [Fact]
    public void Every_generator_draws_values_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Arbitrary.HorseNumber().Generate();
            Arbitrary.Combination().Generate();
            Arbitrary.Race().Generate();
            Arbitrary.PoolRules().Generate();
            Arbitrary.Stake().Generate();
            Arbitrary.RaceResult().Generate();
            Arbitrary.Dividend().Generate();
            Arbitrary.Settlement().Generate();
            Arbitrary.StakePool().Generate();
        }
    }

    [Fact]
    public void A_drawn_race_result_is_declared_for_a_race_it_can_settle() {
        for (int draw = 0; draw < Draws; draw++) {
            // Arrange
            StakePool  pool   = Arbitrary.StakePool().Generate();
            RaceResult result = Arbitrary.RaceResult().WithRace(pool.Race).Generate();
            pool.Close();

            // Act & Assert
            // Only the arity can stop a drawn result: fewer finishers than a Quinella, Exacta or Trifecta needs.
            if (result.FinishingOrder.Count >= pool.BetType.Arity() || pool.BetType == BetType.Place) {
                pool.Settle(result);
                Assert.Equal(PoolStatus.Settled, pool.Status);
            }
        }
    }

    [Fact]
    public void A_pool_settled_with_drawn_stakes_accounts_for_every_cent() {
        for (int draw = 0; draw < Draws; draw++) {
            // Arrange
            StakePool pool = Arbitrary.StakePool().WithBetType(BetType.Win).Generate();
            List<Stake> stakes = Any.ListOf(Arbitrary.Stake().WithCombination(c => c.WithBetType(BetType.Win).WithHorsesAmong(pool.Race.FieldSize)))
                                    .WithCountBetween(0, 12).Generate();
            foreach (Stake stake in stakes) { pool.Accept(stake); }

            pool.Close();
            RaceResult result = Arbitrary.RaceResult().WithRace(pool.Race).Generate();

            // Act
            Settlement settlement = pool.Settle(result);

            // Assert: what was collected is either refunded, deducted, paid, kept as breakage or carried over
            Assert.Equal(pool.GrossAmount, settlement.Refunded + settlement.Deductions + settlement.TotalPayout + settlement.Breakage + settlement.Carryover);
        }
    }

}
