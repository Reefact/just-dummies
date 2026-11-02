using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Entities;

public sealed class CompensationPolicyTests {

    private static readonly TimeSpan ThreeHours = TimeSpan.FromHours(3);

    // The distance is what the band depends on, so it is spelled out wherever an amount is asserted.
    private static AnyFlight FlightOver(int distanceKm) {
        return Any.Flight().WithRoute(r => r.WithDistanceKm(distanceKm));
    }

    [Fact]
    public void A_missing_flight_is_refused() {
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => CompensationPolicy.CompensationFor(null!));

        Assert.Equal("flight", refusal.ParamName);
    }

    [Theory]
    [InlineData(50, 250)]
    [InlineData(1_000, 250)]
    [InlineData(1_500, 250)]
    [InlineData(1_501, 400)]
    [InlineData(2_500, 400)]
    [InlineData(3_500, 400)]
    [InlineData(3_501, 600)]
    [InlineData(8_000, 600)]
    [InlineData(20_000, 600)]
    public void A_cancelled_flight_earns_the_band_of_its_distance(int distanceKm, int expected) {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(distanceKm), FlightStatus.Cancelled);

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal((decimal)expected, compensation);
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
        Flight flight = Fixtures.FlightIn(FlightOver(distanceKm), FlightStatus.Scheduled, ThreeHours);

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal((decimal)expected, compensation);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    public void Exactly_three_hours_of_delay_earns_compensation_whatever_the_status(FlightStatus status) {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(2_000), status, ThreeHours);

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(400m, compensation);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    public void A_second_short_of_three_hours_earns_nothing_whatever_the_status(FlightStatus status) {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(2_000), status, ThreeHours - TimeSpan.FromSeconds(1));

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(0m, compensation);
    }

    [Fact]
    public void A_flight_that_was_not_delayed_earns_nothing() {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(8_000), FlightStatus.Arrived);

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(0m, compensation);
    }

    [Fact]
    public void Delays_that_add_up_to_three_hours_earn_compensation() {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(2_000), FlightStatus.Boarding, TimeSpan.FromHours(2), TimeSpan.FromHours(1));

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(400m, compensation);
    }

    [Fact]
    public void A_very_late_flight_earns_the_band_once_not_more() {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(2_000), FlightStatus.Arrived, TimeSpan.FromHours(30));

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(400m, compensation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(600)]
    [InlineData(10_799)]
    [InlineData(10_800)]
    [InlineData(50_000)]
    public void A_cancelled_flight_earns_the_band_whatever_its_delay_was(int delaySeconds) {
        // Arrange
        Flight flight = delaySeconds == 0
            ? Fixtures.FlightIn(FlightOver(2_000), FlightStatus.Cancelled)
            : Fixtures.FlightIn(FlightOver(2_000), FlightStatus.Cancelled, TimeSpan.FromSeconds(delaySeconds));

        // Act
        decimal compensation = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(400m, compensation);
    }

    [Fact]
    public void The_compensation_is_read_when_asked_so_a_later_delay_changes_it() {
        // Arrange
        Flight flight = Fixtures.FlightIn(FlightOver(2_000), FlightStatus.Scheduled, TimeSpan.FromHours(2));

        // Act
        decimal before = CompensationPolicy.CompensationFor(flight);
        flight.Delay(TimeSpan.FromHours(1));
        decimal after = CompensationPolicy.CompensationFor(flight);

        // Assert
        Assert.Equal(0m, before);
        Assert.Equal(400m, after);
    }

}
