using System.Globalization;

namespace PassiveComponents.Domain;

/// <summary>
/// The magnitude of a part's temperature coefficient, in ppm/K about 25 °C: ±25 for a thin-film resistor,
/// ±30 for a C0G capacitor, a few hundred for thick film or ferrite.
/// </summary>
public sealed class TemperatureCoefficient : IEquatable<TemperatureCoefficient>
{
    public const double MaxPpmPerKelvin = 5000;
    public const double ReferenceCelsius = 25;
    public const double MinOperatingCelsius = -55;
    public const double MaxOperatingCelsius = 155;

    public TemperatureCoefficient(double ppmPerKelvin)
    {
        if (!double.IsFinite(ppmPerKelvin))
        {
            throw new ArgumentOutOfRangeException(nameof(ppmPerKelvin), ppmPerKelvin, "A temperature coefficient is finite.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(ppmPerKelvin);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ppmPerKelvin, MaxPpmPerKelvin);
        PpmPerKelvin = ppmPerKelvin;
    }

    public double PpmPerKelvin { get; }

    /// <summary>Worst-case relative drift away from 25 °C, for a temperature in [-55, 155] °C.</summary>
    public double DriftAt(double temperatureCelsius)
    {
        if (double.IsNaN(temperatureCelsius) || temperatureCelsius < MinOperatingCelsius || temperatureCelsius > MaxOperatingCelsius)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureCelsius), temperatureCelsius, "Operating temperatures lie in [-55, 155] °C.");
        }

        return PpmPerKelvin * 1e-6 * Math.Abs(temperatureCelsius - ReferenceCelsius);
    }

    public bool Equals(TemperatureCoefficient? other) => other is not null && PpmPerKelvin.Equals(other.PpmPerKelvin);

    public override bool Equals(object? obj) => Equals(obj as TemperatureCoefficient);

    public override int GetHashCode() => PpmPerKelvin.GetHashCode();

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"±{PpmPerKelvin:0.#} ppm/K");
}
