using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class ReservoirSimulationConstructionTests {

    [Fact]
    public void A_simulation_starts_at_time_zero_with_nothing_injected_produced_or_exchanged() {
        // Arrange
        AnyReservoirSimulation anySimulation = Any.ReservoirSimulation();

        // Act
        ReservoirSimulation simulation = anySimulation.Generate();

        // Assert
        Assert.Equal(0.0, simulation.ElapsedSeconds);
        Assert.Equal(0, simulation.StepsTaken);
        Assert.Equal(0.0, simulation.CumulativeInjectedCubicMetres);
        Assert.Equal(0.0, simulation.CumulativeProducedCubicMetres);
        Assert.Equal(0.0, simulation.CumulativeBoundaryInflowCubicMetres);
        Assert.Equal(0.0, simulation.StoredVolumeChangeCubicMetres());
        Assert.Equal(0.0, simulation.MassBalanceErrorCubicMetres());
    }

    [Fact]
    public void A_simulation_exposes_the_grid_the_boundary_and_the_wells_it_was_given() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              anyWell     = Any.Well().WithCompletion(new CellCoordinate(0, 0, 0)).Generate();

        // Act
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [anyWell], anyBoundary);

        // Assert
        Assert.Same(anyGrid, simulation.Grid);
        Assert.Same(anyBoundary, simulation.Boundary);
        Assert.Same(anyWell, Assert.Single(simulation.Wells));
    }

    [Fact]
    public void A_simulation_without_wells_is_valid() {
        // Act
        ReservoirSimulation simulation = Any.ReservoirSimulation().WithWellCount(0).Generate();

        // Assert
        Assert.Empty(simulation.Wells);
    }

    [Fact]
    public void A_simulation_refuses_each_missing_part() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();

        // Assert
        Assert.Equal("grid", Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(null!, [], anyBoundary)).ParamName);
        Assert.Equal("wells", Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, null!, anyBoundary)).ParamName);
        Assert.Equal("boundary", Assert.Throws<ArgumentNullException>(() => new ReservoirSimulation(anyGrid, [], null!)).ParamName);
    }

    [Fact]
    public void A_simulation_refuses_a_null_well() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well[]            wells       = [null!];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(anyGrid, wells, anyBoundary));

        // Assert
        Assert.Equal("wells", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_simulation_refuses_a_well_completed_outside_the_grid(int axis) {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        CellCoordinate    outside     = axis switch {
            0 => new CellCoordinate(anyGrid.CellsX, 0, 0),
            1 => new CellCoordinate(0, anyGrid.CellsY, 0),
            _ => new CellCoordinate(0, 0, anyGrid.CellsZ)
        };
        Well anyWell = Any.Well().WithCompletion(outside).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(anyGrid, [anyWell], anyBoundary));

        // Assert
        Assert.Contains(anyWell.Id, error.Message);
        Assert.Contains("outside", error.Message);
    }

    [Fact]
    public void A_simulation_refuses_two_wells_with_the_same_identifier() {
        // Arrange: two cells at least, so that both wells have a cell of their own
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().WithCellsX(x => x.Between(2, 4)).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              first       = Any.Well().WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              second      = Any.Well().WithKind(first.Kind).WithId(first.Id).WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(anyGrid, [first, second], anyBoundary));

        // Assert
        Assert.Contains(first.Id, error.Message);
        Assert.Contains("twice", error.Message);
    }

    [Fact]
    public void A_simulation_refuses_two_wells_completed_in_the_same_cell() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        CellCoordinate    sharedCell  = anyGrid.Cells.First().Coordinate;
        Well              injector    = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(sharedCell).Generate();
        Well              producer    = Any.Well().WithKind(WellKind.Producer).WithId("PRD-01").WithCompletion(sharedCell).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ReservoirSimulation(anyGrid, [injector, producer], anyBoundary));

        // Assert
        Assert.Contains(sharedCell.ToString(), error.Message);
    }

    [Fact]
    public void A_simulation_accepts_an_injector_and_a_producer_with_the_same_number() {
        // Arrange: INJ-01 and PRD-01 are two distinct identifiers
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().WithCellsX(x => x.Between(2, 4)).Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              injector    = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01").WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        Well              producer    = Any.Well().WithKind(WellKind.Producer).WithId("PRD-01").WithCompletion(new CellCoordinate(1, 0, 0)).Generate();

        // Act
        ReservoirSimulation simulation = new ReservoirSimulation(anyGrid, [injector, producer], anyBoundary);

        // Assert
        Assert.Equal(2, simulation.Wells.Count);
    }

    [Fact]
    public void Wells_are_not_listed_twice_in_the_simulation_for_a_drawn_reservoir() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(w => w.Between(1, 3)).WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).Generate();

        // Assert
        Assert.Equal(anySimulation.Wells.Count, anySimulation.Wells.Select(w => w.Id).Distinct().Count());
        Assert.Equal(anySimulation.Wells.Count, anySimulation.Wells.Select(w => w.Completion).Distinct().Count());
    }

}

