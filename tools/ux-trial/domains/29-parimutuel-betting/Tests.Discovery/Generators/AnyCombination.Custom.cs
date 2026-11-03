// Written by hand, beside the scaffolded file: `any generate Combination --force` leaves it alone.
using JustDummies;

namespace ParimutuelBetting.Domain;

public static class AnyCombinationRecipes {

    /// <summary>Draws the horses among the runners 1..<paramref name="fieldSize" /> of a race, as the race owns that fact.</summary>
    public static AnyCombination WithHorsesAmong(this AnyCombination combinations, int fieldSize) {
        return combinations.WithHorses(_ => Any.ListOf(new AnyHorseNumber().WithValue(number => number.Between(1, fieldSize)))
                                              .WithCount(Math.Min(3, fieldSize))
                                              .Distinct());
    }

}
