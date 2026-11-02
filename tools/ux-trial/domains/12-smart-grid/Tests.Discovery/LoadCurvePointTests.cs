using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class LoadCurvePointTests {

    [Fact]
    public void A_point_keeps_its_slot_and_its_power() {
        LoadCurvePoint point = new LoadCurvePoint(new TimeOnly(13, 30), 7.25m);

        Assert.Equal(new TimeOnly(13, 30), point.SlotStart);
        Assert.Equal(7.25m, point.AveragePowerKw);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "0.5")]
    [InlineData("7.25", "3.625")]
    [InlineData("36", "18")]
    [InlineData("0.001", "0.0005")]
    public void The_energy_of_a_slot_is_half_its_average_power(string power, string expectedEnergy) {
        LoadCurvePoint anySlot = Any.LoadCurvePoint().WithAveragePowerKw(decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        Assert.Equal(decimal.Parse(expectedEnergy, System.Globalization.CultureInfo.InvariantCulture), anySlot.EnergyKwh);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 30)]
    [InlineData(12, 0)]
    [InlineData(23, 30)]
    public void Slots_on_the_hour_and_on_the_half_hour_are_accepted(int hour, int minute) {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        LoadCurvePoint point = new LoadCurvePoint(new TimeOnly(hour, minute), anyPower);

        Assert.Equal(new TimeOnly(hour, minute), point.SlotStart);
    }

    [Fact]
    public void A_slot_starting_off_the_half_hour_is_refused() {
        int     anyHour        = Any.Int32().Between(0, 23).Generate();
        int     anyOffMinute   = Any.Int32().Between(1, 59).Except(30).Generate();
        decimal anyPower       = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(anyHour, anyOffMinute), anyPower));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(29)]
    [InlineData(59)]
    public void A_slot_with_seconds_is_refused(int seconds) {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 30, seconds), anyPower));

        Assert.Equal("slotStart", thrown.ParamName);
    }

    [Fact]
    public void A_slot_with_milliseconds_is_refused() {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 30, 0, 1), anyPower));
    }

    [Fact]
    public void A_slot_with_microseconds_is_refused() {
        decimal  anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();
        TimeOnly slot     = new TimeOnly(8, 30, 0, 0, 1);

        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(slot, anyPower));
    }

    [Fact]
    public void The_minute_is_checked_before_the_seconds() {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 15, 20, 5), anyPower));

        Assert.Contains("half hour", thrown.Message);
    }

    [Fact]
    public void The_seconds_are_checked_before_the_milliseconds() {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 30, 20, 5), anyPower));

        Assert.Contains("whole minute", thrown.Message);
    }

    [Fact]
    public void The_milliseconds_are_checked_last() {
        decimal anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 30, 0, 5), anyPower));

        Assert.Contains("whole second", thrown.Message);
    }

    [Fact]
    public void Nanoseconds_are_not_inspected() {
        decimal  anyPower = Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate();
        TimeOnly slot     = new TimeOnly(8, 30).Add(TimeSpan.FromTicks(1));

        LoadCurvePoint point = new LoadCurvePoint(slot, anyPower);

        Assert.Equal(slot, point.SlotStart);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("36")]
    [InlineData("35.999")]
    public void The_bounds_of_the_rating_are_accepted(string power) {
        TimeOnly anySlot = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        LoadCurvePoint point = new LoadCurvePoint(anySlot, decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(power, System.Globalization.CultureInfo.InvariantCulture), point.AveragePowerKw);
    }

    [Fact]
    public void A_negative_power_is_refused() {
        TimeOnly anySlot       = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();
        decimal  anyNegative   = Any.Decimal().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new LoadCurvePoint(anySlot, anyNegative));
    }

    [Fact]
    public void A_power_above_the_rating_is_refused() {
        TimeOnly anySlot        = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();
        decimal  anyTooPowerful = Any.Decimal().GreaterThan(LoadCurvePoint.MaxPowerKw).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new LoadCurvePoint(anySlot, anyTooPowerful));
    }

    [Fact]
    public void The_smallest_step_above_the_rating_is_refused() {
        TimeOnly anySlot = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new LoadCurvePoint(anySlot, 36.001m));
    }

    [Fact]
    public void A_bad_slot_is_reported_before_a_bad_power() {
        Assert.Throws<ArgumentException>(() => new LoadCurvePoint(new TimeOnly(8, 15), -1m));
    }

    [Fact]
    public void The_rating_is_thirty_six_kilowatts() {
        Assert.Equal(36m, LoadCurvePoint.MaxPowerKw);
    }

}
