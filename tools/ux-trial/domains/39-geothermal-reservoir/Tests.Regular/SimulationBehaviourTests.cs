using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class TransmissibilityTests {

    private static ReservoirSimulation TwoCellColumn(double permeabilityA, double permeabilityB, double cellSize) {
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithCellsX(2).WithCellsY(1).WithCellsZ(1)
                                 .WithCellSizeMetres(cellSize)
                                 .WithCells((coordinate, cell) => cell.WithPermeability(new Permeability(coordinate.I == 0 ? permeabilityA : permeabilityB)))
                                 .Generate();

        return Any.ReservoirSimulation().WithGrid(grid).WithWellCount(0).Generate();
    }

    [Fact]
    public void Two_cells_of_1e_minus_12_across_ten_metres_have_a_transmissibility_of_3_point_33e_minus_8() {
        // Arrange: 1e-12 * 100 m² / (3e-4 Pa.s * 10 m)
        ReservoirSimulation simulation = TwoCellColumn(1e-12, 1e-12, 10.0);
        RockCell            left       = simulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            right      = simulation.Grid.CellAt(new CellCoordinate(1, 0, 0));

        // Act
        double transmissibility = simulation.Transmissibility(left, right);

        // Assert
        Assert.Equal(1e-10 / 3e-3, transmissibility, 1e-15);
    }

    [Fact]
    public void Across_a_contrast_the_harmonic_mean_governs_the_transmissibility() {
        // Arrange: k_harm(1e-12, 4e-12) = 1.6e-12, over 100 m² / (3e-4 * 10 m)
        ReservoirSimulation simulation = TwoCellColumn(1e-12, 4e-12, 10.0);
        RockCell            left       = simulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            right      = simulation.Grid.CellAt(new CellCoordinate(1, 0, 0));

        // Act
        double transmissibility = simulation.Transmissibility(left, right);

        // Assert
        Assert.Equal(1.6e-12 * 100.0 / 3e-3, transmissibility, 1e-15);
    }

    [Fact]
    public void Transmissibility_is_the_same_in_both_directions() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();
        RockCell            left          = anySimulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            right         = anySimulation.Grid.CellAt(new CellCoordinate(1, 0, 0));

        // Act
        double forward  = anySimulation.Transmissibility(left, right);
        double backward = anySimulation.Transmissibility(right, left);

        // Assert
        Assert.Equal(forward, backward);
        Assert.True(forward > 0.0);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(0, 0, 0)]
    public void Cells_that_do_not_share_a_face_have_no_transmissibility(int i, int j, int k) {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithGrid(g => g.WithCellsX(3).WithCellsY(3).WithCellsZ(1)).Generate();
        RockCell            origin        = anySimulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            other         = anySimulation.Grid.CellAt(new CellCoordinate(i, j, k));

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anySimulation.Transmissibility(origin, other));

        // Assert
        Assert.Equal("right", error.ParamName);
        Assert.Contains("do not share a face", error.Message);
    }

    [Fact]
    public void A_missing_cell_is_refused() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Generate();
        RockCell            anyCell       = anySimulation.Grid.Cells.First();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySimulation.Transmissibility(null!, anyCell));
        Assert.Throws<ArgumentNullException>(() => anySimulation.Transmissibility(anyCell, null!));
    }

}

public sealed class StabilityLimitTests {

