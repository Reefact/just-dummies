namespace ParimutuelBetting.Domain.Tests.Expert.Support;

/// <summary>Spells out the horses, tickets and pools that a test is about, so that the numbers stay visible in the test.</summary>
internal static class Tickets {

    public static IReadOnlyList<HorseNumber> Horses(params int[] numbers) {
        return numbers.Select(number => new HorseNumber(number)).ToArray();
    }

    public static Combination Bet(BetType betType, params int[] horses) {
        return new Combination(betType, Horses(horses));
    }

    public static Stake Ticket(BetType betType, int units, params int[] horses) {
        return new Stake(Bet(betType, horses), units);
    }

    public static StakePool ClosedPool(Race race, BetType betType, decimal unitStake, PoolRules rules, params Stake[] stakes) {
        StakePool pool = new StakePool(race, betType, unitStake, rules);
        foreach (Stake stake in stakes) {
            pool.Accept(stake);
        }

        pool.Close();

        return pool;
    }

    public static RaceResult Result(Race race, int[] finishingOrder, params int[] scratched) {
        return new RaceResult(race, Horses(finishingOrder), Horses(scratched));
    }

    /// <summary>Rules where nothing but the sharing of the pool happens: no takeout, finest breakage, no top-up beyond the stake.</summary>
    public static PoolRules PlainRules(int placesPaid = 1) {
        return new PoolRules(takeoutRate: 0m, breakageStep: 0.01m, minimumReturnRatio: 1.00m, placesPaid: placesPaid);
    }

}
