using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class CompensationPolicyTests {

    [Fact]
    public void A_null_flight_is_refused() {
        Assert.Throws<ArgumentNullException>(() => CompensationPolicy.CompensationFor(null!));
    }

    [Theory]
    [InlineData(50, 250)]
    [InlineData(1_500, 250)]
    [InlineData(1_501, 400)]
    [InlineData(3_500, 400)]
    [InlineData(3_501, 600)]
    [InlineData(20_000, 600)]
    public void A_flight_three_hours_late_earns_the_band_of_its_distance(int distanceKm, int expected) {
        // Arrange
        Flight flight = Any.Flight().WithRoute(r => r.WithDistanceKm(distanceKm)).Generate();
        flight.Delay(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal(expected, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_delay_of_exactly_three_hours_is_included_and_a_tick_less_is_not() {
        // Arrange
        Flight exactly  = Any.Flight().WithRoute(r => r.WithDistanceKm(1_000)).Generate();
        Flight justUnder = Any.Flight().WithRoute(r => r.WithDistanceKm(1_000)).Generate();
        exactly.Delay(TimeSpan.FromHours(3));
        justUnder.Delay(TimeSpan.FromHours(3) - TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(250m, CompensationPolicy.CompensationFor(exactly));
        Assert.Equal(0m, CompensationPolicy.CompensationFor(justUnder));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(179)]
    public void A_flight_less_than_three_hours_late_earns_nothing(int delayMinutes) {
        // Arrange
        Flight flight = Any.Flight().Generate();
        if (delayMinutes > 0) { flight.Delay(TimeSpan.FromMinutes(delayMinutes)); }

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void Several_delays_count_together_towards_the_three_hours() {
        // Arrange
        Flight flight = Any.Flight().WithRoute(r => r.WithDistanceKm(2_000)).Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(100));
        flight.Delay(TimeSpan.FromMinutes(80));

        // Assert
        Assert.Equal(400m, CompensationPolicy.CompensationFor(flight));
    }

    [Theory]
    [InlineData(0, 1_000, 250)]
    [InlineData(10, 2_000, 400)]
    [InlineData(300, 5_000, 600)]
    [InlineData(0, 1_500, 250)]
    [InlineData(0, 1_501, 400)]
    [InlineData(0, 3_500, 400)]
    [InlineData(0, 3_501, 600)]
    public void A_cancelled_flight_earns_the_band_whatever_its_delay(int delayMinutes, int distanceKm, int expected) {
        // Arrange
        Flight flight = Any.Flight().WithRoute(r => r.WithDistanceKm(distanceKm)).Generate();
        if (delayMinutes > 0) { flight.Delay(TimeSpan.FromMinutes(delayMinutes)); }
        flight.Cancel();

        // Assert
        Assert.Equal(expected, CompensationPolicy.CompensationFor(flight));
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    public void A_flight_still_to_leave_that_is_three_hours_late_earns_the_band(FlightStatus status) {
        // Arrange
        Flight flight = Any.Flight().WithRoute(r => r.WithDistanceKm(2_000)).Generate();
        if (status == FlightStatus.Boarding) { flight.StartBoarding(); }
        flight.Delay(TimeSpan.FromHours(4));

        // Assert
        Assert.Equal(400m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_flight_that_departed_or_arrived_three_hours_late_earns_the_band() {
        // Arrange
        Flight departed = Any.Flight().WithRoute(r => r.WithDistanceKm(4_000)).Generate();
        departed.Delay(TimeSpan.FromHours(3));
        departed.StartBoarding();
        departed.Depart();

        Flight arrived = Any.Flight().WithRoute(r => r.WithDistanceKm(4_000)).Generate();
        arrived.Delay(TimeSpan.FromHours(5));
        arrived.StartBoarding();
        arrived.Depart();
        arrived.Arrive();

        // Assert
        Assert.Equal(600m, CompensationPolicy.CompensationFor(departed));
        Assert.Equal(600m, CompensationPolicy.CompensationFor(arrived));
    }

    [Fact]
    public void A_flight_that_arrived_on_time_earns_nothing() {
        // Arrange
        Flight flight = FlightTests.FlightIn(FlightStatus.Arrived);

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(flight));
    }

}
