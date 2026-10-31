using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class ReservoirSimulationTests {

    private static readonly Pressure InitialPressure = Pressure.FromBars(200);
    private static readonly TimeStep OneHour         = new TimeStep(3600);

    // A sandstone of 100 m cells at 200 bar. These properties are not dummies: they size the storage
    // and the stability limit, and a one-hour step at up to 0.5 m³/s stays physical only because of them.
    private static AnyGeologicalGrid Sandstone() {
        return Any.GeologicalGrid()
                  .WithCellSizeMetres(100.0)
                  .WithCells(c => c.WithPorosity(new Porosity(0.2))
                                   .WithPermeability(new Permeability(1e-15))
                                   .WithPressure(InitialPressure));
    }

    private static BoundaryCondition SealedBoundary() {
        return Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();
    }

    private static CellCoordinate AnyCellOf(GeologicalGrid grid) {
        return Any.CellCoordinate()
                  .WithI(i => i.LessThan(grid.CellsX))
                  .WithJ(j => j.LessThan(grid.CellsY))
                  .WithK(k => k.LessThan(grid.CellsZ))
                  .Generate();
    }

    [Fact]
    public void Two_wells_cannot_be_completed_in_the_same_cell() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        CellCoordinate    anyCell     = AnyCellOf(anyGrid);
        Well              injector    = Any.Well().WithId("INJ-01").WithKind(WellKind.Injector).WithCompletion(anyCell).Generate();
        Well              producer    = Any.Well().WithId("PRD-01").WithKind(WellKind.Producer).WithCompletion(anyCell).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        // Act
        Exception? error = Record.Exception(() => new ReservoirSimulation(anyGrid, [injector, producer], anyBoundary));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Contains("Two wells", argumentError.Message);
    }

    [Fact]
    public void A_well_must_be_completed_inside_the_grid() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().WithDimensions(2, 2, 2).Generate();
        Well              anyWell     = Any.Well().WithCompletion(new CellCoordinate(0, 0, 2)).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        // Act
        Exception? error = Record.Exception(() => new ReservoirSimulation(anyGrid, [anyWell], anyBoundary));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Contains("outside the grid", argumentError.Message);
    }

    [Fact]
    public void Two_wells_cannot_share_an_id() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().WithDimensions(2, 1, 1).Generate();
        Well              first       = Any.Well().WithId("PRD-07").WithKind(WellKind.Producer).WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              second      = Any.Well().WithId("PRD-07").WithKind(WellKind.Producer).WithCompletion(new CellCoordinate(1, 0, 0)).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        // Act
        Exception? error = Record.Exception(() => new ReservoirSimulation(anyGrid, [first, second], anyBoundary));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Contains("used twice", argumentError.Message);
    }

    [Fact]
    public void The_transmissibility_between_two_cells_follows_darcy_across_their_shared_face() {
        // Arrange
        GeologicalGrid      grid       = Sandstone().WithDimensions(2, 1, 1).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], Any.BoundaryCondition().Generate());

        // Act
        double transmissibility = simulation.Transmissibility(grid.CellAt(new CellCoordinate(0, 0, 0)),
                                                              grid.CellAt(new CellCoordinate(1, 0, 0)));

        // Assert
        // k·A / (mu·dx) = 1e-15 · 100² / (3e-4 · 100)
        Assert.Equal(1e-15 * 10_000 / (3e-4 * 100), transmissibility, 1e-22);
    }

    [Fact]
    public void Cells_that_do_not_share_a_face_have_no_transmissibility() {
        // Arrange
        GeologicalGrid      anyGrid    = Any.GeologicalGrid().WithDimensions(3, 1, 1).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [], Any.BoundaryCondition().Generate());

        // Act
        Exception? error = Record.Exception(() => simulation.Transmissibility(anyGrid.CellAt(new CellCoordinate(0, 0, 0)),
                                                                              anyGrid.CellAt(new CellCoordinate(2, 0, 0))));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_sealed_reservoir_without_wells_at_uniform_pressure_keeps_its_pressure() {
        // Arrange
        GeologicalGrid      anyGrid    = Sandstone().Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [], SealedBoundary());

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.All(anyGrid.Cells, cell => Assert.Equal(InitialPressure, cell.Pressure));
    }

    [Fact]
    public void Injecting_raises_the_pressure_of_the_injector_cell() {
        // Arrange
        GeologicalGrid      anyGrid     = Sandstone().Generate();
        CellCoordinate      anyWellCell = AnyCellOf(anyGrid);
        Well                anyInjector = Any.Well().WithKind(WellKind.Injector).WithCompletion(anyWellCell).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyInjector], SealedBoundary());

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.True(anyGrid.CellAt(anyWellCell).Pressure.Pascals > InitialPressure.Pascals);
    }

    [Fact]
    public void Producing_lowers_the_pressure_of_the_producer_cell() {
        // Arrange
        GeologicalGrid      anyGrid     = Sandstone().Generate();
        CellCoordinate      anyWellCell = AnyCellOf(anyGrid);
        Well                anyProducer = Any.Well().WithKind(WellKind.Producer).WithCompletion(anyWellCell).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyProducer], SealedBoundary());

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.True(anyGrid.CellAt(anyWellCell).Pressure.Pascals < InitialPressure.Pascals);
    }

    [Fact]
    public void Each_step_adds_the_injected_volume_of_every_flowing_injector() {
        // Arrange
        GeologicalGrid      anyGrid     = Sandstone().Generate();
        Well                anyInjector = Any.Well().WithKind(WellKind.Injector).WithRateCubicMetresPerSecond(0.05).WithCompletion(AnyCellOf(anyGrid)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyInjector], SealedBoundary());

        // Act
        simulation.Advance(OneHour);
        simulation.Advance(OneHour);

        // Assert
        Assert.Equal(360.0, simulation.CumulativeInjectedCubicMetres, 1e-9);
        Assert.Equal(7200.0, simulation.ElapsedSeconds);
        Assert.Equal(2, simulation.StepsTaken);
    }

    [Fact]
    public void Fluid_volume_is_conserved_with_wells_and_an_open_boundary() {
        // Arrange
        GeologicalGrid    anyGrid      = Sandstone().WithDimensions(3, 3, 2).Generate();
        Well              anyInjector  = Any.Well().WithKind(WellKind.Injector).WithCompletion(new CellCoordinate(0, 0, 1)).Generate();
        Well              anyProducer  = Any.Well().WithKind(WellKind.Producer).WithCompletion(new CellCoordinate(2, 2, 0)).Generate();
        BoundaryCondition anyAquifer   = Any.BoundaryCondition().WithTransmissibilityMultiplier(m => m.GreaterThan(0.0)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [anyInjector, anyProducer], anyAquifer);

        // Act
        simulation.Advance(OneHour);
        simulation.Advance(OneHour);
        simulation.Advance(OneHour);

        // Assert
        Assert.Equal(0.0, simulation.MassBalanceErrorCubicMetres(), 1e-6);
    }

    [Fact]
    public void A_shut_in_injector_injects_nothing() {
        // Arrange
        GeologicalGrid      anyGrid     = Sandstone().Generate();
        Well                anyInjector = Any.Well().WithId("INJ-01").WithKind(WellKind.Injector).WithCompletion(AnyCellOf(anyGrid)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyInjector], SealedBoundary());
        simulation.ShutIn("INJ-01");

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
        Assert.All(anyGrid.Cells, cell => Assert.Equal(InitialPressure, cell.Pressure));
    }

    [Fact]
    public void A_reopened_well_is_back_on_stream() {
        // Arrange
        GeologicalGrid      anyGrid     = Any.GeologicalGrid().Generate();
        Well                anyProducer = Any.Well().WithId("PRD-03").WithKind(WellKind.Producer).WithCompletion(AnyCellOf(anyGrid)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyProducer], Any.BoundaryCondition().Generate());
        simulation.ShutIn("PRD-03");

        // Act
        simulation.Reopen("PRD-03");

        // Assert
        Assert.False(simulation.IsShutIn("PRD-03"));
    }

    [Fact]
    public void Shutting_in_a_well_twice_is_refused() {
        // Arrange
        GeologicalGrid      anyGrid     = Any.GeologicalGrid().Generate();
        Well                anyProducer = Any.Well().WithId("PRD-03").WithKind(WellKind.Producer).WithCompletion(AnyCellOf(anyGrid)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyProducer], Any.BoundaryCondition().Generate());
        simulation.ShutIn("PRD-03");

        // Act
        Exception? error = Record.Exception(() => simulation.ShutIn("PRD-03"));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Reopening_a_flowing_well_is_refused() {
        // Arrange
        GeologicalGrid      anyGrid     = Any.GeologicalGrid().Generate();
        Well                anyInjector = Any.Well().WithId("INJ-02").WithKind(WellKind.Injector).WithCompletion(AnyCellOf(anyGrid)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(anyGrid, [anyInjector], Any.BoundaryCondition().Generate());

        // Act
        Exception? error = Record.Exception(() => simulation.Reopen("INJ-02"));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Shutting_in_an_unknown_well_is_refused() {
        // Arrange
        GeologicalGrid      anyGrid    = Any.GeologicalGrid().Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [], Any.BoundaryCondition().Generate());

        // Act
        Exception? error = Record.Exception(() => simulation.ShutIn("PRD-99"));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_sealed_boundary_lets_no_fluid_in_from_a_higher_pressure_aquifer() {
        // Arrange
        GeologicalGrid      anyGrid    = Sandstone().Generate();
        BoundaryCondition   sealedHigh = Any.BoundaryCondition().WithFarFieldPressure(Pressure.FromBars(300)).WithTransmissibilityMultiplier(0.0).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [], sealedHigh);

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.Equal(0.0, simulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void An_open_boundary_lets_fluid_in_from_a_higher_pressure_aquifer() {
        // Arrange
        GeologicalGrid      anyGrid    = Sandstone().Generate();
        BoundaryCondition   openHigh   = Any.BoundaryCondition().WithFarFieldPressure(Pressure.FromBars(300)).WithTransmissibilityMultiplier(m => m.GreaterThan(0.0)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [], openHigh);

        // Act
        simulation.Advance(OneHour);

        // Assert
        Assert.True(simulation.CumulativeBoundaryInflowCubicMetres > 0.0);
    }

    [Fact]
    public void Heat_conducts_from_the_hotter_cell_to_the_colder_one() {
        // Arrange
        List<RockCell> cells = [
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPressure(InitialPressure).WithTemperature(Temperature.FromCelsius(100)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPressure(InitialPressure).WithTemperature(Temperature.FromCelsius(50)).Generate()
        ];
        double              anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();
        GeologicalGrid      grid        = new GeologicalGrid(2, 1, 1, anyCellSize, cells);
        ReservoirSimulation simulation  = new ReservoirSimulation(grid, [], SealedBoundary());
        TimeStep            halfStable  = new TimeStep(Math.Min(simulation.MaximumStableTimeStepSeconds() / 2, TimeStep.MaximumSeconds));

        // Act
        simulation.Advance(halfStable);

        // Assert
        Assert.True(cells[0].Temperature.Celsius < 100.0);
        Assert.True(cells[1].Temperature.Celsius > 50.0);
    }

    [Fact]
    public void A_step_beyond_the_stability_limit_is_refused_and_changes_nothing() {
        // Arrange
        GeologicalGrid      grid       = Sandstone().WithDimensions(2, 1, 1).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], SealedBoundary());
        TimeStep            tooLong    = new TimeStep(1.5 * simulation.MaximumStableTimeStepSeconds());

        // Act
        Exception? error = Record.Exception(() => simulation.Advance(tooLong));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, simulation.StepsTaken);
    }

    [Fact]
    public void A_step_that_would_drain_a_cell_below_zero_pressure_is_refused_and_changes_nothing() {
        // Arrange
        GeologicalGrid      singleCell  = Sandstone().WithDimensions(1, 1, 1).Generate();
        Well                anyProducer = Any.Well().WithKind(WellKind.Producer).WithRateCubicMetresPerSecond(0.5).WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        ReservoirSimulation simulation  = new ReservoirSimulation(singleCell, [anyProducer], SealedBoundary());

        // Act
        // 0.5 m³/s for a day out of a 100 m cell (storage 3e-4 m³/Pa) would remove 1440 bar from 200 bar.
        Exception? error = Record.Exception(() => simulation.Advance(new TimeStep(86_400)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(InitialPressure, singleCell.CellAt(new CellCoordinate(0, 0, 0)).Pressure);
        Assert.Equal(0, simulation.StepsTaken);
    }

}
