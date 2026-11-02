using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Entities;

public sealed class FlightTests {

    private static Flight FlightIn(FlightStatus status, params TimeSpan[] delays) {
        return Fixtures.FlightIn(Any.Flight(), status, delays);
    }

    // ---- construction ----

    [Fact]
    public void A_new_flight_is_scheduled_with_no_delay() {
        // Act
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Equal(FlightStatus.Scheduled, anyFlight.Status);
        Assert.Equal(TimeSpan.Zero, anyFlight.TotalDelay);
        Assert.Equal(DelayCategory.OnTime, anyFlight.DelayCategory);
    }

    [Fact]
    public void A_new_flight_keeps_what_it_was_built_from() {
        // Arrange
        FlightNumber number    = new FlightNumber("AF", 1234);
        Route        route     = new Route(new IataAirportCode("CDG"), new IataAirportCode("JFK"), 5_837);
        Aircraft     aircraft  = Any.Aircraft().Generate();
        DateTime     departure = new DateTime(2026, 3, 14, 8, 30, 0, DateTimeKind.Utc);
        TimeSpan     blockTime = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(5);

        // Act
        Flight flight = new Flight(number, route, aircraft, departure, blockTime);

        // Assert
        Assert.Same(number, flight.Number);
        Assert.Same(route, flight.Route);
        Assert.Same(aircraft, flight.Aircraft);
        Assert.Equal(departure, flight.ScheduledDeparture);
        Assert.Equal(blockTime, flight.BlockTime);
    }

