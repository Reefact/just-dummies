using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

/// <summary>
///     The generators of this project are code we own: a bug in one would make every test above lie. Each draws a few
///     hundred values; a draw the domain refuses throws from <c>Generate()</c> and fails the test, with its seed.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    private static void DrawMany<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.NotNull(generator.Generate());
        }
    }

    [Fact] public void Bus_ids_are_always_acceptable() { DrawMany(Any.BusId()); }

    [Fact] public void Impedances_are_always_acceptable() { DrawMany(Any.Impedance()); }

    [Fact] public void Power_injections_are_always_acceptable() { DrawMany(Any.PowerInjection()); }

    [Fact] public void Voltage_limits_are_always_acceptable() { DrawMany(Any.VoltageLimits()); }

    [Fact] public void Solver_settings_are_always_acceptable() { DrawMany(Any.SolverSettings()); }

    [Fact] public void Buses_are_always_acceptable() { DrawMany(Any.Bus()); }

    [Fact] public void Lines_are_always_acceptable() { DrawMany(Any.Line()); }

    [Fact] public void Transformers_are_always_acceptable() { DrawMany(Any.Transformer()); }

    [Fact] public void Branch_flows_are_always_acceptable() { DrawMany(Any.BranchFlow()); }

    [Fact] public void Load_flow_results_are_always_acceptable() { DrawMany(Any.LoadFlowResult()); }

    [Fact] public void Grid_networks_are_always_acceptable() { DrawMany(Any.GridNetwork()); }

    [Fact]
    public void A_drawn_bus_regulates_its_voltage_exactly_when_it_is_not_a_load_bus() {
        for (int draw = 0; draw < Draws; draw++) {
            Bus bus = Any.Bus().Generate();

            Assert.Equal(bus.Type != BusType.Pq, bus.VoltageSetpointPu.HasValue);
        }
    }

    [Fact]
    public void Every_bus_type_is_drawn() {
        HashSet<BusType> seen = new();
        for (int draw = 0; draw < Draws; draw++) {
            seen.Add(Any.Bus().Generate().Type);
        }

        Assert.Equal(3, seen.Count);
    }

}
