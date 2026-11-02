using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class BatteryVoltageTests {

    [Fact]
    public void A_battery_keeps_the_voltage_it_was_given() {
        // Act
        BatteryVoltage battery = new BatteryVoltage((Half)3.75f);

        // Assert
        Assert.Equal((Half)3.75f, battery.Volts);
    }

    [Fact]
    public void The_lowest_and_the_highest_voltage_a_cell_can_report_are_valid() {
        // Act
        BatteryVoltage lowest  = new BatteryVoltage(BatteryVoltage.Minimum);
        BatteryVoltage highest = new BatteryVoltage(BatteryVoltage.Maximum);

        // Assert
        Assert.Equal((Half)2.5f, lowest.Volts);
        Assert.Equal((Half)4.19921875f, highest.Volts);
    }

    [Fact]
    public void The_bounds_are_the_half_precision_roundings_of_2_5_and_4_2_volts() {
        Assert.Equal(2.5f, (float)BatteryVoltage.Minimum);
        Assert.Equal(4.19921875f, (float)BatteryVoltage.Maximum);
        Assert.Equal(3.30078125f, (float)BatteryVoltage.LowThreshold);
    }

    [Fact]
    public void A_voltage_just_below_the_minimum_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitDecrement(BatteryVoltage.Minimum)));

        // Assert
        Assert.Equal("volts", refusal.ParamName);
    }

    [Fact]
    public void A_voltage_just_above_the_maximum_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitIncrement(BatteryVoltage.Maximum)));

        // Assert
        Assert.Equal("volts", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-3.0f)]
    [InlineData(5.0f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void A_voltage_outside_the_cell_range_or_not_a_number_is_refused(float volts) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage((Half)volts));
    }

    [Fact]
    public void The_half_precision_value_of_a_cell_at_4_2_volts_is_still_valid() {
        // 4.2 V does not exist in half precision: it travels as 4.19921875.
        Assert.Equal((Half)4.19921875f, (Half)4.2f);
        BatteryVoltage battery = new BatteryVoltage((Half)4.2f);

        Assert.False(battery.IsLow);
    }

    [Fact]
    public void A_battery_is_low_one_step_under_the_threshold() {
        // Arrange
        BatteryVoltage battery = new BatteryVoltage(Half.BitDecrement(BatteryVoltage.LowThreshold));

        // Assert
        Assert.True(battery.IsLow);
    }

    [Fact]
    public void A_battery_exactly_at_the_threshold_is_not_low() {
        // Arrange
        BatteryVoltage battery = new BatteryVoltage(BatteryVoltage.LowThreshold);

        // Assert
        Assert.False(battery.IsLow);
    }

    [Theory]
    [InlineData(2.5f, true)]
    [InlineData(3.0f, true)]
    [InlineData(3.3f, false)]
    [InlineData(3.6f, false)]
    [InlineData(4.19f, false)]
    public void A_battery_is_low_below_roughly_3_3_volts_whatever_the_rest_of_its_range(float volts, bool expectedLow) {
        // Arrange
        BatteryVoltage battery = new BatteryVoltage((Half)volts);

        // Assert
        Assert.Equal(expectedLow, battery.IsLow);
    }

}
