using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class BatteryVoltageTests {

    [Fact]
    public void The_bounds_are_the_half_precision_values_nearest_to_2_5_3_3_and_4_2() {
        Assert.Equal(2.5f, (float)BatteryVoltage.Minimum);
        Assert.Equal(4.19921875f, (float)BatteryVoltage.Maximum);
        Assert.Equal(3.30078125f, (float)BatteryVoltage.LowThreshold);
    }

    [Theory]
    [InlineData(2.5f)]            // Minimum, inclusive
    [InlineData(4.19921875f)]     // Maximum, inclusive
    [InlineData(3.7f)]
    public void A_voltage_between_the_bounds_is_kept_as_given(float volts) {
        Half half = (Half)volts;

        Assert.Equal(half, new BatteryVoltage(half).Volts);
    }

    [Fact]
    public void A_voltage_drawn_between_the_bounds_is_accepted() {
        // Arrange
        Half anyVolts = Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum).Generate();

        // Act
        BatteryVoltage battery = new BatteryVoltage(anyVolts);

        // Assert
        Assert.Equal(anyVolts, battery.Volts);
    }

    [Fact]
    public void A_voltage_just_below_the_minimum_is_refused() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitDecrement(BatteryVoltage.Minimum)));

        Assert.Equal("volts", exception.ParamName);
    }

    [Fact]
    public void A_voltage_just_above_the_maximum_is_refused() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitIncrement(BatteryVoltage.Maximum)));

        Assert.Equal("volts", exception.ParamName);
    }

    [Fact]
    public void The_largest_value_a_float_4_2_would_round_to_is_the_maximum_not_beyond_it() {
        // 4.2f is not representable in half precision; it is stored as 4.19921875, the Maximum itself.
        Assert.Equal(BatteryVoltage.Maximum, new BatteryVoltage((Half)4.2f).Volts);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-3.7f)]
    [InlineData(5f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void A_voltage_outside_the_cell_range_is_refused(float volts) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage((Half)volts));
    }

    [Fact]
    public void A_voltage_drawn_below_the_minimum_is_refused() {
        // Arrange
        Half anyTooLow = Any.Half().LessThan(BatteryVoltage.Minimum).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(anyTooLow));
    }

    [Fact]
    public void A_voltage_drawn_above_the_maximum_is_refused() {
        // Arrange
        Half anyTooHigh = Any.Half().GreaterThan(BatteryVoltage.Maximum).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(anyTooHigh));
    }

    [Fact]
    public void A_cell_at_the_low_threshold_is_not_low() {
        Assert.False(new BatteryVoltage(BatteryVoltage.LowThreshold).IsLow);
    }

    [Fact]
    public void A_cell_one_half_precision_step_under_the_low_threshold_is_low() {
        Assert.True(new BatteryVoltage(Half.BitDecrement(BatteryVoltage.LowThreshold)).IsLow);
    }

    [Fact]
    public void A_cell_at_the_minimum_is_low() {
        Assert.True(new BatteryVoltage(BatteryVoltage.Minimum).IsLow);
    }

    [Fact]
    public void A_cell_at_the_maximum_is_not_low() {
        Assert.False(new BatteryVoltage(BatteryVoltage.Maximum).IsLow);
    }

    [Fact]
    public void A_cell_charged_to_3_3_volts_as_sent_by_a_float_is_not_low() {
        // The threshold is (Half)3.3f; the same conversion of the same float lands exactly on it.
        Assert.False(new BatteryVoltage((Half)3.3f).IsLow);
    }

}
