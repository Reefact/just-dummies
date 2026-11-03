using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     Named refinements of the scaffolded generators. They live beside the generated files, not in them,
///     so that <c>any generate --force</c> leaves them alone. Each one says what a test chose.
/// </summary>
public static class Recipes {

    /// <summary>Grows from 10 to 50 degrees, fastest at 30.</summary>
    public static AnySpecies Mesophilic(this AnySpecies species) {
        return species.WithMinTemperatureCelsius(10d)
                      .WithOptimalTemperatureCelsius(30d)
                      .WithMaxTemperatureCelsius(50d);
    }

    public static AnySpecies GrowingAt(this AnySpecies species, double maxPerHour) {
        return species.WithMaxGrowthRate(rate => rate.WithPerHour(maxPerHour));
    }

    /// <summary>The 1 g control at 30 degrees, 5 g/L of nutrient. The volume is whatever the chamber takes.</summary>
    public static AnyExperimentalConditions OnTheControlBench(this AnyExperimentalConditions conditions) {
        return conditions.WithGravityG(1d)
                         .WithTemperatureCelsius(30d)
                         .WithNutrientGramsPerLitre(5d);
    }

    /// <summary>A concentration far below the smallest carrying capacity a 5 g/L chamber offers (5e6 cells/mL).</summary>
    public static AnyConcentration Dilute(this AnyConcentration concentration) {
        return concentration.WithCellsPerMillilitre(cells => cells.Between(1e3, 1e5));
    }

}
