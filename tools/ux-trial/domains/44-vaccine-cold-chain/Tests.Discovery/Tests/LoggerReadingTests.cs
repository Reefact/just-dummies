using AgriTech.Domain;

using ColdChain.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class LoggerReadingTests {

    private static readonly DateTimeOffset Noon = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_reading_keeps_everything_it_was_built_with() {
        // Arrange
        SensorId       anyLogger  = new AnySensorId().Generate();
        BatteryVoltage anyBattery = new AnyBatteryVoltage().Generate();

        // Act
        LoggerReading reading = new LoggerReading(anyLogger, Noon, 4.5m, anyBattery);

        // Assert
        Assert.Same(anyLogger, reading.Logger);
        Assert.Equal(Noon, reading.TakenAt);
        Assert.Equal(4.5m, reading.Celsius);
        Assert.Same(anyBattery, reading.Battery);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(5.5)]
    public void A_time_stamp_with_an_offset_is_refused(double offsetHours) {
        // Arrange
        DateTimeOffset local = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.FromHours(offsetHours));

        // Act
        Exception? thrown = Record.Exception(() => new AnyLoggerReading().WithTakenAt(local).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("takenAt", refusal.ParamName);
    }

    [Fact]
    public void A_time_stamp_with_offset_zero_is_accepted_whatever_the_instant() {
        // Arrange
        DateTimeOffset anyInstant = Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        LoggerReading reading = new AnyLoggerReading().WithTakenAt(anyInstant).Generate();

        // Assert
        Assert.Equal(anyInstant, reading.TakenAt);
    }

    [Theory]
    [InlineData(-40)]
    [InlineData(-0.1)]
    [InlineData(0)]
    [InlineData(2.0)]
    [InlineData(8.5)]
    [InlineData(25)]
    [InlineData(84.9)]
    [InlineData(85)]
    public void A_temperature_between_minus_40_and_85_degrees_is_accepted(double celsius) {
        // Act
        LoggerReading reading = new AnyLoggerReading().WithCelsius((decimal)celsius).Generate();

        // Assert
        Assert.Equal((decimal)celsius, reading.Celsius);
    }

    [Theory]
    [InlineData(-40.1)]
    [InlineData(-100)]
    [InlineData(85.1)]
    [InlineData(1000)]
    public void A_temperature_outside_the_logger_range_is_refused(double celsius) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyLoggerReading().WithCelsius((decimal)celsius).Generate());

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData("4.15")]
    [InlineData("4.05")]
    [InlineData("4.123")]
    [InlineData("-0.01")]
    public void A_temperature_finer_than_a_tenth_of_a_degree_is_refused(string celsius) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyLoggerReading().WithCelsius(decimal.Parse(celsius, System.Globalization.CultureInfo.InvariantCulture)).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("celsius", refusal.ParamName);
    }

    [Theory]
    [InlineData("4.1")]
    [InlineData("4.10")]
    [InlineData("4.100")]
    [InlineData("4")]
    public void A_temperature_written_with_trailing_zeros_is_still_a_tenth_of_a_degree(string celsius) {
        // Act
        LoggerReading reading = new AnyLoggerReading().WithCelsius(decimal.Parse(celsius, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(celsius, System.Globalization.CultureInfo.InvariantCulture), reading.Celsius);
    }

    [Fact]
    public void A_missing_logger_or_battery_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new LoggerReading(null!, Noon, 4m, new AnyBatteryVoltage().Generate()));
        Assert.Throws<ArgumentNullException>(() => new LoggerReading(new AnySensorId().Generate(), Noon, 4m, null!));
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(25, false)]
    [InlineData(25.1, true)]
    [InlineData(85, true)]
    [InlineData(-40, true)]
    public void A_reading_breaches_the_hard_limit_below_zero_and_above_twenty_five_degrees(double celsius, bool breaches) {
        // Arrange
        LoggerReading reading = new AnyLoggerReading().WithCelsius((decimal)celsius).Generate();

        // Assert
        Assert.Equal(breaches, reading.BreachesHardLimit);
    }

    [Fact]
    public void Readings_are_equal_when_everything_but_the_instance_is() {
        // Arrange
        SensorId       anyLogger  = new AnySensorId().Generate();
        BatteryVoltage anyBattery = new AnyBatteryVoltage().Generate();
        LoggerReading  reading    = new LoggerReading(anyLogger, Noon, 4.5m, anyBattery);
        LoggerReading  same       = new LoggerReading(anyLogger, Noon, 4.5m, anyBattery);

        // Assert
        Assert.Equal(reading, same);
        Assert.Equal(reading.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(reading, new LoggerReading(anyLogger, Noon.AddMinutes(1), 4.5m, anyBattery));
        Assert.NotEqual(reading, new LoggerReading(anyLogger, Noon, 4.6m, anyBattery));
    }

    [Fact]
    public void A_different_battery_makes_a_different_reading() {
        // Arrange
        SensorId       anyLogger    = new AnySensorId().Generate();
        LoggerReading  reading      = new AnyLoggerReading().WithLogger(anyLogger).WithTakenAt(Noon).WithCelsius(4.5m).WithBattery(new BatteryVoltage((Half)3.0f)).Generate();
        LoggerReading  other        = new AnyLoggerReading().WithLogger(anyLogger).WithTakenAt(Noon).WithCelsius(4.5m).WithBattery(new BatteryVoltage((Half)3.5f)).Generate();

        // Assert
        Assert.NotEqual(reading, other);
    }

}
