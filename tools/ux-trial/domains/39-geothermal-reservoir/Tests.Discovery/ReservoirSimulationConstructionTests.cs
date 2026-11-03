using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class ReservoirSimulationConstructionTests {

    [Fact]
    public void Keeps_its_grid_boundary_and_wells() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().WithCellsX(2).WithCellsY(2).WithCellsZ(1).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              injector    = Any.Well().WithKind(WellKind.Injector).WithId("01").WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              producer    = Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(new CellCoordinate(1, 1, 0)).Generate();

        // Act
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [injector, producer], anyBoundary);

        // Assert
        Assert.Same(anyGrid, simulation.Grid);
        Assert.Same(anyBoundary, simulation.Boundary);
        Assert.Equivalent(new[] { injector, producer }, simulation.Wells);
    }

    [Fact]
    public void Starts_at_rest_with_empty_balances() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Generate();

        Assert.Equal(0.0, anySimulation.ElapsedSeconds);
        Assert.Equal(0, anySimulation.StepsTaken);
        Assert.Equal(0.0, anySimulation.CumulativeInjectedCubicMetres);
        Assert.Equal(0.0, anySimulation.CumulativeProducedCubicMetres);
        Assert.Equal(0.0, anySimulation.CumulativeBoundaryInflowCubicMetres);
        Assert.Equal(0.0, anySimulation.StoredVolumeChangeCubicMetres());
        Assert.Equal(0.0, anySimulation.MassBalanceErrorCubicMetres());
    }

    [Fact]
    public void Accepts_a_reservoir_without_any_well() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithoutWells().Generate();

        Assert.Empty(anySimulation.Wells);
    }

    [Fact]
    public void Refuses_missing_arguments() {
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(null!, [], anyBoundary));
        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, null!, anyBoundary));
        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, [], null!));
    }

    [Fact]
    public void Refuses_a_null_well_in_the_list() {
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(anyGrid, [null!], anyBoundary));

        Assert.Equal("wells", error.ParamName);
    }

    [Fact]
    public void Refuses_a_well_completed_outside_the_grid() {
        GeologicalGrid    grid        = Any.GeologicalGrid().WithCellsX(2).WithCellsY(2).WithCellsZ(2).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        int               anyBeyond   = Any.Int32().Between(2, 1_000_000).Generate();
        Well              outside     = Any.Well().WithCompletion(new CellCoordinate(0, 0, anyBeyond)).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(grid, [outside], anyBoundary));

        Assert.Contains("outside the grid", error.Message);
    }

    [Fact]
    public void Refuses_two_wells_with_the_same_id() {
        GeologicalGrid    grid        = Any.GeologicalGrid().WithCellsX(2).WithCellsY(1).WithCellsZ(1).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              first       = Any.Well().WithKind(WellKind.Producer).WithId("05").WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              second      = Any.Well().WithKind(WellKind.Producer).WithId("05").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(grid, [first, second], anyBoundary));

        Assert.Contains("PRD-05", error.Message);
    }

    [Fact]
    public void Refuses_two_wells_in_the_same_cell() {
        GeologicalGrid    grid        = Any.GeologicalGrid().WithCellsX(2).WithCellsY(1).WithCellsZ(1).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              injector    = Any.Well().WithKind(WellKind.Injector).WithId("01").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();
        Well              producer    = Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(grid, [injector, producer], anyBoundary));

        Assert.Contains("Two wells", error.Message);
    }

    [Fact]
    public void An_injector_and_a_producer_may_share_a_number() {
        GeologicalGrid    grid        = Any.GeologicalGrid().WithCellsX(2).WithCellsY(1).WithCellsZ(1).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              injector    = Any.Well().WithKind(WellKind.Injector).WithId("01").WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              producer    = Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        ReservoirSimulation simulation = new ReservoirSimulation(grid, [injector, producer], anyBoundary);

        Assert.Equal(2, simulation.Wells.Count);
    }

}
