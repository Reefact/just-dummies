using Serialisation.Domain;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
/// A vaccine as the cold chain sees it: its trade item, the range printed on its label (inside the 2-8 °C chain),
/// its stability budget, and the shelf life a receiving warehouse requires before accepting it.
/// Outside the hard limits (below 0 °C, above 25 °C) the product is lost at once, whatever its budget.
/// </summary>
public sealed class VaccineProduct : IEquatable<VaccineProduct>
{
    public const decimal ColdChainMinCelsius = 2m;
    public const decimal ColdChainMaxCelsius = 8m;
    public const decimal FreezeLimitCelsius = 0m;
    public const decimal HeatLimitCelsius = 25m;
    public const int MaxNameLength = 60;

    public VaccineProduct(Gtin gtin, string name, TemperatureRange labelledRange, StabilityBudget budget, int minimumShelfLifeOnReceiptDays)
    {
        ArgumentNullException.ThrowIfNull(gtin);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(labelledRange);
        ArgumentNullException.ThrowIfNull(budget);
        if (name.Length > MaxNameLength || name != name.Trim())
        {
            throw new ArgumentException($"A product name has at most {MaxNameLength} characters and no surrounding spaces.", nameof(name));
        }

        if (labelledRange.MinCelsius < ColdChainMinCelsius || labelledRange.MaxCelsius > ColdChainMaxCelsius)
        {
            throw new ArgumentException(
                $"A cold-chain vaccine is labelled within {ColdChainMinCelsius}-{ColdChainMaxCelsius} °C, not {labelledRange.MinCelsius}-{labelledRange.MaxCelsius} °C.",
                nameof(labelledRange));
        }

        if (minimumShelfLifeOnReceiptDays is < 0 or > 365)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumShelfLifeOnReceiptDays), minimumShelfLifeOnReceiptDays, "The shelf life required on receipt lies between 0 and 365 days.");
        }

        Gtin = gtin;
        Name = name;
        LabelledRange = labelledRange;
        Budget = budget;
        MinimumShelfLifeOnReceiptDays = minimumShelfLifeOnReceiptDays;
    }

    public Gtin Gtin { get; }

    public string Name { get; }

    public TemperatureRange LabelledRange { get; }

    public StabilityBudget Budget { get; }

    public int MinimumShelfLifeOnReceiptDays { get; }

    /// <summary>Below 0 °C or above 25 °C: the product is destroyed, whatever the budget left.</summary>
    public static bool BreachesHardLimit(decimal celsius) => celsius < FreezeLimitCelsius || celsius > HeatLimitCelsius;

    public bool IsWithinLabel(decimal celsius) => celsius >= LabelledRange.MinCelsius && celsius <= LabelledRange.MaxCelsius;

    /// <summary>Whether a store held at <paramref name="range"/> keeps the product within its label.</summary>
    public bool CanBeStoredAt(TemperatureRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        return range.MinCelsius >= LabelledRange.MinCelsius && range.MaxCelsius <= LabelledRange.MaxCelsius;
    }

    public bool Equals(VaccineProduct? other) => other is not null && other.Gtin.Equals(Gtin);

    public override bool Equals(object? obj) => Equals(obj as VaccineProduct);

    public override int GetHashCode() => Gtin.GetHashCode();

    public override string ToString() => $"{Name} ({Gtin})";
}
