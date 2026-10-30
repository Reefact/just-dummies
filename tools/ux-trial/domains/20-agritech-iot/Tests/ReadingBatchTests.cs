using AgriTech.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace AgriTech.Tests;

[Reproducible]
public sealed class ReadingBatchTests {

    private static readonly Geofence NorthField = new Geofence(new GeoCoordinate(48.80, 2.20), new GeoCoordinate(48.90, 2.40));

    private static IAny<SensorReading> AnyReadingWithHumidity(double percent) =>
        new AnySensorReading().WithHumidity(new RelativeHumidity(percent));

    [Fact]
    public void Average_humidity_is_the_mean_of_the_readings() {
        // Arrange
        SensorId anyGateway = new AnySensorId().Generate();
        List<SensorReading> readings = [
            AnyReadingWithHumidity(20.0).Generate(),
            AnyReadingWithHumidity(40.0).Generate(),
            AnyReadingWithHumidity(60.0).Generate()
        ];
        ReadingBatch batch = new ReadingBatch(anyGateway, readings);

        // Act
        double average = batch.AverageHumidity();

        // Assert
        Assert.Equal(40.0, average, precision: 10);
    }

    [Fact]
    public void Counts_only_the_readings_drier_than_the_threshold() {
        // Arrange: which side of 30 % each reading falls on matters; its exact value does not.
        IAny<SensorReading> anyDryReading = new AnySensorReading().WithHumidity(h => h.WithPercent(p => p.LessThan(30.0)));
        IAny<SensorReading> anyWetReading = new AnySensorReading().WithHumidity(h => h.WithPercent(p => p.GreaterThanOrEqualTo(30.0)));
        List<SensorReading> readings = Any.ListOf(anyDryReading).WithCount(3).Generate();
        readings.AddRange(Any.ListOf(anyWetReading).WithCount(2).Generate());
        ReadingBatch batch = new AnyReadingBatch().WithReadings(readings).Generate();

        // Act
        int dry = batch.CountDrierThan(30.0);

        // Assert
        Assert.Equal(3, dry);
    }

    [Fact]
    public void Lists_the_readings_taken_outside_the_geofence() {
        // Arrange
        AnySensorReading anyReading = new AnySensorReading();
        List<SensorReading> inside  = Any.ListOf(anyReading.WithPosition(p => p.WithLatitude(l => l.Between(48.80, 48.90))
                                                                              .WithLongitude(l => l.Between(2.20, 2.40))))
                                         .WithCountBetween(1, 10)
                                         .Generate();
        List<SensorReading> outside = Any.ListOf(anyReading.WithPosition(p => p.WithLatitude(l => l.LessThan(48.80))))
                                         .WithCount(2)
                                         .Generate();
        ReadingBatch batch = new AnyReadingBatch().WithReadings([.. inside, .. outside]).Generate();

        // Act
        IReadOnlyList<SensorReading> strays = batch.OutsideOf(NorthField);

        // Assert
        Assert.Equal(outside, strays);
    }

    [Fact]
    public void Reports_each_sensor_with_a_low_battery_once() {
        // Arrange
        SensorId lowSensor = new SensorId("AG-00A1F3");
        IAny<SensorReading> anyLowReading = new AnySensorReading().WithSensor(lowSensor)
                                                                  .WithBattery(b => b.WithVolts(v => v.LessThan(BatteryVoltage.LowThreshold)));
        IAny<SensorReading> anyHealthyReading = new AnySensorReading().WithSensor(s => s.WithValue(v => v.DifferentFrom("AG-00A1F3")))
                                                                      .WithBattery(b => b.WithVolts(v => v.GreaterThanOrEqualTo(BatteryVoltage.LowThreshold)));
        List<SensorReading> readings = Any.ListOf(anyLowReading).WithCountBetween(2, 5).Generate();
        readings.AddRange(Any.ListOf(anyHealthyReading).WithCountBetween(1, 5).Generate());
        ReadingBatch batch = new AnyReadingBatch().WithReadings(readings).Generate();

        // Act
        IReadOnlyCollection<SensorId> low = batch.SensorsWithLowBattery();

        // Assert
        Assert.Equal([lowSensor], low);
    }

    [Fact]
    public void An_empty_batch_is_refused() {
        SensorId anyGateway = new AnySensorId().Generate();

        Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, []));
    }

    [Fact]
    public void A_batch_above_the_maximum_is_refused() {
        // Arrange: the content of each reading is irrelevant; only the count is significant.
        SensorId anyGateway = new AnySensorId().Generate();
        SensorReading anyReading = new AnySensorReading().Generate();
        SensorReading[] tooMany = Enumerable.Repeat(anyReading, ReadingBatch.MaximumReadings + 1).ToArray();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, tooMany));
    }

}
