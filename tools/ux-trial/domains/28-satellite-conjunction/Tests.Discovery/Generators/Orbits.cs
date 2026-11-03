using System;
using System.Collections.Generic;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     Hand-written building blocks shared by the generators of orbital states, trajectories and conjunctions:
///     a position above the Earth, a plausible velocity, and a trajectory that stays physically coherent
///     (a circular orbit sampled at strictly increasing epochs), so that interpolating it never leaves the
///     domain of <see cref="OrbitalState" />.
/// </summary>
public static class Orbits {

    private const double GravitationalParameter = 398_600.4418; // km^3/s^2

    /// <summary>A point strictly above the surface and below the altitude ceiling, anywhere around the Earth.</summary>
    public static IAny<Vector3> PositionKm() {
        return Any.Combine(Any.Double().Between(OrbitalState.EarthRadiusKm + 1, OrbitalState.EarthRadiusKm + OrbitalState.MaximumAltitudeKm - 1),
                           Any.Double().Between(-Math.PI / 2, Math.PI / 2),
                           Any.Double().Between(-Math.PI, Math.PI),
                           (radius, latitude, longitude) => new Vector3(radius * Math.Cos(latitude) * Math.Cos(longitude),
                                                                        radius * Math.Cos(latitude) * Math.Sin(longitude),
                                                                        radius * Math.Sin(latitude)));
    }

    /// <summary>A velocity whose speed is positive and below the ceiling, in any direction.</summary>
    public static IAny<Vector3> VelocityKmPerSecond() {
        return Any.Combine(Any.Double().Between(0.01, OrbitalState.MaximumSpeedKmPerSecond - 0.01),
                           Any.Double().Between(-Math.PI / 2, Math.PI / 2),
                           Any.Double().Between(-Math.PI, Math.PI),
                           (speed, latitude, longitude) => new Vector3(speed * Math.Cos(latitude) * Math.Cos(longitude),
                                                                       speed * Math.Cos(latitude) * Math.Sin(longitude),
                                                                       speed * Math.Sin(latitude)));
    }

    /// <summary>
    ///     A trajectory's worth of states — 2 to 12 of them, 1 to 600 seconds apart — on a circular orbit, with
    ///     <paramref name="anchor" /> falling somewhere inside the span (on a node or between two).
    /// </summary>
    public static IEnumerable<OrbitalState> StatesAround(Epoch anchor, IAny<PositionCovariance> covariances) {
        int         count = Any.Int32().Between(2, 12).Generate();
        List<int>   steps = Any.ListOf(Any.Int32().Between(1, (int)Trajectory.MaximumStep.TotalSeconds)).WithCount(count - 1).Generate();
        int         span  = 0;
        foreach (int step in steps) { span += step; }

        int   offset = Any.Int32().Between(0, span).Generate();
        Epoch epoch  = anchor.Add(TimeSpan.FromSeconds(-offset));

        double radius      = Any.Double().Between(6900, 45_000).Generate();
        double latitude    = Any.Double().Between(-Math.PI / 2, Math.PI / 2).Generate();
        double longitude   = Any.Double().Between(-Math.PI, Math.PI).Generate();
        double heading     = Any.Double().Between(0, 2 * Math.PI).Generate();
        double speed       = Math.Sqrt(GravitationalParameter / radius);
        double angularRate = speed / radius;

        Vector3 east  = new Vector3(-Math.Sin(longitude), Math.Cos(longitude), 0);
        Vector3 north = new Vector3(-Math.Sin(latitude) * Math.Cos(longitude), -Math.Sin(latitude) * Math.Sin(longitude), Math.Cos(latitude));
        Vector3 e1    = new Vector3(Math.Cos(latitude) * Math.Cos(longitude), Math.Cos(latitude) * Math.Sin(longitude), Math.Sin(latitude));
        Vector3 e2    = east.Scale(Math.Cos(heading)).Plus(north.Scale(Math.Sin(heading)));

        List<OrbitalState> states = new List<OrbitalState>();
        for (int index = 0; index < count; index++) {
            double  phase    = angularRate * epoch.SecondsSince(anchor);
            Vector3 position = e1.Scale(radius * Math.Cos(phase)).Plus(e2.Scale(radius * Math.Sin(phase)));
            Vector3 velocity = e1.Scale(-speed * Math.Sin(phase)).Plus(e2.Scale(speed * Math.Cos(phase)));
            states.Add(new OrbitalState(epoch, position, velocity, covariances.Generate()));

            if (index < steps.Count) { epoch = epoch.Add(TimeSpan.FromSeconds(steps[index])); }
        }

        return states;
    }

}
