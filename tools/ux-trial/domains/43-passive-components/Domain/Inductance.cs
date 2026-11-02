namespace PassiveComponents.Domain;

/// <summary>An inductance in henries, from 1 nH to 1 H.</summary>
public sealed class Inductance : IEquatable<Inductance>, IComparable<Inductance>
{
    public const double MinHenries = 1e-9;
    public const double MaxHenries = 1;

    public Inductance(double henries)
    {
        if (!double.IsFinite(henries) || henries < MinHenries || henries > MaxHenries)
        {
            throw new ArgumentOutOfRangeException(nameof(henries), henries, "An inductance lies in [1 nH, 1 H].");
        }

        Henries = henries;
    }

    public double Henries { get; }

    public static Inductance FromNanohenries(double nanohenries) => new(nanohenries / 1e9);

    public static Inductance FromMicrohenries(double microhenries) => new(microhenries / 1e6);

    public static Inductance FromMillihenries(double millihenries) => new(millihenries / 1e3);

    /// <summary>Uncoupled inductors in a chain add up.</summary>
    public static Inductance InSeries(params Inductance[] inductors)
    {
        RequireCombination(inductors);
        return Combined(inductors.Sum(l => l.Henries), nameof(inductors));
    }

    /// <summary>Uncoupled inductors side by side combine by their reciprocals.</summary>
    public static Inductance InParallel(params Inductance[] inductors)
    {
        RequireCombination(inductors);
        return Combined(1.0 / inductors.Sum(l => 1.0 / l.Henries), nameof(inductors));
    }

    /// <summary>Resonant frequency of an LC tank, 1 / (2π√(LC)), in hertz: up to about 16 GHz at 1 nH and 0.1 pF.</summary>
    public double ResonantFrequencyWith(Capacitance capacitance)
    {
        ArgumentNullException.ThrowIfNull(capacitance);
        return 1.0 / (2 * Math.PI * Math.Sqrt(Henries * capacitance.Farads));
    }

    /// <summary>Reactance magnitude 2πfL, in ohms; the frequency is finite and positive.</summary>
    public double ReactanceAt(double frequencyHertz)
    {
        if (!double.IsFinite(frequencyHertz) || frequencyHertz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyHertz), frequencyHertz, "A frequency is finite and positive.");
        }

        return 2 * Math.PI * frequencyHertz * Henries;
    }

    public bool IsCloseTo(Inductance other, double relativeTolerance = Magnitude.DefaultRelativeTolerance)
    {
        ArgumentNullException.ThrowIfNull(other);
        Magnitude.RequireRelativeTolerance(relativeTolerance, nameof(relativeTolerance));
        return Magnitude.AreClose(Henries, other.Henries, relativeTolerance);
    }

    public int CompareTo(Inductance? other) => other is null ? 1 : Henries.CompareTo(other.Henries);

    public bool Equals(Inductance? other) => other is not null && Henries.Equals(other.Henries);

    public override bool Equals(object? obj) => Equals(obj as Inductance);

    public override int GetHashCode() => Henries.GetHashCode();

    public override string ToString() => Magnitude.Format(Henries, "H");

    private static void RequireCombination(Inductance[] inductors)
    {
        ArgumentNullException.ThrowIfNull(inductors);
        if (inductors.Length < 2)
        {
            throw new ArgumentException("A combination takes at least two inductors.", nameof(inductors));
        }

        if (inductors.Any(l => l is null))
        {
            throw new ArgumentException("A combination holds no null inductor.", nameof(inductors));
        }
    }

    private static Inductance Combined(double henries, string parameterName)
    {
        if (henries < MinHenries || henries > MaxHenries)
        {
            throw new ArgumentOutOfRangeException(parameterName, henries, "The combined inductance falls outside [1 nH, 1 H].");
        }

        return new Inductance(henries);
    }
}
