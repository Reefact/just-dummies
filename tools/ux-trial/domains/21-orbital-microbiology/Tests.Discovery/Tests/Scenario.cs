using JustDummies;

using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Discovery;

/// <summary>
///     Shared recipes for the culture tests. What a test is about is pinned as a literal at its own
///     call site; what is only needed so that a culture can grow at all is stated here once.
/// </summary>
internal static class Scenario {

    public const double MesophileMin     = 10;
    public const double MesophileOptimum = 30;
    public const double MesophileMax     = 50;

    /// <summary>A species that grows between 10 and 50 °C, best at 30 °C, at up to 1 per hour, with no lag.</summary>
    public static AnySpecies Mesophile() {
        return Any.Species()
                  .WithCardinalTemperatures(MesophileMin, MesophileOptimum, MesophileMax)
                  .WithMaxGrowthRate(r => r.WithPerHour(1.0))
                  .WithLagHours(0);
    }

    /// <summary>A chamber in which <see cref="Mesophile" /> grows: microgravity, optimum temperature, a rich medium.</summary>
    public static AnyExperimentalConditions Favourable(AnyExperimentalConditions conditions) {
        return conditions.WithGravityG(0)
                         .WithTemperatureCelsius(MesophileOptimum)
                         .WithNutrientGramsPerLitre(n => n.GreaterThanOrEqualTo(5));
    }

    public static AnyCulture FavourableCulture() {
        return Any.Culture().WithConditions(Favourable);
    }

    /// <summary>The same chamber but at the given temperature, for the tests about growth stopping outside the cardinal range.</summary>
    public static AnyCulture FavourableCultureAt(double temperatureCelsius) {
        return Any.Culture().WithConditions(c => Favourable(c).WithTemperatureCelsius(temperatureCelsius));
    }

    /// <summary>A dilute inoculum, far under the carrying capacity of a 5 g/L medium (at least 5e6 cells/mL) and over 1 cell/mL.</summary>
    public static AnyConcentration Inoculum() {
        return Any.Concentration().WithCellsPerMillilitre(c => c.Between(100d, 1e5));
    }

    public static Concentration AnyInoculum() {
        return Inoculum().Generate();
    }

}
