namespace Manufacturing.Domain.Tests.Expert;

public sealed class ShiftWindowTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    [Fact]
    public void A_window_keeps_its_two_instants() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 9, 6, 0, 0, DateTimeKind.Utc);
        DateTime end   = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Utc);

        // Act
        ShiftWindow window = new ShiftWindow(start, end);

        // Assert
        Assert.Equal(start, window.Start);
        Assert.Equal(end, window.End);
    }

    [Fact]
    public void The_length_of_a_window_is_the_time_between_its_instants() {
        // Arrange
        ShiftWindow anyEightHourWindow = Any.ShiftWindow().WithLength(TimeSpan.FromHours(8)).Generate();

        // Act
        TimeSpan length = anyEightHourWindow.Length;

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), length);
    }

    [Fact]
    public void A_window_may_cross_midnight() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 9, 22, 0, 0, DateTimeKind.Utc);
        DateTime end   = new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc);

        // Act
        ShiftWindow window = new ShiftWindow(start, end);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), window.Length);
    }

    [Fact]
    public void A_window_of_exactly_twelve_hours_is_accepted() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart + TimeSpan.FromHours(12));

        // Assert
        Assert.Equal(TimeSpan.FromHours(12), window.Length);
    }

    [Fact]
    public void The_longest_shift_is_twelve_hours() {
        Assert.Equal(TimeSpan.FromHours(12), ShiftWindow.LongestShift);
    }

    [Fact]
    public void A_window_longer_than_twelve_hours_is_refused_even_by_one_tick() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).Generate();

        // Act
        Action create = () => new ShiftWindow(anyStart, anyStart + TimeSpan.FromHours(12) + OneTick);

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(create);
        Assert.Equal("end", error.ParamName);
    }

    [Fact]
    public void A_window_ending_where_it_starts_is_refused() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).Generate();

        // Act
        Action create = () => new ShiftWindow(anyInstant, anyInstant);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_window_ending_before_it_starts_is_refused() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).Generate();

        // Act
        Action create = () => new ShiftWindow(anyStart, anyStart - OneTick);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_window_of_one_tick_is_accepted() {
        // Arrange
        DateTime anyStart = Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).Generate();

        // Act
        ShiftWindow window = new ShiftWindow(anyStart, anyStart + OneTick);

        // Assert
        Assert.Equal(OneTick, window.Length);
    }

    [Fact]
    public void The_kind_of_the_instants_is_not_looked_at() {
        // Arrange
        DateTime start = new DateTime(2026, 3, 9, 6, 0, 0, DateTimeKind.Utc);
        DateTime end   = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Unspecified);

        // Act
        ShiftWindow window = new ShiftWindow(start, end);

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), window.Length);
    }

}
