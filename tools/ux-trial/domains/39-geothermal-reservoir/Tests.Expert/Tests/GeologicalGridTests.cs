using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

using static GeothermalReservoir.Tests.Expert.Support.Numeric;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class GeologicalGridTests {

    // The grid generator completes the grid by itself; the tests below that REFUSE a grid assemble the cells by hand,
    // because the cells (their count, their coordinates) are exactly what is under test.

    private static List<RockCell> CellsAt(params (int I, int J, int K)[] coordinates) {
        return coordinates.Select(c => Any.RockCell().WithCoordinate(new CellCoordinate(c.I, c.J, c.K)).Generate()).ToList();
    }

    private static List<RockCell> CompleteCells(int cellsX, int cellsY, int cellsZ) {
        List<(int, int, int)> coordinates = new List<(int, int, int)>();
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    coordinates.Add((i, j, k));
                }
            }
        }

        return CellsAt(coordinates.ToArray());
    }

    [Fact]
    public void A_grid_has_the_dimensions_and_cell_size_it_was_given() {
        List<RockCell> cells = CompleteCells(2, 3, 4);

        GeologicalGrid grid = new GeologicalGrid(2, 3, 4, 20.0, cells);

        Assert.Equal((2, 3, 4), (grid.CellsX, grid.CellsY, grid.CellsZ));
        Assert.Equal(20.0, grid.CellSizeMetres);
    }

    [Fact]
    public void A_grid_of_twenty_metre_cells_has_cells_of_eight_thousand_cubic_metres_and_faces_of_four_hundred_square_metres() {
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(20.0).Generate();

        Assert.Equal(8_000.0, grid.CellVolumeCubicMetres);
        Assert.Equal(400.0, grid.FaceAreaSquareMetres);
    }

    [Fact]
    public void A_grid_enumerates_each_of_its_cells_once() {
        List<RockCell> cells = CompleteCells(2, 3, 4);

        GeologicalGrid grid = new GeologicalGrid(2, 3, 4, 20.0, cells);

        Assert.Equal(24, grid.Cells.Count());
        Assert.Equal(cells.OrderBy(c => c.Coordinate.ToString()).Select(c => c.Coordinate),
                     grid.Cells.OrderBy(c => c.Coordinate.ToString()).Select(c => c.Coordinate));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(250.0)]
    [InlineData(GeologicalGrid.MaximumCellSizeMetres)]
    public void A_cell_size_from_one_metre_to_a_kilometre_is_accepted(double cellSizeMetres) {
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(cellSizeMetres).Generate();

        Assert.Equal(cellSizeMetres, grid.CellSizeMetres);
    }

    [Theory]
    [InlineData(0.9999999999999999)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(1000.0000000000001)]
    [InlineData(1e6)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_cell_size_outside_one_metre_to_a_kilometre_is_refused(double cellSizeMetres) {
        AnyGeologicalGrid gridGenerator = Any.GeologicalGrid().WithCellSizeMetres(cellSizeMetres);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => gridGenerator.Generate());

        Assert.Equal("cellSizeMetres", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, 1, 1, "cellsX")]
    [InlineData(1, 0, 1, "cellsY")]
    [InlineData(1, 1, 0, "cellsZ")]
    [InlineData(-1, 1, 1, "cellsX")]
    [InlineData(1, int.MinValue, 1, "cellsY")]
    public void A_grid_needs_at_least_one_cell_along_each_axis(int cellsX, int cellsY, int cellsZ, string parameter) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeologicalGrid(cellsX, cellsY, cellsZ, 10.0, new List<RockCell>()));

        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void A_grid_without_a_cell_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new GeologicalGrid(1, 1, 1, 10.0, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void A_grid_refuses_a_cell_count_that_is_not_the_product_of_its_dimensions(int cellCount) {
        List<RockCell> cells = CompleteCells(cellCount, 1, 1);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 1, 1, 10.0, cells));

        Assert.Equal("cells", refusal.ParamName);
        Assert.Contains("needs 2 cells", refusal.Message);
    }

    [Fact]
    public void A_grid_refuses_a_cell_lying_beyond_one_of_its_axes() {
        List<RockCell> cells = CellsAt((1, 0, 0));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GeologicalGrid(1, 1, 1, 10.0, cells));

        Assert.Contains("(1,0,0)", refusal.Message);
        Assert.Contains("outside", refusal.Message);
    }

    [Fact]
    public void A_grid_refuses_a_cell_that_appears_twice_even_when_the_count_is_right() {
        List<RockCell> cells = CellsAt((0, 0, 0), (0, 0, 0));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 1, 1, 10.0, cells));

        Assert.Contains("(0,0,0)", refusal.Message);
        Assert.Contains("twice", refusal.Message);
    }

    [Fact]
    public void A_grid_refuses_a_missing_cell_inside_the_list() {
        List<RockCell> cells = CellsAt((0, 0, 0));
        cells.Add(null!);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 1, 1, 10.0, cells));

        Assert.IsNotType<ArgumentNullException>(refusal);
        Assert.Equal("cells", refusal.ParamName);
    }

    [Fact]
    public void A_cell_is_found_by_its_coordinate() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 3, 4).Generate();

        RockCell cell = grid.CellAt(new CellCoordinate(1, 2, 3));

        Assert.Equal(new CellCoordinate(1, 2, 3), cell.Coordinate);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(0, 3, 0)]
    [InlineData(0, 0, 4)]
    public void A_coordinate_beyond_the_grid_has_no_cell(int i, int j, int k) {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 3, 4).Generate();

        Assert.False(grid.Contains(new CellCoordinate(i, j, k)));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.CellAt(new CellCoordinate(i, j, k)));
    }

    [Fact]
    public void The_last_cell_along_every_axis_is_inside_the_grid() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 3, 4).Generate();

        Assert.True(grid.Contains(new CellCoordinate(1, 2, 3)));
    }

    [Fact]
    public void Looking_for_the_cell_of_no_coordinate_is_refused() {
        GeologicalGrid anyGrid = Any.GeologicalGrid().Generate();

        Assert.Throws<ArgumentNullException>(() => anyGrid.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => anyGrid.CellAt(null!));
    }

    [Theory]
    [InlineData(1, 1, 1, 6)] // the centre of a 3x3x3 grid
    [InlineData(0, 1, 1, 5)] // the middle of a face
    [InlineData(0, 0, 1, 4)] // the middle of an edge
    [InlineData(0, 0, 0, 3)] // a corner
    [InlineData(2, 2, 2, 3)] // the opposite corner
    public void A_cell_has_up_to_six_face_neighbours_fewer_at_the_edge_of_the_grid(int i, int j, int k, int expected) {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();

        IReadOnlyCollection<RockCell> neighbours = grid.NeighboursOf(new CellCoordinate(i, j, k)).ToList();

        Assert.Equal(expected, neighbours.Count);
    }

    [Fact]
    public void The_neighbours_of_the_centre_are_the_six_cells_one_step_along_each_axis() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();

        IEnumerable<string> neighbours = grid.NeighboursOf(new CellCoordinate(1, 1, 1)).Select(c => c.Coordinate.ToString()).OrderBy(s => s);

        Assert.Equal(new[] { "(0,1,1)", "(1,0,1)", "(1,1,0)", "(1,1,2)", "(1,2,1)", "(2,1,1)" }, neighbours);
    }

    [Fact]
    public void A_neighbour_is_never_the_cell_itself_nor_a_diagonal() {
        GeologicalGrid grid   = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();
        CellCoordinate corner = new CellCoordinate(0, 0, 0);

        IEnumerable<CellCoordinate> neighbours = grid.NeighboursOf(corner).Select(c => c.Coordinate);

        Assert.All(neighbours, neighbour => Assert.True(corner.IsFaceNeighbourOf(neighbour)));
    }

    [Fact]
    public void A_single_cell_grid_has_no_neighbours() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(1, 1, 1).Generate();

        Assert.Empty(grid.NeighboursOf(new CellCoordinate(0, 0, 0)));
    }

    [Fact]
    public void Looking_for_the_neighbours_of_a_cell_beyond_the_grid_is_refused() {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(2, 2, 2).Generate();

        // The sequence is lazy: the refusal comes on enumeration, not on the call.
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.NeighboursOf(new CellCoordinate(2, 0, 0)).ToList());
    }

    [Theory]
    [InlineData(1, 1, 0, 0)] // inside the 3x3 footprint: no lateral face on the outer boundary
    [InlineData(1, 1, 1, 0)] // the depth makes no difference
    [InlineData(1, 0, 0, 1)] // against the south boundary
    [InlineData(0, 1, 1, 1)] // against the west boundary
    [InlineData(0, 0, 0, 2)] // a corner column
    [InlineData(2, 2, 1, 2)] // the opposite corner column
    public void A_cell_counts_the_lateral_faces_it_has_on_the_outer_boundary(int i, int j, int k, int expected) {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(3, 3, 2).Generate();

        Assert.Equal(expected, grid.LateralBoundaryFaceCount(new CellCoordinate(i, j, k)));
    }

    [Theory]
    [InlineData(1, 1, 1, 0, 0, 0, 4)] // a lone cell shows all four lateral faces
    [InlineData(1, 3, 1, 0, 0, 0, 3)] // the end of a one-cell-wide corridor
    [InlineData(1, 3, 1, 0, 1, 0, 2)] // the middle of that corridor: both sides, no end
    [InlineData(3, 1, 5, 1, 0, 4, 2)] // a corridor along X, whatever the depth
    public void A_one_cell_wide_grid_exposes_both_of_its_opposite_faces(int cellsX, int cellsY, int cellsZ, int i, int j, int k, int expected) {
        GeologicalGrid grid = Any.GeologicalGrid().WithDimensions(cellsX, cellsY, cellsZ).Generate();

        Assert.Equal(expected, grid.LateralBoundaryFaceCount(new CellCoordinate(i, j, k)));
    }

    [Fact]
    public void The_thermal_gradient_runs_from_the_top_layer_to_the_bottom_layer() {
        // Arrange: 20 C at the top, 100 C at the bottom, 100 m cells. The layer in between is not the point.
        GeologicalGrid grid = ColumnOfTemperatures(100.0, 20.0, Any.Temperature().Generate(), 100.0);

        // Act
        double gradient = grid.AverageThermalGradient();

        // Assert: 80 K over two cell sizes
        AssertClose(0.4, gradient);
    }

    [Fact]
    public void A_colder_bottom_gives_a_negative_gradient() {
        GeologicalGrid grid = ColumnOfTemperatures(10.0, 80.0, Any.Temperature().Generate(), 20.0);

        AssertClose(-3.0, grid.AverageThermalGradient());
    }

    [Fact]
    public void The_gradient_averages_each_layer_before_comparing_them() {
        // Arrange: a 2x2 footprint, two layers of 25 m cells; the top layer averages 25 C and the bottom layer 75 C.
        List<RockCell> cells = new List<RockCell>();
        double[]       tops    = { 10.0, 20.0, 30.0, 40.0 };
        double[]       bottoms = { 50.0, 60.0, 90.0, 100.0 };
        int            index   = 0;
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 2; j++) {
                cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(i, j, 0)).WithTemperature(Temperature.FromCelsius(tops[index])).Generate());
                cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(i, j, 1)).WithTemperature(Temperature.FromCelsius(bottoms[index])).Generate());
                index++;
            }
        }

        GeologicalGrid grid = new GeologicalGrid(2, 2, 2, 25.0, cells);

        // 50 K between the two layer means, over one cell size of 25 m
        AssertClose(2.0, grid.AverageThermalGradient());
    }

    [Fact]
    public void A_single_layer_has_no_gradient_whatever_its_temperatures() {
        GeologicalGrid anySingleLayer = Any.GeologicalGrid().WithCellsZ(1).Generate();

        Assert.Equal(0.0, anySingleLayer.AverageThermalGradient());
    }

    private static GeologicalGrid ColumnOfTemperatures(double cellSizeMetres, double topCelsius, Temperature middle, double bottomCelsius) {
        return ColumnOfTemperaturesCore(cellSizeMetres, Temperature.FromCelsius(topCelsius), middle, Temperature.FromCelsius(bottomCelsius));
    }

    private static GeologicalGrid ColumnOfTemperaturesCore(double cellSizeMetres, Temperature top, Temperature middle, Temperature bottom) {
        List<RockCell> cells = new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(top).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithTemperature(middle).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 2)).WithTemperature(bottom).Generate()
        };

        return new GeologicalGrid(1, 1, 3, cellSizeMetres, cells);
    }

}
