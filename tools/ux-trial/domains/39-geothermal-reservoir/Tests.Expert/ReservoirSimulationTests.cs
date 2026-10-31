using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

/// <summary>Setting a field up, and operating its wells.</summary>
public sealed class ReservoirSimulationTests {

    [Fact]
    public void A_well_must_be_completed_inside_the_grid() {
        // Arrange
        GeologicalGrid    anyGrid     = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary = Any.BoundaryCondition().Generate();
        Well              anyOutsider = Any.Well().WithCompletion(new CellCoordinate(anyGrid.CellsX, 0, 0)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new ReservoirSimulation(anyGrid, [anyOutsider], anyBoundary));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Two_wells_cannot_share_a_cell() {
        // Arrange
        GeologicalGrid    anyGrid      = Any.GeologicalGrid().Generate();
        BoundaryCondition anyBoundary  = Any.BoundaryCondition().Generate();
        CellCoordinate    anyCell      = Any.ElementOf(anyGrid.Cells.Select(cell => cell.Coordinate)).Generate();
        Well              anyInjector  = Any.Well().Injector().WithCompletion(anyCell).Generate();
        Well              anyProducer  = Any.Well().Producer().WithCompletion(anyCell).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new ReservoirSimulation(anyGrid, [anyInjector, anyProducer], anyBoundary));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("Two wells", argumentException.Message);
    }

    [Fact]
    public void Two_wells_cannot_share_an_id() {
        // Arrange
        GeologicalGrid       anyGrid        = Any.GeologicalGrid().WithCellsX(x => x.GreaterThanOrEqualTo(2)).Generate();
        BoundaryCondition    anyBoundary    = Any.BoundaryCondition().Generate();
        List<CellCoordinate> anyTwoCells    = Any.ListOf(Any.ElementOf(anyGrid.Cells.Select(cell => cell.Coordinate))).WithCount(2).Distinct().Generate();
        Well                 anyProducer    = Any.Well().Producer().WithSerial(7).WithCompletion(anyTwoCells[0]).Generate();
        Well                 anyNamesake    = Any.Well().Producer().WithSerial(7).WithCompletion(anyTwoCells[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new ReservoirSimulation(anyGrid, [anyProducer, anyNamesake], anyBoundary));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("used twice", argumentException.Message);
    }

    [Fact]
    public void Fluid_only_flows_between_cells_that_share_a_face() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithGrid(grid => grid.WithDimensions(3, 1, 1)).Generate();
        RockCell            west          = anySimulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell            east          = anySimulation.Grid.CellAt(new CellCoordinate(2, 0, 0));

        // Act
        Exception? refusal = Record.Exception(() => anySimulation.Transmissibility(west, east));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Transmissibility_follows_darcy_across_a_face() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation()
                                              .WithGrid(grid => grid.WithDimensions(2, 1, 1)
                                                                    .WithCellSizeMetres(10)
                                                                    .WithRock(rock => rock.WithPermeability(new Permeability(3e-13))))
                                              .Generate();
        RockCell west = anySimulation.Grid.CellAt(new CellCoordinate(0, 0, 0));
        RockCell east = anySimulation.Grid.CellAt(new CellCoordinate(1, 0, 0));

        // Act
        double transmissibility = anySimulation.Transmissibility(west, east);

        // Assert: k·A / (mu·dx) = 3e-13 · 100 / (3e-4 · 10)
        Assert.Equal(1e-8, transmissibility, 1e-20);
    }

    [Fact]
    public void A_shut_in_well_can_be_put_back_on_stream() {
        // Arrange
        Well                anyWell       = Any.Well().WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWells(anyWell).Generate();
        anySimulation.ShutIn(anyWell.Id);

        // Act
        anySimulation.Reopen(anyWell.Id);

        // Assert
        Assert.False(anySimulation.IsShutIn(anyWell.Id));
    }

    [Fact]
    public void A_well_already_shut_in_cannot_be_shut_in_again() {
        // Arrange
        Well                anyWell       = Any.Well().WithCompletion(new CellCoordinate(0, 0, 0)).Generate();
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWells(anyWell).Generate();
        anySimulation.ShutIn(anyWell.Id);

        // Act
        Exception? refusal = Record.Exception(() => anySimulation.ShutIn(anyWell.Id));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Only_a_well_of_the_field_can_be_shut_in() {
        // Arrange
        ReservoirSimulation anySimulation = Any.ReservoirSimulation().WithWells().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySimulation.ShutIn("PRD-42"));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
