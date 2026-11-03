using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

using static GeothermalReservoir.Tests.Expert.Support.Numeric;
using static GeothermalReservoir.Tests.Expert.Support.Scenarios;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class WellLifecycleTests {

    private static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);
    private static readonly CellCoordinate Next   = new CellCoordinate(1, 0, 0);

    /// <summary>An injector INJ-01 and a producer PRD-01 in a 2x1x1 reservoir; rates, temperatures and rocks are dummies.</summary>
    private static ReservoirSimulation InjectorAndProducer() {
        Well injector = Any.Well().Injector().WithId("INJ-01").WithCompletion(Origin).Generate();
        Well producer = Any.Well().Producer().WithId("PRD-01").WithCompletion(Next).Generate();

        return SimulationOver(Any.GeologicalGrid().WithDimensions(2, 1, 1).Generate(), injector, producer);
    }

    [Fact]
    public void A_well_is_flowing_until_it_is_shut_in() {
        ReservoirSimulation simulation = InjectorAndProducer();

        Assert.False(simulation.IsShutIn("INJ-01"));
        Assert.False(simulation.IsShutIn("PRD-01"));
    }

    [Fact]
    public void Shutting_in_a_well_stops_that_well_only() {
        ReservoirSimulation simulation = InjectorAndProducer();

        simulation.ShutIn("INJ-01");

        Assert.True(simulation.IsShutIn("INJ-01"));
        Assert.False(simulation.IsShutIn("PRD-01"));
    }

    [Fact]
    public void Reopening_a_shut_in_well_puts_it_back_on_stream() {
        ReservoirSimulation simulation = InjectorAndProducer();
        simulation.ShutIn("PRD-01");

        simulation.Reopen("PRD-01");

        Assert.False(simulation.IsShutIn("PRD-01"));
    }

    [Fact]
    public void A_well_can_be_shut_in_and_reopened_again_and_again() {
        ReservoirSimulation simulation = InjectorAndProducer();

        simulation.ShutIn("INJ-01");
        simulation.Reopen("INJ-01");
        simulation.ShutIn("INJ-01");

        Assert.True(simulation.IsShutIn("INJ-01"));
    }

    [Fact]
    public void Shutting_in_a_well_already_shut_in_is_refused() {
        ReservoirSimulation simulation = InjectorAndProducer();
        simulation.ShutIn("INJ-01");

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => simulation.ShutIn("INJ-01"));

        Assert.Contains("INJ-01", refusal.Message);
        Assert.True(simulation.IsShutIn("INJ-01"));
    }

    [Fact]
    public void Reopening_a_well_that_is_flowing_is_refused() {
        ReservoirSimulation simulation = InjectorAndProducer();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => simulation.Reopen("INJ-01"));

        Assert.Contains("INJ-01", refusal.Message);
        Assert.False(simulation.IsShutIn("INJ-01"));
    }

    [Theory]
    [InlineData("INJ-02")]
    [InlineData("inj-01")]
    [InlineData("")]
    public void Naming_a_well_the_simulation_does_not_have_is_refused(string unknownId) {
        ReservoirSimulation simulation = InjectorAndProducer();

        Assert.Throws<ArgumentException>(() => simulation.IsShutIn(unknownId));
        Assert.Throws<ArgumentException>(() => simulation.ShutIn(unknownId));
        Assert.Throws<ArgumentException>(() => simulation.Reopen(unknownId));
    }

    [Fact]
    public void Naming_no_well_at_all_is_refused() {
        ReservoirSimulation simulation = InjectorAndProducer();

        Assert.Throws<ArgumentNullException>(() => simulation.IsShutIn(null!));
        Assert.Throws<ArgumentNullException>(() => simulation.ShutIn(null!));
        Assert.Throws<ArgumentNullException>(() => simulation.Reopen(null!));
    }

}

public sealed class BalanceTests {

