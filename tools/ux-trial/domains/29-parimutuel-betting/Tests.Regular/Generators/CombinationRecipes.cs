using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>Recipes on <see cref="AnyCombination" /> that state a fact of the race the combination is placed in.</summary>
public static class CombinationRecipes {

    /// <summary>Names only runners of a race with <paramref name="fieldSize" /> runners, each horse once.</summary>
    public static AnyCombination WithHorsesAmong(this AnyCombination combinations, int fieldSize) {
        return combinations.WithHorses(_ => Any.ListOf(new AnyHorseNumber().WithValue(number => number.Between(1, fieldSize)))
                                              .Distinct());
    }

}
