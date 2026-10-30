namespace AgriTech.Domain;

/// <summary>Decides whether a field needs water from a batch of humidity readings.</summary>
public sealed class IrrigationPolicy {

    public IrrigationPolicy(double dryThresholdPercent, int minimumReadings) {
        ArgumentOutOfRangeException.ThrowIfNegative(dryThresholdPercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dryThresholdPercent, 100.0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumReadings);
        DryThresholdPercent = dryThresholdPercent;
        MinimumReadings     = minimumReadings;
    }

    public double DryThresholdPercent { get; }
    public int    MinimumReadings     { get; }

    /// <summary>Irrigate when there are enough readings and their average humidity is below the threshold.</summary>
    public bool ShouldIrrigate(ReadingBatch batch) =>
        batch.Readings.Count >= MinimumReadings && batch.AverageHumidity() < DryThresholdPercent;

}