    private static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);

    /// <summary>A sealed 100 m cell of 20 % porosity holding the given well; pressure, temperature, permeability and conductivity are dummies.</summary>
    private static ReservoirSimulation SealedCellWith(Well well) {
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithDimensions(1, 1, 1)
                                 .WithCellSizeMetres(100.0)
                                 .WithCell(cell => cell.WithPorosity(new Porosity(0.2)).WithPressure(p => p.WithPascals(pascals => pascals.Between(20e6, 70e6))))
                                 .Generate();

        return new ReservoirSimulation(grid, new[] { well }, Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate());
    }

    private static void AssertWaterIsConserved(ReservoirSimulation simulation) {
        double stored = simulation.StoredVolumeChangeCubicMetres();
        double scale  = Math.Abs(simulation.CumulativeInjectedCubicMetres) + Math.Abs(simulation.CumulativeProducedCubicMetres)
                      + Math.Abs(simulation.CumulativeBoundaryInflowCubicMetres) + Math.Abs(stored);

        Assert.True(Math.Abs(simulation.MassBalanceErrorCubicMetres()) <= 1e-9 * scale + 1e-9,
                    $"Mass balance error {simulation.MassBalanceErrorCubicMetres():R} m3 against flows of {scale:R} m3.");
    }

    [Fact]
    public void An_injector_adds_its_rate_times_the_step_to_the_injected_volume_and_stores_all_of_it() {
        // Arrange: 0.01 m3/s for 1 000 s into a sealed cell
        Well injector = Any.Well().Injector().WithCompletion(Origin).WithRate(0.01).Generate();
        ReservoirSimulation simulation = SealedCellWith(injector);

        // Act
        simulation.Advance(new TimeStep(1000.0));

        // Assert
        AssertClose(10.0, simulation.CumulativeInjectedCubicMetres);
        AssertClose(10.0, simulation.StoredVolumeChangeCubicMetres());
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
        AssertWaterIsConserved(simulation);
    }

    [Fact]
    public void A_producer_adds_its_rate_times_the_step_to_the_produced_volume_and_depletes_the_store_by_as_much() {
        Well producer = Any.Well().Producer().WithCompletion(Origin).WithRate(0.01).Generate();
        ReservoirSimulation simulation = SealedCellWith(producer);

        simulation.Advance(new TimeStep(1000.0));

        AssertClose(10.0, simulation.CumulativeProducedCubicMetres);
        AssertClose(-10.0, simulation.StoredVolumeChangeCubicMetres());
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
        AssertWaterIsConserved(simulation);
    }

    [Fact]
    public void A_well_that_is_shut_in_contributes_nothing_until_it_is_reopened() {
        // Arrange
        Well injector = Any.Well().Injector().WithId("INJ-01").WithCompletion(Origin).WithRate(0.01).Generate();
        ReservoirSimulation simulation = SealedCellWith(injector);
        simulation.ShutIn("INJ-01");

        // Act
        simulation.Advance(new TimeStep(1000.0));
        double injectedWhileShutIn = simulation.CumulativeInjectedCubicMetres;
        simulation.Reopen("INJ-01");
        simulation.Advance(new TimeStep(1000.0));

        // Assert
        Assert.Equal(0.0, injectedWhileShutIn);
        AssertClose(10.0, simulation.CumulativeInjectedCubicMetres);
        Assert.Equal(2, simulation.StepsTaken);
    }

    [Fact]
    public void Water_is_conserved_over_several_steps_whatever_the_wells_the_rocks_and_the_aquifer() {
        // Arrange: at least one well and a boundary that is not sealed, so that there are flows to account for
        ReservoirSimulation anySimulation = Gentle()
                                           .WithWellCount(count => count.GreaterThanOrEqualTo(1))
                                           .WithBoundary(boundary => boundary.WithTransmissibilityMultiplier(multiplier => multiplier.GreaterThanOrEqualTo(0.01)))
                                           .Generate();

        // Act
        for (int step = 0; step < 4; step++) {
            anySimulation.Advance(SafeStepFor(anySimulation));
        }

        // Assert
        AssertWaterIsConserved(anySimulation);
        Assert.True(anySimulation.CumulativeInjectedCubicMetres + anySimulation.CumulativeProducedCubicMetres > 0.0);
    }

    [Fact]
    public void Water_is_conserved_when_a_well_is_shut_in_and_reopened_between_steps() {
        ReservoirSimulation anySimulation = Gentle().WithWellCount(count => count.GreaterThanOrEqualTo(1)).Generate();
        string              wellId        = anySimulation.Wells.First().Id;

        anySimulation.Advance(SafeStepFor(anySimulation));
        anySimulation.ShutIn(wellId);
        anySimulation.Advance(SafeStepFor(anySimulation));
        anySimulation.Reopen(wellId);
        anySimulation.Advance(SafeStepFor(anySimulation));

        AssertWaterIsConserved(anySimulation);
    }

    [Fact]
    public void The_injected_and_produced_volumes_are_the_rates_of_the_open_wells_times_the_elapsed_time() {
        // Arrange: no shut-in, so every well flows for the whole run
        ReservoirSimulation anySimulation = Gentle().WithWellCount(count => count.GreaterThanOrEqualTo(1)).Generate();

        // Act
        for (int step = 0; step < 3; step++) {
            anySimulation.Advance(SafeStepFor(anySimulation));
        }

        // Assert
        double injectorRates = anySimulation.Wells.Where(w => w.Kind == WellKind.Injector).Sum(w => w.RateCubicMetresPerSecond);
        double producerRates = anySimulation.Wells.Where(w => w.Kind == WellKind.Producer).Sum(w => w.RateCubicMetresPerSecond);
        AssertClose(injectorRates * anySimulation.ElapsedSeconds, anySimulation.CumulativeInjectedCubicMetres);
        AssertClose(producerRates * anySimulation.ElapsedSeconds, anySimulation.CumulativeProducedCubicMetres);
    }

}
