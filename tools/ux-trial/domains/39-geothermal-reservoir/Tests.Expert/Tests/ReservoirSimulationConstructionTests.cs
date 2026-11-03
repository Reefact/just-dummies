using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

using static GeothermalReservoir.Tests.Expert.Support.Scenarios;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class ReservoirSimulationConstructionTests {

    private static Well InjectorAt(string id, int i, int j, int k) {
        return Any.Well().Injector().WithId(id).WithCompletion(new CellCoordinate(i, j, k)).Generate();
    }

    private static Well ProducerAt(string id, int i, int j, int k) {
        return Any.Well().Producer().WithId(id).WithCompletion(new CellCoordinate(i, j, k)).Generate();
    }

    [Fact]
    public void A_simulation_exposes_its_grid_its_boundary_and_its_wells() {
        GeologicalGrid    grid     = Any.GeologicalGrid().WithDimensions(2, 2, 2).Generate();
        BoundaryCondition boundary = Any.BoundaryCondition().Generate();
        Well              injector = InjectorAt("INJ-01", 0, 0, 0);
        Well              producer = ProducerAt("PRD-01", 1, 1, 1);

        ReservoirSimulation simulation = new ReservoirSimulation(grid, new[] { injector, producer }, boundary);

        Assert.Same(grid, simulation.Grid);
        Assert.Same(boundary, simulation.Boundary);
        Assert.Equivalent(new[] { injector, producer }, simulation.Wells, strict: true);
    }

    [Fact]
    public void A_simulation_can_have_no_well_at_all() {
        GeologicalGrid grid = Any.GeologicalGrid().Generate();

        ReservoirSimulation simulation = SimulationOver(grid);

        Assert.Empty(simulation.Wells);
    }

    [Fact]
    public void A_simulation_starts_at_time_zero_with_nothing_injected_produced_or_exchanged() {
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
    public void A_well_may_be_completed_in_the_last_cell_along_every_axis() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 3, 4).Generate();

        ReservoirSimulation simulation = SimulationOver(grid, InjectorAt("INJ-01", 1, 2, 3));

        Assert.Single(simulation.Wells);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(0, 3, 0)]
    [InlineData(0, 0, 4)]
    public void A_well_completed_beyond_the_grid_is_refused(int i, int j, int k) {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 3, 4).Generate();
        Well           lost = InjectorAt("INJ-01", i, j, k);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SimulationOver(grid, lost));

        Assert.Equal("wells", refusal.ParamName);
        Assert.Contains("INJ-01", refusal.Message);
    }

    [Fact]
    public void Two_wells_with_the_same_id_are_refused_even_in_different_cells() {
        GeologicalGrid grid  = Any.GeologicalGrid().WithDimensions(2, 1, 1).Generate();
        Well           first = InjectorAt("INJ-01", 0, 0, 0);
        Well           again = InjectorAt("INJ-01", 1, 0, 0);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SimulationOver(grid, first, again));

        Assert.Contains("INJ-01", refusal.Message);
        Assert.Contains("twice", refusal.Message);
    }

    [Fact]
    public void Two_wells_in_the_same_cell_are_refused_even_with_different_ids() {
        GeologicalGrid grid     = Any.GeologicalGrid().WithDimensions(2, 1, 1).Generate();
        Well           injector = InjectorAt("INJ-01", 1, 0, 0);
        Well           producer = ProducerAt("PRD-01", 1, 0, 0);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SimulationOver(grid, injector, producer));

        Assert.Contains("(1,0,0)", refusal.Message);
    }

    [Fact]
    public void A_missing_well_in_the_list_is_refused() {
        GeologicalGrid grid = Any.GeologicalGrid().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SimulationOver(grid, (Well)null!));

        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_simulation_without_a_grid_is_refused() {
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(null!, Array.Empty<Well>(), anyBoundary));
    }

    [Fact]
    public void A_simulation_without_a_well_list_is_refused() {
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, null!, anyBoundary));
    }

    [Fact]
    public void A_simulation_without_a_boundary_is_refused() {
        GeologicalGrid anyGrid = Any.GeologicalGrid().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, Array.Empty<Well>(), null!));
    }

    [Fact]
    public void A_generated_simulation_always_satisfies_its_own_invariants() {
        // The generator places its wells; this is the scaffold's weakest spot, so it is exercised from here too.
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(3).Generate();

        Assert.All(anySimulation.Wells, well => Assert.True(anySimulation.Grid.Contains(well.Completion)));
        Assert.Equal(anySimulation.Wells.Count, anySimulation.Wells.Select(w => w.Id).Distinct().Count());
        Assert.Equal(anySimulation.Wells.Count, anySimulation.Wells.Select(w => w.Completion).Distinct().Count());
    }

}
