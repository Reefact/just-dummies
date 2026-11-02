using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class SensorReadingTests {

    private static readonly DateTimeOffset RangeStart = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RangeEnd   = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_reading_exposes_the_parts_it_was_built_from() {
        // Arrange
        SensorId         anySensor   = Any.SensorId().Generate();
        GeoCoordinate    anyPosition = Any.GeoCoordinate().Generate();
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();
        BatteryVoltage   anyBattery  = Any.BatteryVoltage().Generate();
        DateTimeOffset   takenAt     = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

        // Act
        SensorReading reading = new SensorReading(anySensor, anyPosition, anyHumidity, 21.5f, anyBattery, takenAt);

        // Assert
        Assert.Same(anySensor, reading.Sensor);
        Assert.Same(anyPosition, reading.Position);
        Assert.Same(anyHumidity, reading.Humidity);
        Assert.Same(anyBattery, reading.Battery);
        Assert.Equal(21.5f, reading.SoilTemperatureCelsius);
        Assert.Equal(takenAt, reading.TakenAt);
    }

    [Fact]
    public void A_missing_sensor_is_refused() {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new SensorReading(null!, anyReading.Position, anyReading.Humidity, anyReading.SoilTemperatureCelsius, anyReading.Battery, anyReading.TakenAt));

        // Assert
        Assert.Equal("sensor", refusal.ParamName);
    }

    [Fact]
    public void A_missing_position_is_refused() {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new SensorReading(anyReading.Sensor, null!, anyReading.Humidity, anyReading.SoilTemperatureCelsius, anyReading.Battery, anyReading.TakenAt));

        // Assert
        Assert.Equal("position", refusal.ParamName);
    }

    [Fact]
    public void A_missing_humidity_is_refused() {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new SensorReading(anyReading.Sensor, anyReading.Position, null!, anyReading.SoilTemperatureCelsius, anyReading.Battery, anyReading.TakenAt));

        // Assert
        Assert.Equal("humidity", refusal.ParamName);
    }

    [Fact]
    public void A_missing_battery_is_refused() {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new SensorReading(anyReading.Sensor, anyReading.Position, anyReading.Humidity, anyReading.SoilTemperatureCelsius, null!, anyReading.TakenAt));

        // Assert
        Assert.Equal("battery", refusal.ParamName);
    }

    [Theory]
    [InlineData(-40.0f)]
    [InlineData(85.0f)]
    [InlineData(0.0f)]
    public void The_extremes_of_the_probe_range_are_valid_soil_temperatures(float celsius) {
        // Act
        SensorReading reading = Any.SensorReading().WithSoilTemperatureCelsius(celsius).Generate();

        // Assert
        Assert.Equal(celsius, reading.SoilTemperatureCelsius);
    }

    [Theory]
    [InlineData(-40.00001f)]
    [InlineData(85.00001f)]
    [InlineData(-273.15f)]
    [InlineData(1000.0f)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void A_soil_temperature_outside_the_probe_range_or_not_a_number_is_refused(float celsius) {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SensorReading(anyReading.Sensor, anyReading.Position, anyReading.Humidity, celsius, anyReading.Battery, anyReading.TakenAt));

        // Assert
        Assert.Equal("soilTemperatureCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_reading_is_valid_whatever_the_utc_instant_it_was_taken_at() {
        // Arrange
        DateTimeOffset anyUtcInstant = Any.DateTimeOffset().Between(RangeStart, RangeEnd).Generate();

        // Act
        SensorReading reading = Any.SensorReading().WithTakenAt(anyUtcInstant).Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, reading.TakenAt.Offset);
    }

    [Fact]
    public void Even_the_first_and_the_last_instants_of_the_type_are_valid_utc_stamps() {
        Assert.Equal(DateTimeOffset.MinValue, Any.SensorReading().WithTakenAt(DateTimeOffset.MinValue).Generate().TakenAt);
        Assert.Equal(DateTimeOffset.MaxValue, Any.SensorReading().WithTakenAt(DateTimeOffset.MaxValue).Generate().TakenAt);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-5, 0)]
    [InlineData(5, 30)]
    [InlineData(0, 1)]
    [InlineData(14, 0)]
    [InlineData(-12, 0)]
    public void A_time_stamp_with_a_non_zero_offset_is_refused_even_though_it_names_a_valid_instant(int hours, int minutes) {
        // Arrange
        SensorReading  anyReading  = Any.SensorReading().Generate();
        DateTimeOffset anyInstant  = Any.DateTimeOffset().Between(RangeStart, RangeEnd).WithOffset(new TimeSpan(hours, minutes, 0)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new SensorReading(anyReading.Sensor, anyReading.Position, anyReading.Humidity, anyReading.SoilTemperatureCelsius, anyReading.Battery, anyInstant));

        // Assert
        Assert.Equal("takenAt", refusal.ParamName);
    }

}
