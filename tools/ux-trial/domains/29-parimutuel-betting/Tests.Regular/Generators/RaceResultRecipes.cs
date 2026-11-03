using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>Recipes on <see cref="AnyRaceResult" /> that state a fact of the race the result is declared for.</summary>
public static class RaceResultRecipes {

    /// <summary>A race with at least <paramref name="runners" /> runners, so that horses chosen by number belong to it.</summary>
    public static AnyRaceResult WithAtLeastRunners(this AnyRaceResult results, int runners) {
        return results.WithRace(race => race.WithFieldSize(size => size.GreaterThanOrEqualTo(runners)));
    }

}
