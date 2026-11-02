using System;

using JustDummies;

namespace RealEstate.Domain;

public sealed partial class AnyEnergyPerformanceDiagnosis {

    // Band bounds restated from the DPE grid: [lowest, highest] energy of each band, and the highest emissions
    // that do not push the rating past it.
    private static readonly (int EnergyLow, int EnergyHigh, int EmissionsHigh)[] Bands = {
        (0, 69, 5),       // A
        (70, 109, 10),    // B
        (110, 179, 29),   // C
        (180, 249, 49),   // D
        (250, 329, 69),   // E
        (330, 419, 99),   // F
        (420, 600, 150)   // G
    };

    /// <summary>A diagnosis whose rating is <paramref name="rating" />, the figures staying arbitrary within it.</summary>
    public AnyEnergyPerformanceDiagnosis OfRating(EnergyRating rating) {
        (int energyLow, int energyHigh, int emissionsHigh) = Bands[(int)rating];

        return WithPrimaryEnergyKwhPerSquareMeterYear(energy => energy.Between(energyLow, energyHigh))
              .WithEmissionsKgCo2PerSquareMeterYear(emissions => emissions.Between(0, emissionsHigh));
    }

    /// <summary>A diagnosis rated A to D: never banned from letting.</summary>
    public AnyEnergyPerformanceDiagnosis Lettable() {
        return WithPrimaryEnergyKwhPerSquareMeterYear(energy => energy.LessThan(250))
              .WithEmissionsKgCo2PerSquareMeterYear(emissions => emissions.LessThan(50));
    }

}