public sealed class WellStatusTests {

    [Fact]
    public void A_well_is_flowing_until_it_is_shut_in() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(w => w.Between(1, 3)).Generate();
        string              anyWellId     = anySimulation.Wells.First().Id;

        // Act
        bool beforeShutIn = anySimulation.IsShutIn(anyWellId);
        anySimulation.ShutIn(anyWellId);
        bool afterShutIn = anySimulation.IsShutIn(anyWellId);

        // Assert
        Assert.False(beforeShutIn);
        Assert.True(afterShutIn);
    }

    [Fact]
    public void Shutting_in_a_well_leaves_the_other_wells_flowing() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithGrid(g => g.WithCellsX(x => x.Between(2, 4))).WithWellCount(2).Generate();
        string              shutWellId    = anySimulation.Wells.First().Id;
        string              otherWellId   = anySimulation.Wells.Last().Id;

        // Act
        anySimulation.ShutIn(shutWellId);

        // Assert
        Assert.False(anySimulation.IsShutIn(otherWellId));
    }

    [Fact]
    public void A_shut_in_well_can_be_reopened_and_shut_in_again() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(w => w.Between(1, 3)).Generate();
        string              anyWellId     = anySimulation.Wells.First().Id;
        anySimulation.ShutIn(anyWellId);

        // Act
        anySimulation.Reopen(anyWellId);
        bool reopened = anySimulation.IsShutIn(anyWellId);
        anySimulation.ShutIn(anyWellId);

        // Assert
        Assert.False(reopened);
        Assert.True(anySimulation.IsShutIn(anyWellId));
    }

    [Fact]
    public void Shutting_in_a_well_twice_is_refused() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(w => w.Between(1, 3)).Generate();
        string              anyWellId     = anySimulation.Wells.First().Id;
        anySimulation.ShutIn(anyWellId);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anySimulation.ShutIn(anyWellId));

        // Assert
        Assert.Contains(anyWellId, error.Message);
        Assert.True(anySimulation.IsShutIn(anyWellId));
    }

    [Fact]
    public void Reopening_a_well_that_is_flowing_is_refused() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(w => w.Between(1, 3)).Generate();
        string              anyWellId     = anySimulation.Wells.First().Id;

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anySimulation.Reopen(anyWellId));

        // Assert
        Assert.Contains(anyWellId, error.Message);
        Assert.False(anySimulation.IsShutIn(anyWellId));
    }

    [Fact]
    public void An_unknown_well_is_refused_by_every_status_operation() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(0).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anySimulation.IsShutIn("INJ-01"));
        Assert.Throws<ArgumentException>(() => anySimulation.ShutIn("INJ-01"));
        Assert.Throws<ArgumentException>(() => anySimulation.Reopen("PRD-01"));
    }

    [Fact]
    public void A_missing_well_identifier_is_refused_by_every_status_operation() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySimulation.IsShutIn(null!));
        Assert.Throws<ArgumentNullException>(() => anySimulation.ShutIn(null!));
        Assert.Throws<ArgumentNullException>(() => anySimulation.Reopen(null!));
    }

    [Fact]
    public void Well_identifiers_are_case_sensitive() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWellCount(1).WithWells(w => w.WithKind(WellKind.Injector)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anySimulation.IsShutIn(anySimulation.Wells.Single().Id.ToLowerInvariant()));
    }

}
