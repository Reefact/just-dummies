using System;

using JustDummies;

namespace RealEstate.Domain;

// Written by hand, beside the scaffolded file: the rating is derived from the figures, so a test that needs
// a given rating asks for figures that give it.
public sealed partial class AnyEnergyPerformanceDiagnosis {

    // Upper bounds (exclusive) of the energy band and of the emissions band of each rating, A to G.
    private static readonly int[] EnergyUpperBounds    = [70, 110, 180, 250, 330, 420, 601];
    private static readonly int[] EmissionsUpperBounds = [6, 11, 30, 50, 70, 100, 151];

    /// <summary>The figures give exactly <paramref name="rating" />: the energy lies in its band, the emissions in its band or a better one.</summary>
    public AnyEnergyPerformanceDiagnosis WithRating(EnergyRating rating) {
        int index = (int)rating;
        int energyFloor = index == 0 ? 0 : EnergyUpperBounds[index - 1];

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._primaryEnergyKwhPerSquareMeterYear = Any.Int32().Between(energyFloor, EnergyUpperBounds[index] - 1);
        copy._emissionsKgCo2PerSquareMeterYear   = Any.Int32().Between(0, EmissionsUpperBounds[index] - 1);

        return copy;
    }

}
