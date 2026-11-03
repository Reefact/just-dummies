using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class MilkFlowReadingTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    [Fact]
    public void A_reading_keeps_what_the_sensors_reported() {
        // Arrange
        TimeSpan elapsed = TimeSpan.FromMinutes(7);

        // Act
        MilkFlowReading reading = Any.MilkFlowReading()
                                     .WithQuarter(UdderQuarter.RightRear)
                                     .WithElapsed(elapsed)
                                     .WithFlowKgPerMinute(3.25m)
                                     .WithConductivityMsPerCm(5.5m)
                                     .WithTemperatureCelsius(38.5m)
                                     .WithBloodDetected(true)
                                     .Generate();

        // Assert
        Assert.Equal(UdderQuarter.RightRear, reading.Quarter);
        Assert.Equal(elapsed, reading.Elapsed);
        Assert.Equal(3.25m, reading.FlowKgPerMinute);
        Assert.Equal(5.5m, reading.ConductivityMsPerCm);
        Assert.Equal(38.5m, reading.TemperatureCelsius);
        Assert.True(reading.BloodDetected);
    }

    [Fact]
    public void The_extremes_of_every_range_are_accepted() {
        // Act
        MilkFlowReading lowest  = Any.MilkFlowReading().WithElapsed(OneTick).WithFlowKgPerMinute(0m).WithConductivityMsPerCm(2m).WithTemperatureCelsius(30m).Generate();
        MilkFlowReading highest = Any.MilkFlowReading().WithElapsed(TimeSpan.FromHours(1)).WithFlowKgPerMinute(8m).WithConductivityMsPerCm(15m).WithTemperatureCelsius(42m).Generate();

        // Assert
        Assert.Equal(OneTick, lowest.Elapsed);
        Assert.Equal(TimeSpan.FromHours(1), highest.Elapsed);
        Assert.Equal(MilkFlowReading.MaxFlowKgPerMinute, highest.FlowKgPerMinute);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_reading_taken_at_or_before_the_start_is_refused(long ticks) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithElapsed(TimeSpan.FromTicks(ticks)).Generate());

        // Assert
        Assert.Equal("elapsed", refusal.ParamName);
    }

    [Fact]
    public void A_reading_taken_after_the_first_hour_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithElapsed(TimeSpan.FromHours(1) + OneTick).Generate());

        // Assert
        Assert.Equal("elapsed", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(8.01)]
    public void A_flow_outside_zero_to_eight_kilos_a_minute_is_refused(double flow) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithFlowKgPerMinute((decimal)flow).Generate());

        // Assert
        Assert.Equal("flowKgPerMinute", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.99)]
    [InlineData(15.01)]
    public void A_conductivity_outside_two_to_fifteen_is_refused(double conductivity) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithConductivityMsPerCm((decimal)conductivity).Generate());

        // Assert
        Assert.Equal("conductivityMsPerCm", refusal.ParamName);
    }

    [Theory]
    [InlineData(29.99)]
    [InlineData(42.01)]
    public void A_temperature_outside_thirty_to_forty_two_degrees_is_refused(double temperature) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithTemperatureCelsius((decimal)temperature).Generate());

        // Assert
        Assert.Equal("temperatureCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_quarter_the_udder_does_not_have_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkFlowReading().WithQuarter((UdderQuarter)42).Generate());

        // Assert
        Assert.Equal("quarter", refusal.ParamName);
    }

    [Fact]
    public void A_conductivity_above_seven_and_a_half_suggests_mastitis() {
        // Act
        MilkFlowReading reading = Any.MilkFlowReading().WithConductivityMsPerCm(7.51m).Generate();

        // Assert
        Assert.True(reading.SuggestsMastitis);
    }

    [Fact]
    public void A_conductivity_of_exactly_seven_and_a_half_does_not_suggest_mastitis() {
        // Act
        MilkFlowReading reading = Any.MilkFlowReading().WithConductivityMsPerCm(MilkFlowReading.MastitisConductivityThreshold).Generate();

        // Assert
        Assert.False(reading.SuggestsMastitis);
    }

}