    [Fact]
    public void A_lone_sealed_cell_without_wells_has_no_stability_limit() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation()
                                               .WithGrid(g => g.WithCellsX(1).WithCellsY(1).WithCellsZ(1))
                                               .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0))
                                               .WithWellCount(0)
                                               .Generate();

        // Assert
        Assert.Equal(double.PositiveInfinity, anySimulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void A_connected_reservoir_has_a_finite_positive_limit() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();

        // Act
        double limit = anySimulation.MaximumStableTimeStepSeconds();

        // Assert
        Assert.True(limit > 0.0);
        Assert.True(double.IsFinite(limit));
    }

    [Fact]
    public void A_step_at_the_limit_is_accepted() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();
        TimeStep            atLimit       = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds(), TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(atLimit);

        // Assert
        Assert.Equal(1, anySimulation.StepsTaken);
    }

    [Fact]
    public void A_step_beyond_the_limit_is_refused_and_leaves_the_reservoir_untouched() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();
        double              limit         = anySimulation.MaximumStableTimeStepSeconds();
        TimeStep            beyond        = new TimeStep(limit * 1.01);
        List<double>        pressures     = anySimulation.Grid.Cells.Select(c => c.Pressure.Pascals).ToList();
        List<double>        temperatures  = anySimulation.Grid.Cells.Select(c => c.Temperature.Kelvin).ToList();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anySimulation.Advance(beyond));

        // Assert
        Assert.Contains("stability", error.Message);
        Assert.Equal(pressures, anySimulation.Grid.Cells.Select(c => c.Pressure.Pascals));
        Assert.Equal(temperatures, anySimulation.Grid.Cells.Select(c => c.Temperature.Kelvin));
        Assert.Equal(0, anySimulation.StepsTaken);
        Assert.Equal(0.0, anySimulation.ElapsedSeconds);
    }

    [Fact]
    public void Shutting_in_an_injector_relaxes_the_thermal_limit() {
        // Arrange: a lone sealed cell, so that the injector alone sets the limit
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(1).WithCellsY(1).WithCellsZ(1).Calm().Generate();
        Well injector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(1e-3).Generate();
        ReservoirSimulation anySimulation = Any.ReservoirSimulation()
                                               .WithGrid(grid)
                                               .WithWells([injector])
                                               .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0))
                                               .Generate();
        double flowingLimit = anySimulation.MaximumStableTimeStepSeconds();

        // Act
        anySimulation.ShutIn("INJ-01");

        // Assert
        Assert.True(double.IsFinite(flowingLimit));
        Assert.Equal(double.PositiveInfinity, anySimulation.MaximumStableTimeStepSeconds());
    }

    [Fact]
    public void A_stronger_boundary_connection_cannot_raise_the_limit() {
        // Arrange: same reservoir, sealed then opened
        GeologicalGrid anyGrid = Any.GeologicalGrid().Calm().WithCellsX(x => x.Between(2, 4)).Generate();
        BoundaryCondition sealedBoundary = Any.BoundaryCondition().Calm().WithTransmissibilityMultiplier(0.0).Generate();
        BoundaryCondition openBoundary   = new BoundaryCondition(sealedBoundary.FarFieldPressure, sealedBoundary.FarFieldTemperature, 1.0);

        ReservoirSimulation sealedSimulation = new ReservoirSimulation(anyGrid, [], sealedBoundary);
        ReservoirSimulation openSimulation   = new ReservoirSimulation(anyGrid, [], openBoundary);

        // Assert
        Assert.True(openSimulation.MaximumStableTimeStepSeconds() <= sealedSimulation.MaximumStableTimeStepSeconds());
    }

}

public sealed class AdvanceTests {

