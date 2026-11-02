using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class FlightTests {

    private static readonly DateTime Scheduled = new DateTime(2026, 6, 1, 8, 30, 0, DateTimeKind.Utc);

    public enum Move {
        StartBoarding,
        Depart,
        Arrive
    }

    private static Flight FlightIn(FlightStatus status) {
        Flight flight = Arbitrary.Flight().Generate();
        switch (status) {
            case FlightStatus.Scheduled:
                break;
            case FlightStatus.Boarding:
                flight.StartBoarding();
                break;
            case FlightStatus.Departed:
                flight.StartBoarding();
                flight.Depart();
                break;
            case FlightStatus.Arrived:
                flight.StartBoarding();
                flight.Depart();
                flight.Arrive();
                break;
            case FlightStatus.Cancelled:
                flight.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return flight;
    }

    private static void Apply(Flight flight, Move move) {
        switch (move) {
            case Move.StartBoarding:
                flight.StartBoarding();
                break;
            case Move.Depart:
                flight.Depart();
                break;
            case Move.Arrive:
                flight.Arrive();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(move), move, null);
        }
    }

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_new_flight_keeps_what_it_was_given_and_is_scheduled_without_delay() {
        // Arrange
        FlightNumber anyNumber   = Arbitrary.FlightNumber().Generate();
        Route        anyRoute    = Arbitrary.Route().Generate();
        Aircraft     anyAircraft = Arbitrary.Aircraft().Generate();

        // Act
        Flight flight = new Flight(anyNumber, anyRoute, anyAircraft, Scheduled, TimeSpan.FromMinutes(95));

        // Assert
        Assert.Same(anyNumber, flight.Number);
        Assert.Same(anyRoute, flight.Route);
        Assert.Same(anyAircraft, flight.Aircraft);
        Assert.Equal(Scheduled, flight.ScheduledDeparture);
        Assert.Equal(TimeSpan.FromMinutes(95), flight.BlockTime);
        Assert.Equal(FlightStatus.Scheduled, flight.Status);
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
        Assert.Equal(DelayCategory.OnTime, flight.DelayCategory);
    }

    [Fact]
    public void Without_delay_the_estimated_times_follow_the_schedule_and_the_block_time() {
        // Arrange
        Flight flight = Arbitrary.Flight()
                                 .WithScheduledDeparture(Scheduled)
                                 .WithBlockTime(TimeSpan.FromMinutes(95))
                                 .Generate();

        // Assert
        Assert.Equal(Scheduled, flight.EstimatedDeparture);
        Assert.Equal(Scheduled.AddMinutes(95), flight.EstimatedArrival);
    }

    [Fact]
    public void A_null_number_is_refused() {
        // Arrange
        Route    anyRoute    = Arbitrary.Route().Generate();
        Aircraft anyAircraft = Arbitrary.Aircraft().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(null!, anyRoute, anyAircraft, Scheduled, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void A_null_route_is_refused() {
        // Arrange
        FlightNumber anyNumber   = Arbitrary.FlightNumber().Generate();
        Aircraft     anyAircraft = Arbitrary.Aircraft().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyNumber, null!, anyAircraft, Scheduled, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void A_null_aircraft_is_refused() {
        // Arrange
        FlightNumber anyNumber = Arbitrary.FlightNumber().Generate();
        Route        anyRoute  = Arbitrary.Route().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyNumber, anyRoute, null!, Scheduled, TimeSpan.FromHours(2)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60 * 60)]
    [InlineData(20 * 60 * 60)]
    public void A_block_time_up_to_twenty_hours_is_accepted(int seconds) {
        // Act
        Flight flight = Arbitrary.Flight().WithBlockTime(TimeSpan.FromSeconds(seconds)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), flight.BlockTime);
    }

    [Fact]
    public void The_smallest_positive_block_time_is_accepted() {
        // Act
        Flight flight = Arbitrary.Flight().WithBlockTime(TimeSpan.FromTicks(1)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), flight.BlockTime);
    }

    [Fact]
    public void A_zero_block_time_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Flight().WithBlockTime(TimeSpan.Zero).Generate());
    }

    [Fact]
    public void A_negative_block_time_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Flight().WithBlockTime(TimeSpan.FromMinutes(-90)).Generate());
    }

    [Fact]
    public void A_block_time_just_over_twenty_hours_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Flight().WithBlockTime(TimeSpan.FromHours(20).Add(TimeSpan.FromTicks(1))).Generate());
    }

    [Fact]
    public void Any_scheduled_departure_is_accepted_even_in_the_past_and_local() {
        // Act
        Flight utcFlight   = Arbitrary.Flight().WithScheduledDeparture(new DateTime(1999, 12, 31, 23, 59, 0, DateTimeKind.Utc)).Generate();
        Flight localFlight = Arbitrary.Flight().WithScheduledDeparture(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)).Generate();

        // Assert
        Assert.Equal(DateTimeKind.Utc, utcFlight.ScheduledDeparture.Kind);
        Assert.Equal(DateTimeKind.Local, localFlight.ScheduledDeparture.Kind);
    }

    // ---------------------------------------------------------------- delays

    [Fact]
    public void Delays_accumulate() {
        // Arrange
        Flight flight = Arbitrary.Flight().WithScheduledDeparture(Scheduled).WithBlockTime(TimeSpan.FromMinutes(100)).Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(20));
        flight.Delay(TimeSpan.FromMinutes(35));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(55), flight.TotalDelay);
        Assert.Equal(Scheduled.AddMinutes(55), flight.EstimatedDeparture);
        Assert.Equal(Scheduled.AddMinutes(155), flight.EstimatedArrival);
    }

    [Fact]
    public void A_delay_does_not_move_the_scheduled_departure() {
        // Arrange
        Flight flight = Arbitrary.Flight().WithScheduledDeparture(Scheduled).Generate();

        // Act
        flight.Delay(TimeSpan.FromHours(2));

        // Assert
        Assert.Equal(Scheduled, flight.ScheduledDeparture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void A_delay_that_is_not_positive_is_refused(int seconds) {
        // Arrange
        Flight anyFlight = Arbitrary.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyFlight.Delay(TimeSpan.FromSeconds(seconds)));
        Assert.Equal(TimeSpan.Zero, anyFlight.TotalDelay);
    }

    [Fact]
    public void The_smallest_positive_delay_is_accepted() {
        // Arrange
        Flight anyFlight = Arbitrary.Flight().Generate();

        // Act
        anyFlight.Delay(TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), anyFlight.TotalDelay);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    public void A_flight_can_be_delayed_until_it_departs(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Act
        flight.Delay(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), flight.TotalDelay);
        Assert.Equal(status, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_departed_arrived_or_cancelled_flight_cannot_be_delayed(FlightStatus status) {
        // Arrange
        Flight flight = FlightIn(status);

        // Assert
        Assert.Throws<InvalidOperationException>(() => flight.Delay(TimeSpan.FromMinutes(10)));
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
    }

    [Theory]
    [InlineData(1, DelayCategory.OnTime)]
    [InlineData(14, DelayCategory.OnTime)]
    [InlineData(15, DelayCategory.Minor)]
    [InlineData(16, DelayCategory.Minor)]
    [InlineData(59, DelayCategory.Minor)]
    [InlineData(60, DelayCategory.Significant)]
    [InlineData(61, DelayCategory.Significant)]
    [InlineData(179, DelayCategory.Significant)]
    [InlineData(180, DelayCategory.Major)]
    [InlineData(181, DelayCategory.Major)]
    [InlineData(1440, DelayCategory.Major)]
    public void The_delay_category_follows_the_total_delay_in_minutes(int minutes, DelayCategory expected) {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(minutes));

        // Assert
        Assert.Equal(expected, flight.DelayCategory);
    }

    [Theory]
    [InlineData(14 * 60 + 59, DelayCategory.OnTime)]
    [InlineData(60 * 60 - 1, DelayCategory.Minor)]
    [InlineData(180 * 60 - 1, DelayCategory.Significant)]
    public void The_category_boundaries_are_exact_to_the_second(int seconds, DelayCategory expected) {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();

        // Act
        flight.Delay(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(expected, flight.DelayCategory);
    }

    [Fact]
    public void The_delay_category_is_judged_on_the_total_not_on_each_delay() {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(10));
        flight.Delay(TimeSpan.FromMinutes(10));
        flight.Delay(TimeSpan.FromMinutes(45));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(65), flight.TotalDelay);
        Assert.Equal(DelayCategory.Significant, flight.DelayCategory);
    }

    // ---------------------------------------------------------------- status moves

    [Fact]
    public void A_flight_goes_through_its_day_in_order() {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();

        // Act / Assert
        flight.StartBoarding();
        Assert.Equal(FlightStatus.Boarding, flight.Status);
        flight.Depart();
        Assert.Equal(FlightStatus.Departed, flight.Status);
        flight.Arrive();
        Assert.Equal(FlightStatus.Arrived, flight.Status);
    }

    [Theory]
    [InlineData(Move.StartBoarding, FlightStatus.Scheduled, FlightStatus.Boarding)]
    [InlineData(Move.Depart, FlightStatus.Boarding, FlightStatus.Departed)]
    [InlineData(Move.Arrive, FlightStatus.Departed, FlightStatus.Arrived)]
    public void A_move_is_accepted_from_the_one_status_it_expects(Move move, FlightStatus from, FlightStatus to) {
        // Arrange
        Flight flight = FlightIn(from);

        // Act
        Apply(flight, move);

        // Assert
        Assert.Equal(to, flight.Status);
    }

    [Theory]
    [InlineData(Move.StartBoarding, FlightStatus.Boarding)]
    [InlineData(Move.StartBoarding, FlightStatus.Departed)]
    [InlineData(Move.StartBoarding, FlightStatus.Arrived)]
    [InlineData(Move.StartBoarding, FlightStatus.Cancelled)]
    [InlineData(Move.Depart, FlightStatus.Scheduled)]
    [InlineData(Move.Depart, FlightStatus.Departed)]
    [InlineData(Move.Depart, FlightStatus.Arrived)]
    [InlineData(Move.Depart, FlightStatus.Cancelled)]
    [InlineData(Move.Arrive, FlightStatus.Scheduled)]
    [InlineData(Move.Arrive, FlightStatus.Boarding)]
    [InlineData(Move.Arrive, FlightStatus.Arrived)]
    [InlineData(Move.Arrive, FlightStatus.Cancelled)]
    public void A_move_is_refused_from_any_other_status_and_leaves_the_status_alone(Move move, FlightStatus from) {
        // Arrange
        Flight flight = FlightIn(from);

        // Assert
        Assert.Throws<InvalidOperationException>(() => Apply(flight, move));
        Assert.Equal(from, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Boarding)]
    [InlineData(FlightStatus.Cancelled)]
    public void A_flight_can_be_cancelled_before_it_departs_and_cancelling_twice_is_harmless(FlightStatus from) {
        // Arrange
        Flight flight = FlightIn(from);

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(FlightStatus.Cancelled, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    public void A_departed_or_arrived_flight_cannot_be_cancelled(FlightStatus from) {
        // Arrange
        Flight flight = FlightIn(from);

        // Assert
        Assert.Throws<InvalidOperationException>(() => flight.Cancel());
        Assert.Equal(from, flight.Status);
    }

    [Fact]
    public void Delays_taken_before_a_cancellation_are_kept() {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();
        flight.Delay(TimeSpan.FromMinutes(40));

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(40), flight.TotalDelay);
    }

    [Fact]
    public void A_delay_taken_in_boarding_still_counts_after_departure() {
        // Arrange
        Flight flight = Arbitrary.Flight().Generate();
        flight.StartBoarding();
        flight.Delay(TimeSpan.FromMinutes(25));

        // Act
        flight.Depart();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(25), flight.TotalDelay);
        Assert.Equal(DelayCategory.Minor, flight.DelayCategory);
    }

}
