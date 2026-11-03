using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class LegTests {

    private static readonly DateTimeOffset Departure = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static (UnLocode Origin, UnLocode Destination) AnyTwoPorts() {
        UnLocode origin      = Any.UnLocode().Generate();
        UnLocode destination = Any.UnLocode().WithCode(c => c.DifferentFrom(origin.Code)).Generate();

        return (origin, destination);
    }

    [Fact]
    public void The_longest_transit_of_a_leg_is_ninety_days() {
        Assert.Equal(TimeSpan.FromDays(90), Leg.MaxTransit);
    }

    [Fact]
    public void A_leg_keeps_what_it_was_given_and_lasts_from_departure_to_arrival() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();
        DateTimeOffset arrival = Departure.AddDays(12).AddHours(5);

        Leg leg = new Leg(origin, destination, Departure, arrival);

        Assert.Same(origin, leg.Origin);
        Assert.Same(destination, leg.Destination);
        Assert.Equal(Departure, leg.Departure);
        Assert.Equal(arrival, leg.Arrival);
        Assert.Equal(TimeSpan.FromHours(12 * 24 + 5), leg.Duration);
    }

    [Fact]
    public void A_leg_between_ports_in_different_time_zones_lasts_the_difference_of_the_instants() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();
        DateTimeOffset departureFromShanghai = new DateTimeOffset(2026, 3, 1, 20, 0, 0, TimeSpan.FromHours(8));    // 12:00 UTC
        DateTimeOffset arrivalAtLosAngeles   = new DateTimeOffset(2026, 3, 15, 5, 0, 0, TimeSpan.FromHours(-8));   // 13:00 UTC, two weeks on

        Leg leg = new Leg(origin, destination, departureFromShanghai, arrivalAtLosAngeles);

        Assert.Equal(TimeSpan.FromDays(14) + TimeSpan.FromHours(1), leg.Duration);
    }

    [Fact]
    public void A_leg_cannot_leave_and_end_at_the_same_port() {
        UnLocode port        = Any.UnLocode().Generate();
        UnLocode samePortAgain = new UnLocode(port.Code);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(port, samePortAgain, Departure, Departure.AddDays(3)));

        Assert.Equal("destination", refusal.ParamName);
    }

    [Fact]
    public void A_leg_without_an_origin_is_refused_as_missing() {
        UnLocode anyDestination = Any.UnLocode().Generate();

        Assert.Throws<ArgumentNullException>(() => new Leg(null!, anyDestination, Departure, Departure.AddDays(3)));
    }

    [Fact]
    public void A_leg_without_a_destination_is_refused_as_missing() {
        UnLocode anyOrigin = Any.UnLocode().Generate();

        Assert.Throws<ArgumentNullException>(() => new Leg(anyOrigin, null!, Departure, Departure.AddDays(3)));
    }

    [Fact]
    public void A_leg_arriving_when_it_departs_is_refused() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(origin, destination, Departure, Departure));

        Assert.Equal("arrival", refusal.ParamName);
    }

    [Fact]
    public void A_leg_arriving_before_it_departs_is_refused() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();

        Assert.Throws<ArgumentException>(() => new Leg(origin, destination, Departure, Departure.AddTicks(-1)));
    }

    [Fact]
    public void A_leg_that_arrives_one_tick_after_it_departs_is_accepted() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();

        Leg leg = new Leg(origin, destination, Departure, Departure.AddTicks(1));

        Assert.Equal(TimeSpan.FromTicks(1), leg.Duration);
    }

    [Fact]
    public void Times_are_compared_as_instants_not_as_local_clocks() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();
        // 10:00 at +02:00 is 08:00 UTC: the local clock of the arrival reads later, the instant is the same.
        DateTimeOffset departureAtTen = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset arrivalAtEight = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new Leg(origin, destination, departureAtTen, arrivalAtEight));
    }

    [Fact]
    public void An_arrival_whose_local_clock_reads_earlier_can_still_be_later_as_an_instant() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();
        DateTimeOffset departureAtNoon = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(8));    // 04:00 UTC
        DateTimeOffset arrivalAtEleven = new DateTimeOffset(2026, 3, 1, 11, 0, 0, TimeSpan.FromHours(-5));    // 16:00 UTC

        Leg leg = new Leg(origin, destination, departureAtNoon, arrivalAtEleven);

        Assert.Equal(TimeSpan.FromHours(12), leg.Duration);
    }

    [Fact]
    public void A_leg_lasting_exactly_ninety_days_is_accepted() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();

        Leg leg = new Leg(origin, destination, Departure, Departure.AddDays(90));

        Assert.Equal(TimeSpan.FromDays(90), leg.Duration);
    }

    [Fact]
    public void A_leg_lasting_one_tick_more_than_ninety_days_is_refused() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Leg(origin, destination, Departure, Departure.AddDays(90).AddTicks(1)));

        Assert.Equal("arrival", refusal.ParamName);
    }

    [Fact]
    public void The_ninety_days_ceiling_is_measured_between_instants() {
        (UnLocode origin, UnLocode destination) = AnyTwoPorts();
        DateTimeOffset departureEast = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(14));    // 2026-02-28T18:00Z
        DateTimeOffset arrivalWest   = new DateTimeOffset(2026, 5, 29, 8, 0, 0, TimeSpan.FromHours(-12));   // 2026-05-29T20:00Z, 90 days and 2 hours later

        Assert.Throws<ArgumentException>(() => new Leg(origin, destination, departureEast, arrivalWest));
    }

}
