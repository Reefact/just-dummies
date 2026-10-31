using System.Globalization;

namespace GeothermalReservoir.Domain;

/// <summary>Fraction of a rock cell's bulk volume that is pore space, strictly between 0 and 0.5.</summary>
public sealed class Porosity : IEquatable<Porosity>
{
    public const double Maximum = 0.5;

    public Porosity(double fraction)
    {
        if (double.IsNaN(fraction) || fraction <= 0.0 || fraction > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, $"Porosity must be in (0, {Maximum}].");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public bool Equals(Porosity? other) => other is not null && Fraction.Equals(other.Fraction);
    public override bool Equals(object? obj) => Equals(obj as Porosity);
    public override int GetHashCode() => Fraction.GetHashCode();
    public override string ToString() => Fraction.ToString("0.###", CultureInfo.InvariantCulture);
}

/// <summary>Intrinsic rock permeability in square metres, between 1e-20 m² (tight rock) and 1e-9 m² (gravel).</summary>
public sealed class Permeability : IEquatable<Permeability>
{
    public const double MinimumSquareMetres = 1e-20;
    public const double MaximumSquareMetres = 1e-9;

    public Permeability(double squareMetres)
    {
        if (double.IsNaN(squareMetres) || squareMetres < MinimumSquareMetres || squareMetres > MaximumSquareMetres)
        {
            throw new ArgumentOutOfRangeException(nameof(squareMetres), squareMetres,
                $"Permeability must be in [{MinimumSquareMetres}, {MaximumSquareMetres}] m².");
        }

        SquareMetres = squareMetres;
    }

    public double SquareMetres { get; }

    /// <summary>Harmonic mean, the effective permeability across the face shared by two cells.</summary>
    public static double HarmonicMean(Permeability left, Permeability right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return 2.0 * left.SquareMetres * right.SquareMetres / (left.SquareMetres + right.SquareMetres);
    }

    public bool Equals(Permeability? other) => other is not null && SquareMetres.Equals(other.SquareMetres);
    public override bool Equals(object? obj) => Equals(obj as Permeability);
    public override int GetHashCode() => SquareMetres.GetHashCode();
    public override string ToString() => SquareMetres.ToString("0.###E+0", CultureInfo.InvariantCulture) + " m²";
}

/// <summary>Absolute pore pressure in pascals, in (0, 100 MPa].</summary>
public sealed class Pressure : IEquatable<Pressure>, IComparable<Pressure>
{
    public const double MaximumPascals = 100e6;

    public Pressure(double pascals)
    {
        if (double.IsNaN(pascals) || pascals <= 0.0 || pascals > MaximumPascals)
        {
            throw new ArgumentOutOfRangeException(nameof(pascals), pascals, $"Pressure must be in (0, {MaximumPascals}] Pa.");
        }

        Pascals = pascals;
    }

    public double Pascals { get; }
    public double Bars => Pascals / 1e5;

    public static Pressure FromBars(double bars) => new(bars * 1e5);

    public int CompareTo(Pressure? other) => other is null ? 1 : Pascals.CompareTo(other.Pascals);
    public bool Equals(Pressure? other) => other is not null && Pascals.Equals(other.Pascals);
    public override bool Equals(object? obj) => Equals(obj as Pressure);
    public override int GetHashCode() => Pascals.GetHashCode();
    public override string ToString() => Bars.ToString("0.##", CultureInfo.InvariantCulture) + " bar";
}

/// <summary>Temperature in kelvin, restricted to liquid-water reservoirs: (273.15 K, 623.15 K].</summary>
public sealed class Temperature : IEquatable<Temperature>, IComparable<Temperature>
{
    public const double FreezingKelvin = 273.15;
    public const double MaximumKelvin = 623.15;

    public Temperature(double kelvin)
    {
        if (double.IsNaN(kelvin) || kelvin <= FreezingKelvin || kelvin > MaximumKelvin)
        {
            throw new ArgumentOutOfRangeException(nameof(kelvin), kelvin,
                $"Temperature must be in ({FreezingKelvin}, {MaximumKelvin}] K.");
        }

        Kelvin = kelvin;
    }

    public double Kelvin { get; }
    public double Celsius => Kelvin - FreezingKelvin;

    public static Temperature FromCelsius(double celsius) => new(celsius + FreezingKelvin);

    public int CompareTo(Temperature? other) => other is null ? 1 : Kelvin.CompareTo(other.Kelvin);
    public bool Equals(Temperature? other) => other is not null && Kelvin.Equals(other.Kelvin);
    public override bool Equals(object? obj) => Equals(obj as Temperature);
    public override int GetHashCode() => Kelvin.GetHashCode();
    public override string ToString() => Celsius.ToString("0.##", CultureInfo.InvariantCulture) + " °C";
}

/// <summary>Duration of one explicit time step, in (0 s, 1 year].</summary>
public sealed class TimeStep : IEquatable<TimeStep>
{
    public const double MaximumSeconds = 365.25 * 24 * 3600;

    public TimeStep(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0.0 || seconds > MaximumSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, $"A time step must be in (0, {MaximumSeconds}] s.");
        }

        Seconds = seconds;
    }

    public double Seconds { get; }

    public bool Equals(TimeStep? other) => other is not null && Seconds.Equals(other.Seconds);
    public override bool Equals(object? obj) => Equals(obj as TimeStep);
    public override int GetHashCode() => Seconds.GetHashCode();
    public override string ToString() => Seconds.ToString("0.###", CultureInfo.InvariantCulture) + " s";
}
