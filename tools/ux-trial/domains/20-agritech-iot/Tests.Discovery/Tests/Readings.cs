using JustDummies;

namespace AgriTech.Domain.Tests;

/// <summary>Shortcuts for the readings whose one significant property a test pins; everything else stays drawn.</summary>
internal static class Readings {

    public static SensorReading WithHumidity(double percent) {
        return Any.SensorReading().WithHumidity(new RelativeHumidity(percent)).Generate();
    }

    public static SensorReading At(double latitude, double longitude) {
        return Any.SensorReading().WithPosition(new GeoCoordinate(latitude, longitude)).Generate();
    }

    public static SensorReading FromSensor(string sensorId, Half volts) {
        return Any.SensorReading().WithSensor(new SensorId(sensorId)).WithBattery(new BatteryVoltage(volts)).Generate();
    }

    public static ReadingBatch BatchOf(params SensorReading[] readings) {
        return Any.ReadingBatch().WithReadings(readings).Generate();
    }

}
