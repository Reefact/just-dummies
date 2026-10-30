namespace Warehouse.Domain;

/// <summary>The storage temperature a pallet requires, in degrees Celsius.</summary>
public sealed class TemperatureRange {

    public TemperatureRange(decimal minCelsius, decimal maxCelsius) {
        if (minCelsius < -40m || maxCelsius > 40m) {
            throw new ArgumentOutOfRangeException(nameof(minCelsius), "Storage temperatures lie between -40 and 40 degrees.");
        }
        if (minCelsius >= maxCelsius) {
            throw new ArgumentException($"The minimum {minCelsius} must be below the maximum {maxCelsius}.");
        }

        MinCelsius = minCelsius;
        MaxCelsius = maxCelsius;
    }

    public decimal MinCelsius { get; }
    public decimal MaxCelsius { get; }

    public StorageZone Zone => MaxCelsius switch {
        <= -18m => StorageZone.Frozen,
        <= 8m   => StorageZone.Chilled,
        _       => StorageZone.Ambient
    };

}
