using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class BatteryVoltageTests {

    [Fact]
    public void The_bounds_are_the_half_precision_values_nearest_to_2_5_4_2_and_3_3_volts() {
        // Assert
        Assert.Equal(2.5f, (float)BatteryVoltage.Minimum);
        Assert.Equal(4.19921875f, (float)BatteryVoltage.Maximum);
        Assert.Equal(3.30078125f, (float)BatteryVoltage.LowThreshold);
    }

    [Fact]
    public void A_voltage_inside_the_range_is_kept() {
        // Arrange
        Half anyVolts = Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum).Generate();

        // Act
        BatteryVoltage anyBattery = new BatteryVoltage(anyVolts);

        // Assert
        Assert.Equal(anyVolts, anyBattery.Volts);
    }

    [Fact]
    public void Both_ends_of_the_range_are_accepted() {
        // Act
        BatteryVoltage lowest  = new BatteryVoltage(BatteryVoltage.Minimum);
        BatteryVoltage highest = new BatteryVoltage(BatteryVoltage.Maximum);

        // Assert
        Assert.Equal(BatteryVoltage.Minimum, lowest.Volts);
        Assert.Equal(BatteryVoltage.Maximum, highest.Volts);
    }

    [Fact]
    public void A_voltage_below_the_minimum_is_refused() {
        // Arrange
        Half anyVolts = Any.Half().LessThan(BatteryVoltage.Minimum).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(anyVolts));

        // Assert
        Assert.Equal("volts", exception.ParamName);
    }

    [Fact]
    public void A_voltage_above_the_maximum_is_refused() {
        // Arrange
        Half anyVolts = Any.Half().GreaterThan(BatteryVoltage.Maximum).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(anyVolts));

        // Assert
        Assert.Equal("volts", exception.ParamName);
    }

    [Fact]
    public void A_voltage_one_step_beyond_either_end_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitDecrement(BatteryVoltage.Minimum)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.BitIncrement(BatteryVoltage.Maximum)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_infinite_voltage_is_refused(bool positive) {
        // Arrange
        Half infinity = positive ? Half.PositiveInfinity : Half.NegativeInfinity;

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(infinity));
    }

    [Fact]
    public void A_voltage_that_is_not_a_number_is_refused() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryVoltage(Half.NaN));

        // Assert
        Assert.Equal("volts", exception.ParamName);
    }

    [Fact]
    public void A_voltage_below_the_low_threshold_is_low() {
        // Arrange
        Half anyVolts = Any.Half().GreaterThanOrEqualTo(BatteryVoltage.Minimum).LessThan(BatteryVoltage.LowThreshold).Generate();

        // Act
        BatteryVoltage anyLowBattery = new BatteryVoltage(anyVolts);

        // Assert
        Assert.True(anyLowBattery.IsLow);
    }

    [Fact]
    public void A_voltage_at_or_above_the_low_threshold_is_not_low() {
        // Arrange
        Half anyVolts = Any.Half().Between(BatteryVoltage.LowThreshold, BatteryVoltage.Maximum).Generate();

        // Act
        BatteryVoltage anyHealthyBattery = new BatteryVoltage(anyVolts);

        // Assert
        Assert.False(anyHealthyBattery.IsLow);
    }

    [Fact]
    public void The_low_threshold_itself_is_not_low_but_the_step_under_it_is() {
        // Act
        BatteryVoltage atThreshold = new BatteryVoltage(BatteryVoltage.LowThreshold);
        BatteryVoltage justUnder   = new BatteryVoltage(Half.BitDecrement(BatteryVoltage.LowThreshold));

        // Assert
        Assert.False(atThreshold.IsLow);
        Assert.True(justUnder.IsLow);
    }

    [Fact]
    public void A_battery_reading_of_3_3_volts_as_transmitted_is_not_low() {
        // Arrange: the radio rounds 3.3 V to the nearest half, which is the threshold itself.
        Half transmitted = (Half)3.3f;

        // Act
        BatteryVoltage battery = new BatteryVoltage(transmitted);

        // Assert
        Assert.False(battery.IsLow);
    }

}
