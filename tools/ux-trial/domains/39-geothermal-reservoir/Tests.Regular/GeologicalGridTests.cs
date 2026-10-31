using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Regular;

public sealed class GeologicalGridTests {

    [Fact]
    public void A_grid_refuses_a_cell_list_that_does_not_fill_it() {
        // Arrange
        List<RockCell> anyTwoCells = [
            Arbitrary.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate(),
            Arbitrary.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).Generate()
        ];
        double anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();

        // Act
        Action build = () => new GeologicalGrid(3, 1, 1, anyCellSize, anyTwoCells);

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_grid_refuses_the_same_cell_twice() {
        // Arrange
        CellCoordinate origin = new(0, 0, 0);
        List<RockCell> anyCells = [
            Arbitrary.RockCell().WithCoordinate(origin).Generate(),
            Arbitrary.RockCell().WithCoordinate(origin).Generate()
        ];
        double anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();

        // Act
        Action build = () => new GeologicalGrid(2, 1, 1, anyCellSize, anyCells);

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void An_interior_cell_has_six_face_neighbours_and_a_corner_cell_three() {
        // Arrange
        GeologicalGrid anyGrid = Arbitrary.GeologicalGrid().WithCellsX(3).WithCellsY(3).WithCellsZ(3).Generate();

        // Act
        int interior = anyGrid.NeighboursOf(new CellCoordinate(1, 1, 1)).Count();
        int corner   = anyGrid.NeighboursOf(new CellCoordinate(0, 0, 0)).Count();

        // Assert
        Assert.Equal(6, interior);
        Assert.Equal(3, corner);
    }

    [Fact]
    public void Only_lateral_faces_on_the_outer_boundary_count_towards_the_aquifer() {
        // Arrange
        GeologicalGrid anyGrid = Arbitrary.GeologicalGrid().WithCellsX(3).WithCellsY(3).WithCellsZ(2).Generate();

        // Act
        int corner = anyGrid.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 1));
        int edge   = anyGrid.LateralBoundaryFaceCount(new CellCoordinate(1, 0, 0));
        int centre = anyGrid.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 1));

        // Assert
        Assert.Equal(2, corner);
        Assert.Equal(1, edge);
        Assert.Equal(0, centre);
    }

    [Fact]
    public void The_thermal_gradient_runs_from_the_top_layer_to_the_bottom_layer() {
        // Arrange
        List<RockCell> anyColumn = [
            Arbitrary.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(new Temperature(300.0)).Generate(),
            Arbitrary.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithTemperature(new Temperature(310.0)).Generate(),
            Arbitrary.RockCell().WithCoordinate(new CellCoordinate(0, 0, 2)).WithTemperature(new Temperature(330.0)).Generate()
        ];
        GeologicalGrid layered = new(1, 1, 3, 10.0, anyColumn);

        // Act
        double gradient = layered.AverageThermalGradient();

        // Assert
        Assert.Equal(1.5, gradient, 1e-9);
    }

    [Fact]
    public void A_single_layer_grid_has_no_thermal_gradient() {
        // Arrange
        GeologicalGrid anyGrid = Arbitrary.GeologicalGrid().WithCellsZ(1).Generate();

        // Act
        double gradient = anyGrid.AverageThermalGradient();

        // Assert
        Assert.Equal(0.0, gradient);
    }

}
