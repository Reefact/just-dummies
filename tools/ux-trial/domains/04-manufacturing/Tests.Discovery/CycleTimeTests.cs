using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class CycleTimeTests {

    [Fact]
    public void Both_ends_of_the_range_are_accepted() {
        Assert.Equal(TimeSpan.FromSeconds(1), new CycleTime(TimeSpan.FromSeconds(1)).Duration);
        Assert.Equal(TimeSpan.FromHours(1), new CycleTime(TimeSpan.FromHours(1)).Duration);
    }

    [Fact]
    public void A_cycle_exposes_the_duration_it_was_given() {
        // Act
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(45));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(45), cycle.Duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void A_zero_or_negative_cycle_is_refused_as_too_short(int seconds) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void A_cycle_shorter_than_a_second_is_refused_as_too_short_even_though_it_is_not_whole() {
        // The range is checked before the granularity.
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromMilliseconds(500)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromTicks(TimeSpan.TicksPerSecond - 1)));
    }

    [Fact]
    public void A_cycle_longer_than_an_hour_is_refused_as_too_long_even_though_it_is_not_whole() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromHours(1) + TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.MaxValue));
    }

    [Theory]
    [InlineData(1_500)]
    [InlineData(1_001)]
    [InlineData(59_999)]
    public void A_cycle_that_is_not_a_whole_number_of_seconds_is_refused(int milliseconds) {
        Assert.Throws<ArgumentException>(() => new CycleTime(TimeSpan.FromMilliseconds(milliseconds)));
    }

    [Fact]
    public void One_tick_off_a_whole_second_is_refused() {
        Assert.Throws<ArgumentException>(() => new CycleTime(TimeSpan.FromSeconds(2) + TimeSpan.FromTicks(1)));
    }

    [Theory]
    [InlineData(1, 3_600)]
    [InlineData(2, 1_800)]
    [InlineData(3, 1_200)]
    [InlineData(7, 514)]        // 514.28, truncated
    [InlineData(1_799, 2)]      // 2.0011, truncated
    [InlineData(1_801, 1)]      // 1.9989, truncated
    [InlineData(3_599, 1)]
    [InlineData(3_600, 1)]
    public void The_parts_per_hour_are_truncated(int seconds, int expectedParts) {
        // Arrange
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(expectedParts, cycle.PartsPerHour);
    }

    [Fact]
    public void Two_cycles_of_the_same_duration_are_equal() {
        // Arrange
        TimeSpan anyDuration = Any.CycleTime().Generate().Duration;

        // Act
        CycleTime first  = new CycleTime(anyDuration);
        CycleTime second = new CycleTime(anyDuration);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_cycles_of_different_durations_are_different() {
        Assert.NotEqual(new CycleTime(TimeSpan.FromSeconds(30)), new CycleTime(TimeSpan.FromSeconds(31)));
        Assert.False(new CycleTime(TimeSpan.FromSeconds(30)).Equals(null));
    }

}
