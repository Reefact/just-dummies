using JustDummies;
using JustDummies.Xunit;

using OrbitalMicrobiology.Domain;

[assembly: Reproducible]

namespace OrbitalMicrobiology.Tests.Regular;

/// <summary>Recipes the culture tests share: what a growing culture needs, and nothing the tests assert on.</summary>
internal static class Lab {

    public const double MinTemperature     = 10;
    public const double OptimalTemperature = 30;
    public const double MaxTemperature     = 40;

    /// <summary>A species with cardinal temperatures 10 / 30 / 40 °C; everything else is arbitrary.</summary>
    public static AnySpecies Mesophile() {
        return Arbitrary.Species()
                        .WithMinTemperatureCelsius(MinTemperature)
                        .WithOptimalTemperatureCelsius(OptimalTemperature)
                        .WithMaxTemperatureCelsius(MaxTemperature);
    }

    /// <summary>Conditions with at least 1 g/L of nutrient, so a small inoculum always lies below the carrying capacity.</summary>
    public static readonly Func<AnyExperimentalConditions, AnyExperimentalConditions> Fed =
        c => c.WithNutrientGramsPerLitre(n => n.GreaterThanOrEqualTo(1));

    /// <summary>Fed conditions at a temperature where the <see cref="Mesophile" /> grows.</summary>
    public static readonly Func<AnyExperimentalConditions, AnyExperimentalConditions> WhereMesophilesGrow =
        c => Fed(c).WithTemperatureCelsius(t => t.GreaterThan(MinTemperature).LessThan(MaxTemperature));

    /// <summary>An inoculum far below any fed carrying capacity (at least 1e6 cells/mL), and large enough to survive a die-off.</summary>
    public static AnyConcentration SmallInoculum() {
        return Arbitrary.Concentration().WithCellsPerMillilitre(c => c.Between(100, 1_000));
    }

}
