using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Regular;

public sealed class OffPeakWindowTests {

    private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);

    [Fact]
    public void A_window_keeps_its_two_bounds() {
        // Act
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 30));

        // Assert
        Assert.Equal(new TimeOnly(22, 0), window.Start);
        Assert.Equal(new TimeOnly(6, 30), window.End);
    }

    [Fact]
    public void An_empty_window_is_refused() {
        // Arrange
        TimeOnly anyBoundary = Any.TimeOnly().WithGranularity(HalfHour).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, anyBoundary));
    }

    [Theory]
    [InlineData(22, 15)]
    [InlineData(22, 1)]
    [InlineData(22, 45)]
    public void A_start_off_the_half_hour_is_refused(int hour, int minute) {
        // Arrange
        TimeOnly anyEnd = Any.TimeOnly().WithGranularity(HalfHour).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(hour, minute), anyEnd));
    }

    [Theory]
    [InlineData(6, 15)]
    [InlineData(6, 1)]
    [InlineData(6, 59)]
    public void An_end_off_the_half_hour_is_refused(int hour, int minute) {
        // Arrange
        TimeOnly anyStart = Any.TimeOnly().WithGranularity(HalfHour).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyStart, new TimeOnly(hour, minute)));
    }

    [Fact]
    public void A_boundary_with_a_stray_second_or_tick_is_refused() {
        // Arrange
        TimeOnly anyBoundary = Any.TimeOnly().WithGranularity(HalfHour).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(22, 0, 1), anyBoundary));
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, new TimeOnly(6, 0, 1)));
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(new TimeOnly(22, 0).Add(TimeSpan.FromTicks(1)), anyBoundary));
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, new TimeOnly(6, 0).Add(TimeSpan.FromTicks(1))));
    }

    [Theory]
    [InlineData(22, 0, 6, 0, true)]
    [InlineData(23, 30, 0, 0, true)]
    [InlineData(12, 30, 12, 0, true)]
    [InlineData(6, 0, 22, 0, false)]
    [InlineData(0, 0, 23, 30, false)]
    [InlineData(0, 0, 0, 30, false)]
    public void A_window_wraps_midnight_when_its_end_is_before_its_start(int startHour, int startMinute, int endHour, int endMinute, bool expected) {
        // Act
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

        // Assert
        Assert.Equal(expected, window.WrapsMidnight);
    }

    [Theory]
    [InlineData(6, 0, true)]
    [InlineData(6, 30, true)]
    [InlineData(21, 30, true)]
    [InlineData(22, 0, false)]
    [InlineData(23, 30, false)]
    [InlineData(5, 30, false)]
    [InlineData(0, 0, false)]
    public void A_daytime_window_contains_its_start_but_not_its_end(int hour, int minute, bool expected) {
        // Arrange
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(6, 0), new TimeOnly(22, 0));

        // Assert
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
    public void A_window_that_wraps_midnight_contains_both_sides_of_it(int hour, int minute, bool expected) {
        // Arrange
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Assert
        Assert.Equal(expected, window.Contains(new TimeOnly(hour, minute)));
    }

    [Theory]
    [InlineData(23, 30, true)]
    [InlineData(22, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(21, 30, false)]
    public void A_window_ending_at_midnight_runs_to_the_end_of_the_day(int hour, int minute, bool expected) {
        // Arrange: 00:00 is before 22:00, so this window "wraps" and ends at the very start of the day.
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(0, 0));

        // Assert
        Assert.True(window.WrapsMidnight);
        Assert.Equal(expected, window.Contains(new TimeOnly(hour, minute)));
    }

    [Fact]
    public void A_one_slot_window_contains_only_that_slot() {
        // Arrange
        OffPeakWindow window = new OffPeakWindow(new TimeOnly(12, 30), new TimeOnly(13, 0));

        // Assert
        Assert.True(window.Contains(new TimeOnly(12, 30)));
        Assert.False(window.Contains(new TimeOnly(12, 0)));
        Assert.False(window.Contains(new TimeOnly(13, 0)));
    }

    [Fact]
    public void A_slot_drawn_inside_a_night_window_is_contained() {
        // Arrange
        OffPeakWindow window         = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));
        TimeOnly      anyNightSlot   = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Before(new TimeOnly(6, 0)).WithGranularity(HalfHour).Generate();

        // Assert
        Assert.True(window.Contains(anyNightSlot));
    }

    [Fact]
    public void A_slot_drawn_inside_the_day_is_not_in_a_night_window() {
        // Arrange
        OffPeakWindow window       = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));
        TimeOnly      anyDaySlot   = Any.TimeOnly().Between(new TimeOnly(6, 0), new TimeOnly(21, 30)).WithGranularity(HalfHour).Generate();

        // Assert
        Assert.False(window.Contains(anyDaySlot));
    }

}
