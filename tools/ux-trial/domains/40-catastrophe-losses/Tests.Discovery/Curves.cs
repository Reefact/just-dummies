using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Discovery;

/// <summary>Curves whose shape a test depends on: their points are chosen, only their name is a dummy.</summary>
internal static class Curves {

    /// <summary>Damage ratio = intensity / 100, capped at 1 from intensity 100.</summary>
    public static VulnerabilityCurve Linear() {
        return Any.VulnerabilityCurve().WithIntensities([0.0, 100.0]).WithDamageRatios([0.0, 1.0]).Generate();
    }

}
