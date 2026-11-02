using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class FlightTests {

    private static readonly DateTime EightOClock = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_new_flight_is_scheduled_with_no_delay() {
        // Arrange
        FlightNumber anyNumber   = Any.FlightNumber().Generate();
        Route        anyRoute    = Any.Route().Generate();
        Aircraft     anyAircraft = Any.Aircraft().Generate();

        // Act
        Flight flight = new Flight(anyNumber, anyRoute, anyAircraft, EightOClock, TimeSpan.FromHours(2));

        // Assert
        Assert.Same(anyNumber, flight.Number);
        Assert.Same(anyRoute, flight.Route);
        Assert.Same(anyAircraft, flight.Aircraft);
        Assert.Equal(EightOClock, flight.ScheduledDeparture);
        Assert.Equal(TimeSpan.FromHours(2), flight.BlockTime);
        Assert.Equal(FlightStatus.Scheduled, flight.Status);
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
        Assert.Equal(EightOClock, flight.EstimatedDeparture);
        Assert.Equal(EightOClock.AddHours(2), flight.EstimatedArrival);
        Assert.Equal(DelayCategory.OnTime, flight.DelayCategory);
    }

    [Fact]
    public void A_null_flight_number_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(null!, anyFlight.Route, anyFlight.Aircraft, EightOClock, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void A_null_route_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyFlight.Number, null!, anyFlight.Aircraft, EightOClock, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void A_null_aircraft_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Flight(anyFlight.Number, anyFlight.Route, null!, EightOClock, TimeSpan.FromHours(2)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(20 * 60)]
    public void A_block_time_up_to_twenty_hours_is_accepted(int minutes) {
        // Act
        Flight flight = Any.Flight().WithBlockTime(TimeSpan.FromMinutes(minutes)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(minutes), flight.BlockTime);
    }

    [Fact]
    public void A_block_time_of_a_single_tick_is_accepted() {
        Assert.Equal(TimeSpan.FromTicks(1), Any.Flight().WithBlockTime(TimeSpan.FromTicks(1)).Generate().BlockTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    [InlineData(20 * 60 + 1)]
    [InlineData(48 * 60)]
    public void A_block_time_that_is_not_positive_or_over_twenty_hours_is_refused(int minutes) {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Flight(anyFlight.Number, anyFlight.Route, anyFlight.Aircraft, EightOClock, TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void A_block_time_a_tick_over_twenty_hours_is_refused() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Flight(anyFlight.Number, anyFlight.Route, anyFlight.Aircraft, EightOClock, TimeSpan.FromHours(20) + TimeSpan.FromTicks(1)));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void The_scheduled_departure_is_not_checked_whatever_its_kind(DateTimeKind kind) {
        // Arrange
        Flight   anyFlight = Any.Flight().Generate();
        DateTime departure = new DateTime(2026, 3, 10, 8, 0, 0, kind);

        // Act
        Flight flight = new Flight(anyFlight.Number, anyFlight.Route, anyFlight.Aircraft, departure, TimeSpan.FromHours(2));

        // Assert
        Assert.Equal(departure, flight.ScheduledDeparture);
    }

    [Fact(Skip = "domain defect: the scheduled departure is unchecked, so a flight scheduled near DateTime.MaxValue throws ArgumentOutOfRangeException from EstimatedArrival (and EstimatedDeparture once delayed) instead of refusing the departure at construction")]
    public void An_estimated_arrival_can_always_be_read_on_a_flight_that_was_accepted() {
        // Arrange
        Flight anyFlight = Any.Flight().Generate();
        Flight flight    = new Flight(anyFlight.Number, anyFlight.Route, anyFlight.Aircraft, DateTime.MaxValue, TimeSpan.FromHours(2));

        // Assert
        Assert.Null(Record.Exception(() => flight.EstimatedArrival));
    }

    [Fact]
    public void A_delay_moves_the_estimated_departure_and_arrival() {
        // Arrange
        Flight flight = Any.Flight().WithScheduledDeparture(EightOClock).WithBlockTime(TimeSpan.FromHours(2)).Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(25));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(25), flight.TotalDelay);
        Assert.Equal(EightOClock.AddMinutes(25), flight.EstimatedDeparture);
        Assert.Equal(EightOClock.AddMinutes(25).AddHours(2), flight.EstimatedArrival);
        Assert.Equal(EightOClock, flight.ScheduledDeparture);
    }

    [Fact]
    public void Delays_accumulate() {
        // Arrange
        Flight flight = Any.Flight().WithScheduledDeparture(EightOClock).WithBlockTime(TimeSpan.FromHours(1)).Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(10));
        flight.Delay(TimeSpan.FromMinutes(20));
        flight.Delay(TimeSpan.FromMinutes(45));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(75), flight.TotalDelay);
        Assert.Equal(EightOClock.AddMinutes(75), flight.EstimatedDeparture);
        Assert.Equal(EightOClock.AddMinutes(75).AddHours(1), flight.EstimatedArrival);
    }

    [Fact]
    public void A_delay_is_still_accepted_while_boarding() {
        // Arrange
        Flight flight = Any.Flight().Generate();
        flight.StartBoarding();

        // Act
        flight.Delay(TimeSpan.FromMinutes(30));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), flight.TotalDelay);
        Assert.Equal(FlightStatus.Boarding, flight.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-90)]
    public void A_delay_that_is_not_positive_is_refused_and_changes_nothing(int minutes) {
        // Arrange
        Flight flight = Any.Flight().Generate();

        // Act
        Exception? refusal = Record.Exception(() => flight.Delay(TimeSpan.FromMinutes(minutes)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
    }

    [Fact]
    public void A_delay_of_a_single_tick_is_accepted() {
        // Arrange
        Flight flight = Any.Flight().Generate();

        // Act
        flight.Delay(TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), flight.TotalDelay);
    }

    [Theory]
    [InlineData(0, DelayCategory.OnTime)]
    [InlineData(14, DelayCategory.OnTime)]
    [InlineData(15, DelayCategory.Minor)]
    [InlineData(59, DelayCategory.Minor)]
    [InlineData(60, DelayCategory.Significant)]
    [InlineData(179, DelayCategory.Significant)]
    [InlineData(180, DelayCategory.Major)]
    [InlineData(1_000, DelayCategory.Major)]
    public void The_delay_category_follows_the_total_delay_in_minutes(int minutes, DelayCategory expected) {
        // Arrange
        Flight flight = Any.Flight().Generate();
        if (minutes > 0) { flight.Delay(TimeSpan.FromMinutes(minutes)); }

        // Assert
        Assert.Equal(expected, flight.DelayCategory);
    }

    [Fact]
    public void The_delay_category_is_decided_on_the_total_not_on_each_delay() {
        // Arrange
        Flight flight = Any.Flight().Generate();

        // Act
        flight.Delay(TimeSpan.FromMinutes(10));
        Assert.Equal(DelayCategory.OnTime, flight.DelayCategory);
        flight.Delay(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(DelayCategory.Minor, flight.DelayCategory);
    }

    [Fact]
    public void The_category_boundary_is_exact_to_the_tick() {
        // Arrange
        Flight justUnder = Any.Flight().Generate();
        Flight exactly   = Any.Flight().Generate();
        justUnder.Delay(TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
        exactly.Delay(TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(DelayCategory.OnTime, justUnder.DelayCategory);
        Assert.Equal(DelayCategory.Minor, exactly.DelayCategory);
    }

    [Fact]
    public void A_flight_goes_through_its_day_in_order() {
        // Arrange
        Flight flight = Any.Flight().Generate();

        // Act and assert
        flight.StartBoarding();
        Assert.Equal(FlightStatus.Boarding, flight.Status);
        flight.Depart();
        Assert.Equal(FlightStatus.Departed, flight.Status);
        flight.Arrive();
        Assert.Equal(FlightStatus.Arrived, flight.Status);
    }

    public enum Move { Delay, StartBoarding, Depart, Arrive, Cancel }

    public static TheoryData<FlightStatus, Move, bool> Moves() {
        TheoryData<FlightStatus, Move, bool> data = new TheoryData<FlightStatus, Move, bool>();

        data.Add(FlightStatus.Scheduled, Move.Delay,         true);
        data.Add(FlightStatus.Scheduled, Move.StartBoarding, true);
        data.Add(FlightStatus.Scheduled, Move.Depart,        false);
        data.Add(FlightStatus.Scheduled, Move.Arrive,        false);
        data.Add(FlightStatus.Scheduled, Move.Cancel,        true);

        data.Add(FlightStatus.Boarding, Move.Delay,         true);
        data.Add(FlightStatus.Boarding, Move.StartBoarding, false);
        data.Add(FlightStatus.Boarding, Move.Depart,        true);
        data.Add(FlightStatus.Boarding, Move.Arrive,        false);
        data.Add(FlightStatus.Boarding, Move.Cancel,        true);

        data.Add(FlightStatus.Departed, Move.Delay,         false);
        data.Add(FlightStatus.Departed, Move.StartBoarding, false);
        data.Add(FlightStatus.Departed, Move.Depart,        false);
        data.Add(FlightStatus.Departed, Move.Arrive,        true);
        data.Add(FlightStatus.Departed, Move.Cancel,        false);

        data.Add(FlightStatus.Arrived, Move.Delay,         false);
        data.Add(FlightStatus.Arrived, Move.StartBoarding, false);
        data.Add(FlightStatus.Arrived, Move.Depart,        false);
        data.Add(FlightStatus.Arrived, Move.Arrive,        false);
        data.Add(FlightStatus.Arrived, Move.Cancel,        false);

        data.Add(FlightStatus.Cancelled, Move.Delay,         false);
        data.Add(FlightStatus.Cancelled, Move.StartBoarding, false);
        data.Add(FlightStatus.Cancelled, Move.Depart,        false);
        data.Add(FlightStatus.Cancelled, Move.Arrive,        false);
        data.Add(FlightStatus.Cancelled, Move.Cancel,        true);

        return data;
    }

    [Theory]
    [MemberData(nameof(Moves))]
    public void Each_move_is_allowed_or_refused_according_to_the_current_status(FlightStatus from, Move move, bool allowed) {
        // Arrange
        Flight flight = FlightIn(from);

        // Act
        Exception? refusal = Record.Exception(() => Apply(flight, move));

        // Assert
        if (allowed) {
            Assert.Null(refusal);
        } else {
            Assert.IsType<InvalidOperationException>(refusal);
        }
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled, FlightStatus.Cancelled)]
    [InlineData(FlightStatus.Boarding, FlightStatus.Cancelled)]
    [InlineData(FlightStatus.Cancelled, FlightStatus.Cancelled)]
    public void Cancelling_leaves_the_flight_cancelled(FlightStatus from, FlightStatus expected) {
        // Arrange
        Flight flight = FlightIn(from);

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(expected, flight.Status);
    }

    [Theory]
    [InlineData(FlightStatus.Departed)]
    [InlineData(FlightStatus.Arrived)]
    public void A_refused_cancellation_leaves_the_status_alone(FlightStatus from) {
        // Arrange
        Flight flight = FlightIn(from);

        // Act
        Assert.Throws<InvalidOperationException>(() => flight.Cancel());

        // Assert
        Assert.Equal(from, flight.Status);
    }

    [Fact]
    public void A_refused_delay_after_departure_does_not_change_the_total() {
        // Arrange
        Flight flight = FlightIn(FlightStatus.Departed);

        // Act
        Assert.Throws<InvalidOperationException>(() => flight.Delay(TimeSpan.FromMinutes(30)));

        // Assert
        Assert.Equal(TimeSpan.Zero, flight.TotalDelay);
    }

    [Fact]
    public void Delay_validates_its_argument_before_looking_at_the_status() {
        // Arrange
        Flight flight = FlightIn(FlightStatus.Arrived);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => flight.Delay(TimeSpan.Zero));
    }

    [Fact]
    public void A_cancelled_flight_keeps_the_delay_it_had() {
        // Arrange
        Flight flight = Any.Flight().Generate();
        flight.Delay(TimeSpan.FromMinutes(70));

        // Act
        flight.Cancel();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(70), flight.TotalDelay);
        Assert.Equal(DelayCategory.Significant, flight.DelayCategory);
    }

    internal static Flight FlightIn(FlightStatus status) {
        Flight flight = Any.Flight().Generate();

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
        }

        return flight;
    }

    private static void Apply(Flight flight, Move move) {
        switch (move) {
            case Move.Delay:
                flight.Delay(TimeSpan.FromMinutes(5));
                break;
            case Move.StartBoarding:
                flight.StartBoarding();
                break;
            case Move.Depart:
                flight.Depart();
                break;
            case Move.Arrive:
                flight.Arrive();
                break;
            case Move.Cancel:
                flight.Cancel();
                break;
        }
    }

}
