using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

/// <summary>
///     Pools whose stakes, rules, unit stake and result are all arbitrary. None of those values reaches an assertion:
///     the draw only asks the settlement code whether it depends on any of them.
/// </summary>
public sealed class StakePoolArbitrarySettlementTests {

    [Theory]
    [InlineData(BetType.Win)]
    [InlineData(BetType.Place)]
    [InlineData(BetType.Quinella)]
    [InlineData(BetType.Exacta)]
    [InlineData(BetType.Trifecta)]
    public void A_pool_with_arbitrary_stakes_rules_and_result_settles(BetType betType) {
        Race       race   = Any.Race().WithFieldSize(fieldSize => fieldSize.GreaterThanOrEqualTo(8)).Generate();
        RaceResult result = Any.RaceResult().WithRace(race).WithFinishersCount(3).Generate();

        StakePool pool = Any.StakePool().WithRace(race).WithBetType(betType).Generate();
        foreach (Stake stake in Any.ListOf(Any.Stake().WithCombination(c => c.WithBetType(betType).AmongRunnersOf(race.FieldSize))).WithCountBetween(0, 20).Generate()) {
            pool.Accept(stake);
        }

        // One stake on the combination that wins, so that the dividend branch runs as often as the carry-over one.
        Combination winning = betType == BetType.Place
            ? new Combination(BetType.Place, new[] { result.FinishingOrder[0] })
            : result.WinningCombination(betType);
        pool.Accept(Any.Stake().WithCombination(new Combination(betType, winning.Horses)).Generate());
        pool.Close();

        pool.Settle(result);

        Assert.Equal(PoolStatus.Settled, pool.Status);
    }

}
