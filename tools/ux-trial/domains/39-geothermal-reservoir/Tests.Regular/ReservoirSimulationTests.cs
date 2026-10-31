using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Regular;

public sealed class ReservoirSimulationTests {

    private static readonly CellCoordinate Origin = new(0, 0, 0);

    [Fact]
    public void Two_wells_cannot_be_completed_in_the_same_cell() {
        // Arrange
        GeologicalGrid anyGrid     = Arbitrary.GeologicalGrid().Generate();
        Well           anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin).Generate();
        Well           anyProducer = Arbitrary.Well().WithKind(WellKind.Producer).WithCompletion(Origin).Generate();

        // Act
        Action build = () => Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyInjector, anyProducer]).Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void Two_wells_cannot_share_an_id() {
        // Arrange
        GeologicalGrid anyGrid  = Arbitrary.GeologicalGrid().WithCellsX(2).Generate();
        AnyWell        producer = Arbitrary.Well().WithKind(WellKind.Producer).WithId("03");
        Well           anyFirst  = producer.WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well           anySecond = producer.WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        // Act
        Action build = () => Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyFirst, anySecond]).Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_well_completed_outside_the_grid_is_refused() {
        // Arrange
        GeologicalGrid anyGrid = Arbitrary.GeologicalGrid().WithCellsX(2).Generate();
        Well           anyWell = Arbitrary.Well().WithCompletion(new CellCoordinate(2, 0, 0)).Generate();

        // Act
        Action build = () => Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyWell]).Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void Transmissibility_follows_darcy_across_a_shared_face() {
        // Arrange
        // 1e-13 m² both sides, 100 m cells: k·A / (mu·dx) = 1e-13 · 1e4 / (3e-4 · 100) = 3.33e-8 m³/(s·Pa).
        GeologicalGrid anyGrid = Arbitrary.GeologicalGrid()
                                          .WithCellsX(2).WithCellsY(1).WithCellsZ(1)
                                          .WithCellSizeMetres(100.0)
                                          .WithCells(c => c.WithPermeability(new Permeability(1e-13)))
                                          .Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).Generate();

        // Act
        double transmissibility = anySimulation.Transmissibility(anyGrid.CellAt(Origin), anyGrid.CellAt(new CellCoordinate(1, 0, 0)));

        // Assert
        Assert.Equal(3.3333333333333333e-8, transmissibility, 1e-18);
    }

    [Fact]
    public void Cells_that_do_not_share_a_face_have_no_transmissibility() {
        // Arrange
        GeologicalGrid      anyGrid       = Arbitrary.GeologicalGrid().WithCellsX(3).WithCellsY(1).WithCellsZ(1).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).Generate();

        // Act
        Action transmissibility = () => anySimulation.Transmissibility(anyGrid.CellAt(Origin), anyGrid.CellAt(new CellCoordinate(2, 0, 0)));

        // Assert
        Assert.Throws<ArgumentException>(transmissibility);
    }

    [Fact]
    public void A_shut_in_well_is_reported_as_shut_in_and_reopening_puts_it_back_on_stream() {
        // Arrange
        Well                anyWell       = Arbitrary.Well().WithCompletion(Origin).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithWells([anyWell]).Generate();

        // Act
        anySimulation.ShutIn(anyWell.Id);
        bool shutIn = anySimulation.IsShutIn(anyWell.Id);
        anySimulation.Reopen(anyWell.Id);
        bool reopened = !anySimulation.IsShutIn(anyWell.Id);

        // Assert
        Assert.True(shutIn);
        Assert.True(reopened);
    }

    [Fact]
    public void Shutting_in_a_well_twice_is_refused() {
        // Arrange
        Well                anyWell       = Arbitrary.Well().WithCompletion(Origin).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithWells([anyWell]).Generate();
        anySimulation.ShutIn(anyWell.Id);

        // Act
        Action again = () => anySimulation.ShutIn(anyWell.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(again);
    }

    [Fact]
    public void Reopening_a_flowing_well_is_refused() {
        // Arrange
        Well                anyWell       = Arbitrary.Well().WithCompletion(Origin).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithWells([anyWell]).Generate();

        // Act
        Action reopen = () => anySimulation.Reopen(anyWell.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(reopen);
    }

    [Fact]
    public void Shutting_in_an_unknown_well_is_refused() {
        // Arrange
        ReservoirSimulation anySimulationWithoutWells = Arbitrary.ReservoirSimulation().Generate();

        // Act
        Action shutIn = () => anySimulationWithoutWells.ShutIn("PRD-01");

        // Assert
        Assert.Throws<ArgumentException>(shutIn);
    }

    [Fact]
    public void A_sealed_reservoir_at_equilibrium_without_wells_does_not_change() {
        // Arrange
        Pressure            pressure      = Pressure.FromBars(200.0);
        Temperature         temperature   = Temperature.FromCelsius(150.0);
        GeologicalGrid      anyGrid       = Sandstone.UniformGrid(3, 3, 2, pressure, temperature).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.All(anyGrid.Cells, c => Assert.Equal(pressure, c.Pressure));
        Assert.All(anyGrid.Cells, c => Assert.Equal(temperature, c.Temperature));
        Assert.Equal(Sandstone.OneHour, anySimulation.ElapsedSeconds);
        Assert.Equal(1, anySimulation.StepsTaken);
    }

    [Fact]
    public void A_step_beyond_the_stability_limit_is_refused_and_changes_nothing() {
        // Arrange
        GeologicalGrid      anyGrid       = Sandstone.Grid(3, 3, 3).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).Generate();
        Dictionary<CellCoordinate, Pressure> before = anyGrid.Cells.ToDictionary(c => c.Coordinate, c => c.Pressure);
        TimeStep tooLong = new(2.0 * anySimulation.MaximumStableTimeStepSeconds());

        // Act
        Action advance = () => anySimulation.Advance(tooLong);

        // Assert
        Assert.Throws<InvalidOperationException>(advance);
        Assert.All(anyGrid.Cells, c => Assert.Equal(before[c.Coordinate], c.Pressure));
        Assert.Equal(0, anySimulation.StepsTaken);
    }

    [Fact]
    public void Injecting_into_a_sealed_reservoir_stores_exactly_the_injected_volume() {
        // Arrange
        Well injector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin).WithRateCubicMetresPerSecond(0.01).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation()
                                                     .WithGrid(Sandstone.Grid(1, 1, 1).Generate())
                                                     .WithWells([injector])
                                                     .WithBoundary(Sandstone.Sealed())
                                                     .Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.Equal(36.0, anySimulation.CumulativeInjectedCubicMetres, 1e-9);
        Assert.Equal(36.0, anySimulation.StoredVolumeChangeCubicMetres(), 1e-6);
    }

    [Fact]
    public void Injection_raises_the_pressure_of_the_injector_cell() {
        // Arrange
        Pressure       initial  = Pressure.FromBars(200.0);
        GeologicalGrid anyGrid  = Sandstone.Grid(2, 1, 1).WithCells(c => Sandstone.Cell(c).WithPressure(initial)).Generate();
        Well           anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin)
                                              .WithRateCubicMetresPerSecond(r => r.Between(0.001, 0.01)).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyInjector]).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.True(anyGrid.CellAt(Origin).Pressure.Pascals > initial.Pascals);
    }

    [Fact]
    public void Injecting_colder_water_cools_the_injector_cell() {
        // Arrange
        Temperature    reservoir = Temperature.FromCelsius(150.0);
        Temperature    injected  = Temperature.FromCelsius(40.0);
        GeologicalGrid anyGrid   = Sandstone.UniformGrid(1, 1, 1, Pressure.FromBars(200.0), reservoir).Generate();
        Well           anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin)
                                              .WithRateCubicMetresPerSecond(r => r.Between(0.001, 0.01))
                                              .WithInjectionTemperature(injected).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyInjector]).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.True(anyGrid.CellAt(Origin).Temperature.Kelvin < reservoir.Kelvin);
    }

    [Fact]
    public void Production_draws_the_reservoir_down_and_is_counted_as_produced() {
        // Arrange
        Pressure       initial  = Pressure.FromBars(200.0);
        GeologicalGrid anyGrid  = Sandstone.Grid(1, 1, 1).WithCells(c => Sandstone.Cell(c).WithPressure(initial)).Generate();
        Well           producer = Arbitrary.Well().WithKind(WellKind.Producer).WithCompletion(Origin).WithRateCubicMetresPerSecond(0.02).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([producer]).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.Equal(72.0, anySimulation.CumulativeProducedCubicMetres, 1e-9);
        Assert.True(anyGrid.CellAt(Origin).Pressure.Pascals < initial.Pascals);
    }

    [Fact]
    public void A_shut_in_injector_injects_nothing() {
        // Arrange
        Pressure       pressure = Pressure.FromBars(200.0);
        GeologicalGrid anyGrid  = Sandstone.UniformGrid(1, 1, 1, pressure, Temperature.FromCelsius(150.0)).Generate();
        Well           anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin)
                                              .WithRateCubicMetresPerSecond(r => r.Between(0.001, 0.01)).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([anyInjector]).WithBoundary(Sandstone.Sealed()).Generate();
        anySimulation.ShutIn(anyInjector.Id);

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.Equal(0.0, anySimulation.CumulativeInjectedCubicMetres);
        Assert.Equal(pressure, anyGrid.CellAt(Origin).Pressure);
    }

    [Fact]
    public void A_step_that_would_empty_the_reservoir_is_refused_and_changes_nothing() {
        // Arrange
        Pressure       nearlyDepleted = Pressure.FromBars(2.0);
        GeologicalGrid anyGrid        = Sandstone.Grid(1, 1, 1).WithCells(c => Sandstone.Cell(c).WithPressure(nearlyDepleted)).Generate();
        Well           producer       = Arbitrary.Well().WithKind(WellKind.Producer).WithCompletion(Origin).WithRateCubicMetresPerSecond(0.5).Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithWells([producer]).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        Action advance = () => anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.Throws<InvalidOperationException>(advance);
        Assert.Equal(nearlyDepleted, anyGrid.CellAt(Origin).Pressure);
        Assert.Equal(0.0, anySimulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void Pressure_diffuses_from_the_high_pressure_cell_to_its_neighbour_without_losing_fluid() {
        // Arrange
        CellCoordinate east = new(1, 0, 0);
        double         anyCellSize = Any.Double().Between(100.0, 200.0).Generate();
        // Each cell needs its own initial pressure, which one cell recipe cannot say: build the pair by hand.
        GeologicalGrid anyUnbalanced = new(2, 1, 1, anyCellSize, [
            Sandstone.Cell(Arbitrary.RockCell()).WithCoordinate(Origin).WithPressure(Pressure.FromBars(250.0)).Generate(),
            Sandstone.Cell(Arbitrary.RockCell()).WithCoordinate(east).WithPressure(Pressure.FromBars(150.0)).Generate()
        ]);
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyUnbalanced).WithBoundary(Sandstone.Sealed()).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.True(anyUnbalanced.CellAt(Origin).Pressure.Bars < 250.0);
        Assert.True(anyUnbalanced.CellAt(east).Pressure.Bars > 150.0);
        Assert.Equal(0.0, anySimulation.StoredVolumeChangeCubicMetres(), 1e-6);
    }

    [Fact]
    public void An_aquifer_at_higher_pressure_flows_into_the_reservoir() {
        // Arrange
        GeologicalGrid    anyGrid  = Sandstone.Grid(2, 2, 1).WithCells(c => Sandstone.Cell(c).WithPressure(Pressure.FromBars(150.0))).Generate();
        BoundaryCondition anyAquifer = Arbitrary.BoundaryCondition()
                                                .WithFarFieldPressure(Pressure.FromBars(200.0))
                                                .WithFarFieldTemperature(t => t.WithKelvin(k => k.Between(333.15, 473.15)))
                                                .WithTransmissibilityMultiplier(m => m.Between(0.1, 1.0))
                                                .Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation().WithGrid(anyGrid).WithBoundary(anyAquifer).Generate();

        // Act
        anySimulation.Advance(new TimeStep(Sandstone.OneHour));

        // Assert
        Assert.True(anySimulation.CumulativeBoundaryInflowCubicMetres > 0.0);
    }

    [Fact]
    public void Fluid_volume_balances_over_several_steps_with_wells_and_an_open_boundary() {
        // Arrange
        CellCoordinate farCorner = new(2, 2, 1);
        GeologicalGrid anyGrid   = Sandstone.Grid(3, 3, 2).Generate();
        Well anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithCompletion(Origin)
                                    .WithRateCubicMetresPerSecond(r => r.Between(0.001, 0.01))
                                    .WithInjectionTemperature(t => t.WithKelvin(k => k.Between(293.15, 333.15))).Generate();
        Well anyProducer = Arbitrary.Well().WithKind(WellKind.Producer).WithCompletion(farCorner)
                                    .WithRateCubicMetresPerSecond(r => r.Between(0.001, 0.01)).Generate();
        BoundaryCondition anyAquifer = Arbitrary.BoundaryCondition()
                                                .WithFarFieldPressure(p => p.WithPascals(pa => pa.Between(150e5, 250e5)))
                                                .WithFarFieldTemperature(t => t.WithKelvin(k => k.Between(333.15, 473.15)))
                                                .Generate();
        ReservoirSimulation anySimulation = Arbitrary.ReservoirSimulation()
                                                     .WithGrid(anyGrid).WithWells([anyInjector, anyProducer]).WithBoundary(anyAquifer)
                                                     .Generate();

        // Act
        for (int step = 0; step < 5; step++) {
            anySimulation.Advance(new TimeStep(Sandstone.OneHour));
        }

        // Assert
        Assert.Equal(5, anySimulation.StepsTaken);
        Assert.Equal(0.0, anySimulation.MassBalanceErrorCubicMetres(), 1e-6);
    }

}
