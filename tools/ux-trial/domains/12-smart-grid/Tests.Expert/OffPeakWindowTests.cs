using System.Globalization;

using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

public sealed class OffPeakWindowTests {

    [Fact]
    public void Carries_the_start_and_the_end_it_was_built_with() {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 30));

        Assert.Equal(new TimeOnly(22, 0), window.Start);
        Assert.Equal(new TimeOnly(6, 30), window.End);
    }

    [Theory]
    [InlineData("22:00", "06:00", true)]
    [InlineData("23:30", "00:00", true)]  // ends exactly at midnight, yet starts later than it ends
    [InlineData("12:00", "00:00", true)]
    [InlineData("06:00", "22:00", false)]
    [InlineData("00:00", "23:30", false)]
    [InlineData("00:00", "00:30", false)]
    [InlineData("00:30", "01:00", false)]
    public void Wraps_midnight_when_it_ends_earlier_than_it_starts(string start, string end, bool expected) {
        OffPeakWindow window = new OffPeakWindow(Time(start), Time(end));

        Assert.Equal(expected, window.WrapsMidnight);
    }

    [Theory]
    [InlineData("07:30", false)]
    [InlineData("07:59", false)]
    [InlineData("08:00", true)]  // start is inclusive
    [InlineData("08:30", true)]
    [InlineData("11:30", true)]
    [InlineData("11:59", true)]
    [InlineData("12:00", false)] // end is exclusive
    [InlineData("12:30", false)]
    [InlineData("00:00", false)]
    [InlineData("23:59", false)]
    public void A_window_within_one_day_contains_the_slots_from_its_start_up_to_its_end_excluded(string slotStart, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(8, 0), new TimeOnly(12, 0));

        Assert.Equal(expected, window.Contains(Time(slotStart)));
    }

    [Theory]
    [InlineData("21:30", false)]
    [InlineData("21:59", false)]
    [InlineData("22:00", true)]  // start is inclusive
    [InlineData("23:30", true)]
    [InlineData("23:59", true)]
    [InlineData("00:00", true)]  // across midnight
    [InlineData("03:00", true)]
    [InlineData("05:30", true)]
    [InlineData("05:59", true)]
    [InlineData("06:00", false)] // end is exclusive
    [InlineData("06:30", false)]
    [InlineData("12:00", false)]
    public void A_window_wrapping_midnight_contains_the_slots_after_its_start_and_before_its_end(string slotStart, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));

        Assert.Equal(expected, window.Contains(Time(slotStart)));
    }

    [Theory]
    [InlineData("23:30", true)]
    [InlineData("23:59", true)]
    [InlineData("00:00", false)] // the window ending at midnight stops before it
    [InlineData("23:00", false)]
    public void A_window_ending_at_midnight_stops_short_of_the_next_day(string slotStart, bool expected) {
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(23, 30), new TimeOnly(0, 0));

        Assert.Equal(expected, window.Contains(Time(slotStart)));
    }

    [Fact]
    public void Contains_its_own_start_and_not_its_own_end_whatever_the_window() {
        // Arrange: the window's actual hours do not matter, only the relation between its edges and itself
        OffPeakWindow anyWindow = Any.OffPeakWindow().Generate();

        // Assert
        Assert.True(anyWindow.Contains(anyWindow.Start));
        Assert.False(anyWindow.Contains(anyWindow.End));
    }

    [Fact]
    public void Refuses_an_empty_window() {
        TimeOnly anyBoundary = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, anyBoundary));

        Assert.Equal("end", refusal.ParamName);
        Assert.Contains("empty", refusal.Message);
    }

    [Theory]
    [InlineData("08:01")]
    [InlineData("08:15")]
    [InlineData("08:29")]
    [InlineData("08:31")]
    public void Refuses_a_start_that_is_not_on_a_half_hour_boundary(string start) {
        TimeOnly anyEnd = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OffPeakWindow(Time(start), anyEnd));

        Assert.Equal("start", refusal.ParamName);
    }

    [Theory]
    [InlineData("12:01")]
    [InlineData("12:15")]
    [InlineData("12:45")]
    public void Refuses_an_end_that_is_not_on_a_half_hour_boundary(string end) {
        TimeOnly anyStart = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyStart, Time(end)));

        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_boundary_a_single_second_off_the_half_hour() {
        TimeOnly anyEnd = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(8, 0, 1), anyEnd));
    }

    [Fact]
    public void Refuses_a_boundary_a_single_tick_off_the_half_hour() {
        TimeOnly anyEnd = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(new TimeOnly(8, 0).Ticks + 1), anyEnd));
    }

    [Fact]
    public void Reports_equal_boundaries_before_misaligned_ones() {
        // Both rules are broken: the emptiness is reported first, against "end"
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(8, 15), new TimeOnly(8, 15)));

        Assert.Equal("end", refusal.ParamName);
    }

    private static TimeOnly Time(string text) {
        return TimeOnly.Parse(text, CultureInfo.InvariantCulture);
    }

}
