using Aviation.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Aviation.Tests;

[Reproducible]
public sealed class FlightTests {

    [Fact]
    public void Delays_accumulate_into_the_estimated_departure() {
        // Arrange
        DateTime scheduledDeparture = new DateTime(2026, 7, 14, 9, 30, 0, DateTimeKind.Utc);
        Flight   anyFlight          = Any.Flight().WithScheduledDeparture(scheduledDeparture).Generate();

        // Act
        anyFlight.Delay(TimeSpan.FromMinutes(20));
        anyFlight.Delay(TimeSpan.FromMinutes(25));

        // Assert
        Assert.Equal(new DateTime(2026, 7, 14, 10, 15, 0, DateTimeKind.Utc), anyFlight.EstimatedDeparture);
    }

    [Fact]
    public void A_delay_moves_the_estimated_arrival_by_the_same_amount() {
        // Arrange
        Flight   anyFlight         = Any.Flight().Generate();
        DateTime arrivalBeforehand = anyFlight.EstimatedArrival;

        // Act
        anyFlight.Delay(TimeSpan.FromMinutes(40));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(40), anyFlight.EstimatedArrival - arrivalBeforehand);
    }

    [Fact]
    public void The_estimated_arrival_adds_the_block_time_to_the_departure() {
        // Arrange
        Flight anyFlight = Any.Flight()
                              .WithScheduledDeparture(new DateTime(2026, 3, 1, 22, 0, 0, DateTimeKind.Utc))
                              .WithBlockTime(TimeSpan.FromHours(7) + TimeSpan.FromMinutes(45))
                              .Generate();

        // Act
        DateTime arrival = anyFlight.EstimatedArrival;

        // Assert
        Assert.Equal(new DateTime(2026, 3, 2, 5, 45, 0, DateTimeKind.Utc), arrival);
    }

    [Theory]
    [InlineData(14, DelayCategory.OnTime)]
    [InlineData(15, DelayCategory.Minor)]
    [InlineData(59, DelayCategory.Minor)]
    [InlineData(60, DelayCategory.Significant)]
    [InlineData(179, DelayCategory.Significant)]
    [InlineData(180, DelayCategory.Major)]
    public void The_delay_category_follows_the_total_delay(int delayMinutes, DelayCategory expected) {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        anyFlight.Delay(TimeSpan.FromMinutes(delayMinutes));

        // Assert
        Assert.Equal(expected, anyFlight.DelayCategory);
    }

    [Fact]
    public void A_flight_goes_from_scheduled_to_arrived_through_boarding_and_departure() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        anyFlight.StartBoarding();
        anyFlight.Depart();
        anyFlight.Arrive();

        // Assert
        Assert.Equal(FlightStatus.Arrived, anyFlight.Status);
    }

    [Fact]
    public void Departing_without_boarding_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        Action departing = anyFlight.Depart;

        // Assert
        Assert.Throws<InvalidOperationException>(departing);
    }

    [Fact]
    public void A_departed_flight_cannot_be_cancelled_or_delayed() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();
        anyFlight.StartBoarding();
        anyFlight.Depart();
        TimeSpan anyDelay = Any.TimeSpan().Positive().LessThan(TimeSpan.FromHours(6)).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(anyFlight.Cancel);
        Assert.Throws<InvalidOperationException>(() => anyFlight.Delay(anyDelay));
    }

    [Fact]
    public void A_boarding_flight_can_still_be_cancelled() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();
        anyFlight.StartBoarding();

        // Act
        anyFlight.Cancel();

        // Assert
        Assert.Equal(FlightStatus.Cancelled, anyFlight.Status);
    }

}
