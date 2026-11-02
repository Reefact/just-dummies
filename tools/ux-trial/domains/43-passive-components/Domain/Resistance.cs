namespace PassiveComponents.Domain;

/// <summary>A resistance in ohms, from 1 mΩ (current-sense shunts) to 1 TΩ (electrometer inputs).</summary>
public sealed class Resistance : IEquatable<Resistance>, IComparable<Resistance>
{
    public const double MinOhms = 1e-3;
    public const double MaxOhms = 1e12;

    public Resistance(double ohms)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ohms, MinOhms);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ohms, MaxOhms);
        if (double.IsNaN(ohms))
        {
            throw new ArgumentOutOfRangeException(nameof(ohms), ohms, "A resistance is a number.");
        }

        Ohms = ohms;
    }

    public double Ohms { get; }

    public static Resistance FromKiloohms(double kiloohms) => new(kiloohms * 1e3);

    public static Resistance FromMegaohms(double megaohms) => new(megaohms * 1e6);

    public static Resistance InSeries(params Resistance[] resistors)
    {
        RequireCombination(resistors);
        return Combined(resistors.Sum(r => r.Ohms), nameof(resistors));
    }

    public static Resistance InParallel(params Resistance[] resistors)
    {
        RequireCombination(resistors);
        return Combined(1.0 / resistors.Sum(r => 1.0 / r.Ohms), nameof(resistors));
    }

    /// <summary>The RC time constant τ = RC, in seconds: from about 1e-16 s to hours.</summary>
    public double TimeConstantWith(Capacitance capacitance)
    {
        ArgumentNullException.ThrowIfNull(capacitance);
        return Ohms * capacitance.Farads;
    }

    /// <summary>The -3 dB corner of an RC low-pass filter, 1 / (2πRC), in hertz.</summary>
    public double CutoffFrequencyWith(Capacitance capacitance) => 1.0 / (2 * Math.PI * TimeConstantWith(capacitance));

    public bool IsCloseTo(Resistance other, double relativeTolerance = Magnitude.DefaultRelativeTolerance)
    {
        ArgumentNullException.ThrowIfNull(other);
        Magnitude.RequireRelativeTolerance(relativeTolerance, nameof(relativeTolerance));
        return Magnitude.AreClose(Ohms, other.Ohms, relativeTolerance);
    }

    public int CompareTo(Resistance? other) => other is null ? 1 : Ohms.CompareTo(other.Ohms);

    public bool Equals(Resistance? other) => other is not null && Ohms.Equals(other.Ohms);

    public override bool Equals(object? obj) => Equals(obj as Resistance);

    public override int GetHashCode() => Ohms.GetHashCode();

    public override string ToString() => Magnitude.Format(Ohms, "Ω");

    private static void RequireCombination(Resistance[] resistors)
    {
        ArgumentNullException.ThrowIfNull(resistors);
        if (resistors.Length < 2)
        {
            throw new ArgumentException("A combination takes at least two resistors.", nameof(resistors));
        }

        if (resistors.Any(r => r is null))
        {
            throw new ArgumentException("A combination holds no null resistor.", nameof(resistors));
        }
    }

    private static Resistance Combined(double ohms, string parameterName)
    {
        if (ohms < MinOhms || ohms > MaxOhms)
        {
            throw new ArgumentOutOfRangeException(parameterName, ohms, "The combined resistance falls outside [1 mΩ, 1 TΩ].");
        }

        return new Resistance(ohms);
    }
}
