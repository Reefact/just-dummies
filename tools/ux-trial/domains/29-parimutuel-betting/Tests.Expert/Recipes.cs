using JustDummies;

using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Tests.Expert;

/// <summary>The few shapes the settlement tests keep reaching for.</summary>
internal static class Recipes {

    /// <summary>Distinct runners of the race: which horse plays which part is arbitrary.</summary>
    public static HorseNumber[] DistinctRunnersOf(Race race, int count) {
        return Any.ArrayOf(Any.HorseNumber().RunnerOf(race)).WithCount(count).Distinct().Generate();
    }

    /// <summary>A unit stake, for a test opening a pool through its constructor: the pool generator's own base.</summary>
    public static AnyDecimal AnyUnitStake() {
        return AnyStakePool.AnyValidUnitStake();
    }

    /// <summary>A race whose card holds at least that many runners.</summary>
    public static Race AnyRaceWithAtLeast(int runners) {
        return Any.Race().WithFieldSize(size => size.GreaterThanOrEqualTo(runners)).Generate();
    }

    public static Combination Win(HorseNumber horse) => new Combination(BetType.Win, [horse]);

    public static Combination Place(HorseNumber horse) => new Combination(BetType.Place, [horse]);

    public static Combination Quinella(HorseNumber one, HorseNumber other) => new Combination(BetType.Quinella, [one, other]);

    public static Combination Exacta(HorseNumber first, HorseNumber second) => new Combination(BetType.Exacta, [first, second]);

    public static Combination Trifecta(HorseNumber first, HorseNumber second, HorseNumber third) =>
        new Combination(BetType.Trifecta, [first, second, third]);

    /// <summary>Takes the stakes, closes the pool at the off and settles it against the official order.</summary>
    public static Settlement SettleAfter(StakePool pool, IReadOnlyList<HorseNumber> finishingOrder, params Stake[] stakes) {
        return SettleAfter(pool, finishingOrder, [], stakes);
    }

    public static Settlement SettleAfter(StakePool pool, IReadOnlyList<HorseNumber> finishingOrder, IReadOnlyCollection<HorseNumber> scratched, params Stake[] stakes) {
        foreach (Stake stake in stakes) {
            pool.Accept(stake);
        }

        pool.Close();

        return pool.Settle(new RaceResult(pool.Race, finishingOrder, scratched));
    }

}
