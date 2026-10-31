using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class GeologicalGridTests {

    [Fact]
    public void A_grid_refuses_a_cell_list_that_does_not_fill_it() {
        // Arrange
        double         anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();
        List<RockCell> threeCells  = [
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 1, 0)).Generate()
        ];

        // Act
        Exception? error = Record.Exception(() => new GeologicalGrid(2, 2, 1, anyCellSize, threeCells));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("cells", argumentError.ParamName);
    }

    [Fact]
    public void A_grid_refuses_the_same_cell_twice() {
        // Arrange
        double         anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();
        List<RockCell> cells       = [
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate()
        ];

        // Act
        Exception? error = Record.Exception(() => new GeologicalGrid(2, 1, 1, anyCellSize, cells));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Contains("appears twice", argumentError.Message);
    }

    [Theory]
    [InlineData(0, 0, 0, 3)]
    [InlineData(1, 1, 1, 6)]
    [InlineData(1, 0, 1, 5)]
    public void A_cell_has_one_neighbour_per_face_it_shares_with_the_grid(int i, int j, int k, int expectedNeighbours) {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();

        // Act
        int neighbours = grid.NeighboursOf(new CellCoordinate(i, j, k)).Count();

        // Assert
        Assert.Equal(expectedNeighbours, neighbours);
    }

    [Fact]
    public void A_single_column_is_exposed_to_the_far_field_on_all_four_lateral_faces() {
        // Arrange
        GeologicalGrid column = Any.GeologicalGrid().WithCellsX(1).WithCellsY(1).Generate();

        // Act
        int faces = column.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 0));

        // Assert
        Assert.Equal(4, faces);
    }

    [Fact]
    public void An_interior_cell_has_no_lateral_boundary_face() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(3).WithCellsY(3).Generate();

        // Act
        int faces = grid.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 0));

        // Assert
        Assert.Equal(0, faces);
    }

    [Fact]
    public void The_thermal_gradient_runs_from_the_top_layer_to_the_bottom_layer() {
        // Arrange
        List<RockCell> column = [
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(Temperature.FromCelsius(50)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 2)).WithTemperature(Temperature.FromCelsius(80)).Generate()
        ];
        GeologicalGrid grid = new GeologicalGrid(1, 1, 3, 100.0, column);

        // Act
        double gradient = grid.AverageThermalGradient();

        // Assert
        Assert.Equal(0.15, gradient, 1e-9);
    }

    [Fact]
    public void A_coordinate_outside_the_grid_has_no_cell() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 2, 2).Generate();

        // Act
        Exception? error = Record.Exception(() => grid.CellAt(new CellCoordinate(2, 0, 0)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}
