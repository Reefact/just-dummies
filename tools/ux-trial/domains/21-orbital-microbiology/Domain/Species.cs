using System.Text.RegularExpressions;

namespace OrbitalMicrobiology.Domain;

/// <summary>
/// A microbial species and its growth traits: maximum rate, cardinal temperatures
/// (minimum &lt; optimum &lt; maximum), lag before growth, and biomass yield on the nutrient.
/// Two species are the same species when their binomial names match.
/// </summary>
public sealed partial class Species : IEquatable<Species>
{
    public const double MinYieldCellsPerGram = 1e9;
    public const double MaxYieldCellsPerGram = 1e13;
    public const double MaxLagHours = 240;

    public Species(
        string binomialName,
        GrowthRate maxGrowthRate,
        double minTemperatureCelsius,
        double optimalTemperatureCelsius,
        double maxTemperatureCelsius,
        double lagHours,
        double yieldCellsPerGram)
    {
        ArgumentNullException.ThrowIfNull(binomialName);
        ArgumentNullException.ThrowIfNull(maxGrowthRate);
        if (!BinomialFormat().IsMatch(binomialName))
        {
            throw new ArgumentException($"A species name is binomial, 'Genus species', got '{binomialName}'.", nameof(binomialName));
        }

        if (maxGrowthRate.PerHour <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGrowthRate), maxGrowthRate, "A species grows at some positive maximum rate.");
        }

        ThrowIfOutside(minTemperatureCelsius, -20, 80, nameof(minTemperatureCelsius));
        ThrowIfOutside(optimalTemperatureCelsius, -20, 80, nameof(optimalTemperatureCelsius));
        ThrowIfOutside(maxTemperatureCelsius, -20, 80, nameof(maxTemperatureCelsius));
        if (!(minTemperatureCelsius < optimalTemperatureCelsius && optimalTemperatureCelsius < maxTemperatureCelsius))
        {
            throw new ArgumentException(
                $"Cardinal temperatures must satisfy minimum ({minTemperatureCelsius}) < optimum ({optimalTemperatureCelsius}) < maximum ({maxTemperatureCelsius}).",
                nameof(optimalTemperatureCelsius));
        }

        ThrowIfOutside(lagHours, 0, MaxLagHours, nameof(lagHours));
        ThrowIfOutside(yieldCellsPerGram, MinYieldCellsPerGram, MaxYieldCellsPerGram, nameof(yieldCellsPerGram));

        BinomialName = binomialName;
        MaxGrowthRate = maxGrowthRate;
        MinTemperatureCelsius = minTemperatureCelsius;
        OptimalTemperatureCelsius = optimalTemperatureCelsius;
        MaxTemperatureCelsius = maxTemperatureCelsius;
        LagHours = lagHours;
        YieldCellsPerGram = yieldCellsPerGram;
    }

    public string BinomialName { get; }

    public GrowthRate MaxGrowthRate { get; }

    public double MinTemperatureCelsius { get; }

    public double OptimalTemperatureCelsius { get; }

    public double MaxTemperatureCelsius { get; }

    public double LagHours { get; }

    public double YieldCellsPerGram { get; }

    /// <summary>
    /// The fraction of the maximum rate reached at a temperature: 0 outside the cardinal range,
    /// rising linearly to 1 at the optimum and falling linearly back to 0 at the maximum.
    /// </summary>
    public double TemperatureFactor(double temperatureCelsius)
    {
        if (temperatureCelsius <= MinTemperatureCelsius || temperatureCelsius >= MaxTemperatureCelsius)
        {
            return 0;
        }

        return temperatureCelsius <= OptimalTemperatureCelsius
            ? (temperatureCelsius - MinTemperatureCelsius) / (OptimalTemperatureCelsius - MinTemperatureCelsius)
            : (MaxTemperatureCelsius - temperatureCelsius) / (MaxTemperatureCelsius - OptimalTemperatureCelsius);
    }

    public bool Equals(Species? other) => other is not null && BinomialName == other.BinomialName;

    public override bool Equals(object? obj) => Equals(obj as Species);

    public override int GetHashCode() => BinomialName.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => BinomialName;

    private static void ThrowIfOutside(double value, double min, double max, string name)
    {
        if (!double.IsFinite(value) || value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, value, $"Expected a value in [{min}, {max}].");
        }
    }

    [GeneratedRegex(@"^[A-Z][a-z]+ [a-z]+$")]
    private static partial Regex BinomialFormat();
}
