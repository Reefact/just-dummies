using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert.Entities;

/// <summary>
///     The two set-ups the culture tests share. What each one pins is what the scenarios below depend on:
///     a species that grows at 30 degrees without lag and a chamber that lets it.
/// </summary>
internal static class Scenes {

    /// <summary>
    ///     Grows from 10 to 50 degrees, optimum 30, at up to 1 per hour, with no lag and a yield of 1e10 cells/g.
    ///     On the control bench (30 degrees, 5 g/L, 1 g) it grows at 5 / 5.5 per hour towards 5e7 cells/mL.
    /// </summary>
    public static AnySpecies AnyGrowingSpecies() {
        return Any.Species().Mesophilic().GrowingAt(1d).WithLagHours(0d).WithYieldCellsPerGram(1e10);
    }

    public static AnyCulture AnyControlCulture() {
        return Any.Culture().WithConditions(conditions => conditions.OnTheControlBench());
    }

    /// <summary>The control bench turned up to 60 degrees: above the maximum of <see cref="AnyGrowingSpecies" />.</summary>
    public static AnyCulture AnyScaldingCulture() {
        return Any.Culture().WithConditions(conditions => conditions.OnTheControlBench().WithTemperatureCelsius(60d));
    }

    public static Species AnotherSpeciesThan(params Species[] taken) {
        return Any.Species().WithBinomialName(name => name.Except(taken.Select(species => species.BinomialName))).Generate();
    }

}
