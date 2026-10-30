namespace AgriTech.Domain;

/// <summary>A batch uploaded by a gateway: between one and <see cref="MaximumReadings"/> readings.</summary>
public sealed class ReadingBatch {

    public const int MaximumReadings = 500_000;

    private readonly SensorReading[] _readings;

    public ReadingBatch(SensorId gateway, IReadOnlyList<SensorReading> readings) {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(readings);
        if (readings.Count == 0) {
            throw new ArgumentException("A batch carries at least one reading.", nameof(readings));
        }
        if (readings.Count > MaximumReadings) {
            throw new ArgumentException($"A batch carries at most {MaximumReadings} readings.", nameof(readings));
        }
        Gateway   = gateway;
        _readings = readings.ToArray();
    }

    public SensorId Gateway { get; }

    public IReadOnlyList<SensorReading> Readings => _readings;

    public double AverageHumidity() => _readings.Average(reading => reading.Humidity.Percent);

    public int CountDrierThan(double thresholdPercent) => _readings.Count(reading => reading.Humidity.IsBelow(thresholdPercent));

    public IReadOnlyList<SensorReading> OutsideOf(Geofence fence) => _readings.Where(reading => !fence.Contains(reading.Position)).ToArray();

    public IReadOnlyCollection<SensorId> SensorsWithLowBattery() => _readings.Where(reading => reading.Battery.IsLow).Select(reading => reading.Sensor).ToHashSet();

}
