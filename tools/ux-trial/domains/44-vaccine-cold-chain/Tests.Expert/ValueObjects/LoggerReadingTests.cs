using AgriTech.Domain;
using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class LoggerReadingTests {

    private static readonly DateTimeOffset TakenAt = new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("-40")]
    [InlineData("-0.1")]
    [InlineData("0")]
    [InlineData("4.2")]
    [InlineData("4.20")]
    [InlineData("85")]
    public void A_logger_reads_from_minus_40_to_85_degrees_at_a_tenth_of_a_degree(string celsius) {
        // Act
        LoggerReading reading = Any.LoggerReading().WithCelsius(Celsius.Of(celsius)).Generate();

        // Assert
        Assert.Equal(Celsius.Of(celsius), reading.Celsius);
    }

    [Theory]
    [InlineData("-40.1")]
    [InlineData("85.1")]
    [InlineData("100")]
    public void A_temperature_beyond_what_the_logger_measures_is_out_of_range(string celsius) {
        // Arrange
        AnyLoggerReading readings = Any.LoggerReading().WithCelsius(Celsius.Of(celsius));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => readings.Generate());
    }

    [Theory]
    [InlineData("4.25")]
    [InlineData("4.01")]
    [InlineData("-0.05")]
    public void A_temperature_finer_than_a_tenth_of_a_degree_is_refused(string celsius) {
        // Arrange
        AnyLoggerReading readings = Any.LoggerReading().WithCelsius(Celsius.Of(celsius));

        // Assert
        Assert.Throws<ArgumentException>(() => readings.Generate());
    }

    [Fact]
    public void A_reading_time_stamped_in_utc_is_accepted() {
        // Act
        LoggerReading reading = Any.LoggerReading().WithTakenAt(TakenAt).Generate();

        // Assert
        Assert.Equal(TakenAt, reading.TakenAt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-5)]
    public void A_reading_time_stamped_in_another_offset_is_refused(int offsetHours) {
        // Arrange
        AnyLoggerReading readings = Any.LoggerReading().WithTakenAt(new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.FromHours(offsetHours)));

        // Assert
        Assert.Throws<ArgumentException>(() => readings.Generate());
    }

    [Theory]
    [InlineData("-0.1", true)]
    [InlineData("0", false)]
    [InlineData("25", false)]
    [InlineData("25.1", true)]
    public void A_reading_breaches_the_hard_limit_below_zero_and_above_25_degrees(string celsius, bool expectedBreach) {
        // Arrange
        LoggerReading reading = Any.LoggerReading().WithCelsius(Celsius.Of(celsius)).Generate();

        // Assert
        Assert.Equal(expectedBreach, reading.BreachesHardLimit);
    }

    [Fact]
    public void A_missing_battery_is_refused() {
        // Arrange
        SensorId anySensor = Any.SensorId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LoggerReading(anySensor, TakenAt, 5m, null!));
    }

    [Fact]
    public void Two_readings_of_the_same_logger_at_the_same_instant_with_the_same_temperature_and_battery_are_equal() {
        // Arrange
        SensorId       sharedSensor  = Any.SensorId().Generate();
        BatteryVoltage sharedBattery = Any.BatteryVoltage().Generate();

        LoggerReading first  = Any.LoggerReading().WithLogger(sharedSensor).WithTakenAt(TakenAt).WithCelsius(5m).WithBattery(sharedBattery).Generate();
        LoggerReading second = Any.LoggerReading().WithLogger(sharedSensor).WithTakenAt(TakenAt).WithCelsius(5m).WithBattery(sharedBattery).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Readings_differing_by_the_temperature_are_different() {
        // Arrange
        SensorId       sharedSensor  = Any.SensorId().Generate();
        BatteryVoltage sharedBattery = Any.BatteryVoltage().Generate();

        LoggerReading cooler = Any.LoggerReading().WithLogger(sharedSensor).WithTakenAt(TakenAt).WithCelsius(5m).WithBattery(sharedBattery).Generate();
        LoggerReading warmer = Any.LoggerReading().WithLogger(sharedSensor).WithTakenAt(TakenAt).WithCelsius(5.1m).WithBattery(sharedBattery).Generate();

        // Assert
        Assert.NotEqual(cooler, warmer);
    }

}
