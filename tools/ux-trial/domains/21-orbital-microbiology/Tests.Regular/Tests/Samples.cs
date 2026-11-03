using JustDummies;

using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests;

/// <summary>Named, reusable arrangements: what is significant is spelled out, everything else is drawn.</summary>
internal static class Samples {

    /// <summary>A species that grows best at 30 °C (cardinals 5 / 30 / 45); its name is a dummy.</summary>
    public static Species GrowerAt30C(double maxRatePerHour, double lagHours, double yieldCellsPerGram) {
        return Any.Species()
                  .WithCardinalTemperatures(5, 30, 45)
                  .WithMaxGrowthRate(new GrowthRate(maxRatePerHour))
                  .WithLagHours(lagHours)
                  .WithYieldCellsPerGram(yieldCellsPerGram)
                  .Generate();
    }

    /// <summary>The same traits as <paramref name="first" />, under another name.</summary>
    public static Species Sibling(Species first) {
        return Any.Species()
                  .WithBinomialName(name => name.DifferentFrom(first.BinomialName))
                  .WithCardinalTemperatures(first.MinTemperatureCelsius, first.OptimalTemperatureCelsius, first.MaxTemperatureCelsius)
                  .WithMaxGrowthRate(first.MaxGrowthRate)
                  .WithLagHours(first.LagHours)
                  .WithYieldCellsPerGram(first.YieldCellsPerGram)
                  .Generate();
    }

    /// <summary>1 g, 30 °C, 10 g/L, 5 mL: the arrangement the growth tests reason about by hand.</summary>
    public static ExperimentalConditions Earthlike30C() {
        return new ExperimentalConditions(gravityG: 1, temperatureCelsius: 30, nutrientGramsPerLitre: 10, volumeMillilitres: 5);
    }

    public static Culture NewCulture(ExperimentalConditions conditions) {
        return Any.Culture().WithConditions(conditions).Generate();
    }

    public static Observation ObservationOf(Culture culture, Species species, double elapsedHours, double cellsPerMillilitre, double relativeUncertainty) {
        return Any.Observation()
                  .WithCultureId(culture.Id)
                  .WithSpecies(species)
                  .WithElapsedHours(elapsedHours)
                  .WithMeasured(new Concentration(cellsPerMillilitre))
                  .WithRelativeUncertainty(relativeUncertainty)
                  .WithMethod(ObservationMethod.PlateCount)
                  .Generate();
    }
}
