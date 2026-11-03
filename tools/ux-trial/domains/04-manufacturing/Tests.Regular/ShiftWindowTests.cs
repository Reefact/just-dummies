using JustDummies;

using Manufacturing.Domain;

using Xunit;

namespace Manufacturing.Tests.Regular;

public sealed class ShiftWindowTests {

    private static AnyDateTime AnyInstant() {
        return Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1));
    }

    [Fact]
    public void The_length_is_the_distance_between_start_and_end() {
        // Arrange
        DateTime anyStart = AnyInstant().Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart.AddHours(8));

        // Assert
        Assert.Equal(anyStart, window.Start);
        Assert.Equal(anyStart.AddHours(8), window.End);
        Assert.Equal(TimeSpan.FromHours(8), window.Length);
    }

    [Fact]
    public void A_shift_of_exactly_twelve_hours_is_accepted() {
        // Arrange
        DateTime anyStart = AnyInstant().Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart + ShiftWindow.LongestShift);

        // Assert
        Assert.Equal(TimeSpan.FromHours(12), window.Length);
    }

    [Fact]
    public void A_shift_one_tick_over_twelve_hours_is_refused() {
        // Arrange
        DateTime anyStart = AnyInstant().Generate();

        // Act
        Action act = () => new ShiftWindow(anyStart, anyStart + ShiftWindow.LongestShift + TimeSpan.FromTicks(1));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_shift_longer_than_twelve_hours_is_refused() {
        // Arrange
        DateTime anyStart   = AnyInstant().Generate();
        TimeSpan anyTooLong = Any.TimeSpan().GreaterThan(ShiftWindow.LongestShift).LessThan(TimeSpan.FromDays(30)).Generate();

        // Act
        Action act = () => new ShiftWindow(anyStart, anyStart + anyTooLong);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_shift_must_start_strictly_before_it_ends() {
        // Arrange
        DateTime anyInstant = AnyInstant().Generate();

        // Act
        Action act = () => new ShiftWindow(anyInstant, anyInstant);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_shift_ending_before_it_starts_is_refused() {
        // Arrange
        DateTime anyEnd   = AnyInstant().Generate();
        TimeSpan anyShift = Any.TimeSpan().Between(TimeSpan.FromTicks(1), ShiftWindow.LongestShift).Generate();

        // Act
        Action act = () => new ShiftWindow(anyEnd + anyShift, anyEnd);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void The_kind_of_the_instants_is_not_checked() {
        // Arrange
        DateTime utcStart       = new DateTime(2026, 3, 2, 6, 0, 0, DateTimeKind.Utc);
        DateTime unspecifiedEnd = new DateTime(2026, 3, 2, 14, 0, 0, DateTimeKind.Unspecified);

        // Act
        ShiftWindow window = new ShiftWindow(utcStart, unspecifiedEnd);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), window.Length);
    }

    [Fact]
    public void A_generated_window_is_a_valid_one() {
        // Arrange
        ShiftWindow anyWindow = new AnyShiftWindow().Generate();

        // Assert
        Assert.True(anyWindow.Start < anyWindow.End);
        Assert.True(anyWindow.Length <= ShiftWindow.LongestShift);
    }

}
