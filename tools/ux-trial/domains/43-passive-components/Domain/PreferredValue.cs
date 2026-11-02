using System.Globalization;

namespace PassiveComponents.Domain;

/// <summary>
/// One value of an E-series: the <c>index</c>-th mantissa of the series times 10^<c>decade</c>
/// (E24, decade -9, index 8 = 2.2 nF or 2.2 nH). Decades run from -13 (0.1 pF) to 12 (1 TΩ).
/// Snapping to the nearest preferred value works on the logarithmic grid the series is built on.
/// </summary>
public sealed class PreferredValue : IEquatable<PreferredValue>, IComparable<PreferredValue>
{
    public const int MinDecade = -13;
    public const int MaxDecade = 12;

    private static readonly int[] E6 = [10, 15, 22, 33, 47, 68];

    private static readonly int[] E12 = [10, 12, 15, 18, 22, 27, 33, 39, 47, 56, 68, 82];

    private static readonly int[] E24 =
        [10, 11, 12, 13, 15, 16, 18, 20, 22, 24, 27, 30, 33, 36, 39, 43, 47, 51, 56, 62, 68, 75, 82, 91];

    private static readonly int[] E96 =
    [
        100, 102, 105, 107, 110, 113, 115, 118, 121, 124, 127, 130, 133, 137, 140, 143,
        147, 150, 154, 158, 162, 165, 169, 174, 178, 182, 187, 191, 196, 200, 205, 210,
        215, 221, 226, 232, 237, 243, 249, 255, 261, 267, 274, 280, 287, 294, 301, 309,
        316, 324, 332, 340, 348, 357, 365, 374, 383, 392, 402, 412, 422, 432, 442, 453,
        464, 475, 487, 499, 511, 523, 536, 549, 562, 576, 590, 604, 619, 634, 649, 665,
        681, 698, 715, 732, 750, 768, 787, 806, 825, 845, 866, 887, 909, 931, 953, 976,
    ];

    public PreferredValue(ESeries series, int decade, int index)
    {
        int[] mantissas = DigitsOf(series);
        if (decade < MinDecade || decade > MaxDecade)
        {
            throw new ArgumentOutOfRangeException(nameof(decade), decade, $"A decade lies in [{MinDecade}, {MaxDecade}].");
        }

        if (index < 0 || index >= mantissas.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"{series} has {mantissas.Length} values per decade: index in [0, {mantissas.Length - 1}].");
        }

        Series = series;
        Decade = decade;
        Index = index;
        Mantissa = mantissas[index] / Divisor(series);
        Value = Magnitude.Scientific(mantissas[index].ToString(CultureInfo.InvariantCulture), decade - SignificantDigits(series) + 1);
    }

    public ESeries Series { get; }

    public int Decade { get; }

    public int Index { get; }

    /// <summary>The mantissa in [1, 10): 2.2, 4.7, 4.99…</summary>
    public double Mantissa { get; }

    /// <summary>The value in SI base units, the closest double to mantissa × 10^decade.</summary>
    public double Value { get; }

    public static int ValuesPerDecade(ESeries series) => DigitsOf(series).Length;

    public static IReadOnlyList<double> Mantissas(ESeries series)
    {
        int[] digits = DigitsOf(series);
        double divisor = Divisor(series);
        return Array.AsReadOnly(digits.Select(d => d / divisor).ToArray());
    }

    /// <summary>
    /// The preferred value nearest to <paramref name="value"/> on a logarithmic scale (a tie goes to the lower one).
    /// The value is finite and positive, and its nearest neighbour must fall within decades -13..12.
    /// </summary>
    public static PreferredValue Nearest(double value, ESeries series)
    {
        RequirePositive(value);
        (int decade, int index) = Locate(value, series);
        if (decade < MinDecade || decade > MaxDecade)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The nearest preferred value lies outside 1e-13..9.76e12.");
        }

        return new PreferredValue(series, decade, index);
    }

    /// <summary>True when the value sits on the series' grid, within a relative 1e-9.</summary>
    public static bool IsPreferred(double value, ESeries series)
    {
        RequirePositive(value);
        (int decade, int index) = Locate(value, series);
        if (decade < MinDecade || decade > MaxDecade)
        {
            return false;
        }

        return Magnitude.AreClose(new PreferredValue(series, decade, index).Value, value);
    }

    /// <summary>The next value up the series, crossing into the next decade after the last mantissa.</summary>
    public PreferredValue Next()
    {
        if (Index + 1 < ValuesPerDecade(Series))
        {
            return new PreferredValue(Series, Decade, Index + 1);
        }

        if (Decade == MaxDecade)
        {
            throw new InvalidOperationException("No preferred value above the top decade.");
        }

        return new PreferredValue(Series, Decade + 1, 0);
    }

    public PreferredValue Previous()
    {
        if (Index > 0)
        {
            return new PreferredValue(Series, Decade, Index - 1);
        }

        if (Decade == MinDecade)
        {
            throw new InvalidOperationException("No preferred value below the bottom decade.");
        }

        return new PreferredValue(Series, Decade - 1, ValuesPerDecade(Series) - 1);
    }

    public int CompareTo(PreferredValue? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(PreferredValue? other) =>
        other is not null && other.Series == Series && other.Decade == Decade && other.Index == Index;

    public override bool Equals(object? obj) => Equals(obj as PreferredValue);

    public override int GetHashCode() => HashCode.Combine(Series, Decade, Index);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Series} {Mantissa}e{Decade}");

    private static void RequirePositive(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Only a finite, positive value has a preferred neighbour.");
        }
    }

    private static (int Decade, int Index) Locate(double value, ESeries series)
    {
        int[] digits = DigitsOf(series);
        double divisor = Divisor(series);
        int decade = Magnitude.DecadeOf(value);
        double logMantissa = Math.Log10(value) - decade;
        int best = 0;
        double bestDistance = double.MaxValue;
        for (int i = 0; i <= digits.Length; i++)
        {
            double candidate = i < digits.Length ? digits[i] / divisor : 10;
            double distance = Math.Abs(logMantissa - Math.Log10(candidate));
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best == digits.Length ? (decade + 1, 0) : (decade, best);
    }

    private static int SignificantDigits(ESeries series) => series == ESeries.E96 ? 3 : 2;

    private static double Divisor(ESeries series) => series == ESeries.E96 ? 100.0 : 10.0;

    private static int[] DigitsOf(ESeries series) => series switch
    {
        ESeries.E6 => E6,
        ESeries.E12 => E12,
        ESeries.E24 => E24,
        ESeries.E96 => E96,
        _ => throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown E-series."),
    };
}
