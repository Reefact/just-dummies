using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class LegTests {

    [Fact]
    public void A_leg_keeps_its_ports_and_its_times_and_knows_its_duration() {
        // Arrange
        UnLocode       origin      = new UnLocode("FRLEH");
        UnLocode       destination = new UnLocode("GBSOU");
        DateTimeOffset departure   = Calendar.At(1, 6);
        DateTimeOffset arrival     = Calendar.At(2, 18);

        // Act
        Leg leg = new Leg(origin, destination, departure, arrival);

        // Assert
        Assert.Equal(origin, leg.Origin);
        Assert.Equal(destination, leg.Destination);
        Assert.Equal(departure, leg.Departure);
        Assert.Equal(arrival, leg.Arrival);
        Assert.Equal(TimeSpan.FromHours(36), leg.Duration);
    }

    [Fact]
    public void Times_are_compared_as_instants_whatever_their_offsets() {
        // Arrange: 10:00 at +02:00 is 08:00 UTC; 04:00 at -05:00 is 09:00 UTC
        DateTimeOffset departure = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset arrival   = new DateTimeOffset(2026, 3, 1, 4, 0, 0, TimeSpan.FromHours(-5));

        // Act
        Leg leg = Any.Leg().WithDeparture(departure).WithDuration(TimeSpan.FromHours(1)).Generate();
        Leg crossing = new Leg(new UnLocode("FRLEH"), new UnLocode("USNYC"), departure, arrival);

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), crossing.Duration);
        Assert.Equal(departure, leg.Departure);
    }

    [Fact]
    public void A_leg_cannot_leave_and_arrive_at_the_same_port() {
        // Arrange
        UnLocode anyPort = Any.UnLocode().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyPort, new UnLocode(anyPort.Code), Calendar.At(1), Calendar.At(2)));
    }

    [Fact]
    public void A_null_origin_is_refused_with_an_argument_null_exception() {
        // Arrange
        UnLocode anyDestination = Any.UnLocode().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Leg(null!, anyDestination, Calendar.At(1), Calendar.At(2)));
    }

    [Fact]
    public void A_null_destination_is_refused_with_an_argument_null_exception() {
        // Arrange
        UnLocode anyOrigin = Any.UnLocode().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Leg(anyOrigin, null!, Calendar.At(1), Calendar.At(2)));
    }

    [Fact]
    public void A_leg_arriving_at_the_instant_it_departs_is_refused() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyLeg.Origin, anyLeg.Destination, anyLeg.Departure, anyLeg.Departure));
    }

    [Fact]
    public void A_leg_arriving_before_it_departs_is_refused() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyLeg.Origin, anyLeg.Destination, anyLeg.Departure, anyLeg.Departure.AddTicks(-1)));
    }

    [Fact]
    public void A_leg_lasting_exactly_ninety_days_is_accepted() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Act
        Leg leg = new Leg(anyLeg.Origin, anyLeg.Destination, anyLeg.Departure, anyLeg.Departure.AddDays(90));

        // Assert
        Assert.Equal(TimeSpan.FromDays(90), leg.Duration);
        Assert.Equal(Leg.MaxTransit, leg.Duration);
    }

    [Fact]
    public void A_leg_lasting_a_tick_more_than_ninety_days_is_refused() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Leg(anyLeg.Origin, anyLeg.Destination, anyLeg.Departure, anyLeg.Departure.AddDays(90).AddTicks(1)));
    }

    [Fact]
    public void A_leg_lasting_one_tick_is_accepted() {
        // Arrange
        Leg anyLeg = Any.Leg().Generate();

        // Act
        Leg leg = new Leg(anyLeg.Origin, anyLeg.Destination, anyLeg.Departure, anyLeg.Departure.AddTicks(1));

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), leg.Duration);
    }

}
