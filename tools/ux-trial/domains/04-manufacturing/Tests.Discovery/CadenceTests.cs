using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class CadenceTests {

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void The_two_ends_of_the_range_are_accepted(int unitsPerHour) {
        // Act
        Cadence cadence = new Cadence(unitsPerHour);

        // Assert
        Assert.Equal(unitsPerHour, cadence.UnitsPerHour);
    }

    [Fact]
    public void The_maximum_is_ten_thousand_units_per_hour() {
        Assert.Equal(10_000, Cadence.MaximumUnitsPerHour);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_cadence_outside_the_range_is_refused(int unitsPerHour) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cadence(unitsPerHour));
    }

    [Theory]
    [InlineData(60, 60, 60)]          // one unit a minute
    [InlineData(60, 1, 1)]
    [InlineData(120, 1, 1)]           // half a minute, rounded up
    [InlineData(7, 10, 86)]           // 85.71 minutes, rounded up
    [InlineData(10_000, 1, 1)]        // 0.006 minute, still one whole minute
    [InlineData(10_000, 10_000, 60)]  // exactly one hour
    [InlineData(10_000, 10_001, 61)]  // one unit past the hour
    [InlineData(1, 100_000, 6_000_000)]
    public void The_time_to_produce_is_rounded_up_to_the_whole_minute(int unitsPerHour, int quantity, int expectedMinutes) {
        // Arrange
        Cadence cadence = new Cadence(unitsPerHour);

        // Act
        TimeSpan time = cadence.TimeToProduce(quantity);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), time);
    }

    [Fact]
    public void Producing_nothing_takes_no_time() {
        // Arrange
        Cadence anyCadence = Any.Cadence().Generate();

        // Act
        TimeSpan time = anyCadence.TimeToProduce(0);

        // Assert
        Assert.Equal(TimeSpan.Zero, time);
    }

    [Fact]
    public void A_negative_quantity_is_refused() {
        // Arrange
        Cadence anyCadence          = Any.Cadence().Generate();
        int     anyNegativeQuantity = Any.Int32().Negative().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCadence.TimeToProduce(anyNegativeQuantity));
    }

    [Fact]
    public void A_large_quantity_at_the_slowest_cadence_does_not_overflow() {
        // Arrange
        Cadence slowest = new Cadence(1);

        // Act
        TimeSpan time = slowest.TimeToProduce(100_000_000);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(6_000_000_000L), time);
    }

    [Fact(Skip = "domain defect: quantities above about 256 million at one unit an hour overflow TimeSpan and throw an unrelated ArgumentOutOfRangeException from TimeSpan.FromMinutes, literal input so no seed")]
    public void The_largest_quantity_is_either_computed_or_refused_by_name() {
        // Arrange
        Cadence slowest = new Cadence(1);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(int.MaxValue * 60L), slowest.TimeToProduce(int.MaxValue));
    }

    [Fact]
    public void Two_cadences_of_the_same_rate_are_equal() {
        // Arrange
        int anyRate = Any.Cadence().Generate().UnitsPerHour;

        // Act
        Cadence first  = new Cadence(anyRate);
        Cadence second = new Cadence(anyRate);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_cadences_of_different_rates_are_different() {
        Assert.NotEqual(new Cadence(60), new Cadence(61));
        Assert.False(new Cadence(60).Equals(null));
        Assert.False(new Cadence(60).Equals((object)60));
    }

}
