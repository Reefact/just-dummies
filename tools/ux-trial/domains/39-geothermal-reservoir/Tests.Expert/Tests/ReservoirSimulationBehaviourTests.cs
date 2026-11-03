using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

using static GeothermalReservoir.Tests.Expert.Support.Numeric;
using static GeothermalReservoir.Tests.Expert.Support.Scenarios;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class TransmissibilityTests {

    // Two cells side by side, 10 m cubes, permeabilities 1 and 3 darcy-ish (1e-12 and 3e-12 m2). Everything else is a dummy.
    private static ReservoirSimulation TwoCells(double cellSizeMetres, double leftPermeability, double rightPermeability) {
        List<RockCell> cells = new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPermeability(new Permeability(leftPermeability)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPermeability(new Permeability(rightPermeability)).Generate()
        };

        return SimulationOver(new GeologicalGrid(2, 1, 1, cellSizeMetres, cells));
    }

    [Fact]
    public void Transmissibility_is_the_harmonic_permeability_times_the_face_over_viscosity_and_distance() {
        ReservoirSimulation simulation = TwoCells(10.0, 1e-12, 3e-12);

        double transmissibility = simulation.Transmissibility(simulation.Grid.CellAt(new CellCoordinate(0, 0, 0)),
                                                              simulation.Grid.CellAt(new CellCoordinate(1, 0, 0)));

        // k_harm = 1.5e-12 m2; A = 100 m2; mu = 3e-4 Pa.s; dx = 10 m  ->  1.5e-12 * 100 / (3e-4 * 10) = 5e-8 m3/(s.Pa)
        AssertClose(5e-8, transmissibility);
    }

    [Fact]
    public void Transmissibility_does_not_depend_on_which_cell_is_asked_first() {
        ReservoirSimulation simulation = TwoCells(10.0, 1e-15, 4e-10);
        RockCell            left       = simulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            right      = simulation.Grid.CellAt(new CellCoordinate(1, 0, 0));

        Assert.Equal(simulation.Transmissibility(left, right), simulation.Transmissibility(right, left));
    }

    [Theory]
    [InlineData(10.0, 5e-8)]
    [InlineData(20.0, 1e-7)] // twice the cell size: the face grows with the square, the distance with the size
    [InlineData(1.0, 5e-9)]
    public void Transmissibility_grows_in_proportion_to_the_cell_size(double cellSizeMetres, double expected) {
        ReservoirSimulation simulation = TwoCells(cellSizeMetres, 1e-12, 3e-12);

        double transmissibility = simulation.Transmissibility(simulation.Grid.CellAt(new CellCoordinate(0, 0, 0)),
                                                              simulation.Grid.CellAt(new CellCoordinate(1, 0, 0)));

        AssertClose(expected, transmissibility);
    }

    [Fact]
    public void Transmissibility_works_between_vertical_neighbours_as_between_lateral_ones() {
        List<RockCell> cells = new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPermeability(new Permeability(1e-12)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithPermeability(new Permeability(3e-12)).Generate()
        };
        ReservoirSimulation simulation = SimulationOver(new GeologicalGrid(1, 1, 2, 10.0, cells));

        double transmissibility = simulation.Transmissibility(simulation.Grid.CellAt(new CellCoordinate(0, 0, 0)),
                                                              simulation.Grid.CellAt(new CellCoordinate(0, 0, 1)));

        AssertClose(5e-8, transmissibility);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)] // a cell and itself
    [InlineData(0, 0, 0, 1, 1, 0)] // a diagonal
    [InlineData(0, 0, 0, 2, 0, 0)] // two cells apart
    [InlineData(0, 0, 0, 1, 0, 1)] // an edge neighbour
    public void Transmissibility_between_cells_that_share_no_face_is_refused(int i1, int j1, int k1, int i2, int j2, int k2) {
        GeologicalGrid      grid       = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();
        ReservoirSimulation simulation = SimulationOver(grid);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => simulation.Transmissibility(grid.CellAt(new CellCoordinate(i1, j1, k1)), grid.CellAt(new CellCoordinate(i2, j2, k2))));

        Assert.Equal("right", refusal.ParamName);
        Assert.Contains("do not share a face", refusal.Message);
    }

    [Fact]
    public void Transmissibility_between_nothing_and_a_cell_is_refused() {
        GeologicalGrid      anyGrid       = Any.GeologicalGrid().Generate();
        ReservoirSimulation anySimulation = SimulationOver(anyGrid);
        RockCell            anyCell       = anyGrid.Cells.First();

        Assert.Throws<ArgumentNullException>(() => anySimulation.Transmissibility(null!, anyCell));
        Assert.Throws<ArgumentNullException>(() => anySimulation.Transmissibility(anyCell, null!));
    }

}

