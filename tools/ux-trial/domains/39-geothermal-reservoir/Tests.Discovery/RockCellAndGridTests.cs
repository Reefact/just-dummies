using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class RockCellTests {

    [Fact]
    public void Keeps_what_it_was_built_with() {
        // Arrange
        CellCoordinate anyCoordinate  = Any.CellCoordinate().Generate();
        Porosity       anyPorosity    = Any.Porosity().Generate();
        Permeability   anyPermeability = Any.Permeability().Generate();
        double         anyConductivity = Any.Double().Between(RockCell.MinimumThermalConductivity, RockCell.MaximumThermalConductivity).Generate();
        Pressure       anyPressure    = Any.Pressure().Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Act
        RockCell cell = new RockCell(anyCoordinate, anyPorosity, anyPermeability, anyConductivity, anyPressure, anyTemperature);

        // Assert
        Assert.Same(anyCoordinate, cell.Coordinate);
        Assert.Same(anyPorosity, cell.Porosity);
        Assert.Same(anyPermeability, cell.Permeability);
        Assert.Equal(anyConductivity, cell.ThermalConductivity);
        Assert.Same(anyPressure, cell.Pressure);
        Assert.Same(anyTemperature, cell.Temperature);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(10.0)]
    public void Accepts_both_conductivity_bounds(double conductivity) {
        RockCell cell = Any.RockCell().WithThermalConductivity(conductivity).Generate();

        Assert.Equal(conductivity, cell.ThermalConductivity);
    }

    [Fact]
    public void Refuses_a_conductivity_below_the_floor() {
        double anyTooLow = Any.Double().LessThan(RockCell.MinimumThermalConductivity).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.RockCell().WithThermalConductivity(anyTooLow).Generate());
    }

    [Fact]
    public void Refuses_a_conductivity_above_the_ceiling() {
        double anyTooHigh = Any.Double().GreaterThan(RockCell.MaximumThermalConductivity).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.RockCell().WithThermalConductivity(anyTooHigh).Generate());
    }

    [Fact]
    public void Refuses_a_NaN_conductivity() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.RockCell().WithThermalConductivity(double.NaN).Generate());
    }

    [Fact]
    public void Refuses_every_missing_component() {
        CellCoordinate anyCoordinate   = Any.CellCoordinate().Generate();
        Porosity       anyPorosity     = Any.Porosity().Generate();
        Permeability   anyPermeability = Any.Permeability().Generate();
        Pressure       anyPressure     = Any.Pressure().Generate();
        Temperature    anyTemperature  = Any.Temperature().Generate();

        Assert.Throws<ArgumentNullException>(() => new RockCell(null!, anyPorosity, anyPermeability, 2.0, anyPressure, anyTemperature));
        Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, null!, anyPermeability, 2.0, anyPressure, anyTemperature));
        Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, null!, 2.0, anyPressure, anyTemperature));
        Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, anyPermeability, 2.0, null!, anyTemperature));
        Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, anyPermeability, 2.0, anyPressure, null!));
    }

}

public sealed class GeologicalGridTests {

    private static AnyGeologicalGrid AnyGridOf(int x, int y, int z) {
        return Any.GeologicalGrid().WithCellsX(x).WithCellsY(y).WithCellsZ(z);
    }

    [Fact]
    public void Holds_every_cell_once_at_its_own_coordinate() {
        // Arrange
        int anyX = Any.Int32().Between(1, 4).Generate();
        int anyY = Any.Int32().Between(1, 4).Generate();
        int anyZ = Any.Int32().Between(1, 4).Generate();

        // Act
        GeologicalGrid grid = AnyGridOf(anyX, anyY, anyZ).Generate();

        // Assert
        Assert.Equal(anyX * anyY * anyZ, grid.Cells.Count());
        Assert.Equal(anyX * anyY * anyZ, grid.Cells.Select(c => c.Coordinate).Distinct().Count());
        Assert.All(grid.Cells, cell => Assert.Same(cell, grid.CellAt(cell.Coordinate)));
    }

    [Fact]
    public void Keeps_its_dimensions_and_cell_size() {
        GeologicalGrid grid = AnyGridOf(3, 2, 5).WithCellSizeMetres(25.0).Generate();

        Assert.Equal((3, 2, 5, 25.0), (grid.CellsX, grid.CellsY, grid.CellsZ, grid.CellSizeMetres));
    }

