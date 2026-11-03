namespace ParimutuelBetting.Tests.Discovery;

/// <summary>Literals the tests are about, spelled shortly: the horses of a combination, a ticket, a race.</summary>
internal static class Horses {

    internal static HorseNumber[] Of(params int[] numbers) {
        return numbers.Select(number => new HorseNumber(number)).ToArray();
    }

    internal static Combination Combination(BetType betType, params int[] numbers) {
        return new Combination(betType, Of(numbers));
    }

    internal static Stake Stake(BetType betType, int units, params int[] numbers) {
        return new Stake(Combination(betType, numbers), units);
    }

    /// <summary>A race of the given field size; its code is a dummy.</summary>
    internal static Race RaceOf(int fieldSize) {
        return Any.Race().WithFieldSize(fieldSize).Generate();
    }

    /// <summary>A ticket valid for the pool (right bet type, runners of its race) whose content is a dummy.</summary>
    internal static Stake AnyStakeFor(StakePool pool) {
        return Any.Stake()
                  .WithCombination(c => c.WithBetType(pool.BetType).WithHorsesAmong(pool.Race.FieldSize))
                  .Generate();
    }

}
