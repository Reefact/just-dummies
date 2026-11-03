using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class ReservoirSimulationTransmissibilityTests {

    private static ReservoirSimulation SimulationOfTwoCellsWith(double leftPermeability, double rightPermeability, out RockCell left, out RockCell right) {
        left = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPermeability(new Permeability(leftPermeability)).Generate();
        right = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPermeability(new Permeability(rightPermeability)).Generate();
        GeologicalGrid grid = new GeologicalGrid(2, 1, 1, 10.0, [left, right]);

        return new ReservoirSimulation(grid, [], Scenarios.SealedBoundary());
    }

    [Fact]
    public void Between_two_equal_rocks_it_is_k_times_area_over_viscosity_and_distance() {
        // 1e-12 m2 across a 100 m2 face and 10 m, at 3e-4 Pa.s.
        ReservoirSimulation simulation = SimulationOfTwoCellsWith(1e-12, 1e-12, out RockCell left, out RockCell right);

        double transmissibility = simulation.Transmissibility(left, right);

        Assert.Equal(1e-12 * 100.0 / (3e-4 * 10.0), transmissibility, 1e-20);
    }

    [Fact]
    public void Between_two_different_rocks_it_uses_their_harmonic_mean() {
        ReservoirSimulation simulation = SimulationOfTwoCellsWith(1e-12, 3e-12, out RockCell left, out RockCell right);

        double transmissibility = simulation.Transmissibility(left, right);

        Assert.Equal(1.5e-12 * 100.0 / (3e-4 * 10.0), transmissibility, 1e-20);
    }

    [Fact]
    public void It_does_not_depend_on_the_direction() {
        Permeability anyLeftPermeability  = Any.Permeability().Generate();
        Permeability anyRightPermeability = Any.Permeability().Generate();
        ReservoirSimulation simulation = SimulationOfTwoCellsWith(anyLeftPermeability.SquareMetres, anyRightPermeability.SquareMetres, out RockCell left, out RockCell right);

        Assert.Equal(simulation.Transmissibility(left, right), simulation.Transmissibility(right, left));
    }

    [Fact]
    public void It_is_positive() {
        Permeability anyLeftPermeability  = Any.Permeability().Generate();
        Permeability anyRightPermeability = Any.Permeability().Generate();
        ReservoirSimulation simulation = SimulationOfTwoCellsWith(anyLeftPermeability.SquareMetres, anyRightPermeability.SquareMetres, out RockCell left, out RockCell right);

        Assert.True(simulation.Transmissibility(left, right) > 0.0);
    }

    [Fact]
    public void Refuses_two_cells_that_only_touch_by_an_edge() {
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(2).WithCellsY(2).WithCellsZ(1).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], Scenarios.SealedBoundary());

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => simulation.Transmissibility(grid.CellAt(new CellCoordinate(0, 0, 0)), grid.CellAt(new CellCoordinate(1, 1, 0))));

        Assert.Contains("do not share a face", error.Message);
    }

    [Fact]
    public void Refuses_a_cell_paired_with_itself() {
        GeologicalGrid grid = Any.GeologicalGrid().Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], Scenarios.SealedBoundary());
        RockCell anyCell = grid.Cells.First();

        Assert.Throws<ArgumentException>(() => simulation.Transmissibility(anyCell, anyCell));
    }

    [Fact]
    public void Refuses_a_missing_cell() {
        GeologicalGrid grid = Any.GeologicalGrid().Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], Scenarios.SealedBoundary());
        RockCell anyCell = grid.Cells.First();

        Assert.Throws<ArgumentNullException>(() => simulation.Transmissibility(null!, anyCell));
        Assert.Throws<ArgumentNullException>(() => simulation.Transmissibility(anyCell, null!));
    }

}

public sealed class ReservoirSimulationStabilityLimitTests {

    [Fact]
    public void A_lone_sealed_cell_with_no_well_has_no_limit() {
        ReservoirSimulation simulation = Scenarios.LoneCell(well: null);

        Assert.True(double.IsPositiveInfinity(simulation.MaximumStableTimeStepSeconds()));
    }

    [Fact]
    public void An_injector_limits_the_step_to_the_time_it_takes_to_replace_the_heat_of_its_cell() {
        // 2.836e9 J/K of heat capacity against 4.18e6 J/(m3.K) x 0.01 m3/s of cold water.
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));

        Assert.Equal(2.836e9 / 4.18e4, simulation.MaximumStableTimeStepSeconds(), 1e-6);
    }

    [Fact]
    public void Shutting_in_the_injector_lifts_the_limit() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));

        simulation.ShutIn("INJ-01");

        Assert.True(double.IsPositiveInfinity(simulation.MaximumStableTimeStepSeconds()));
    }

    [Fact]
    public void A_producer_does_not_limit_the_step() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.ProducerAtOrigin(0.01));

        Assert.True(double.IsPositiveInfinity(simulation.MaximumStableTimeStepSeconds()));
    }

    [Fact]
    public void A_reservoir_with_neighbours_has_a_finite_positive_limit() {
        ReservoirSimulation anySimulation = Scenarios.AnyWorkingReservoir()
                                                     .WithGrid(g => g.WithCellsX(2))
                                                     .Generate();

        double limit = anySimulation.MaximumStableTimeStepSeconds();

        Assert.True(limit > 0.0 && double.IsFinite(limit), $"limit was {limit}");
    }

    [Fact]
    public void A_tighter_rock_tolerates_a_longer_step() {
        // Same geometry and state, only the permeability differs: less flow coupling, more room.
        double Limit(double permeability) {
            RockCell left  = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPorosity(new Porosity(0.2)).WithPermeability(new Permeability(permeability)).Generate();
            RockCell right = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPorosity(new Porosity(0.2)).WithPermeability(new Permeability(permeability)).Generate();
            GeologicalGrid grid = new GeologicalGrid(2, 1, 1, 10.0, [left, right]);

            return new ReservoirSimulation(grid, [], Scenarios.SealedBoundary()).MaximumStableTimeStepSeconds();
        }

        Assert.True(Limit(1e-14) > Limit(1e-12));
    }

}
