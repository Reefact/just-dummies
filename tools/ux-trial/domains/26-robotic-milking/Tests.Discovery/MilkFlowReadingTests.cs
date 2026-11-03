using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class MilkFlowReadingTests {

    public static TheoryData<decimal> FlowInRange => new() { 0m, 0.001m, 4m, 7.999m, 8m };
    public static TheoryData<decimal> FlowOutOfRange => new() { -0.001m, -1m, 8.001m, 100m };
    public static TheoryData<decimal> ConductivityInRange => new() { 2m, 2.01m, 7.5m, 14.99m, 15m };
    public static TheoryData<decimal> ConductivityOutOfRange => new() { 0m, 1.99m, 15.01m, 40m };
    public static TheoryData<decimal> TemperatureInRange => new() { 30m, 30.01m, 38.5m, 41.99m, 42m };
    public static TheoryData<decimal> TemperatureOutOfRange => new() { 0m, 29.99m, 42.01m, 100m };

    [Fact]
    public void A_reading_keeps_the_sample_it_was_built_from() {
        // Act
        MilkFlowReading reading = new MilkFlowReading(UdderQuarter.RightFront, TimeSpan.FromSeconds(90), 2.5m, 5.2m, 38.6m, bloodDetected: true);

        // Assert
        Assert.Equal(UdderQuarter.RightFront, reading.Quarter);
        Assert.Equal(TimeSpan.FromSeconds(90), reading.Elapsed);
        Assert.Equal(2.5m, reading.FlowKgPerMinute);
        Assert.Equal(5.2m, reading.ConductivityMsPerCm);
        Assert.Equal(38.6m, reading.TemperatureCelsius);
        Assert.True(reading.BloodDetected);
    }

    [Theory]
    [InlineData(0.0001)]
    [InlineData(1)]
    [InlineData(1800)]
    [InlineData(3600)]
    public void A_reading_taken_within_the_first_hour_is_accepted(double seconds) {
        MilkFlowReading anyReading = Any.MilkFlowReading().WithElapsed(TimeSpan.FromSeconds(seconds)).Generate();

        Assert.Equal(TimeSpan.FromSeconds(seconds), anyReading.Elapsed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3600.001)]
    [InlineData(7200)]
    public void A_reading_at_time_zero_or_after_the_first_hour_is_refused(double seconds) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkFlowReading().WithElapsed(TimeSpan.FromSeconds(seconds)).Generate());

        Assert.Equal("elapsed", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(FlowInRange))]
    public void A_flow_from_zero_to_eight_kilograms_per_minute_is_accepted(decimal flow) {
        MilkFlowReading anyReading = Any.MilkFlowReading().WithFlowKgPerMinute(flow).Generate();

        Assert.Equal(flow, anyReading.FlowKgPerMinute);
    }

    [Theory]
    [MemberData(nameof(FlowOutOfRange))]
    public void A_negative_flow_or_one_above_eight_is_refused(decimal flow) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkFlowReading().WithFlowKgPerMinute(flow).Generate());

        Assert.Equal("flowKgPerMinute", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(ConductivityInRange))]
    public void A_conductivity_from_2_to_15_is_accepted(decimal conductivity) {
        MilkFlowReading anyReading = Any.MilkFlowReading().WithConductivityMsPerCm(conductivity).Generate();

        Assert.Equal(conductivity, anyReading.ConductivityMsPerCm);
    }

    [Theory]
    [MemberData(nameof(ConductivityOutOfRange))]
    public void A_conductivity_outside_2_to_15_is_refused(decimal conductivity) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkFlowReading().WithConductivityMsPerCm(conductivity).Generate());

        Assert.Equal("conductivityMsPerCm", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(TemperatureInRange))]
    public void A_milk_temperature_from_30_to_42_degrees_is_accepted(decimal temperature) {
        MilkFlowReading anyReading = Any.MilkFlowReading().WithTemperatureCelsius(temperature).Generate();

        Assert.Equal(temperature, anyReading.TemperatureCelsius);
    }

    [Theory]
    [MemberData(nameof(TemperatureOutOfRange))]
    public void A_milk_temperature_outside_30_to_42_degrees_is_refused(decimal temperature) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkFlowReading().WithTemperatureCelsius(temperature).Generate());

        Assert.Equal("temperatureCelsius", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_udder_quarter_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkFlowReading().WithQuarter((UdderQuarter)17).Generate());

        Assert.Equal("quarter", refusal.ParamName);
    }

    [Theory]
    [InlineData("7.5", false)]
    [InlineData("7.49", false)]
    [InlineData("2", false)]
    [InlineData("7.51", true)]
    [InlineData("7.500001", true)]
    [InlineData("15", true)]
    public void Mastitis_is_suggested_only_by_a_conductivity_above_seven_and_a_half(string conductivity, bool suggestsMastitis) {
        MilkFlowReading anyReading = Any.MilkFlowReading().WithConductivityMsPerCm(decimal.Parse(conductivity, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        Assert.Equal(suggestsMastitis, anyReading.SuggestsMastitis);
    }

}