    [Fact]
    public void A_flight_without_a_number_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(null!, anyFlight.Route, anyFlight.Aircraft, anyFlight.ScheduledDeparture, anyFlight.BlockTime));
    }

    [Fact]
    public void A_flight_without_a_route_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyFlight.Number, null!, anyFlight.Aircraft, anyFlight.ScheduledDeparture, anyFlight.BlockTime));
    }

    [Fact]
    public void A_flight_without_an_aircraft_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyFlight.Number, anyFlight.Route, null!, anyFlight.ScheduledDeparture, anyFlight.BlockTime));
    }

    [Theory]
    [InlineData(1)]                 // one tick
    [InlineData(600_000_000)]       // one minute
    [InlineData(720_000_000_000)]   // twenty hours
    public void A_block_time_above_zero_and_up_to_twenty_hours_is_accepted(long ticks) {
        // Act
        Flight flight = Any.Flight().WithBlockTime(TimeSpan.FromTicks(ticks)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromTicks(ticks), flight.BlockTime);
    }

    [Fact]
    public void A_block_time_a_tick_over_twenty_hours_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Flight().WithBlockTime(TimeSpan.FromHours(20) + TimeSpan.FromTicks(1)).Generate());

        Assert.Equal("blockTime", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-36_000_000_000)]
    public void A_block_time_of_zero_or_less_is_refused(long ticks) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Flight().WithBlockTime(TimeSpan.FromTicks(ticks)).Generate());
    }

    [Fact]
    public void The_estimated_times_of_a_flight_that_has_not_been_delayed_follow_its_schedule() {
        // Arrange
        DateTime departure = new DateTime(2026, 3, 14, 8, 30, 0, DateTimeKind.Utc);

        // Act
        Flight flight = Any.Flight().WithScheduledDeparture(departure).WithBlockTime(TimeSpan.FromMinutes(135)).Generate();

        // Assert
        Assert.Equal(departure, flight.EstimatedDeparture);
        Assert.Equal(new DateTime(2026, 3, 14, 10, 45, 0, DateTimeKind.Utc), flight.EstimatedArrival);
    }

    [Fact]
    public void Any_scheduled_departure_is_accepted() {
        // The constructor does not check it: neither a distant past nor a distant future.
        Assert.Equal(DateTime.MinValue, Any.Flight().WithScheduledDeparture(DateTime.MinValue).Generate().ScheduledDeparture);
        Assert.Equal(DateTime.MaxValue, Any.Flight().WithScheduledDeparture(DateTime.MaxValue).Generate().ScheduledDeparture);
    }

    // ---- delays ----

    [Fact]
    public void Delays_accumulate() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        anyFlight.Delay(TimeSpan.FromMinutes(20));
        anyFlight.Delay(TimeSpan.FromMinutes(25));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(45), anyFlight.TotalDelay);
    }

    [Fact]
    public void Delays_push_the_estimated_departure_and_arrival_back_by_their_sum() {
        // Arrange
        DateTime departure = new DateTime(2026, 3, 14, 8, 30, 0, DateTimeKind.Utc);
        Flight   flight    = Any.Flight().WithScheduledDeparture(departure).WithBlockTime(TimeSpan.FromMinutes(135)).Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(20));
        flight.Delay(TimeSpan.FromMinutes(25));

        // Assert
        Assert.Equal(new DateTime(2026, 3, 14, 9, 15, 0, DateTimeKind.Utc), flight.EstimatedDeparture);
        Assert.Equal(new DateTime(2026, 3, 14, 11, 30, 0, DateTimeKind.Utc), flight.EstimatedArrival);
        Assert.Equal(departure, flight.ScheduledDeparture);
        Assert.Equal(TimeSpan.FromMinutes(135), flight.BlockTime);
    }

    [Theory]
    [InlineData(0, DelayCategory.OnTime)]
    [InlineData(1, DelayCategory.OnTime)]
    [InlineData(899, DelayCategory.OnTime)]           // 14 min 59 s
    [InlineData(900, DelayCategory.Minor)]            // 15 min
    [InlineData(3_599, DelayCategory.Minor)]          // 59 min 59 s
    [InlineData(3_600, DelayCategory.Significant)]    // 1 h
    [InlineData(10_799, DelayCategory.Significant)]   // 2 h 59 min 59 s
    [InlineData(10_800, DelayCategory.Major)]         // 3 h
    [InlineData(86_400, DelayCategory.Major)]
    public void The_delay_category_follows_the_total_delay(int totalSeconds, DelayCategory expected) {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        if (totalSeconds > 0) {
            anyFlight.Delay(TimeSpan.FromSeconds(totalSeconds));
        }

        // Assert
        Assert.Equal(expected, anyFlight.DelayCategory);
    }

    [Fact]
    public void The_delay_category_follows_the_sum_of_the_delays_not_each_of_them() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act
        anyFlight.Delay(TimeSpan.FromMinutes(10));
        anyFlight.Delay(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(DelayCategory.Minor, anyFlight.DelayCategory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3_600)]
    public void A_delay_of_zero_or_less_is_refused_and_leaves_the_flight_as_it_was(int seconds) {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();
        anyFlight.Delay(TimeSpan.FromMinutes(10));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyFlight.Delay(TimeSpan.FromSeconds(seconds)));

        // Assert
        Assert.Equal("by", refusal.ParamName);
        Assert.Equal(TimeSpan.FromMinutes(10), anyFlight.TotalDelay);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    public void A_flight_can_still_be_delayed_while_it_is_scheduled_or_boarding_and_keeps_its_status(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act
        flight.Delay(TimeSpan.FromMinutes(30));

        // Assert
        Assert.Equal(status, flight.Status);
        Assert.Equal(TimeSpan.FromMinutes(30), flight.TotalDelay);
    }

    [Theory]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_flight_that_has_left_or_been_cancelled_can_no_longer_be_delayed(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => flight.Delay(TimeSpan.FromMinutes(30)));
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
    }

    // ---- the day's status changes ----

    [Fact]
    public void A_flight_goes_through_its_day_from_scheduled_to_arrived() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Act & Assert
        anyFlight.StartBoarding();
        Assert.Equal(FlightStatus.Boarding, anyFlight.Status);

        anyFlight.Depart();
        Assert.Equal(FlightStatus.Departed, anyFlight.Status);

        anyFlight.Arrive();
        Assert.Equal(FlightStatus.Arrived, anyFlight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    [InlineData(FlightStatus.Cancelled)]
    public void Boarding_can_only_start_from_scheduled(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => flight.StartBoarding());
        Assert.Equal(status, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_flight_can_only_depart_from_boarding(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => flight.Depart());
        Assert.Equal(status, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Arrived)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_flight_can_only_arrive_from_departed(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => flight.Arrive());
        Assert.Equal(status, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_flight_that_has_not_left_can_be_cancelled(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(FlightStatus.Cancelled, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    public void A_flight_that_has_left_cannot_be_cancelled(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => flight.Cancel());
        Assert.Equal(status, flight.Status);
    }

    [Fact]
    public void Cancelling_a_flight_twice_is_accepted() {
        // Arrange
        Flight flight = FlightIn(FlightStatus.Cancelled);

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(FlightStatus.Cancelled, flight.Status);
    }

    [Fact]
    public void Cancelling_a_flight_keeps_the_delay_it_had_accumulated() {
        // Arrange
        Flight flight = FlightIn(FlightStatus.Scheduled, TimeSpan.FromMinutes(90));

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(90), flight.TotalDelay);
        Assert.Equal(DelayCategory.Significant, flight.DelayCategory);
    }

}
