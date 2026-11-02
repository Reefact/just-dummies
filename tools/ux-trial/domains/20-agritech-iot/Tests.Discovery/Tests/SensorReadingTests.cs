using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class SensorReadingTests {

    private static SensorReading Make(SensorId? sensor, GeoCoordinate? position, RelativeHumidity? humidity, float soilTemperatureCelsius, BatteryVoltage? battery, DateTimeOffset takenAt) {
        return new SensorReading(sensor!, position!, humidity!, soilTemperatureCelsius, battery!, takenAt);
    }

    [Fact]
    public void A_reading_exposes_what_it_was_built_with() {
        // Arrange
        SensorId         anySensor   = Any.SensorId().Generate();
        GeoCoordinate    anyPosition = Any.GeoCoordinate().Generate();
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();
        BatteryVoltage   anyBattery  = Any.BatteryVoltage().Generate();
        DateTimeOffset   anyTakenAt  = Any.DateTimeOffset().Generate();

        // Act
        SensorReading reading = new SensorReading(anySensor, anyPosition, anyHumidity, 21.5f, anyBattery, anyTakenAt);

        // Assert
        Assert.Same(anySensor, reading.Sensor);
        Assert.Same(anyPosition, reading.Position);
        Assert.Same(anyHumidity, reading.Humidity);
        Assert.Equal(21.5f, reading.SoilTemperatureCelsius);
        Assert.Same(anyBattery, reading.Battery);
        Assert.Equal(anyTakenAt, reading.TakenAt);
    }

    [Fact]
    public void A_missing_sensor_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Make(null, Any.GeoCoordinate().Generate(), Any.RelativeHumidity().Generate(), 20f, Any.BatteryVoltage().Generate(), Any.DateTimeOffset().Generate()));

        Assert.Equal("sensor", exception.ParamName);
    }

    [Fact]
    public void A_missing_position_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Make(Any.SensorId().Generate(), null, Any.RelativeHumidity().Generate(), 20f, Any.BatteryVoltage().Generate(), Any.DateTimeOffset().Generate()));

        Assert.Equal("position", exception.ParamName);
    }

    [Fact]
    public void A_missing_humidity_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Make(Any.SensorId().Generate(), Any.GeoCoordinate().Generate(), null, 20f, Any.BatteryVoltage().Generate(), Any.DateTimeOffset().Generate()));

        Assert.Equal("humidity", exception.ParamName);
    }

    [Fact]
    public void A_missing_battery_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Make(Any.SensorId().Generate(), Any.GeoCoordinate().Generate(), Any.RelativeHumidity().Generate(), 20f, null, Any.DateTimeOffset().Generate()));

        Assert.Equal("battery", exception.ParamName);
    }

    [Theory]
    [InlineData(-40f)]
    [InlineData(85f)]
    [InlineData(0f)]
    [InlineData(-39.999996f)]
    [InlineData(84.99999f)]
    public void A_soil_temperature_from_minus_40_to_85_degrees_is_kept(float celsius) {
        SensorReading reading = Any.SensorReading().WithSoilTemperatureCelsius(celsius).Generate();

        Assert.Equal(celsius, reading.SoilTemperatureCelsius);
    }

    [Theory]
    [InlineData(-40.000004f)]     // the next float below -40
    [InlineData(85.000008f)]      // the next float above 85
    [InlineData(-273.15f)]
    [InlineData(1000f)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void A_soil_temperature_outside_minus_40_to_85_degrees_is_refused(float celsius) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Make(Any.SensorId().Generate(), Any.GeoCoordinate().Generate(), Any.RelativeHumidity().Generate(), celsius, Any.BatteryVoltage().Generate(), Any.DateTimeOffset().Generate()));

        Assert.Equal("soilTemperatureCelsius", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(120)]
    [InlineData(-330)]
    [InlineData(840)]
    public void A_timestamp_with_an_offset_from_UTC_is_refused(int offsetMinutes) {
        // Arrange
        DateTimeOffset anyInstant = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();
        DateTimeOffset local      = anyInstant.ToOffset(TimeSpan.FromMinutes(offsetMinutes));

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Make(Any.SensorId().Generate(), Any.GeoCoordinate().Generate(), Any.RelativeHumidity().Generate(), 20f, Any.BatteryVoltage().Generate(), local));

        // Assert
        Assert.Equal("takenAt", exception.ParamName);
    }

    [Fact]
    public void A_timestamp_in_UTC_is_accepted() {
        // Arrange
        DateTimeOffset anyUtcInstant = Any.DateTimeOffset().Generate();

        // Act
        SensorReading reading = Any.SensorReading().WithTakenAt(anyUtcInstant).Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, reading.TakenAt.Offset);
    }

    [Fact]
    public void A_drawn_reading_is_always_buildable() {
        // Act and assert: the scaffolded generator draws a UTC time, a soil temperature in range, and valid parts.
        SensorReading anyReading = Any.SensorReading().Generate();

        Assert.Equal(TimeSpan.Zero, anyReading.TakenAt.Offset);
    }

}
