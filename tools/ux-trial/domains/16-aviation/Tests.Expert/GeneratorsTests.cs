using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert;

// The bases written in Generators/ stand for the domain's rules: if one of them drifts from its constructor,
// every other test would fail far from the cause. These tests fail at the cause.
public sealed class GeneratorsTests {

    private const int Draws = 2_000;

    [Fact]
    public void Every_generator_draws_values_its_constructor_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.IataAirportCode().Generate();
            Any.IcaoAirportCode().Generate();
            Any.FlightNumber().Generate();
            Any.AircraftRegistration().Generate();
            Any.AircraftRegistration().UnitedStates().Generate();
            Any.AircraftRegistration().Foreign().Generate();
            Any.Route().Generate();
            Any.Slot().Generate();
            Any.Aircraft().Generate();
            Any.Flight().Generate();
        }
    }

    [Fact]
    public void A_drawn_route_never_joins_an_airport_to_itself_even_when_one_end_is_pinned() {
        // Arrange
        IataAirportCode anyAirport = Any.IataAirportCode().Generate();

        for (int draw = 0; draw < Draws; draw++) {
            // Act
            Route fromThere = Any.Route().WithOrigin(anyAirport).Generate();
            Route toThere   = Any.Route().WithDestination(anyAirport).Generate();

            // Assert
            Assert.NotEqual(anyAirport, fromThere.Destination);
            Assert.NotEqual(anyAirport, toThere.Origin);
        }
    }

}
