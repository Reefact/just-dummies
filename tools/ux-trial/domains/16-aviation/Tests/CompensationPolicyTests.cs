using Aviation.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Aviation.Tests;

[Reproducible]
public sealed class CompensationPolicyTests {

    [Fact]
    public void A_long_haul_flight_three_hours_late_is_owed_600() {
        // Arrange
        Flight anyLongHaulFlight = Any.Flight().WithRoute(route => route.WithDistanceKm(5_800)).Generate();
        anyLongHaulFlight.Delay(TimeSpan.FromHours(3));

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(anyLongHaulFlight);

        // Assert
        Assert.Equal(600m, compensation);
    }

    [Fact]
    public void A_flight_less_than_three_hours_late_is_owed_nothing_whatever_its_distance() {
        // Arrange
        Flight   anyFlight = Any.Flight().Generate();
        TimeSpan anyDelay  = Any.TimeSpan().Positive().LessThan(TimeSpan.FromHours(3)).Generate();
        anyFlight.Delay(anyDelay);

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(anyFlight);

        // Assert
        Assert.Equal(0m, compensation);
    }

    [Theory]
    [InlineData(1_500, 250)]
    [InlineData(1_501, 400)]
    [InlineData(3_500, 400)]
    [InlineData(3_501, 600)]
    public void A_cancelled_flight_is_owed_the_band_of_its_distance(int distanceKm, int expected) {
        // Arrange
        Flight anyFlight = Any.Flight().WithRoute(route => route.WithDistanceKm(distanceKm)).Generate();
        anyFlight.Cancel();

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(anyFlight);

        // Assert
        Assert.Equal(expected, compensation);
    }

}
