namespace AgriTech.Domain;

/// <summary>One reading uploaded by a field sensor.</summary>
public sealed class SensorReading {

    public SensorReading(SensorId sensor, GeoCoordinate position, RelativeHumidity humidity, float soilTemperatureCelsius, BatteryVoltage battery, DateTimeOffset takenAt) {
        ArgumentNullException.ThrowIfNull(sensor);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(humidity);
        ArgumentNullException.ThrowIfNull(battery);
        ArgumentOutOfRangeException.ThrowIfLessThan(soilTemperatureCelsius, -40f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(soilTemperatureCelsius, 85f);
        if (takenAt.Offset != TimeSpan.Zero) {
            throw new ArgumentException("Readings are time-stamped in UTC.", nameof(takenAt));
        }
        Sensor                 = sensor;
        Position               = position;
        Humidity               = humidity;
        SoilTemperatureCelsius = soilTemperatureCelsius;
        Battery                = battery;
        TakenAt                = takenAt;
    }

    public SensorId         Sensor                 { get; }
    public GeoCoordinate    Position               { get; }
    public RelativeHumidity Humidity               { get; }
    public float            SoilTemperatureCelsius { get; }
    public BatteryVoltage   Battery                { get; }
    public DateTimeOffset   TakenAt                { get; }

}
