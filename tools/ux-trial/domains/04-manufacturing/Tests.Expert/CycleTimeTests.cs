namespace Manufacturing.Domain.Tests.Expert;

public sealed class CycleTimeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(59)]
    [InlineData(3_599)]
    [InlineData(3_600)]
    public void A_cycle_of_whole_seconds_from_one_second_to_one_hour_is_accepted(int seconds) {
        // Act
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), cycle.Duration);
    }

    [Fact]
    public void A_cycle_shorter_than_one_second_is_refused_even_by_one_tick() {
        // Act
        Action create = () => new CycleTime(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(create);
    }

    [Fact]
    public void A_cycle_longer_than_one_hour_is_refused_even_by_one_tick() {
        // Act
        Action create = () => new CycleTime(TimeSpan.FromHours(1) + TimeSpan.FromTicks(1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(create);
    }

    [Fact]
    public void A_zero_or_negative_cycle_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromSeconds(-5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.MinValue));
    }

    [Fact]
    public void A_cycle_with_a_fraction_of_a_second_is_refused_as_an_argument_error() {
        // Act
        Action create = () => new CycleTime(TimeSpan.FromMilliseconds(1_500));

        // Assert: ArgumentException itself, not its out-of-range subtype
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_cycle_out_of_range_is_reported_as_out_of_range_before_its_fraction_is_looked_at() {
        // Act
        Action create = () => new CycleTime(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(create);
    }

    [Theory]
    [InlineData(1, 3_600)]
    [InlineData(2, 1_800)]
    [InlineData(60, 60)]
    [InlineData(3_600, 1)]
    public void The_parts_per_hour_of_a_cycle_dividing_the_hour_is_exact(int seconds, int expectedPartsPerHour) {
        // Arrange
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(expectedPartsPerHour, cycle.PartsPerHour);
    }

    [Theory]
    [InlineData(7, 514)]       // 514.28
    [InlineData(11, 327)]      // 327.27
    [InlineData(1_799, 2)]     // 2.001
    [InlineData(1_801, 1)]     // 1.999: truncated, not rounded
    [InlineData(3_599, 1)]     // 1.0003
    public void The_parts_per_hour_of_any_other_cycle_is_truncated(int seconds, int expectedPartsPerHour) {
        // Arrange
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(expectedPartsPerHour, cycle.PartsPerHour);
    }

    [Fact]
    public void Two_cycles_of_the_same_duration_are_equal() {
        // Arrange
        CycleTime first  = new CycleTime(TimeSpan.FromSeconds(45));
        CycleTime second = new CycleTime(TimeSpan.FromSeconds(45));

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_cycles_of_different_durations_are_different() {
        Assert.NotEqual(new CycleTime(TimeSpan.FromSeconds(45)), new CycleTime(TimeSpan.FromSeconds(46)));
    }

    [Fact]
    public void A_cycle_is_not_equal_to_nothing() {
        // Arrange
        CycleTime anyCycle = Any.CycleTime().Generate();

        // Assert
        Assert.False(anyCycle.Equals((CycleTime?)null));
    }

}
