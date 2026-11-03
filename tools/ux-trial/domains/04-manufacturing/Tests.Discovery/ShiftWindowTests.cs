using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class ShiftWindowTests {

    [Fact]
    public void The_longest_shift_is_twelve_hours() {
        Assert.Equal(TimeSpan.FromHours(12), ShiftWindow.LongestShift);
    }

    [Fact]
    public void The_length_is_the_end_minus_the_start() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        ShiftWindow window = new ShiftWindow(anyStart, anyStart.AddHours(8).AddMinutes(30));

        // Assert
        Assert.Equal(anyStart, window.Start);
        Assert.Equal(anyStart.AddHours(8).AddMinutes(30), window.End);
        Assert.Equal(TimeSpan.FromHours(8.5), window.Length);
    }

    [Fact]
    public void A_shift_of_exactly_twelve_hours_is_accepted() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart.AddHours(12));

        // Assert
        Assert.Equal(TimeSpan.FromHours(12), window.Length);
    }

    [Fact]
    public void A_shift_longer_than_twelve_hours_is_refused() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart.AddHours(12).AddTicks(1)));
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart.AddHours(12).AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart.AddDays(3)));
    }

    [Fact]
    public void A_shift_that_does_not_end_after_it_starts_is_refused() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart));
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart.AddTicks(-1)));
        Assert.Throws<ArgumentException>(() => new ShiftWindow(anyStart, anyStart.AddHours(-8)));
    }

    [Fact]
    public void A_one_tick_shift_is_accepted() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)).Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart.AddTicks(1));

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), window.Length);
    }

    [Fact]
    public void The_kind_of_the_dates_is_not_checked() {
        // Arrange
        DateTime utc   = new DateTime(2026, 3, 1, 6, 0, 0, DateTimeKind.Utc);
        DateTime local = new DateTime(2026, 3, 1, 14, 0, 0, DateTimeKind.Local);

        // Act
        ShiftWindow window = new ShiftWindow(utc, local);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), window.Length);
    }

}
