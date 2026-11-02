using JustDummies;

namespace AgriTech.Domain.Tests;

/// <summary>Builders shared by the tests: a reading or a batch whose only meaningful fact is the one the test names.</summary>
internal static class TestData {

    public static SensorReading ReadingWithHumidity(double percent) {
        return Any.SensorReading().WithHumidity(new RelativeHumidity(percent)).Generate();
    }

    public static SensorReading ReadingAt(double latitude, double longitude) {
        return Any.SensorReading().WithPosition(new GeoCoordinate(latitude, longitude)).Generate();
    }

    public static SensorReading ReadingFrom(string sensorId, Half volts) {
        return Any.SensorReading()
                  .WithSensor(new SensorId(sensorId))
                  .WithBattery(new BatteryVoltage(volts))
                  .Generate();
    }

    public static ReadingBatch BatchWithHumidities(params double[] percents) {
        List<SensorReading> readings = percents.Select(ReadingWithHumidity).ToList();

        return Any.ReadingBatch().WithReadings(readings).Generate();
    }

}