    [Fact]
    public void Derives_volume_and_face_area_from_the_cell_size() {
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(10.0).Generate();

        Assert.Equal(1000.0, grid.CellVolumeCubicMetres);
        Assert.Equal(100.0, grid.FaceAreaSquareMetres);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1000.0)]
    public void Accepts_both_cell_size_bounds(double size) {
        GeologicalGrid grid = Any.GeologicalGrid().WithCellSizeMetres(size).Generate();

        Assert.Equal(size, grid.CellSizeMetres);
    }

    [Fact]
    public void Refuses_a_cell_size_below_one_metre() {
        double anyTooSmall = Any.Double().LessThan(GeologicalGrid.MinimumCellSizeMetres).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.GeologicalGrid().WithCellSizeMetres(anyTooSmall).Generate());
    }

    [Fact]
    public void Refuses_a_cell_size_above_one_kilometre() {
        double anyTooLarge = Any.Double().GreaterThan(GeologicalGrid.MaximumCellSizeMetres).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.GeologicalGrid().WithCellSizeMetres(anyTooLarge).Generate());
    }

    [Fact]
    public void Refuses_a_NaN_cell_size() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.GeologicalGrid().WithCellSizeMetres(double.NaN).Generate());
    }

    [Fact]
    public void Refuses_a_dimension_below_one_along_X() {
        int anyNotPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => AnyGridOf(anyNotPositive, 1, 1).Generate());

        Assert.Equal("cellsX", error.ParamName);
    }

    [Fact]
    public void Refuses_a_dimension_below_one_along_Y() {
        int anyNotPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // A grid of 0 x 1 x 1 would loop over nothing: build the (empty) list by hand.
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeologicalGrid(1, anyNotPositive, 1, 10.0, new List<RockCell>()));

        Assert.Equal("cellsY", error.ParamName);
    }

    [Fact]
    public void Refuses_a_dimension_below_one_along_Z() {
        int anyNotPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeologicalGrid(1, 1, anyNotPositive, 10.0, new List<RockCell>()));

        Assert.Equal("cellsZ", error.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_cell_collection() {
        Assert.Throws<ArgumentNullException>(() => new GeologicalGrid(1, 1, 1, 10.0, null!));
    }

    [Fact]
    public void Refuses_a_collection_with_too_few_cells() {
        List<RockCell> cells = AnyGridOf(2, 2, 1).Generate().Cells.ToList();
        cells.RemoveAt(cells.Count - 1);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));

        Assert.Equal("cells", error.ParamName);
    }

    [Fact]
    public void Refuses_a_collection_with_too_many_cells() {
        List<RockCell> cells = AnyGridOf(2, 2, 1).Generate().Cells.ToList();
        cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).Generate());

        Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));
    }

    [Fact]
    public void Refuses_a_cell_that_appears_twice() {
        // The count is right, the coverage is not: one coordinate is there twice and another is missing.
        List<RockCell> cells = AnyGridOf(2, 2, 1).Generate().Cells.ToList();
        cells[3] = Any.RockCell().WithCoordinate(cells[0].Coordinate).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));

        Assert.Contains("twice", error.Message);
    }

    [Fact]
    public void Refuses_a_cell_outside_the_grid() {
        List<RockCell> cells = AnyGridOf(2, 2, 1).Generate().Cells.ToList();
        int            anyBeyond = Any.Int32().GreaterThanOrEqualTo(2).LessThan(1_000_000).Generate();
        cells[3] = Any.RockCell().WithCoordinate(new CellCoordinate(anyBeyond, 0, 0)).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 2, 1, 10.0, cells));

        Assert.Contains("outside", error.Message);
    }

    [Fact]
    public void Refuses_a_null_cell() {
        List<RockCell> cells = AnyGridOf(2, 1, 1).Generate().Cells.ToList();
        cells[1] = null!;

        Assert.Throws<ArgumentException>(() => new GeologicalGrid(2, 1, 1, 10.0, cells));
    }

    [Fact]
    public void Contains_only_coordinates_inside_its_bounds() {
        GeologicalGrid grid = AnyGridOf(2, 3, 4).Generate();

        Assert.True(grid.Contains(new CellCoordinate(1, 2, 3)));
        Assert.False(grid.Contains(new CellCoordinate(2, 0, 0)));
        Assert.False(grid.Contains(new CellCoordinate(0, 3, 0)));
        Assert.False(grid.Contains(new CellCoordinate(0, 0, 4)));
    }

    [Fact]
    public void CellAt_refuses_a_coordinate_outside_the_grid() {
        GeologicalGrid grid         = AnyGridOf(2, 2, 2).Generate();
        int            anyBeyond    = Any.Int32().GreaterThanOrEqualTo(2).LessThan(1_000_000).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.CellAt(new CellCoordinate(anyBeyond, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => grid.CellAt(null!));
    }

    [Fact]
    public void An_interior_cell_has_six_neighbours() {
        GeologicalGrid grid = AnyGridOf(3, 3, 3).Generate();

        IEnumerable<RockCell> neighbours = grid.NeighboursOf(new CellCoordinate(1, 1, 1));

        Assert.Equal(6, neighbours.Count());
    }

    [Fact]
    public void A_corner_cell_has_three_neighbours() {
        GeologicalGrid grid = AnyGridOf(3, 3, 3).Generate();

        IEnumerable<RockCell> neighbours = grid.NeighboursOf(new CellCoordinate(0, 0, 0));

        Assert.Equal(3, neighbours.Count());
    }

    [Fact]
    public void A_lone_cell_has_no_neighbour() {
        GeologicalGrid grid = AnyGridOf(1, 1, 1).Generate();

        Assert.Empty(grid.NeighboursOf(new CellCoordinate(0, 0, 0)));
    }

    [Fact]
    public void Neighbours_share_a_face_with_the_cell() {
        GeologicalGrid grid = AnyGridOf(3, 3, 3).Generate();
        CellCoordinate centre = new CellCoordinate(1, 1, 1);

        Assert.All(grid.NeighboursOf(centre), neighbour => Assert.True(centre.IsFaceNeighbourOf(neighbour.Coordinate)));
    }

    [Fact]
    public void NeighboursOf_refuses_a_coordinate_outside_the_grid() {
        GeologicalGrid grid = AnyGridOf(2, 2, 2).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.NeighboursOf(new CellCoordinate(5, 0, 0)).ToList());
    }

    [Fact]
    public void Every_cell_of_a_single_column_touches_four_lateral_faces() {
        GeologicalGrid grid = AnyGridOf(1, 1, 3).Generate();

        Assert.All(grid.Cells, cell => Assert.Equal(4, grid.LateralBoundaryFaceCount(cell.Coordinate)));
    }

    [Fact]
    public void Lateral_faces_are_counted_by_position_in_a_three_by_three_layer() {
        GeologicalGrid grid = AnyGridOf(3, 3, 1).Generate();

        Assert.Equal(2, grid.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 0)));
        Assert.Equal(1, grid.LateralBoundaryFaceCount(new CellCoordinate(1, 0, 0)));
        Assert.Equal(0, grid.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 0)));
    }

    [Fact]
    public void Top_and_bottom_faces_are_not_lateral() {
        GeologicalGrid grid = AnyGridOf(3, 3, 3).Generate();

        Assert.Equal(0, grid.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 0)));
        Assert.Equal(0, grid.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 2)));
    }

    [Fact]
    public void A_single_layer_has_no_thermal_gradient() {
        GeologicalGrid grid = AnyGridOf(2, 2, 1).Generate();

        Assert.Equal(0.0, grid.AverageThermalGradient());
    }

    [Fact]
    public void Thermal_gradient_is_the_temperature_rise_per_metre_of_depth() {
        // The temperatures and the 10 m spacing are what the assertion is about.
        Temperature top    = new Temperature(300.0);
        Temperature bottom = new Temperature(320.0);
        GeologicalGrid grid = new GeologicalGrid(1, 1, 2, 10.0, new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(top).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithTemperature(bottom).Generate()
        });

        Assert.Equal(2.0, grid.AverageThermalGradient(), 9);
    }

    [Fact]
    public void Thermal_gradient_averages_each_layer_and_divides_by_the_depth_span() {
        // Two columns, three layers of 5 m: top layer averages 300, bottom layer averages 330.
        List<RockCell> cells = new List<RockCell>();
        double[]       top   = [290.0, 310.0];
        double[]       bottom = [320.0, 340.0];
        for (int column = 0; column < 2; column++) {
            cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(column, 0, 0)).WithTemperature(new Temperature(top[column])).Generate());
            cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(column, 0, 1)).Generate());
            cells.Add(Any.RockCell().WithCoordinate(new CellCoordinate(column, 0, 2)).WithTemperature(new Temperature(bottom[column])).Generate());
        }

        GeologicalGrid grid = new GeologicalGrid(2, 1, 3, 5.0, cells);

        Assert.Equal(3.0, grid.AverageThermalGradient(), 9);
    }

    [Fact]
    public void Thermal_gradient_is_negative_when_the_surface_is_warmer_than_depth() {
        GeologicalGrid grid = new GeologicalGrid(1, 1, 2, 100.0, new List<RockCell> {
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 0)).WithTemperature(new Temperature(400.0)).Generate(),
            Any.RockCell().WithCoordinate(new CellCoordinate(0, 0, 1)).WithTemperature(new Temperature(350.0)).Generate()
        });

        Assert.Equal(-0.5, grid.AverageThermalGradient(), 9);
    }

}
