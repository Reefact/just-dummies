using System.Globalization;

namespace PassiveComponents.Domain;

/// <summary>A symmetric manufacturing tolerance, ±0.1 % to ±20 % of the nominal value.</summary>
public sealed class Tolerance : IEquatable<Tolerance>, IComparable<Tolerance>
{
    public const double MinPercent = 0.1;
    public const double MaxPercent = 20;

    public Tolerance(double percent)
    {
        if (double.IsNaN(percent) || percent < MinPercent || percent > MaxPercent)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "A tolerance lies in [0.1 %, 20 %].");
        }

        Percent = percent;
    }

    public double Percent { get; }

    public double Fraction => Percent / 100;

    /// <summary>The tolerance a series is built for: its neighbouring values just overlap at these bands.</summary>
    public static Tolerance ForSeries(ESeries series) => series switch
    {
        ESeries.E6 => new Tolerance(20),
        ESeries.E12 => new Tolerance(10),
        ESeries.E24 => new Tolerance(5),
        ESeries.E96 => new Tolerance(1),
        _ => throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown E-series."),
    };

    public double LowerBound(double nominal) => nominal * (1 - Fraction);

    public double UpperBound(double nominal) => nominal * (1 + Fraction);

    public bool IsAtLeastAsTightAs(Tolerance other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Magnitude.IsAtMost(Percent, other.Percent);
    }

    public int CompareTo(Tolerance? other) => other is null ? 1 : Percent.CompareTo(other.Percent);

    public bool Equals(Tolerance? other) => other is not null && Percent.Equals(other.Percent);

    public override bool Equals(object? obj) => Equals(obj as Tolerance);

    public override int GetHashCode() => Percent.GetHashCode();

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"±{Percent:0.###} %");
}
