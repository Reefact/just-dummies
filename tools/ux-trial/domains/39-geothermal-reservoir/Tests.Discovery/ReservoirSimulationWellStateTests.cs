using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class ReservoirSimulationWellStateTests {

    private static ReservoirSimulation SimulationWith(Well first, Well second) {
        GeologicalGrid    grid     = Any.GeologicalGrid().WithCellsX(2).WithCellsY(1).WithCellsZ(1).Generate();
        BoundaryCondition boundary = Any.BoundaryCondition().Generate();

        return new ReservoirSimulation(grid, [first, second], boundary);
    }

    [Fact]
    public void Wells_start_flowing() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              wellId        = anySimulation.Wells.Single().Id;

        Assert.False(anySimulation.IsShutIn(wellId));
    }

    [Fact]
    public void Shutting_in_a_well_marks_it_shut_in_and_only_it() {
        // Arrange
        Well first  = Any.Well().WithKind(WellKind.Injector).WithId("01").WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well second = Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();
        ReservoirSimulation simulation = SimulationWith(first, second);

        // Act
        simulation.ShutIn("INJ-01");

        // Assert
        Assert.True(simulation.IsShutIn("INJ-01"));
        Assert.False(simulation.IsShutIn("PRD-01"));
    }

    [Fact]
    public void Reopening_a_shut_in_well_puts_it_back_on_stream() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              wellId        = anySimulation.Wells.Single().Id;
        anySimulation.ShutIn(wellId);

        anySimulation.Reopen(wellId);

        Assert.False(anySimulation.IsShutIn(wellId));
    }

    [Fact]
    public void A_well_can_be_shut_in_again_after_being_reopened() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              wellId        = anySimulation.Wells.Single().Id;
        anySimulation.ShutIn(wellId);
        anySimulation.Reopen(wellId);

        anySimulation.ShutIn(wellId);

        Assert.True(anySimulation.IsShutIn(wellId));
    }

    [Fact]
    public void Shutting_in_a_well_twice_is_refused() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              wellId        = anySimulation.Wells.Single().Id;
        anySimulation.ShutIn(wellId);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anySimulation.ShutIn(wellId));

        Assert.Contains("already shut in", error.Message);
    }

    [Fact]
    public void Reopening_a_flowing_well_is_refused() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              wellId        = anySimulation.Wells.Single().Id;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anySimulation.Reopen(wellId));

        Assert.Contains("not shut in", error.Message);
    }

    [Fact]
    public void An_unknown_well_is_refused_by_every_well_operation() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).Generate();
        string              unknownId     = "INJ-" + Any.Int32().Between(0, 99).Generate().ToString("00") + "x";

        Assert.Throws<ArgumentException>(() => anySimulation.ShutIn(unknownId));
        Assert.Throws<ArgumentException>(() => anySimulation.Reopen(unknownId));
        Assert.Throws<ArgumentException>(() => anySimulation.IsShutIn(unknownId));
    }

    [Fact]
    public void A_missing_well_id_is_refused() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Generate();

        Assert.Throws<ArgumentNullException>(() => anySimulation.ShutIn(null!));
        Assert.Throws<ArgumentNullException>(() => anySimulation.Reopen(null!));
        Assert.Throws<ArgumentNullException>(() => anySimulation.IsShutIn(null!));
    }

    [Fact]
    public void A_simulation_without_wells_refuses_any_id() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithoutWells().Generate();

        Assert.Throws<ArgumentException>(() => anySimulation.IsShutIn("INJ-01"));
    }

    [Fact]
    public void Well_ids_are_compared_exactly() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithGrid(g => g.WithCellsX(1).WithCellsY(1).WithCellsZ(1))
                                               .WithWellCount(1).WithWells(w => w.WithKind(WellKind.Injector)).Generate();

        Assert.False(anySimulation.IsShutIn("INJ-00"));
        Assert.Throws<ArgumentException>(() => anySimulation.IsShutIn("inj-00"));
    }

}
