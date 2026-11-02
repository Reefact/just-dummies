using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class CompensationPolicyTests {

    private static Flight FlightOver(int distanceKm) {
        return Arbitrary.Flight().WithRoute(r => r.WithDistanceKm(distanceKm)).Generate();
    }

    [Fact]
    public void A_null_flight_is_refused() {
        Assert.Throws<ArgumentNullException>(() => CompensationPolicy.CompensationFor(null!));
    }

    [Fact]
    public void A_flight_without_delay_earns_nothing() {
        // Arrange
        Flight anyFlight = Arbitrary.Flight().Generate();

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(anyFlight));
    }

    [Theory]
    [InlineData(50, 250)]
    [InlineData(1_499, 250)]
    [InlineData(1_500, 250)]
    [InlineData(1_501, 400)]
    [InlineData(3_499, 400)]
    [InlineData(3_500, 400)]
    [InlineData(3_501, 600)]
    [InlineData(20_000, 600)]
    public void A_flight_three_hours_late_earns_the_band_of_its_distance(int distanceKm, int expected) {
        // Arrange
        Flight flight = FlightOver(distanceKm);
        flight.Delay(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal((decimal)expected, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_delay_of_exactly_three_hours_is_enough() {
        // Arrange
        Flight flight = FlightOver(2_000);
        flight.Delay(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal(400m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_delay_one_tick_short_of_three_hours_earns_nothing() {
        // Arrange
        Flight flight = FlightOver(2_000);
        flight.Delay(TimeSpan.FromHours(3).Subtract(TimeSpan.FromTicks(1)));

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_delay_of_two_hours_fifty_nine_earns_nothing() {
        // Arrange
        Flight flight = FlightOver(5_000);
        flight.Delay(TimeSpan.FromMinutes(179));

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void Delays_are_added_before_the_threshold_is_compared() {
        // Arrange
        Flight flight = FlightOver(5_000);
        flight.Delay(TimeSpan.FromMinutes(100));
        flight.Delay(TimeSpan.FromMinutes(80));

        // Assert
        Assert.Equal(600m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_much_longer_delay_earns_the_same_band() {
        // Arrange
        Flight flight = FlightOver(1_000);
        flight.Delay(TimeSpan.FromHours(30));

        // Assert
        Assert.Equal(250m, CompensationPolicy.CompensationFor(flight));
    }

    [Theory]
    [InlineData(800, 250)]
    [InlineData(1_500, 250)]
    [InlineData(1_501, 400)]
    [InlineData(3_500, 400)]
    [InlineData(3_501, 600)]
    public void A_cancelled_flight_earns_the_band_of_its_distance_without_any_delay(int distanceKm, int expected) {
        // Arrange
        Flight flight = FlightOver(distanceKm);
        flight.Cancel();

        // Assert
        Assert.Equal((decimal)expected, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_cancelled_flight_earns_the_band_whatever_its_delay() {
        // Arrange
        Flight shortDelay = FlightOver(2_000);
        shortDelay.Delay(TimeSpan.FromMinutes(20));
        shortDelay.Cancel();

        Flight longDelay = FlightOver(2_000);
        longDelay.Delay(TimeSpan.FromHours(5));
        longDelay.Cancel();

        // Assert
        Assert.Equal(400m, CompensationPolicy.CompensationFor(shortDelay));
        Assert.Equal(400m, CompensationPolicy.CompensationFor(longDelay));
    }

    [Fact]
    public void A_cancelled_flight_is_paid_once_not_twice() {
        // Arrange
        Flight flight = FlightOver(2_000);
        flight.Delay(TimeSpan.FromHours(4));
        flight.Cancel();
        flight.Cancel();

        // Assert
        Assert.Equal(400m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void An_arrived_flight_that_was_three_hours_late_earns_the_band() {
        // Arrange
        Flight flight = FlightOver(4_000);
        flight.StartBoarding();
        flight.Delay(TimeSpan.FromHours(3));
        flight.Depart();
        flight.Arrive();

        // Assert
        Assert.Equal(600m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void An_arrived_flight_that_was_on_time_earns_nothing() {
        // Arrange
        Flight flight = FlightOver(4_000);
        flight.StartBoarding();
        flight.Depart();
        flight.Arrive();

        // Assert
        Assert.Equal(0m, CompensationPolicy.CompensationFor(flight));
    }

    [Fact]
    public void A_boarding_flight_already_three_hours_late_earns_the_band() {
        // Arrange
        Flight flight = FlightOver(1_000);
        flight.StartBoarding();
        flight.Delay(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal(250m, CompensationPolicy.CompensationFor(flight));
    }

}
