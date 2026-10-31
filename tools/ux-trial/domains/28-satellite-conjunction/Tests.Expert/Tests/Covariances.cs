using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Tests;

/// <summary>Covariances a test chooses on purpose, because the risk it checks depends on them.</summary>
internal static class Covariances {

    /// <summary>The same dispersion along every axis, uncorrelated.</summary>
    public static PositionCovariance Isotropic(double sigmaKm) {
        double variance = sigmaKm * sigmaKm;
        return new PositionCovariance(variance, variance, variance, 0, 0, 0);
    }

}
