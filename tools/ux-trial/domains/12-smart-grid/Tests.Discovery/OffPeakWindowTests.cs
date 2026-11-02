using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class OffPeakWindowTests {

    [Fact]
    public void A_window_keeps_its_two_ends() {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 30));

        Assert.Equal(new TimeOnly(22, 0), window.Start);
        Assert.Equal(new TimeOnly(6, 30), window.End);
    }

    [Fact]
    public void A_window_that_starts_and_ends_at_the_same_time_is_refused() {
        TimeOnly anyBoundary = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, anyBoundary));

        Assert.Equal("end", thrown.ParamName);
    }

    [Theory]
    [InlineData(22, 15, 0)]
    [InlineData(22, 1, 0)]
    [InlineData(22, 45, 0)]
    [InlineData(22, 30, 1)]
    public void A_start_off_the_half_hour_is_refused(int hour, int minute, int second) {
        TimeOnly anyEnd = new TimeOnly(5, 0);

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(hour, minute, second), anyEnd));

        Assert.Equal("start", thrown.ParamName);
    }

    [Theory]
    [InlineData(6, 15, 0)]
    [InlineData(6, 59, 0)]
    [InlineData(6, 30, 59)]
    public void An_end_off_the_half_hour_is_refused(int hour, int minute, int second) {
        TimeOnly anyStart = new TimeOnly(22, 0);

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyStart, new TimeOnly(hour, minute, second)));

        Assert.Equal("end", thrown.ParamName);
    }

    [Fact]
    public void A_boundary_one_tick_off_the_half_hour_is_refused() {
        TimeOnly onTheHour = new TimeOnly(6, 0);
        TimeOnly oneTick   = onTheHour.Add(TimeSpan.FromTicks(1));

        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(22, 0), oneTick));
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(oneTick, new TimeOnly(23, 0)));
    }

    [Theory]
    [InlineData(22, 0, 6, 0, true)]
    [InlineData(22, 0, 0, 0, true)]
    [InlineData(23, 30, 0, 30, true)]
    [InlineData(0, 0, 6, 0, false)]
    [InlineData(8, 0, 10, 30, false)]
    [InlineData(0, 30, 23, 30, false)]
    public void A_window_wraps_midnight_when_its_end_is_before_its_start(int startHour, int startMinute, int endHour, int endMinute, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

        Assert.Equal(expected, window.WrapsMidnight);
    }

    [Theory]
    [InlineData(8, 0, true)]
    [InlineData(8, 30, true)]
    [InlineData(9, 30, true)]
    [InlineData(10, 0, false)]
    [InlineData(10, 30, false)]
    [InlineData(7, 30, false)]
    [InlineData(0, 0, false)]
    [InlineData(23, 30, false)]
    public void A_window_in_the_day_contains_its_start_but_not_its_end(int hour, int minute, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.Equal(expected, window.Contains(new TimeOnly(hour, minute)));
    }

    [Theory]
    [InlineData(22, 0, true)]
    [InlineData(22, 30, true)]
    [InlineData(23, 30, true)]
    [InlineData(0, 0, true)]
    [InlineData(3, 0, true)]
    [InlineData(5, 30, true)]
    [InlineData(6, 0, false)]
    [InlineData(6, 30, false)]
    [InlineData(12, 0, false)]
    [InlineData(21, 30, false)]
    public void A_window_across_midnight_contains_its_start_but_not_its_end(int hour, int minute, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));

        Assert.Equal(expected, window.Contains(new TimeOnly(hour, minute)));
    }

    [Fact]
    public void A_window_ending_at_midnight_does_not_contain_midnight() {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(0, 0));

        Assert.True(window.WrapsMidnight);
        Assert.True(window.Contains(new TimeOnly(23, 30)));
        Assert.False(window.Contains(new TimeOnly(0, 0)));
        Assert.False(window.Contains(new TimeOnly(0, 30)));
    }

    [Fact]
    public void A_window_starting_at_midnight_contains_it() {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(0, 0), new TimeOnly(2, 0));

        Assert.False(window.WrapsMidnight);
        Assert.True(window.Contains(new TimeOnly(0, 0)));
        Assert.False(window.Contains(new TimeOnly(23, 30)));
    }

    [Fact]
    public void Any_time_of_the_night_is_in_a_night_window() {
        OffPeakWindow night      = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));
        TimeOnly      anyAtNight = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(5, 59)).Generate();

        Assert.True(night.Contains(anyAtNight));
    }

    [Fact]
    public void Any_time_of_the_day_is_out_of_a_night_window() {
        OffPeakWindow night      = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));
        TimeOnly      anyByDay   = Any.TimeOnly().Between(new TimeOnly(6, 0), new TimeOnly(21, 59)).Generate();

        Assert.False(night.Contains(anyByDay));
    }

}
