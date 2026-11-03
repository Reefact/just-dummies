using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class CellCoordinateTests {

    [Theory]
    [InlineData(-1, 0, 0, "i")]
    [InlineData(0, -1, 0, "j")]
    [InlineData(0, 0, -1, "k")]
    [InlineData(int.MinValue, 0, 0, "i")]
    public void A_negative_index_is_refused_and_named(int i, int j, int k, string parameter) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CellCoordinate(i, j, k));

        // Assert
        Assert.Equal(parameter, error.ParamName);
    }

    [Fact]
    public void The_origin_and_the_largest_indices_are_accepted() {
        // Act
        CellCoordinate origin  = new CellCoordinate(0, 0, 0);
        CellCoordinate largest = new CellCoordinate(int.MaxValue, int.MaxValue, int.MaxValue);

        // Assert
        Assert.Equal((0, 0, 0), (origin.I, origin.J, origin.K));
        Assert.Equal((int.MaxValue, int.MaxValue, int.MaxValue), (largest.I, largest.J, largest.K));
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 0, -1)]
    public void Cells_one_step_apart_along_one_axis_share_a_face(int di, int dj, int dk) {
        // Arrange: far enough from the edges for the neighbour to exist whatever the direction
        CellCoordinate anyCoordinate = Any.CellCoordinate()
                                          .WithI(i => i.Between(1, 10_000))
                                          .WithJ(j => j.Between(1, 10_000))
                                          .WithK(k => k.Between(1, 10_000))
                                          .Generate();
        CellCoordinate neighbour = new CellCoordinate(anyCoordinate.I + di, anyCoordinate.J + dj, anyCoordinate.K + dk);

        // Assert
        Assert.True(anyCoordinate.IsFaceNeighbourOf(neighbour));
        Assert.True(neighbour.IsFaceNeighbourOf(anyCoordinate));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 0, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(0, 0, -2)]
    [InlineData(1, -1, 0)]
    public void Cells_that_are_diagonal_identical_or_further_apart_do_not_share_a_face(int di, int dj, int dk) {
        // Arrange
        CellCoordinate anyCoordinate = Any.CellCoordinate()
                                          .WithI(i => i.Between(2, 10_000))
                                          .WithJ(j => j.Between(2, 10_000))
                                          .WithK(k => k.Between(2, 10_000))
                                          .Generate();
        CellCoordinate other = new CellCoordinate(anyCoordinate.I + di, anyCoordinate.J + dj, anyCoordinate.K + dk);

        // Assert
        Assert.False(anyCoordinate.IsFaceNeighbourOf(other));
    }

    [Fact]
    public void Face_adjacency_is_refused_against_a_missing_coordinate() {
        // Arrange
        CellCoordinate anyCoordinate = Any.CellCoordinate().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyCoordinate.IsFaceNeighbourOf(null!));
    }

    [Fact]
    public void Two_coordinates_with_the_same_indices_are_equal_and_hash_alike() {
        // Arrange
        CellCoordinate anyCoordinate = Any.CellCoordinate().Generate();
        CellCoordinate sameIndices   = new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K);

        // Assert
        Assert.Equal(anyCoordinate, sameIndices);
        Assert.Equal(anyCoordinate.GetHashCode(), sameIndices.GetHashCode());
    }

    [Fact]
    public void Coordinates_that_differ_in_one_index_only_are_not_equal() {
        // Arrange
        CellCoordinate anyCoordinate = Any.CellCoordinate().WithI(i => i.Between(0, 1000)).WithJ(j => j.Between(0, 1000)).WithK(k => k.Between(0, 1000)).Generate();

        // Assert
        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I + 1, anyCoordinate.J, anyCoordinate.K));
        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I, anyCoordinate.J + 1, anyCoordinate.K));
        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K + 1));
    }

    [Fact]
    public void A_coordinate_is_written_as_a_triple() {
        // Assert
        Assert.Equal("(3,0,12)", new CellCoordinate(3, 0, 12).ToString());
    }

}

public sealed class RockCellTests {

    [Theory]
    [InlineData(0.5)]
    [InlineData(10.0)]
    [InlineData(2.5)]
    public void A_conductivity_between_half_and_ten_is_accepted(double conductivity) {
        // Act
        RockCell cell = Any.RockCell().WithThermalConductivity(conductivity).Generate();

        // Assert
        Assert.Equal(conductivity, cell.ThermalConductivity);
    }

    [Theory]
    [InlineData(0.4999)]
    [InlineData(10.0001)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_conductivity_outside_the_range_is_refused(double conductivity) {
        // Arrange
        AnyRockCell anyCell = Any.RockCell().WithThermalConductivity(conductivity);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyCell.Generate());

        // Assert
        Assert.Equal("thermalConductivity", error.ParamName);
    }

    [Fact]
    public void A_cell_keeps_what_it_was_built_with() {
        // Arrange
        CellCoordinate anyCoordinate  = Any.CellCoordinate().Generate();
        Porosity       anyPorosity    = Any.Porosity().Generate();
        Permeability   anyPermeability = Any.Permeability().Generate();
        Pressure       anyPressure    = Any.Pressure().Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Act
        RockCell cell = new RockCell(anyCoordinate, anyPorosity, anyPermeability, 3.0, anyPressure, anyTemperature);

        // Assert
        Assert.Same(anyCoordinate, cell.Coordinate);
        Assert.Same(anyPorosity, cell.Porosity);
        Assert.Same(anyPermeability, cell.Permeability);
        Assert.Equal(3.0, cell.ThermalConductivity);
        Assert.Same(anyPressure, cell.Pressure);
        Assert.Same(anyTemperature, cell.Temperature);
    }

    [Fact]
    public void A_cell_refuses_each_missing_component() {
        // Arrange
        CellCoordinate anyCoordinate   = Any.CellCoordinate().Generate();
        Porosity       anyPorosity     = Any.Porosity().Generate();
        Permeability   anyPermeability = Any.Permeability().Generate();
        Pressure       anyPressure     = Any.Pressure().Generate();
        Temperature    anyTemperature  = Any.Temperature().Generate();

        // Assert
        Assert.Equal("coordinate", Assert.Throws<ArgumentNullException>(() => new RockCell(null!, anyPorosity, anyPermeability, 3.0, anyPressure, anyTemperature)).ParamName);
        Assert.Equal("porosity", Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, null!, anyPermeability, 3.0, anyPressure, anyTemperature)).ParamName);
        Assert.Equal("permeability", Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, null!, 3.0, anyPressure, anyTemperature)).ParamName);
        Assert.Equal("pressure", Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, anyPermeability, 3.0, null!, anyTemperature)).ParamName);
        Assert.Equal("temperature", Assert.Throws<ArgumentNullException>(() => new RockCell(anyCoordinate, anyPorosity, anyPermeability, 3.0, anyPressure, null!)).ParamName);
    }

}
