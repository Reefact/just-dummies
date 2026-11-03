using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>Test-side helper shared by the generators that need a vector of a given length (a position, a velocity).</summary>
internal static class OrbitalGeometry {

    /// <summary>A vector pointing anywhere whose length is drawn from <paramref name="length" />.</summary>
    internal static IAny<Vector3> VectorOfLength(IAny<double> length) {
        // x is never zero, so the direction is never the zero vector.
        return Any.Combine(
            Any.Double().NonZero().Between(-1d, 1d),
            Any.Double().Between(-1d, 1d),
            Any.Double().Between(-1d, 1d),
            length,
            (x, y, z, size) => {
                double norm = Math.Sqrt(x * x + y * y + z * z);

                return new Vector3(x / norm * size, y / norm * size, z / norm * size);
            });
    }

}
