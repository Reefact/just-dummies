using JustDummies;

using Manufacturing.Domain;

using Xunit;

namespace Manufacturing.Tests.Regular;

public sealed class CycleTimeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(3_600)]
    public void Both_ends_of_the_range_are_accepted(int seconds) {
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        Assert.Equal(TimeSpan.FromSeconds(seconds), cycle.Duration);
    }

    [Theory]
    [InlineData(1, 3_600)]
    [InlineData(2, 1_800)]
    [InlineData(7, 514)]        // 514.28 truncated
    [InlineData(59, 61)]        // 61.01 truncated
    [InlineData(60, 60)]
    [InlineData(1_800, 2)]
    [InlineData(2_000, 1)]      // 1.8 truncated
    [InlineData(3_600, 1)]
    public void Parts_per_hour_are_truncated(int seconds, int expectedPartsPerHour) {
        // Arrange
        CycleTime cycle = new CycleTime(TimeSpan.FromSeconds(seconds));

        // Act
        int partsPerHour = cycle.PartsPerHour;

        // Assert
        Assert.Equal(expectedPartsPerHour, partsPerHour);
    }

    [Fact]
    public void A_cycle_shorter_than_a_second_is_refused() {
        // Arrange
        TimeSpan anyTooShort = Any.TimeSpan().LessThan(TimeSpan.FromSeconds(1)).Generate();

        // Act
        Action act = () => new CycleTime(anyTooShort);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_cycle_longer_than_an_hour_is_refused() {
        // Arrange
        TimeSpan anyTooLong = Any.TimeSpan().GreaterThan(TimeSpan.FromHours(1)).Generate();

        // Act
        Action act = () => new CycleTime(anyTooLong);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_cycle_which_is_not_a_whole_number_of_seconds_is_refused() {
        // Arrange
        int      anyWholeSeconds = Any.Int32().Between(1, 3_599).Generate();
        long     anyExtraTicks   = Any.Int64().Between(1, TimeSpan.TicksPerSecond - 1).Generate();
        TimeSpan anyFractional   = TimeSpan.FromSeconds(anyWholeSeconds) + TimeSpan.FromTicks(anyExtraTicks);

        // Act
        Action act = () => new CycleTime(anyFractional);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void The_range_is_checked_before_the_granularity() {
        // Half a second is both too short and not whole: the range error wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromMilliseconds(500)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CycleTime(TimeSpan.FromHours(1) + TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void A_generated_cycle_time_is_a_valid_one() {
        // Arrange
        CycleTime anyCycle = new AnyCycleTime().Generate();

        // Act
        int partsPerHour = anyCycle.PartsPerHour;

        // Assert
        Assert.InRange(partsPerHour, 1, 3_600);
    }

    [Fact]
    public void Two_cycles_with_the_same_duration_are_equal() {
        // Arrange
        CycleTime anyCycle = new AnyCycleTime().Generate();

        // Act
        CycleTime sameCycle = new CycleTime(anyCycle.Duration);

        // Assert
        Assert.Equal(anyCycle, sameCycle);
        Assert.Equal(anyCycle.GetHashCode(), sameCycle.GetHashCode());
    }

    [Fact]
    public void Two_cycles_with_different_durations_are_not_equal() {
        // Arrange
        CycleTime anyCycle       = new AnyCycleTime().Generate();
        TimeSpan  anotherLength  = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromSeconds(1)).DifferentFrom(anyCycle.Duration).Generate();

        // Act
        CycleTime anotherCycle = new CycleTime(anotherLength);

        // Assert
        Assert.NotEqual(anyCycle, anotherCycle);
        Assert.False(anyCycle.Equals(null));
    }

}
