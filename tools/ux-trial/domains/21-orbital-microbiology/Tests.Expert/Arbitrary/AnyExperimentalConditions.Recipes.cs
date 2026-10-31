using JustDummies;

using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

// Written by hand, beside the scaffolded file: `any generate --force` never touches it.
public sealed partial class AnyExperimentalConditions {

    /// <summary>
    ///     A chamber on the station, out of any centrifuge: below the microgravity threshold, where
    ///     the whole 50 mL a chamber holds becomes available. This widens the volume base, so it is
    ///     written against the private constructor rather than as a refinement.
    /// </summary>
    public AnyExperimentalConditions InMicrogravity() {
        return new AnyExperimentalConditions(Any.Double().GreaterThanOrEqualTo(0).LessThan(ExperimentalConditions.MicrogravityThresholdG),
                                             _temperatureCelsius,
                                             _nutrientGramsPerLitre,
                                             Any.Double().GreaterThan(0).LessThanOrEqualTo(ExperimentalConditions.MaxVolumeMillilitres));
    }

    /// <summary>Held at the species' optimal temperature, where its temperature factor is 1.</summary>
    public AnyExperimentalConditions AtTheOptimumOf(Species species) {
        return WithTemperatureCelsius(species.OptimalTemperatureCelsius);
    }

    /// <summary>
    ///     A medium rich enough to carry a real population (at least 1 g/L, so at least 10^6 cells/mL
    ///     for the poorest yield). A trace of nutrient is a valid chamber, but not one a growth
    ///     scenario can be run in.
    /// </summary>
    public AnyExperimentalConditions Nourished() {
        return WithNutrientGramsPerLitre(n => n.GreaterThanOrEqualTo(1));
    }

}
