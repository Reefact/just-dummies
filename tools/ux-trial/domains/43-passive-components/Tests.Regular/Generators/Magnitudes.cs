using System.Globalization;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     Values that span decades: a plain <c>Between(1e-13, 1e-2)</c> is uniform in value, so nine draws in ten
///     would land in the last decade. These draw the decade first, then a mantissa, and build the double by
///     parsing so that a power of ten is the closest double to itself.
/// </summary>
public static class Magnitudes {

    /// <summary>A value in [10^minExponent, 10^maxExponent), every decade equally likely.</summary>
    public static IAny<double> AcrossDecades(int minExponent, int maxExponent) {
        return Any.Combine(
            Any.Int32().Between(minExponent, maxExponent - 1),
            Any.Int32().Between(100, 999),
            (exponent, digits) => double.Parse($"{digits}E{exponent - 2}", NumberStyles.Float, CultureInfo.InvariantCulture));
    }

}
