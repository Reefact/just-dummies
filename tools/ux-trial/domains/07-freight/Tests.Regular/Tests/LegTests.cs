namespace Freight.Domain.Tests;

public sealed class LegTests {

    private static readonly DateTimeOffset Noon = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_leg_keeps_what_it_was_built_from() {
        // Arrange
        UnLocode       anyOrigin      = Any.UnLocode().Generate();
        UnLocode       anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();
        TimeSpan       anyTransit     = Any.TimeSpan().Between(TimeSpan.FromMinutes(1), Leg.MaxTransit).Generate();
        DateTimeOffset anyDeparture   = Any.DateTimeOffset().Between(Noon.AddYears(-5), Noon.AddYears(5)).Generate();
        DateTimeOffset anyArrival     = anyDeparture + anyTransit;

        // Act
        Leg leg = new Leg(anyOrigin, anyDestination, anyDeparture, anyArrival);

        // Assert
        Assert.Same(anyOrigin, leg.Origin);
        Assert.Same(anyDestination, leg.Destination);
        Assert.Equal(anyDeparture, leg.Departure);
        Assert.Equal(anyArrival, leg.Arrival);
    }

    [Fact]
    public void The_duration_is_the_arrival_minus_the_departure() {
        // Arrange
        Leg leg = Any.Leg().WithDeparture(Noon).WithArrival(Noon.AddHours(36)).Generate();

        // Act
        TimeSpan duration = leg.Duration;

        // Assert
        Assert.Equal(TimeSpan.FromHours(36), duration);
    }

    [Fact]
    public void The_maximum_transit_is_ninety_days() {
        // Act & Assert
        Assert.Equal(TimeSpan.FromDays(90), Leg.MaxTransit);
    }

    [Fact]
    public void A_leg_of_exactly_ninety_days_is_accepted() {
        // Act
        Leg leg = Any.Leg().WithDeparture(Noon).WithArrival(Noon.AddDays(90)).Generate();

        // Assert
        Assert.Equal(Leg.MaxTransit, leg.Duration);
    }

    [Fact]
    public void A_leg_one_tick_over_ninety_days_is_refused() {
        // Arrange
        UnLocode anyOrigin      = Any.UnLocode().Generate();
        UnLocode anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(anyOrigin, anyDestination, Noon, Noon.AddDays(90).AddTicks(1)));

        // Assert
        Assert.Equal("arrival", refusal.ParamName);
    }

    [Fact]
    public void A_leg_longer_than_ninety_days_is_refused() {
        // Arrange
        UnLocode anyOrigin      = Any.UnLocode().Generate();
        UnLocode anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();
        TimeSpan anyExcess      = Any.TimeSpan().Between(Leg.MaxTransit + TimeSpan.FromTicks(1), TimeSpan.FromDays(3_000)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyOrigin, anyDestination, Noon, Noon + anyExcess));
    }

    [Fact]
    public void A_leg_of_one_tick_is_accepted() {
        // Act
        Leg leg = Any.Leg().WithDeparture(Noon).WithArrival(Noon.AddTicks(1)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), leg.Duration);
    }

    [Fact]
    public void A_leg_arriving_when_it_departs_is_refused() {
        // Arrange
        UnLocode anyOrigin      = Any.UnLocode().Generate();
        UnLocode anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(anyOrigin, anyDestination, Noon, Noon));

        // Assert
        Assert.Equal("arrival", refusal.ParamName);
    }

    [Fact]
    public void A_leg_arriving_before_it_departs_is_refused() {
        // Arrange
        UnLocode anyOrigin      = Any.UnLocode().Generate();
        UnLocode anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();
        TimeSpan anyShift       = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(400)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyOrigin, anyDestination, Noon, Noon - anyShift));
    }

    [Fact]
    public void Times_are_compared_as_instants_not_as_local_clocks() {
        // Arrange: the local clock at arrival reads later (00:30), the instant is earlier (23:30 UTC the day before).
        UnLocode       anyOrigin      = Any.UnLocode().Generate();
        UnLocode       anyDestination = Any.UnLocode().WithCode(code => code.DifferentFrom(anyOrigin.Code)).Generate();
        DateTimeOffset departure      = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset arrival        = new DateTimeOffset(2026, 3, 1, 0, 30, 0, TimeSpan.FromHours(1));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyOrigin, anyDestination, departure, arrival));
    }

    [Fact]
    public void An_arrival_whose_local_clock_reads_earlier_is_accepted_when_the_instant_is_later() {
        // Arrange
        DateTimeOffset departure = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(14));
        DateTimeOffset arrival   = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(-12));

        // Act
        Leg leg = Any.Leg().WithDeparture(departure).WithArrival(arrival).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), leg.Duration);
    }

    [Fact]
    public void The_ninety_days_are_measured_between_instants() {
        // Arrange: 90 days and one hour on the clocks, exactly 90 days in elapsed time.
        DateTimeOffset departure = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset arrival   = new DateTimeOffset(2026, 5, 30, 1, 0, 0, TimeSpan.FromHours(1));

        // Act
        Leg leg = Any.Leg().WithDeparture(departure).WithArrival(arrival).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromDays(90), leg.Duration);
    }

    [Fact]
    public void A_leg_that_stays_where_it_is_is_refused() {
        // Arrange
        UnLocode anyPlace   = Any.UnLocode().Generate();
        UnLocode samePlace  = new UnLocode(anyPlace.Code);
        TimeSpan anyTransit = Any.TimeSpan().Between(TimeSpan.FromMinutes(1), Leg.MaxTransit).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(anyPlace, samePlace, Noon, Noon + anyTransit));

        // Assert
        Assert.Equal("destination", refusal.ParamName);
    }

    [Fact]
    public void A_null_origin_is_refused() {
        // Arrange
        UnLocode anyDestination = Any.UnLocode().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Leg(null!, anyDestination, Noon, Noon.AddHours(1)));
    }

    [Fact]
    public void A_null_destination_is_refused() {
        // Arrange
        UnLocode anyOrigin = Any.UnLocode().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Leg(anyOrigin, null!, Noon, Noon.AddHours(1)));
    }

    [Fact]
    public void The_locations_are_checked_before_the_times() {
        // Arrange
        UnLocode anyPlace = Any.UnLocode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(anyPlace, anyPlace, Noon, Noon.AddDays(-1)));

        // Assert
        Assert.Equal("destination", refusal.ParamName);
    }

    [Fact]
    public void The_departure_and_the_arrival_keep_the_offset_of_their_own_port() {
        // Arrange
        DateTimeOffset departure = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(8));
        DateTimeOffset arrival   = new DateTimeOffset(2026, 3, 20, 17, 30, 0, TimeSpan.FromHours(-5));

        // Act
        Leg leg = Any.Leg().WithDeparture(departure).WithArrival(arrival).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), leg.Departure.Offset);
        Assert.Equal(TimeSpan.FromHours(-5), leg.Arrival.Offset);
    }

}
