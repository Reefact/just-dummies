namespace PassiveComponents.Domain;

/// <summary>A capacitance in farads, from 0.1 pF to 10 mF.</summary>
public sealed class Capacitance : IEquatable<Capacitance>, IComparable<Capacitance>
{
    public const double MinFarads = 1e-13;
    public const double MaxFarads = 1e-2;

    public Capacitance(double farads)
    {
        if (!double.IsFinite(farads) || farads < MinFarads || farads > MaxFarads)
        {
            throw new ArgumentOutOfRangeException(nameof(farads), farads, "A capacitance lies in [0.1 pF, 10 mF].");
        }

        Farads = farads;
    }

    public double Farads { get; }

    public static Capacitance FromPicofarads(double picofarads) => new(picofarads / 1e12);

    public static Capacitance FromNanofarads(double nanofarads) => new(nanofarads / 1e9);

    public static Capacitance FromMicrofarads(double microfarads) => new(microfarads / 1e6);

    /// <summary>Capacitors side by side add up.</summary>
    public static Capacitance InParallel(params Capacitance[] capacitors)
    {
        RequireCombination(capacitors);
        return Combined(capacitors.Sum(c => c.Farads), nameof(capacitors));
    }

    /// <summary>Capacitors in a chain combine by their reciprocals: always less than the smallest.</summary>
    public static Capacitance InSeries(params Capacitance[] capacitors)
    {
        RequireCombination(capacitors);
        return Combined(1.0 / capacitors.Sum(c => 1.0 / c.Farads), nameof(capacitors));
    }

    /// <summary>Reactance magnitude 1 / (2πfC), in ohms; the frequency is finite and positive.</summary>
    public double ReactanceAt(double frequencyHertz)
    {
        if (!double.IsFinite(frequencyHertz) || frequencyHertz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyHertz), frequencyHertz, "A frequency is finite and positive.");
        }

        return 1.0 / (2 * Math.PI * frequencyHertz * Farads);
    }

    /// <summary>Equal within a relative tolerance: 1e-9 by default, so 2.2 nF read two ways still matches.</summary>
    public bool IsCloseTo(Capacitance other, double relativeTolerance = Magnitude.DefaultRelativeTolerance)
    {
        ArgumentNullException.ThrowIfNull(other);
        Magnitude.RequireRelativeTolerance(relativeTolerance, nameof(relativeTolerance));
        return Magnitude.AreClose(Farads, other.Farads, relativeTolerance);
    }

    public int CompareTo(Capacitance? other) => other is null ? 1 : Farads.CompareTo(other.Farads);

    public bool Equals(Capacitance? other) => other is not null && Farads.Equals(other.Farads);

    public override bool Equals(object? obj) => Equals(obj as Capacitance);

    public override int GetHashCode() => Farads.GetHashCode();

    public override string ToString() => Magnitude.Format(Farads, "F");

    private static void RequireCombination(Capacitance[] capacitors)
    {
        ArgumentNullException.ThrowIfNull(capacitors);
        if (capacitors.Length < 2)
        {
            throw new ArgumentException("A combination takes at least two capacitors.", nameof(capacitors));
        }

        if (capacitors.Any(c => c is null))
        {
            throw new ArgumentException("A combination holds no null capacitor.", nameof(capacitors));
        }
    }

    private static Capacitance Combined(double farads, string parameterName)
    {
        if (farads < MinFarads || farads > MaxFarads)
        {
            throw new ArgumentOutOfRangeException(parameterName, farads, "The combined capacitance falls outside [0.1 pF, 10 mF].");
        }

        return new Capacitance(farads);
    }
}
