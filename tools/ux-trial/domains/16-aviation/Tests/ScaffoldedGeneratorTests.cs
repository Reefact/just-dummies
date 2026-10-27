using Aviation.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Aviation.Tests;

/// <summary>
/// One harness check per scaffolded generator: 500 draws, and the real constructor accepts every one.
/// These are not unit tests of the domain; they check the recipes `any` wrote (and that I fixed by hand).
/// </summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyIataAirportCode_always_builds_a_valid_IataAirportCode() => AlwaysBuilds(Any.IataAirportCode());

    [Fact]
    public void Scaffolded_AnyIcaoAirportCode_always_builds_a_valid_IcaoAirportCode() => AlwaysBuilds(Any.IcaoAirportCode());

    [Fact]
    public void Scaffolded_AnyFlightNumber_always_builds_a_valid_FlightNumber() => AlwaysBuilds(Any.FlightNumber());

    [Fact]
    public void Scaffolded_AnyAircraftRegistration_always_builds_a_valid_AircraftRegistration() => AlwaysBuilds(Any.AircraftRegistration());

    [Fact]
    public void Scaffolded_AnyAircraft_always_builds_a_valid_Aircraft() => AlwaysBuilds(Any.Aircraft());

    [Fact]
    public void Scaffolded_AnyRoute_always_builds_a_valid_Route() => AlwaysBuilds(Any.Route());

    [Fact]
    public void Scaffolded_AnySlot_always_builds_a_valid_Slot() => AlwaysBuilds(Arbitrary.Slot());

    [Fact]
    public void Scaffolded_AnyFlight_always_builds_a_valid_Flight() => AlwaysBuilds(Any.Flight());

    /// <summary>
    /// The generator exactly as `any generate AircraftRegistration` emitted it (AsScaffolded/), untouched.
    /// The recap said "1 of 1 parameters inferred" with no "to verify": the file compiled at once, yet the
    /// constructor guards the mark with a switch expression into a local, which the scaffolder never saw.
    /// </summary>
    [Fact]
    public void Bug_Scaffolded_AnyAircraftRegistration_as_emitted_draws_marks_the_constructor_rejects() {
        AlwaysBuilds(new AsScaffolded.AnyAircraftRegistration());
    }

}