    [Fact]
    public void A_reservoir_at_rest_stays_exactly_as_it_is() {
        // Arrange: one pressure and one temperature everywhere, the far field included, and no well
        Pressure    anyPressure    = Any.Pressure().WithPascals(p => p.Between(10e6, 30e6)).Generate();
        Temperature anyTemperature = Any.Temperature().WithKelvin(k => k.Between(350, 450)).Generate();
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)).WithCells(c => c.WithPressure(anyPressure).WithTemperature(anyTemperature)))
                                                .WithBoundary(b => b.WithFarFieldPressure(anyPressure).WithFarFieldTemperature(anyTemperature))
                                                .WithWellCount(0)
                                                .Generate();
        TimeStep step = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds(), TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(step);

        // Assert
        Assert.All(anySimulation.Grid.Cells, c => {
            Assert.Equal(anyPressure.Pascals, c.Pressure.Pascals);
            Assert.Equal(anyTemperature.Kelvin, c.Temperature.Kelvin);
        });
    }

    [Fact]
    public void Each_step_adds_its_duration_to_the_clock_and_one_to_the_step_count() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();
        double              half          = anySimulation.MaximumStableTimeStepSeconds() / 2.0;
        TimeStep            step          = new TimeStep(Math.Min(half, 1000.0));

        // Act
        anySimulation.Advance(step);
        anySimulation.Advance(step);
        anySimulation.Advance(step);

        // Assert
        Assert.Equal(3, anySimulation.StepsTaken);
        Assert.Equal(3 * step.Seconds, anySimulation.ElapsedSeconds, 9);
    }

    [Fact]
    public void A_missing_time_step_is_refused() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySimulation.Advance(null!));
    }

    [Fact]
    public void Pressure_diffuses_towards_the_mean_without_leaving_the_range_it_started_in() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)))
                                                .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0))
                                                .WithWellCount(0)
                                                .Generate();
        double lowest  = anySimulation.Grid.Cells.Min(c => c.Pressure.Pascals);
        double highest = anySimulation.Grid.Cells.Max(c => c.Pressure.Pascals);
        TimeStep step  = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds(), TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(step);

        // Assert
        Assert.All(anySimulation.Grid.Cells, c => Assert.InRange(c.Pressure.Pascals, lowest, highest));
    }

    [Fact]
    public void A_sealed_reservoir_without_wells_keeps_its_total_pore_volume_change_at_zero() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)))
                                                .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0))
                                                .WithWellCount(0)
                                                .Generate();
        double scale = anySimulation.Grid.Cells.Sum(c => c.Porosity.Fraction * ReservoirSimulation.TotalCompressibility * anySimulation.Grid.CellVolumeCubicMetres * c.Pressure.Pascals);
        TimeStep step = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(step);
        anySimulation.Advance(step);

        // Assert
        Assert.Equal(0.0, anySimulation.StoredVolumeChangeCubicMetres(), scale * 1e-9);
        Assert.Equal(0.0, anySimulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void A_lone_injector_in_a_sealed_cell_raises_the_pressure_by_what_it_stores() {
        // Arrange: porosity 0.2 and 10 m cells, so storage = 0.2 * 1.5e-9 * 1000 = 3e-7 m³/Pa;
        //          0.001 m³/s for 10 s is 0.01 m³, hence 0.01 / 3e-7 = 33 333.33 Pa
        GeologicalGrid grid = Any.GeologicalGrid().Calm()
                                 .WithCellsX(1).WithCellsY(1).WithCellsZ(1)
                                 .WithCellSizeMetres(10.0)
                                 .WithCells(c => c.WithPorosity(new Porosity(0.2)))
                                 .Generate();
        Well injector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(0.001)
                           .WithInjectionTemperature(t => t.WithKelvin(k => k.Between(350, 450))).Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid).WithWells([injector])
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();
        double before = grid.Cells.Single().Pressure.Pascals;

        // Act
        simulation.Advance(new TimeStep(10.0));

        // Assert
        Assert.Equal(before + 0.01 / 3e-7, grid.Cells.Single().Pressure.Pascals, 1e-3);
        Assert.Equal(0.01, simulation.CumulativeInjectedCubicMetres, 1e-12);
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void A_lone_producer_in_a_sealed_cell_lowers_the_pressure_by_what_it_withdraws() {
        // Arrange: same cell as above, 0.01 m³ withdrawn
        GeologicalGrid grid = Any.GeologicalGrid().Calm()
                                 .WithCellsX(1).WithCellsY(1).WithCellsZ(1)
                                 .WithCellSizeMetres(10.0)
                                 .WithCells(c => c.WithPorosity(new Porosity(0.2)))
                                 .Generate();
        Well producer = Any.Well().WithKind(WellKind.Producer).WithId("PRD-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(0.001).Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid).WithWells([producer])
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();
        double before = grid.Cells.Single().Pressure.Pascals;

        // Act
        simulation.Advance(new TimeStep(10.0));

        // Assert
        Assert.Equal(before - 0.01 / 3e-7, grid.Cells.Single().Pressure.Pascals, 1e-3);
        Assert.Equal(0.01, simulation.CumulativeProducedCubicMetres, 1e-12);
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
    }

    [Fact]
    public void A_shut_in_well_neither_injects_nor_produces() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)))
                                                .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0))
                                                .WithWellCount(w => w.Between(1, 3))
                                                .Generate();
        foreach (Well well in anySimulation.Wells) {
            anySimulation.ShutIn(well.Id);
        }

        TimeStep step = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(step);

        // Assert
        Assert.Equal(0.0, anySimulation.CumulativeInjectedCubicMetres);
        Assert.Equal(0.0, anySimulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void A_reopened_well_flows_again() {
        // Arrange: a lone injector, shut in then reopened before the step
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1).WithCellSizeMetres(10.0).Generate();
        Well injector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(0.001)
                           .WithInjectionTemperature(t => t.WithKelvin(k => k.Between(350, 450))).Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid).WithWells([injector])
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();
        simulation.ShutIn("INJ-01");
        simulation.Reopen("INJ-01");

        // Act
        simulation.Advance(new TimeStep(10.0));

        // Assert
        Assert.Equal(0.01, simulation.CumulativeInjectedCubicMetres, 1e-12);
    }

    [Fact]
    public void An_injector_warms_or_cools_its_cell_towards_the_injected_temperature() {
        // Arrange: a lone cell at 400 K receiving 300 K water, then 500 K water
        GeologicalGrid coolGrid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1).WithCellSizeMetres(10.0)
                                     .WithCells(c => c.WithTemperature(new Temperature(400))).Generate();
        GeologicalGrid warmGrid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1).WithCellSizeMetres(10.0)
                                     .WithCells(c => c.WithTemperature(new Temperature(400))).Generate();
        Well coolInjector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                               .WithRateCubicMetresPerSecond(1e-6).WithInjectionTemperature(new Temperature(300)).Generate();
        Well warmInjector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                               .WithRateCubicMetresPerSecond(1e-6).WithInjectionTemperature(new Temperature(500)).Generate();
        ReservoirSimulation cooling = Any.ReservoirSimulation().WithGrid(coolGrid).WithWells([coolInjector]).WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();
        ReservoirSimulation warming = Any.ReservoirSimulation().WithGrid(warmGrid).WithWells([warmInjector]).WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();

        // Act
        cooling.Advance(new TimeStep(1000.0));
        warming.Advance(new TimeStep(1000.0));

        // Assert
        Assert.InRange(coolGrid.Cells.Single().Temperature.Kelvin, 300.0, 399.999999);
        Assert.InRange(warmGrid.Cells.Single().Temperature.Kelvin, 400.000001, 500.0);
    }

    [Fact]
    public void A_step_that_would_push_a_pressure_below_zero_is_refused_and_changes_nothing() {
        // Arrange: a lone 1 m cell already at 100 Pa, drained at the maximum rate of 0.5 m³/s for a second
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1).WithCellSizeMetres(1.0)
                                 .WithCells(c => c.WithPressure(new Pressure(100.0))).Generate();
        Well producer = Any.Well().WithKind(WellKind.Producer).WithId("PRD-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(0.5).Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid).WithWells([producer])
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1.0)));

        // Assert
        Assert.Contains("unphysical pressure", error.Message);
        Assert.Equal(100.0, grid.Cells.Single().Pressure.Pascals);
        Assert.Equal(0, simulation.StepsTaken);
        Assert.Equal(0.0, simulation.ElapsedSeconds);
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void A_step_that_would_push_a_pressure_above_a_hundred_megapascals_is_refused() {
        // Arrange: a lone 1 m cell injected at the maximum rate for a second
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1).WithCellSizeMetres(1.0).Generate();
        Well injector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0))
                           .WithRateCubicMetresPerSecond(0.5)
                           .WithInjectionTemperature(t => t.WithKelvin(k => k.Between(350, 450))).Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid).WithWells([injector])
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).Generate();
        double before = grid.Cells.Single().Pressure.Pascals;

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1.0)));

        // Assert: the refusal comes from the step, not from the stability limit
        Assert.Contains("unphysical pressure", error.Message);
        Assert.Equal(before, grid.Cells.Single().Pressure.Pascals);
        Assert.Equal(0, simulation.StepsTaken);
    }

    [Fact]
    public void An_open_boundary_pulls_the_reservoir_towards_the_far_field_pressure() {
        // Arrange: a lone cell at 10 MPa against a 30 MPa far field, boundary fully open
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1)
                                 .WithCells(c => c.WithPressure(new Pressure(10e6))).Generate();
        BoundaryCondition boundary = Any.BoundaryCondition().Calm()
                                        .WithFarFieldPressure(new Pressure(30e6))
                                        .WithTransmissibilityMultiplier(1.0).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], boundary);
        TimeStep            step       = new TimeStep(Math.Min(simulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));

        // Act
        simulation.Advance(step);

        // Assert
        Assert.InRange(grid.Cells.Single().Pressure.Pascals, 10e6 + 1e-6, 30e6);
        Assert.True(simulation.CumulativeBoundaryInflowCubicMetres > 0.0);
    }

    [Fact]
    public void A_sealed_boundary_exchanges_no_fluid_even_against_a_far_field_at_another_pressure() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(1)
                                 .WithCells(c => c.WithPressure(new Pressure(10e6))).Generate();
        BoundaryCondition boundary = Any.BoundaryCondition().Calm()
                                        .WithFarFieldPressure(new Pressure(30e6))
                                        .WithTransmissibilityMultiplier(0.0).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [], boundary);

        // Act
        simulation.Advance(new TimeStep(1000.0));

        // Assert
        Assert.Equal(10e6, grid.Cells.Single().Pressure.Pascals);
        Assert.Equal(0.0, simulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void Fluid_volume_is_conserved_over_several_steps() {
        // Arrange: an open boundary, up to three weak wells and a handful of steps at half the stability limit
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)))
                                                .WithBoundary(b => b.WithTransmissibilityMultiplier(m => m.GreaterThan(0.1)))
                                                .WithWellCount(w => w.Between(1, 3))
                                                .Generate();

        // Act
        for (int step = 0; step < 5; step++) {
            double seconds = Math.Min(anySimulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds);
            anySimulation.Advance(new TimeStep(seconds));
        }

        // Assert
        double scale = Math.Max(Math.Abs(anySimulation.StoredVolumeChangeCubicMetres()),
                       Math.Max(anySimulation.CumulativeInjectedCubicMetres,
                       Math.Max(anySimulation.CumulativeProducedCubicMetres,
                                Math.Abs(anySimulation.CumulativeBoundaryInflowCubicMetres))));
        Assert.Equal(5, anySimulation.StepsTaken);
        Assert.Equal(0.0, anySimulation.MassBalanceErrorCubicMetres(), scale * 1e-9 + 1e-12);
    }

    [Fact]
    public void The_stored_volume_equals_injection_minus_production_plus_boundary_inflow() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Calm()
                                                .WithGrid(g => g.WithCellsX(x => x.Between(2, 4)))
                                                .WithWellCount(w => w.Between(1, 3))
                                                .Generate();
        TimeStep step = new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));

        // Act
        anySimulation.Advance(step);
        anySimulation.Advance(step);

        // Assert
        double netInflow = anySimulation.CumulativeInjectedCubicMetres - anySimulation.CumulativeProducedCubicMetres + anySimulation.CumulativeBoundaryInflowCubicMetres;
        double scale     = Math.Abs(anySimulation.StoredVolumeChangeCubicMetres()) + Math.Abs(netInflow);
        Assert.Equal(netInflow, anySimulation.StoredVolumeChangeCubicMetres(), scale * 1e-9 + 1e-12);
    }

    [Fact]
    public void Heat_conducts_from_a_hot_layer_to_a_cold_one_without_overshooting() {
        // Arrange: a sealed two-layer column, hot (450 K) above cold (350 K)
        GeologicalGrid grid = Any.GeologicalGrid().Calm().WithCellsX(1).WithCellsY(1).WithCellsZ(2)
                                 .WithCells((coordinate, cell) => cell.WithTemperature(new Temperature(coordinate.K == 0 ? 450.0 : 350.0)))
                                 .Generate();
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithGrid(grid)
                                            .WithBoundary(b => b.WithTransmissibilityMultiplier(0.0)).WithWellCount(0).Generate();
        TimeStep step = new TimeStep(Math.Min(simulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));

        // Act
        simulation.Advance(step);

        // Assert
        double top    = grid.CellAt(new CellCoordinate(0, 0, 0)).Temperature.Kelvin;
        double bottom = grid.CellAt(new CellCoordinate(0, 0, 1)).Temperature.Kelvin;
        Assert.InRange(top, 350.0, 450.0);
        Assert.InRange(bottom, 350.0, 450.0);
        Assert.True(top < 450.0);
        Assert.True(bottom > 350.0);
        Assert.True(top >= bottom);
    }

}
