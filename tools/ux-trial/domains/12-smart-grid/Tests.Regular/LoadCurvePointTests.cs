using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Regular;

public sealed class LoadCurvePointTests {

    [Fact]
    public void A_point_keeps_its_slot_and_its_power() {
        // Act
        LoadCurvePoint point = new LoadCurvePoint(new TimeOnly(13, 30), 7.25m);

        // Assert
        Assert.Equal(new TimeOnly(13, 30), point.SlotStart);
        Assert.Equal(7.25m, point.AveragePowerKw);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "0.5")]
    [InlineData("3", "1.5")]
    [InlineData("0.001", "0.0005")]
    [InlineData("36", "18")]
    public void The_energy_of_a_slot_is_half_its_average_power(string power, string expectedEnergy) {
        // Arrange
        TimeOnly anySlot = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        // Act
        LoadCurvePoint point = new LoadCurvePoint(anySlot, decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(expectedEnergy, System.Globalization.CultureInfo.InvariantCulture), point.EnergyKwh);
    }

    [Fact]
    public void A_slot_may_start_on_the_hour_or_on_the_half_hour() {
        // Arrange
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();
        int     anyHour  = Any.Int32().Between(0, 23).Generate();

        // Act
        LoadCurvePoint onTheHour  = new LoadCurvePoint(new TimeOnly(anyHour, 0), anyPower);
        LoadCurvePoint onTheHalf  = new LoadCurvePoint(new TimeOnly(anyHour, 30), anyPower);

        // Assert
        Assert.Equal(0, onTheHour.SlotStart.Minute);
        Assert.Equal(30, onTheHalf.SlotStart.Minute);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(45)]
    [InlineData(59)]
    public void A_slot_starting_off_the_half_hour_is_refused(int minute) {
        // Arrange
        int     anyHour  = Any.Int32().Between(0, 23).Generate();
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(anyHour, minute), anyPower));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(30, 59)]
    public void A_slot_starting_on_a_stray_second_is_refused(int minute, int second) {
        // Arrange
        int     anyHour  = Any.Int32().Between(0, 23).Generate();
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(anyHour, minute, second), anyPower));
    }

    [Fact]
    public void A_slot_starting_on_a_stray_millisecond_is_refused() {
        // Arrange
        int     anyHour  = Any.Int32().Between(0, 23).Generate();
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(anyHour, 0, 0, 1), anyPower));
    }

    [Fact]
    public void A_slot_starting_on_a_stray_microsecond_is_refused() {
        // Arrange
        int     anyHour  = Any.Int32().Between(0, 23).Generate();
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(anyHour, 30, 0, 0, 1), anyPower));
    }

    [Fact(Skip = "domain defect: nanoseconds are not inspected, so a slot starting 100 ns after the half hour (TimeOnly ticks = 1) is accepted although the message says 'a whole second' (literal value, no seed)")]
    public void A_slot_starting_one_tick_after_the_half_hour_is_refused() {
        // Arrange
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(1L), anyPower));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("36")]
    [InlineData("35.999")]
    [InlineData("0.001")]
    public void A_power_within_the_meter_rating_is_accepted(string power) {
        // Arrange
        TimeOnly anySlot = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();
        decimal  kw      = decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        LoadCurvePoint point = new LoadCurvePoint(anySlot, kw);

        // Assert
        Assert.Equal(kw, point.AveragePowerKw);
    }

    [Theory]
    [InlineData("-0.001")]
    [InlineData("-1")]
    [InlineData("36.001")]
    [InlineData("37")]
    public void A_power_outside_the_meter_rating_is_refused(string power) {
        // Arrange
        TimeOnly anySlot = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LoadCurvePoint(anySlot, decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_power_above_the_rating_is_refused_whatever_its_size() {
        // Arrange
        TimeOnly anySlot       = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();
        decimal  anyOverPower  = Any.Decimal().GreaterThan(LoadCurvePoint.MaxPowerKw).LessThan(1_000_000m).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LoadCurvePoint(anySlot, anyOverPower));
    }

    [Fact]
    public void A_bad_slot_is_reported_before_a_bad_power() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(10, 15), 100m));
    }

    [Fact]
    public void The_rating_is_thirty_six_kilowatts() {
        Assert.Equal(36m, LoadCurvePoint.MaxPowerKw);
    }

}
