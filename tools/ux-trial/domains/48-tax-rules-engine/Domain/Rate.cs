using System.Globalization;

namespace TaxRules.Domain;

/// <summary>A tax rate as a fraction in [0, 1], to a hundredth of a percent (four decimals).</summary>
public sealed class Rate : IEquatable<Rate>, IComparable<Rate>
{
    public const int MaxDecimals = 4;

    public Rate(decimal fraction)
    {
        if (fraction < 0m || fraction > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "A rate is a fraction between 0 and 1.");
        }

        if (decimal.Round(fraction, MaxDecimals) != fraction)
        {
            throw new ArgumentException($"A rate is given to a hundredth of a percent; got {fraction}.", nameof(fraction));
        }

        Fraction = fraction;
    }

    public static Rate Zero { get; } = new(0m);

    public static Rate FromPercent(decimal percent) => new(percent / 100m);

    public decimal Fraction { get; }

    public decimal Percent => Fraction * 100m;

    public bool IsZero => Fraction == 0m;

    public int CompareTo(Rate? other) => other is null ? 1 : Fraction.CompareTo(other.Fraction);

    public bool Equals(Rate? other) => other is not null && other.Fraction == Fraction;

    public override bool Equals(object? obj) => Equals(obj as Rate);

    public override int GetHashCode() => Fraction.GetHashCode();

    public override string ToString() => Percent.ToString("0.##", CultureInfo.InvariantCulture) + " %";
}
