using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>Recipes for vectors whose rule is on their magnitude: a direction and a length, drawn apart.</summary>
public static class Orbital {

    /// <summary>A unit vector, uniform over the sphere.</summary>
    public static IAny<Vector3> Directions() {
        return Any.Combine(
            Any.Double().Between(0, 2 * Math.PI),
            Any.Double().Between(-1, 1),
            (azimuth, cosPolar) => {
                double sinPolar = Math.Sqrt(1 - cosPolar * cosPolar);
                return new Vector3(sinPolar * Math.Cos(azimuth), sinPolar * Math.Sin(azimuth), cosPolar);
            });
    }

    /// <summary>A position above the surface, at an altitude in the given band (km).</summary>
    public static IAny<Vector3> Positions(double minAltitudeKm = 200, double maxAltitudeKm = 90_000) {
        return Any.Combine(
            Directions(),
            Any.Double().Between(minAltitudeKm, maxAltitudeKm),
            (direction, altitude) => direction.Scale(OrbitalState.EarthRadiusKm + altitude));
    }

    /// <summary>A velocity whose speed lies in the given band (km/s): never zero, never beyond 12 km/s.</summary>
    public static IAny<Vector3> Velocities(double minSpeed = 1, double maxSpeed = 11) {
        return Any.Combine(
            Directions(),
            Any.Double().Between(minSpeed, maxSpeed),
            (direction, speed) => direction.Scale(speed));
    }

}