public sealed class StabilityLimitTests {

    private static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);

    /// <summary>One cell of the given size and porosity; its remaining properties, permeability and conductivity included, are dummies.</summary>
    private static GeologicalGrid OneCell(double cellSizeMetres, double porosity) {
        return Any.GeologicalGrid()
                  .WithDimensions(1, 1, 1)
                  .WithCellSizeMetres(cellSizeMetres)
                  .WithCell(cell => cell.WithPorosity(new Porosity(porosity)))
                  .Generate();
    }

    private static BoundaryCondition Sealed() {
        return Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();
    }

    [Fact]
    public void A_sealed_cell_without_a_well_has_no_coupling_so_no_limit() {
        ReservoirSimulation simulation = new ReservoirSimulation(OneCell(10.0, 0.2), Array.Empty<Well>(), Sealed());

        Assert.Equal(double.PositiveInfinity, simulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void An_injector_bounds_the_step_by_the_heat_capacity_it_must_warm_or_cool() {
        // Arrange: a 10 m cell of 20 % porosity holds 2.836e9 J/K; 0.01 m3/s of water carries 4.18e4 W/K through it.
        Well injector = Any.Well().Injector().WithCompletion(Origin).WithRate(0.01).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(OneCell(10.0, 0.2), new[] { injector }, Sealed());

        // Act
        double limit = simulation.MaximumStableTimeStepSeconds();

        // Assert
        AssertClose(2.836e9 / 4.18e4, limit);
    }

    [Fact]
    public void A_producer_adds_no_thermal_coupling_since_the_produced_water_leaves_at_the_cell_temperature() {
        Well producer = Any.Well().Producer().WithCompletion(Origin).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(OneCell(10.0, 0.2), new[] { producer }, Sealed());

        Assert.Equal(double.PositiveInfinity, simulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void A_well_that_is_shut_in_no_longer_bounds_the_step() {
        Well injector = Any.Well().Injector().WithId("INJ-01").WithCompletion(Origin).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(OneCell(10.0, 0.2), new[] { injector }, Sealed());

        simulation.ShutIn("INJ-01");

        Assert.Equal(double.PositiveInfinity, simulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void Reopening_a_well_restores_the_limit_it_imposed() {
        Well injector = Any.Well().Injector().WithId("INJ-01").WithCompletion(Origin).WithRate(0.01).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(OneCell(10.0, 0.2), new[] { injector }, Sealed());
        double limitWhileFlowing = simulation.MaximumStableTimeStepSeconds();

        simulation.ShutIn("INJ-01");
        simulation.Reopen("INJ-01");

        Assert.Equal(limitWhileFlowing, simulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void An_open_boundary_couples_each_of_the_four_lateral_faces_of_a_lone_cell() {
        // Arrange: permeability 1e-12 m2, boundary fully open, far field at the cell's own pressure (so no advected heat).
        Pressure sharedPressure = Pressure.FromBars(300.0);
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithDimensions(1, 1, 1)
                                 .WithCellSizeMetres(10.0)
                                 .WithCell(cell => cell.WithPorosity(new Porosity(0.2))
                                                       .WithPermeability(new Permeability(1e-12))
                                                       .WithPressure(sharedPressure))
                                 .Generate();
        BoundaryCondition boundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(1.0).WithFarFieldPressure(sharedPressure).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, Array.Empty<Well>(), boundary);

        // Act
        double limit = simulation.MaximumStableTimeStepSeconds();

        // Assert: half a cell to the boundary face: 1e-12 * 100 / (3e-4 * 10 * 0.5) = 6.667e-8 per face, four faces; storage 3e-7 m3/Pa
        AssertClose(3e-7 / (4 * 6.6666666666666667e-8), limit);
    }

    [Fact]
    public void Tightly_coupled_neighbours_bound_the_step_by_storage_over_transmissibility() {
        // Arrange: 1 m cells, 20 % porosity, 1e-9 m2 everywhere: T = 1e-9 * 1 / (3e-4 * 1) and S = 0.2 * 1.5e-9 * 1.
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithDimensions(2, 1, 1)
                                 .WithCellSizeMetres(1.0)
                                 .WithCell(cell => cell.WithPorosity(new Porosity(0.2)).WithPermeability(new Permeability(1e-9)))
                                 .Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, Array.Empty<Well>(), Sealed());

        // Act
        double limit = simulation.MaximumStableTimeStepSeconds();

        // Assert: 3e-10 / 3.333e-6 = 9e-5 s, whatever the pressures, temperatures and conductivities
        AssertClose(9e-5, limit);
    }

}

public sealed class AdvanceTests {

    private static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);

    /// <summary>Two 1 m cells that couple so tightly that the stable step is 9e-5 s; their pressures and temperatures are dummies.</summary>
    private static ReservoirSimulation TightPair() {
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithDimensions(2, 1, 1)
                                 .WithCellSizeMetres(1.0)
                                 .WithCell(cell => cell.WithPorosity(new Porosity(0.2)).WithPermeability(new Permeability(1e-9)))
                                 .Generate();

        return new ReservoirSimulation(grid, Array.Empty<Well>(), Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate());
    }

    /// <summary>A sealed 100 m cell of 20 % porosity at 200 bar and 100 C, with one well of 0.01 m3/s; permeability and conductivity are dummies.</summary>
    private static ReservoirSimulation SealedCellWith(Well well, Pressure pressure) {
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithDimensions(1, 1, 1)
                                 .WithCellSizeMetres(100.0)
                                 .WithCell(cell => cell.WithPorosity(new Porosity(0.2))
                                                       .WithPressure(pressure)
                                                       .WithTemperature(Temperature.FromCelsius(100.0)))
                                 .Generate();

        return new ReservoirSimulation(grid, new[] { well }, Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate());
    }

    private static Well InjectorOfOneHundredthCubicMetrePerSecond() {
        return Any.Well().Injector().WithId("INJ-01").WithCompletion(Origin).WithRate(0.01).WithInjectionTemperature(Temperature.FromCelsius(20.0)).Generate();
    }

    private static Well ProducerOfOneHundredthCubicMetrePerSecond() {
        return Any.Well().Producer().WithId("PRD-01").WithCompletion(Origin).WithRate(0.01).Generate();
    }

    private static List<(double Pressure, double Temperature)> Snapshot(ReservoirSimulation simulation) {
        return simulation.Grid.Cells.OrderBy(c => c.Coordinate.I).ThenBy(c => c.Coordinate.J).ThenBy(c => c.Coordinate.K)
                         .Select(c => (c.Pressure.Pascals, c.Temperature.Kelvin))
                         .ToList();
    }

    [Fact]
    public void A_step_beyond_the_stability_limit_is_refused() {
        ReservoirSimulation tightPair = TightPair();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tightPair.Advance(new TimeStep(1.0)));

        Assert.Contains("stability limit", refusal.Message);
    }

    [Fact]
    public void A_refused_step_changes_nothing() {
        // Arrange
        ReservoirSimulation tightPair = TightPair();
        var                 before    = Snapshot(tightPair);

        // Act
        Assert.Throws<InvalidOperationException>(() => tightPair.Advance(new TimeStep(1.0)));

        // Assert
        Assert.Equal(before, Snapshot(tightPair));
        Assert.Equal(0, tightPair.StepsTaken);
        Assert.Equal(0.0, tightPair.ElapsedSeconds);
    }

    [Fact]
    public void A_step_exactly_at_the_stability_limit_is_accepted() {
        ReservoirSimulation tightPair = TightPair();

        tightPair.Advance(new TimeStep(tightPair.MaximumStableTimeStepSeconds()));

        Assert.Equal(1, tightPair.StepsTaken);
    }

    [Fact]
    public void Half_the_stable_step_brings_two_connected_cells_halfway_to_each_other() {
        // Arrange: 300 and 500 bar are what the test is about; the rocks around them (permeability from 1e-12 m2 up,
        // porosity, size, temperatures, conductivity) are dummies — they must not change where the pressures meet.
        Porosity                 sharedPorosity = Any.Porosity().Generate();
        AnyPermeability          anyPermeability = Any.Permeability().WithDecade(decade => decade.Between(-12, -9));
        List<RockCell> cells = new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPorosity(sharedPorosity).WithPermeability(anyPermeability.Generate()).WithPressure(Pressure.FromBars(300.0)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPorosity(sharedPorosity).WithPermeability(anyPermeability.Generate()).WithPressure(Pressure.FromBars(500.0)).Generate()
        };
        GeologicalGrid      grid       = new GeologicalGrid(2, 1, 1, Any.Double().Between(1.0, 1000.0).Generate(), cells);
        ReservoirSimulation simulation = new ReservoirSimulation(grid, Array.Empty<Well>(), Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate());

        // Act
        simulation.Advance(new TimeStep(simulation.MaximumStableTimeStepSeconds() / 2.0));

        // Assert
        AssertClose(Pressure.FromBars(400.0).Pascals, grid.CellAt(new CellCoordinate(0, 0, 0)).Pressure.Pascals);
        AssertClose(Pressure.FromBars(400.0).Pascals, grid.CellAt(new CellCoordinate(1, 0, 0)).Pressure.Pascals);
    }

    [Fact]
    public void An_injector_raises_the_pressure_by_its_volume_over_the_storage_and_cools_the_cell() {
        // Arrange: storage 0.2 * 1.5e-9 * 1e6 = 3e-4 m3/Pa; heat capacity 2.836e12 J/K; 80 K colder water at 0.01 m3/s
        ReservoirSimulation simulation = SealedCellWith(InjectorOfOneHundredthCubicMetrePerSecond(), Pressure.FromBars(200.0));

        // Act: 1000 s
        simulation.Advance(new TimeStep(1000.0));

        // Assert: dP = 1000 * 0.01 / 3e-4 = 33 333.3 Pa; dT = 1000 * 4.18e6 * 0.01 * (-80) / 2.836e12 = -1.1791e-3 K
        RockCell cell = simulation.Grid.CellAt(Origin);
        AssertClose(2e7 + 100_000.0 / 3.0, cell.Pressure.Pascals);
        AssertClose(373.15 - 1.1791255289139632e-3, cell.Temperature.Kelvin);
    }

    [Fact]
    public void A_producer_lowers_the_pressure_and_leaves_the_temperature_alone() {
        ReservoirSimulation simulation = SealedCellWith(ProducerOfOneHundredthCubicMetrePerSecond(), Pressure.FromBars(200.0));

        simulation.Advance(new TimeStep(1000.0));

        RockCell cell = simulation.Grid.CellAt(Origin);
        AssertClose(2e7 - 100_000.0 / 3.0, cell.Pressure.Pascals);
        Assert.Equal(Temperature.FromCelsius(100.0).Kelvin, cell.Temperature.Kelvin);
    }

    [Fact]
    public void A_step_that_would_push_the_pressure_above_the_maximum_is_refused_and_changes_nothing() {
        // Arrange: 1 000 s of injection adds 33 333 Pa, the cell has 10 000 Pa of room
        ReservoirSimulation simulation = SealedCellWith(InjectorOfOneHundredthCubicMetrePerSecond(), new Pressure(Pressure.MaximumPascals - 10_000.0));
        var                 before     = Snapshot(simulation);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1000.0)));

        // Assert
        Assert.Contains("unphysical pressure", refusal.Message);
        Assert.Equal(before, Snapshot(simulation));
        Assert.Equal(0, simulation.StepsTaken);
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
    }

    [Fact]
    public void A_step_that_would_drain_the_pressure_to_zero_or_below_is_refused_and_changes_nothing() {
        ReservoirSimulation simulation = SealedCellWith(ProducerOfOneHundredthCubicMetrePerSecond(), new Pressure(10_000.0));
        var                 before     = Snapshot(simulation);

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1000.0)));

        Assert.Contains("unphysical pressure", refusal.Message);
        Assert.Equal(before, Snapshot(simulation));
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void Each_accepted_step_adds_its_length_to_the_elapsed_time_and_one_to_the_step_count() {
        Well anyProducer = Any.Well().Producer().WithCompletion(Origin).WithRate(1e-6).Generate();
        ReservoirSimulation simulation = SealedCellWith(anyProducer, Pressure.FromBars(200.0));

        simulation.Advance(new TimeStep(3600.0));
        simulation.Advance(new TimeStep(1800.0));

        Assert.Equal(5400.0, simulation.ElapsedSeconds);
        Assert.Equal(2, simulation.StepsTaken);
    }

    [Fact]
    public void A_step_of_nothing_is_refused() {
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Generate();

        Assert.Throws<ArgumentNullException>(() => anySimulation.Advance(null!));
    }

    [Fact]
    public void A_sealed_reservoir_at_uniform_pressure_and_temperature_does_not_move() {
        // Arrange: whatever the uniform state and the rocks, with no well and no way in or out, nothing drives a flow.
        Pressure    anyPressure    = Any.Pressure().Generate();
        Temperature anyTemperature = Any.Temperature().Generate();
        ReservoirSimulation anySimulation = Gentle()
                                           .WithWellCount(0)
                                           .WithBoundary(boundary => boundary.WithTransmissibilityMultiplier(0.0))
                                           .WithGrid(grid => grid.WithCell(cell => cell.WithPressure(anyPressure).WithTemperature(anyTemperature)))
                                           .Generate();
        var before = Snapshot(anySimulation);

        // Act
        anySimulation.Advance(SafeStepFor(anySimulation));

        // Assert
        Assert.Equal(before, Snapshot(anySimulation));
    }

    [Fact]
    public void A_sealed_boundary_ignores_the_far_field_whatever_it_holds() {
        // Arrange: the far-field pressure and temperature are the generator's, arbitrary — sealed means they cannot matter.
        ReservoirSimulation anySimulation = Gentle()
                                           .WithWellCount(0)
                                           .WithBoundary(boundary => boundary.WithTransmissibilityMultiplier(0.0))
                                           .WithGrid(grid => grid.WithDimensions(1, 1, 1))
                                           .Generate();
        var before = Snapshot(anySimulation);

        // Act
        anySimulation.Advance(SafeStepFor(anySimulation));

        // Assert
        Assert.Equal(before, Snapshot(anySimulation));
        Assert.Equal(0.0, anySimulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void A_far_field_above_the_reservoir_pressure_pushes_water_in() {
        // Arrange: the reservoir sits between 200 and 700 bar; the aquifer at 900 bar is what the test is about
        ReservoirSimulation anySimulation = OpenBoundaryAgainst(Pressure.FromBars(900.0)).Generate();

        // Act
        anySimulation.Advance(SafeStepFor(anySimulation));

        // Assert
        Assert.True(anySimulation.CumulativeBoundaryInflowCubicMetres > 0.0);
        Assert.True(anySimulation.StoredVolumeChangeCubicMetres() > 0.0);
    }

    [Fact]
    public void A_far_field_below_the_reservoir_pressure_draws_water_out() {
        ReservoirSimulation anySimulation = OpenBoundaryAgainst(Pressure.FromBars(10.0)).Generate();

        anySimulation.Advance(SafeStepFor(anySimulation));

        Assert.True(anySimulation.CumulativeBoundaryInflowCubicMetres < 0.0);
        Assert.True(anySimulation.StoredVolumeChangeCubicMetres() < 0.0);
    }

    [Fact]
    public void Cold_water_flowing_in_from_the_far_field_cools_the_cell_without_going_below_that_far_field() {
        // Arrange: a lone cell between 100 and 300 C, an aquifer at 20 C pushing in from 900 bar
        Temperature coldAquifer = Temperature.FromCelsius(20.0);
        ReservoirSimulation anySimulation = OpenBoundaryAgainst(Pressure.FromBars(900.0))
                                           .WithBoundary(boundary => boundary.WithFarFieldTemperature(coldAquifer))
                                           .WithGrid(grid => grid.WithDimensions(1, 1, 1)
                                                                 .WithCell(cell => cell.WithTemperature(t => t.WithKelvin(k => k.Between(373.15, 573.15)))))
                                           .Generate();
        double before = anySimulation.Grid.CellAt(Origin).Temperature.Kelvin;

        // Act
        anySimulation.Advance(SafeStepFor(anySimulation));

        // Assert
        double after = anySimulation.Grid.CellAt(Origin).Temperature.Kelvin;
        Assert.True(after < before, $"The cell should cool: {before:R} K before, {after:R} K after.");
        Assert.True(after > coldAquifer.Kelvin);
    }

    /// <summary>A wall-less reservoir without wells whose rock lets water through (1e-14 m2 and more), against a far field at the given pressure.</summary>
    private static AnyReservoirSimulation OpenBoundaryAgainst(Pressure farFieldPressure) {
        return Gentle()
              .WithWellCount(0)
              .WithBoundary(boundary => boundary.WithFarFieldPressure(farFieldPressure).WithTransmissibilityMultiplier(m => m.GreaterThanOrEqualTo(0.01)))
              .WithGrid(grid => grid.WithCell(cell => cell.WithPermeability(p => p.WithDecade(d => d.Between(-14, -9)))));
    }

}
