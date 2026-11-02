using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

// The scaffolded generators are code of this test project: they are held to the domain's rules.
public sealed class GeneratorTests {

    [Fact]
    public void Every_generator_draws_values_the_domain_accepts() {
        // Act
        List<Flight>               flights       = Any.ListOf(Arbitrary.Flight()).WithCount(300).Generate();
        List<Slot>                 slots         = Any.ListOf(Arbitrary.Slot()).WithCount(300).Generate();
        List<AircraftRegistration> registrations = Any.ListOf(Arbitrary.AircraftRegistration()).WithCount(300).Generate();
        List<FlightNumber>         flightNumbers = Any.ListOf(Arbitrary.FlightNumber()).WithCount(300).Generate();
        List<IcaoAirportCode>      icaoCodes     = Any.ListOf(Arbitrary.IcaoAirportCode()).WithCount(300).Generate();

        // Assert
        Assert.All(flights, f => Assert.NotEqual(f.Route.Origin, f.Route.Destination));
        Assert.All(flights, f => Assert.True(f.Aircraft.Seats >= 1));
        Assert.All(slots, s => Assert.Equal(0, s.Start.Minute % 5));
        Assert.All(registrations, r => Assert.Equal(r.Mark[0] == 'N', r.IsUnitedStates));
        Assert.All(flightNumbers, n => Assert.InRange(n.Number, 1, 9999));
        Assert.All(icaoCodes, c => Assert.Equal(4, c.Value.Length));
    }

    [Fact]
    public void The_arbitrary_aircraft_cover_every_known_type_over_enough_draws() {
        // Act
        List<Aircraft> aircraft = Any.ListOf(Arbitrary.Aircraft()).WithCount(500).Generate();

        // Assert
        Assert.Equal(Aircraft.KnownTypes.Count, aircraft.Select(a => a.TypeDesignator).Distinct().Count());
    }

}
