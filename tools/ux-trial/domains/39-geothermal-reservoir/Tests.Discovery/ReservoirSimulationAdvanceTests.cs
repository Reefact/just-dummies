using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class ReservoirSimulationAdvanceTests {

    private static double PressureOf(ReservoirSimulation simulation) {
        return simulation.Grid.CellAt(Scenarios.Origin).Pressure.Pascals;
    }

    private static double KelvinOf(ReservoirSimulation simulation) {
        return simulation.Grid.CellAt(Scenarios.Origin).Temperature.Kelvin;
    }

    // ---- what a step is worth, on a cell whose figures are known -------------------------------------------

    [Fact]
    public void An_injector_raises_the_pressure_of_its_cell_by_rate_times_step_over_storage() {
        // 0.01 m3/s for 1 s into a 3e-7 m3/Pa cell.
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 400.0));

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(20e6 + 0.01 / 3e-7, PressureOf(simulation), 1e-3);
    }

    [Fact]
    public void A_producer_lowers_the_pressure_of_its_cell_by_rate_times_step_over_storage() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.ProducerAtOrigin(0.01));

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(20e6 - 0.01 / 3e-7, PressureOf(simulation), 1e-3);
    }

    [Fact]
    public void Cold_injected_water_cools_the_cell_in_proportion_to_its_heat_capacity() {
        // 4.18e6 J/(m3.K) x 0.01 m3/s x (300 - 400) K x 1 s, over 2.836e9 J/K.
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(400.0 - 4.18e6 / 2.836e9, KelvinOf(simulation), 1e-9);
    }

    [Fact]
    public void Injecting_water_at_the_temperature_of_the_cell_leaves_it_as_it_was() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 400.0));

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(400.0, KelvinOf(simulation));
    }

    [Fact]
    public void A_producer_does_not_change_the_temperature_of_its_cell() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.ProducerAtOrigin(0.01));

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(400.0, KelvinOf(simulation));
    }

    [Fact]
    public void The_balances_record_what_the_wells_moved() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 400.0));

        simulation.Advance(new TimeStep(2.0));

        Assert.Equal(0.02, simulation.CumulativeInjectedCubicMetres, 1e-12);
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
        Assert.Equal(0.0, simulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void A_producer_is_booked_as_produced_volume() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.ProducerAtOrigin(0.01));

        simulation.Advance(new TimeStep(2.0));

        Assert.Equal(0.02, simulation.CumulativeProducedCubicMetres, 1e-12);
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
    }

    [Fact]
    public void The_stored_volume_change_matches_the_net_injected_volume() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 400.0));

        simulation.Advance(new TimeStep(2.0));

        Assert.Equal(0.02, simulation.StoredVolumeChangeCubicMetres(), 1e-9);
        Assert.Equal(0.0, simulation.MassBalanceErrorCubicMetres(), 1e-9);
    }

    // ---- time and counters ---------------------------------------------------------------------------------

    [Fact]
    public void Each_step_adds_its_duration_and_counts_once() {
        // Arrange
        ReservoirSimulation simulation = Scenarios.LoneCell(well: null);
        TimeStep            anyFirst   = Any.TimeStep().WithSeconds(s => s.Between(1.0, 1e6)).Generate();
        TimeStep            anySecond  = Any.TimeStep().WithSeconds(s => s.Between(1.0, 1e6)).Generate();

        // Act
        simulation.Advance(anyFirst);
        simulation.Advance(anySecond);

        // Assert
        Assert.Equal(2, simulation.StepsTaken);
        Assert.Equal(anyFirst.Seconds + anySecond.Seconds, simulation.ElapsedSeconds, 1e-6);
    }

    [Fact]
    public void A_step_is_required() {
        ReservoirSimulation anySimulation = Scenarios.AnyWorkingReservoir().Generate();

        Assert.Throws<ArgumentNullException>(() => anySimulation.Advance(null!));
    }

    // ---- shut-in wells -------------------------------------------------------------------------------------

    [Fact]
    public void A_shut_in_injector_neither_injects_nor_heats_nor_pressurises() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));
        simulation.ShutIn("INJ-01");

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(20e6, PressureOf(simulation));
        Assert.Equal(400.0, KelvinOf(simulation));
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
        Assert.Equal(1, simulation.StepsTaken);
    }

    [Fact]
    public void A_shut_in_producer_produces_nothing() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.ProducerAtOrigin(0.01));
        simulation.ShutIn("PRD-01");

        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(20e6, PressureOf(simulation));
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void A_reopened_injector_injects_again() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 400.0));
        simulation.ShutIn("INJ-01");
        simulation.Advance(new TimeStep(1.0));

        simulation.Reopen("INJ-01");
        simulation.Advance(new TimeStep(1.0));

        Assert.Equal(0.01, simulation.CumulativeInjectedCubicMetres, 1e-12);
    }

    // ---- the stability limit -------------------------------------------------------------------------------

    [Fact]
    public void A_step_beyond_the_stability_limit_is_refused() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1e6)));

        Assert.Contains("stability limit", error.Message);
    }

    [Fact]
    public void A_refused_step_changes_nothing() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(0.01, 300.0));

        Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1e6)));

        Assert.Equal(20e6, PressureOf(simulation));
        Assert.Equal(400.0, KelvinOf(simulation));
        Assert.Equal(0, simulation.StepsTaken);
        Assert.Equal(0.0, simulation.ElapsedSeconds);
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
    }

    [Fact]
    public void A_step_exactly_at_the_limit_is_accepted() {
        // A lone cell drawing from an open aquifer at its own temperature: at the limit the pressure lands on the
        // aquifer's, which is a legitimate state. (With an injector the thermal limit is far beyond what the
        // pressure range tolerates: see the notes.)
        RockCell cell = Any.RockCell().WithCoordinate(Scenarios.Origin).WithPorosity(new Porosity(0.2)).WithPermeability(new Permeability(1e-14))
                           .WithPressure(new Pressure(20e6)).WithTemperature(new Temperature(400.0)).Generate();
        BoundaryCondition aquifer = Any.BoundaryCondition().WithTransmissibilityMultiplier(1.0)
                                       .WithFarFieldPressure(new Pressure(30e6)).WithFarFieldTemperature(new Temperature(400.0)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(new GeologicalGrid(1, 1, 1, 10.0, [cell]), [], aquifer);

        simulation.Advance(new TimeStep(simulation.MaximumStableTimeStepSeconds()));

        Assert.Equal(1, simulation.StepsTaken);
        Assert.Equal(30e6, cell.Pressure.Pascals, 1.0);
    }

    [Fact]
    public void A_step_just_above_the_limit_is_refused() {
        ReservoirSimulation simulation = Scenarios.LoneCell(Scenarios.InjectorAtOrigin(1e-4, 300.0));
        double              limit      = simulation.MaximumStableTimeStepSeconds();

        Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(Math.BitIncrement(limit))));
    }

    // ---- steps that would leave the physical range ---------------------------------------------------------

    [Fact]
    public void A_step_that_would_empty_a_cell_of_its_pressure_is_refused_and_nothing_moves() {
        // Cell 0 is a thin-storage cell with a hard-working producer; cell 1 would change legitimately.
        RockCell tight = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPorosity(new Porosity(0.01))
                            .WithPermeability(new Permeability(1e-14)).WithPressure(new Pressure(1e6)).Generate();
        RockCell roomy = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPorosity(new Porosity(0.3))
                            .WithPermeability(new Permeability(1e-14)).WithPressure(new Pressure(1e6)).Generate();
        Well overdrawn  = Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(new CellCoordinate(0, 0, 0)).WithRateCubicMetresPerSecond(0.1).Generate();
        Well gentle     = Any.Well().WithKind(WellKind.Producer).WithId("02").WithCompletion(new CellCoordinate(1, 0, 0)).WithRateCubicMetresPerSecond(0.001).Generate();
        GeologicalGrid grid = new GeologicalGrid(2, 1, 1, 10.0, [tight, roomy]);
        ReservoirSimulation simulation = new ReservoirSimulation(grid, [overdrawn, gentle], Scenarios.SealedBoundary());

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1.0)));

        Assert.Contains("unphysical pressure", error.Message);
        Assert.Equal(1e6, tight.Pressure.Pascals);
        Assert.Equal(1e6, roomy.Pressure.Pascals);
        Assert.Equal(0, simulation.StepsTaken);
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
    }

    [Fact]
    public void A_step_that_would_exceed_one_hundred_megapascals_is_refused() {
        RockCell nearlyFull = Any.RockCell().WithCoordinate(Scenarios.Origin).WithPorosity(new Porosity(0.2))
                                 .WithPressure(new Pressure(99.99e6)).Generate();
        Well     injector   = Scenarios.InjectorAtOrigin(0.1, 400.0);
        ReservoirSimulation simulation = new ReservoirSimulation(new GeologicalGrid(1, 1, 1, 10.0, [nearlyFull]), [injector], Scenarios.SealedBoundary());

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => simulation.Advance(new TimeStep(1.0)));

        Assert.Contains("unphysical pressure", error.Message);
        Assert.Equal(99.99e6, nearlyFull.Pressure.Pascals);
    }

    // ---- driving forces ------------------------------------------------------------------------------------

    [Fact]
    public void A_reservoir_at_one_pressure_and_one_temperature_with_no_well_does_not_move() {
        // Arrange
        Pressure    anyPressure    = Any.Pressure().WithPascals(p => p.Between(10e6, 50e6)).Generate();
        Temperature anyTemperature = Any.Temperature().WithKelvin(k => k.Between(330.0, 500.0)).Generate();
        ReservoirSimulation anySimulation = Scenarios.AnyWorkingReservoir()
                                                     .WithGrid(g => g.WithCells(c => c.WithPressure(anyPressure).WithTemperature(anyTemperature)))
                                                     .WithBoundary(b => b.WithFarFieldPressure(anyPressure).WithFarFieldTemperature(anyTemperature))
                                                     .WithoutWells()
                                                     .Generate();
        TimeStep step = Scenarios.SafeStepFor(anySimulation);

        // Act
        anySimulation.Advance(step);

        // Assert
        Assert.All(anySimulation.Grid.Cells, cell => {
            Assert.Equal(anyPressure.Pascals, cell.Pressure.Pascals);
            Assert.Equal(anyTemperature.Kelvin, cell.Temperature.Kelvin);
        });
    }

    [Fact]
    public void Pressure_flows_from_the_high_cell_to_the_low_cell_without_overshooting() {
        // Arrange: the pressures are what the test is about; the rock between them is arbitrary.
        RockCell high = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPressure(new Pressure(30e6)).Generate();
        RockCell low  = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPressure(new Pressure(10e6)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(new GeologicalGrid(2, 1, 1, 10.0, [high, low]), [], Scenarios.SealedBoundary());

        // Act
        simulation.Advance(Scenarios.SafeStepFor(simulation));

        // Assert
        Assert.InRange(high.Pressure.Pascals, 10e6, 30e6);
        Assert.InRange(low.Pressure.Pascals, 10e6, 30e6);
        Assert.True(high.Pressure.Pascals < 30e6);
        Assert.True(low.Pressure.Pascals > 10e6);
        Assert.True(high.Pressure.Pascals >= low.Pressure.Pascals);
    }

    [Fact]
    public void Flow_between_two_cells_conserves_the_fluid_balance() {
        RockCell high = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPressure(new Pressure(30e6)).Generate();
        RockCell low  = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPressure(new Pressure(10e6)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(new GeologicalGrid(2, 1, 1, 10.0, [high, low]), [], Scenarios.SealedBoundary());

        simulation.Advance(Scenarios.SafeStepFor(simulation));

        Assert.Equal(0.0, simulation.StoredVolumeChangeCubicMetres(), 1e-6);
        Assert.Equal(0.0, simulation.MassBalanceErrorCubicMetres(), 1e-6);
    }

    [Fact]
    public void Heat_conducts_from_the_hot_cell_to_the_cold_cell() {
        // Same pressure on both sides, so only conduction acts.
        Pressure same = new Pressure(20e6);
        RockCell hot  = Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithPressure(same).WithTemperature(new Temperature(450.0)).Generate();
        RockCell cold = Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithPressure(same).WithTemperature(new Temperature(350.0)).Generate();
        ReservoirSimulation simulation = new ReservoirSimulation(new GeologicalGrid(2, 1, 1, 10.0, [hot, cold]), [], Scenarios.SealedBoundary());

        simulation.Advance(Scenarios.SafeStepFor(simulation));

        Assert.True(hot.Temperature.Kelvin < 450.0);
        Assert.True(cold.Temperature.Kelvin > 350.0);
        Assert.True(hot.Temperature.Kelvin >= cold.Temperature.Kelvin);
    }

    // ---- the lateral boundary ------------------------------------------------------------------------------

    [Fact]
    public void A_sealed_boundary_lets_nothing_in_whatever_the_far_field_holds() {
        BoundaryCondition aquifer = Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0)
                                       .WithFarFieldPressure(new Pressure(40e6)).WithFarFieldTemperature(new Temperature(500.0)).Generate();
        ReservoirSimulation simulation = Scenarios.LoneCell(well: null, aquifer);

        simulation.Advance(new TimeStep(1000.0));

        Assert.Equal(20e6, PressureOf(simulation));
        Assert.Equal(400.0, KelvinOf(simulation));
        Assert.Equal(0.0, simulation.CumulativeBoundaryInflowCubicMetres);
    }

    [Fact]
    public void An_aquifer_at_higher_pressure_feeds_the_cell_and_the_inflow_is_booked() {
        // Arrange
        double anyMultiplier = Any.Double().Between(0.1, 1.0).Generate();
        BoundaryCondition aquifer = Any.BoundaryCondition().WithTransmissibilityMultiplier(anyMultiplier)
                                       .WithFarFieldPressure(new Pressure(30e6)).WithFarFieldTemperature(new Temperature(400.0)).Generate();
        ReservoirSimulation simulation = Scenarios.LoneCell(well: null, aquifer);

        // Act
        simulation.Advance(Scenarios.SafeStepFor(simulation));

        // Assert
        Assert.InRange(PressureOf(simulation), 20e6, 30e6);
        Assert.True(PressureOf(simulation) > 20e6);
        Assert.True(simulation.CumulativeBoundaryInflowCubicMetres > 0.0);
        Assert.Equal(0.0, simulation.MassBalanceErrorCubicMetres(), 1e-9);
    }

    [Fact]
    public void An_aquifer_at_lower_pressure_drains_the_cell_and_the_outflow_is_booked_negative() {
        BoundaryCondition aquifer = Any.BoundaryCondition().WithTransmissibilityMultiplier(m => m.Between(0.1, 1.0))
                                       .WithFarFieldPressure(new Pressure(10e6)).Generate();
        ReservoirSimulation simulation = Scenarios.LoneCell(well: null, aquifer);

        simulation.Advance(Scenarios.SafeStepFor(simulation));

        Assert.InRange(PressureOf(simulation), 10e6, 20e6);
        Assert.True(simulation.CumulativeBoundaryInflowCubicMetres < 0.0);
    }

    [Fact]
    public void An_interior_cell_has_no_boundary_face_and_gets_nothing_from_the_aquifer() {
        // A 3x3x1 layer with the aquifer far above the rock: only the ring of cells touches it.
        BoundaryCondition aquifer = Any.BoundaryCondition().WithTransmissibilityMultiplier(1.0)
                                       .WithFarFieldPressure(new Pressure(40e6)).Generate();
        ReservoirSimulation simulation = Scenarios.AnyWorkingReservoir()
                                                  .WithGrid(g => g.WithCellsX(3).WithCellsY(3).WithCellsZ(1)
                                                                  .WithCells(c => c.WithPressure(new Pressure(20e6)).WithTemperature(new Temperature(400.0))))
                                                  .WithBoundary(aquifer)
                                                  .WithoutWells()
                                                  .Generate();
        // Not the full stable step: the neighbours of the ring start moving only after the first step, so the
        // centre cell, which is only coupled to cells still at 20 MPa, is untouched after one.
        simulation.Advance(Scenarios.SafeStepFor(simulation));

        Assert.Equal(20e6, simulation.Grid.CellAt(new CellCoordinate(1, 1, 0)).Pressure.Pascals);
        Assert.True(simulation.Grid.CellAt(new CellCoordinate(0, 0, 0)).Pressure.Pascals > 20e6);
    }

    // ---- marching a whole working reservoir ----------------------------------------------------------------

    [Fact]
    public void Several_steps_keep_the_balance_and_the_state_in_range() {
        // Arrange
        ReservoirSimulation anySimulation = Scenarios.AnyWorkingReservoir().Generate();

        // Act
        for (int step = 0; step < 5; step++) {
            anySimulation.Advance(new TimeStep(Math.Min(anySimulation.MaximumStableTimeStepSeconds() / 2.0, 1.0)));
        }

        // Assert
        double scale = Math.Abs(anySimulation.CumulativeInjectedCubicMetres) + Math.Abs(anySimulation.CumulativeProducedCubicMetres)
                     + Math.Abs(anySimulation.CumulativeBoundaryInflowCubicMetres) + Math.Abs(anySimulation.StoredVolumeChangeCubicMetres());
        Assert.Equal(5, anySimulation.StepsTaken);
        Assert.InRange(Math.Abs(anySimulation.MassBalanceErrorCubicMetres()), 0.0, 1e-9 * scale + 1e-12);
        Assert.All(anySimulation.Grid.Cells, cell => {
            Assert.InRange(cell.Pressure.Pascals, 1.0, Pressure.MaximumPascals);
            Assert.InRange(cell.Temperature.Kelvin, Temperature.FreezingKelvin + 1e-9, Temperature.MaximumKelvin);
        });
    }

}
