using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class GeologicalGridTests {

    private static List<RockCell> CellsOf(int nx, int ny, int nz) {
        List<RockCell> cells = new List<RockCell>();
        for (int i = 0; i < nx; i++) {
            for (int j = 0; j < ny; j++) {
                for (int k = 0; k < nz; k++) {
                    cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(i, j, k)).Generate());
                }
            }
        }

        return cells;
    }

    [Fact]
    public void A_grid_reports_its_dimensions_and_its_cell_size() {
        // Arrange
        AnyGeologicalGrid anyGrid = Any.GeologicalGrid().WithCellsX(3).WithCellsY(2).WithCellsZ(4).WithCellSizeMetres(25.0);

        // Act
        GeologicalGrid grid = anyGrid.Generate();

        // Assert
        Assert.Equal((3, 2, 4), (grid.CellsX, grid.CellsY, grid.CellsZ));
        Assert.Equal(25.0, grid.CellSizeMetres);
        Assert.Equal(24, grid.Cells.Count());
    }

    [Fact]
    public void A_cell_is_a_cube_whose_volume_and_face_area_follow_its_size() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(10.0).Generate();

        // Assert
        Assert.Equal(1000.0, grid.CellVolumeCubicMetres);
        Assert.Equal(100.0, grid.FaceAreaSquareMetres);
    }

    [Theory]
    [InlineData(0, 1, 1, "cellsX")]
    [InlineData(1, 0, 1, "cellsY")]
    [InlineData(1, 1, 0, "cellsZ")]
    [InlineData(-1, 1, 1, "cellsX")]
    [InlineData(1, -3, 1, "cellsY")]
    [InlineData(1, 1, int.MinValue, "cellsZ")]
    public void A_dimension_below_one_is_refused_and_named(int nx, int ny, int nz, string parameter) {
        // Arrange
        IReadOnlyCollection<RockCell> anyCells = CellsOf(1, 1, 1);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeologicalGrid(nx, ny, nz, 10.0, anyCells));

        // Assert
        Assert.Equal(parameter, error.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1000.0)]
    public void A_cell_size_from_one_to_a_thousand_metres_is_accepted(double size) {
        // Act
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(size).Generate();

        // Assert
        Assert.Equal(size, grid.CellSizeMetres);
    }

    [Theory]
    [InlineData(0.999)]
    [InlineData(1000.001)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_cell_size_outside_the_range_is_refused(double size) {
        // Arrange
        IReadOnlyCollection<RockCell> anyCells = CellsOf(1, 1, 1);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeologicalGrid(1, 1, 1, size, anyCells));

        // Assert
        Assert.Equal("cellSizeMetres", error.ParamName);
    }

    [Fact]
    public void A_grid_refuses_a_missing_cell_collection() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new GeologicalGrid(1, 1, 1, 10.0, null!));
    }

    [Fact]
    public void A_grid_refuses_a_cell_collection_of_the_wrong_size() {
        // Arrange
        List<RockCell> oneCellShort = CellsOf(2, 2, 2).Take(7).ToList();
        List<RockCell> oneCellOver  = CellsOf(2, 2, 2).Append(Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate()).ToList();

        // Assert
        Assert.Equal("cells", Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 2, 10.0, oneCellShort)).ParamName);
        Assert.Equal("cells", Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 2, 10.0, oneCellOver)).ParamName);
    }

    [Fact]
    public void A_grid_refuses_a_null_cell() {
        // Arrange
        RockCell[] cells = [null!];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(1, 1, 1, 10.0, cells));

        // Assert
        Assert.Equal("cells", error.ParamName);
    }

    [Fact]
    public void A_grid_refuses_a_cell_outside_its_dimensions() {
        // Arrange
        List<RockCell> cells = CellsOf(2, 2, 1).Take(3).ToList();
        cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(2, 0, 0)).Generate());

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));

        // Assert
        Assert.Contains("(2,0,0)", error.Message);
        Assert.Contains("outside", error.Message);
    }

    [Fact]
    public void A_grid_refuses_two_cells_at_the_same_coordinate() {
        // Arrange
        List<RockCell> cells = CellsOf(2, 2, 1).Take(3).ToList();
        cells.Add(Any.RockCell().WithCoordinate(cells[0].Coordinate).Generate());

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));

        // Assert
        Assert.Contains(cells[0].Coordinate.ToString(), error.Message);
        Assert.Contains("twice", error.Message);
    }

    [Fact]
    public void A_cell_is_found_at_its_coordinate_whatever_the_order_the_cells_were_given_in() {
        // Arrange
        List<RockCell> shuffled = Any.PermutationOf(CellsOf(3, 2, 2)).Generate();

        // Act
        GeologicalGrid grid = new GeologicalGrid(3, 2, 2, 10.0, shuffled);

        // Assert
        foreach (RockCell cell in shuffled) {
            Assert.Same(cell, grid.CellAt(cell.Coordinate));
        }
    }

    [Fact]
    public void Looking_for_a_cell_outside_the_grid_is_refused() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(2).WithCellsY(3).WithCellsZ(4).Generate();

        // Assert
        Assert.False(grid.Contains(new CellCoordinate(2, 0, 0)));
        Assert.False(grid.Contains(new CellCoordinate(0, 3, 0)));
        Assert.False(grid.Contains(new CellCoordinate(0, 0, 4)));
        Assert.True(grid.Contains(new CellCoordinate(1, 2, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.CellAt(new CellCoordinate(0, 0, 4)));
        Assert.Throws<ArgumentNullException>(() => grid.CellAt(null!));
        Assert.Throws<ArgumentNullException>(() => grid.Contains(null!));
    }

    [Theory]
    [InlineData(1, 1, 1, 6)]
    [InlineData(2, 1, 1, 5)]
    [InlineData(1, 0, 1, 5)]
    [InlineData(1, 0, 0, 4)]
    [InlineData(0, 0, 0, 3)]
    [InlineData(2, 2, 2, 3)]
    public void A_cell_has_a_neighbour_through_each_face_that_lies_inside_the_grid(int i, int j, int k, int expectedCount) {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(3).WithCellsY(3).WithCellsZ(3).Generate();
        CellCoordinate at   = new CellCoordinate(i, j, k);

        // Act
        List<RockCell> neighbours = grid.NeighboursOf(at).ToList();

        // Assert
        Assert.Equal(expectedCount, neighbours.Count);
        Assert.All(neighbours, n => Assert.True(n.Coordinate.IsFaceNeighbourOf(at)));
        Assert.Equal(neighbours.Count, neighbours.Select(n => n.Coordinate).Distinct().Count());
    }

    [Fact]
    public void A_lone_cell_has_no_neighbour() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(1).WithCellsY(1).WithCellsZ(1).Generate();

        // Assert
        Assert.Empty(grid.NeighboursOf(new CellCoordinate(0, 0, 0)));
    }

    [Fact]
    public void The_neighbours_of_a_cell_outside_the_grid_are_refused() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(2).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.NeighboursOf(new CellCoordinate(2, 0, 0)).ToList());
    }

    [Theory]
    [InlineData(0, 0, 0, 2)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(2, 2, 1, 2)]
    [InlineData(2, 1, 0, 1)]
    public void The_number_of_lateral_boundary_faces_counts_the_outer_sides_in_x_and_y_only(int i, int j, int k, int expected) {
        // Arrange: 3 x 3 x 2, so depth never counts
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(3).WithCellsY(3).WithCellsZ(2).Generate();

        // Act
        int faces = grid.LateralBoundaryFaceCount(new CellCoordinate(i, j, k));

        // Assert
        Assert.Equal(expected, faces);
    }

    [Fact]
    public void A_lone_cell_has_all_four_lateral_faces_on_the_boundary() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(1).WithCellsY(1).WithCellsZ(1).Generate();

        // Assert
        Assert.Equal(4, grid.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 0)));
    }

    [Fact]
    public void A_cell_of_a_one_cell_wide_grid_has_both_of_its_opposite_faces_on_the_boundary() {
        // Arrange: 1 x 3 x 1, so a cell of the middle row still touches the boundary twice in X
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsX(1).WithCellsY(3).WithCellsZ(1).Generate();

        // Assert
        Assert.Equal(3, grid.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 0)));
        Assert.Equal(2, grid.LateralBoundaryFaceCount(new CellCoordinate(0, 1, 0)));
    }

    [Fact]
    public void A_single_layer_grid_has_no_thermal_gradient() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid().WithCellsZ(1).Generate();

        // Assert
        Assert.Equal(0.0, grid.AverageThermalGradient());
    }

    [Fact]
    public void Ten_kelvin_more_per_layer_over_five_metre_cells_is_a_gradient_of_two_kelvin_per_metre() {
        // Arrange: a profile that grows linearly with depth, whatever the number of layers and the plan size
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithCellsZ(z => z.Between(2, 4))
                                 .WithCellSizeMetres(5.0)
                                 .WithCells((coordinate, cell) => cell.WithTemperature(new Temperature(300.0 + 10.0 * coordinate.K)))
                                 .Generate();

        // Act
        double gradient = grid.AverageThermalGradient();

        // Assert
        Assert.Equal(2.0, gradient, 9);
    }

    [Fact]
    public void A_gradient_averages_the_layers_cell_by_cell() {
        // Arrange: top layer 300 K and 320 K (mean 310), bottom layer 340 K and 360 K (mean 350), 10 m cells
        List<RockCell> cells = [
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(new Temperature(300)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 0)).WithTemperature(new Temperature(320)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithTemperature(new Temperature(340)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(1, 0, 1)).WithTemperature(new Temperature(360)).Generate()
        ];
        GeologicalGrid grid = new GeologicalGrid(2, 1, 2, 10.0, cells);

        // Act
        double gradient = grid.AverageThermalGradient();

        // Assert
        Assert.Equal(4.0, gradient, 9);
    }

    [Fact]
    public void A_gradient_is_negative_when_the_bottom_is_colder_than_the_top() {
        // Arrange
        GeologicalGrid grid = Any.GeologicalGrid()
                                 .WithCellsZ(2)
                                 .WithCellSizeMetres(10.0)
                                 .WithCells((coordinate, cell) => cell.WithTemperature(new Temperature(coordinate.K == 0 ? 400.0 : 380.0)))
                                 .Generate();

        // Assert
        Assert.Equal(-2.0, grid.AverageThermalGradient(), 9);
    }

}
