using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class SensorReadingTests {

    [Fact]
    public void A_reading_exposes_exactly_what_it_was_built_from() {
        // Arrange
        SensorId         sensor   = new SensorId("AG-00A1B2");
        GeoCoordinate    position = new GeoCoordinate(45.5, 4.25);
        RelativeHumidity humidity = new RelativeHumidity(61.5);
        BatteryVoltage   battery  = new BatteryVoltage((Half)3.7f);
        DateTimeOffset   takenAt  = new DateTimeOffset(2026, 5, 17, 6, 30, 0, TimeSpan.Zero);

        // Act
        SensorReading reading = new SensorReading(sensor, position, humidity, 18.25f, battery, takenAt);

        // Assert
        Assert.Same(sensor, reading.Sensor);
        Assert.Same(position, reading.Position);
        Assert.Same(humidity, reading.Humidity);
        Assert.Equal(18.25f, reading.SoilTemperatureCelsius);
        Assert.Same(battery, reading.Battery);
        Assert.Equal(takenAt, reading.TakenAt);
    }

    [Fact]
    public void A_reading_of_the_generator_is_always_well_formed() {
        // Act
        SensorReading anyReading = Any.SensorReading().Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, anyReading.TakenAt.Offset);
        Assert.InRange(anyReading.SoilTemperatureCelsius, -40f, 85f);
    }

    [Fact]
    public void A_missing_sensor_is_refused() {
        // Arrange
        GeoCoordinate    anyPosition = Any.GeoCoordinate().Generate();
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();
        BatteryVoltage   anyBattery  = Any.BatteryVoltage().Generate();
        DateTimeOffset   anyTakenAt  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorReading(null!, anyPosition, anyHumidity, 20f, anyBattery, anyTakenAt));

        // Assert
        Assert.Equal("sensor", exception.ParamName);
    }

    [Fact]
    public void A_missing_position_is_refused() {
        // Arrange
        SensorId         anySensor   = Any.SensorId().Generate();
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();
        BatteryVoltage   anyBattery  = Any.BatteryVoltage().Generate();
        DateTimeOffset   anyTakenAt  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorReading(anySensor, null!, anyHumidity, 20f, anyBattery, anyTakenAt));

        // Assert
        Assert.Equal("position", exception.ParamName);
    }

    [Fact]
    public void A_missing_humidity_is_refused() {
        // Arrange
        SensorId       anySensor   = Any.SensorId().Generate();
        GeoCoordinate  anyPosition = Any.GeoCoordinate().Generate();
        BatteryVoltage anyBattery  = Any.BatteryVoltage().Generate();
        DateTimeOffset anyTakenAt  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorReading(anySensor, anyPosition, null!, 20f, anyBattery, anyTakenAt));

        // Assert
        Assert.Equal("humidity", exception.ParamName);
    }

    [Fact]
    public void A_missing_battery_is_refused() {
        // Arrange
        SensorId         anySensor   = Any.SensorId().Generate();
        GeoCoordinate    anyPosition = Any.GeoCoordinate().Generate();
        RelativeHumidity anyHumidity = Any.RelativeHumidity().Generate();
        DateTimeOffset   anyTakenAt  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorReading(anySensor, anyPosition, anyHumidity, 20f, null!, anyTakenAt));

        // Assert
        Assert.Equal("battery", exception.ParamName);
    }

    [Theory]
    [InlineData(-40f)]
    [InlineData(85f)]
    [InlineData(0f)]
    public void The_soil_temperature_bounds_are_inclusive(float celsius) {
        // Act
        SensorReading reading = Any.SensorReading().WithSoilTemperatureCelsius(celsius).Generate();

        // Assert
        Assert.Equal(celsius, reading.SoilTemperatureCelsius);
    }

    [Fact]
    public void A_soil_temperature_below_minus_40_is_refused() {
        // Arrange
        float anyCelsius = Any.Single().LessThan(-40f).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SensorReading().WithSoilTemperatureCelsius(anyCelsius).Generate());

        // Assert
        Assert.Equal("soilTemperatureCelsius", exception.ParamName);
    }

    [Fact]
    public void A_soil_temperature_above_85_is_refused() {
        // Arrange
        float anyCelsius = Any.Single().GreaterThan(85f).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.SensorReading().WithSoilTemperatureCelsius(anyCelsius).Generate());

        // Assert
        Assert.Equal("soilTemperatureCelsius", exception.ParamName);
    }

    [Fact]
    public void A_soil_temperature_one_step_beyond_either_bound_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.SensorReading().WithSoilTemperatureCelsius(float.BitDecrement(-40f)).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.SensorReading().WithSoilTemperatureCelsius(float.BitIncrement(85f)).Generate());
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void A_soil_temperature_that_is_not_a_finite_number_is_refused(float celsius) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.SensorReading().WithSoilTemperatureCelsius(celsius).Generate());
    }

    [Fact]
    public void A_timestamp_in_UTC_is_accepted() {
        // Arrange
        DateTimeOffset anyUtcInstant = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                                    new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        SensorReading anyReading = Any.SensorReading().WithTakenAt(anyUtcInstant).Generate();

        // Assert
        Assert.Equal(anyUtcInstant, anyReading.TakenAt);
    }

    [Fact]
    public void A_timestamp_with_a_non_zero_offset_is_refused() {
        // Arrange
        DateTimeOffset anyLocalInstant = Any.DateTimeOffset()
                                            .Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                            .WithOffsetBetween(TimeSpan.FromMinutes(1), TimeSpan.FromHours(14))
                                            .Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.SensorReading().WithTakenAt(anyLocalInstant).Generate());

        // Assert
        Assert.Equal("takenAt", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(-300)]
    [InlineData(330)]
    public void A_timestamp_is_refused_whatever_the_sign_of_its_offset(int offsetMinutes) {
        // Arrange
        DateTimeOffset local = new DateTimeOffset(2026, 5, 17, 6, 30, 0, TimeSpan.FromMinutes(offsetMinutes));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Any.SensorReading().WithTakenAt(local).Generate());
    }

}
