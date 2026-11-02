using System.Globalization;

namespace PassiveComponents.Domain;

/// <summary>
/// Arithmetic on magnitudes spanning twenty-five decades: comparisons are relative, never absolute,
/// and powers of ten are built by parsing so that 2.2e-9 is the closest double to 2.2 × 10⁻⁹.
/// </summary>
internal static class Magnitude
{
    public const double DefaultRelativeTolerance = 1e-9;

    private static readonly (double Scale, string Prefix)[] Prefixes =
    [
        (1e12, "T"), (1e9, "G"), (1e6, "M"), (1e3, "k"), (1, ""),
        (1e-3, "m"), (1e-6, "µ"), (1e-9, "n"), (1e-12, "p"), (1e-15, "f"),
    ];

    public static void RequireRelativeTolerance(double relativeTolerance, string parameterName)
    {
        if (!(relativeTolerance > 0 && relativeTolerance < 1))
        {
            throw new ArgumentOutOfRangeException(parameterName, relativeTolerance, "A relative tolerance lies in (0, 1).");
        }
    }

    public static bool AreClose(double a, double b, double relativeTolerance = DefaultRelativeTolerance) =>
        Math.Abs(a - b) <= relativeTolerance * Math.Max(Math.Abs(a), Math.Abs(b));

    /// <summary>a ≤ b, allowing a relative slack: both are positive physical quantities.</summary>
    public static bool IsAtMost(double a, double b) => a <= b || AreClose(a, b);

    /// <summary>a ≥ b, allowing a relative slack.</summary>
    public static bool IsAtLeast(double a, double b) => a >= b || AreClose(a, b);

    /// <summary>The closest double to <paramref name="digits"/> × 10^<paramref name="exponent"/>.</summary>
    public static double Scientific(string digits, int exponent) =>
        double.Parse($"{digits}E{exponent}", NumberStyles.Float, CultureInfo.InvariantCulture);

    public static double PowerOfTen(int exponent) => Scientific("1", exponent);

    /// <summary>The decade of a positive value: the exponent e with 1 ≤ value / 10^e &lt; 10.</summary>
    public static int DecadeOf(double value)
    {
        int decade = (int)Math.Floor(Math.Log10(value));
        double mantissa = value / PowerOfTen(decade);
        if (mantissa >= 10)
        {
            decade++;
        }
        else if (mantissa < 1)
        {
            decade--;
        }

        return decade;
    }

    public static string Format(double value, string unit)
    {
        foreach ((double scale, string prefix) in Prefixes)
        {
            if (Math.Abs(value) >= scale * (1 - DefaultRelativeTolerance))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{value / scale:0.###} {prefix}{unit}");
            }
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value:0.###e0} {unit}");
    }
}
