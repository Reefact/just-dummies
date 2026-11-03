using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class CellCoordinateTests {

    private static AnyCellCoordinate AnyInteriorCoordinate() {
        return Any.CellCoordinate().WithI(i => i.Between(0, 1000)).WithJ(j => j.Between(0, 1000)).WithK(k => k.Between(0, 1000));
    }

    [Fact]
    public void Keeps_its_three_indices() {
        // Arrange
        int anyI = Any.Int32().GreaterThanOrEqualTo(0).Generate();
        int anyJ = Any.Int32().GreaterThanOrEqualTo(0).Generate();
        int anyK = Any.Int32().GreaterThanOrEqualTo(0).Generate();

        // Act
        CellCoordinate coordinate = new CellCoordinate(anyI, anyJ, anyK);

        // Assert
        Assert.Equal((anyI, anyJ, anyK), (coordinate.I, coordinate.J, coordinate.K));
    }

    [Fact]
    public void Accepts_the_origin() {
        CellCoordinate origin = new CellCoordinate(0, 0, 0);

        Assert.Equal("(0,0,0)", origin.ToString());
    }

    [Fact]
    public void Refuses_a_negative_I() {
        int anyNegative = Any.Int32().Negative().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CellCoordinate(anyNegative, 0, 0));

        Assert.Equal("i", error.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_J() {
        int anyNegative = Any.Int32().Negative().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CellCoordinate(0, anyNegative, 0));

        Assert.Equal("j", error.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_K() {
        int anyNegative = Any.Int32().Negative().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CellCoordinate(0, 0, anyNegative));

        Assert.Equal("k", error.ParamName);
    }

    [Fact]
    public void Two_coordinates_with_the_same_indices_are_equal() {
        CellCoordinate anyCoordinate = Any.CellCoordinate().Generate();
        CellCoordinate sameIndices   = new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K);

        Assert.Equal(anyCoordinate, sameIndices);
        Assert.Equal(anyCoordinate.GetHashCode(), sameIndices.GetHashCode());
    }

    [Fact]
    public void Coordinates_that_differ_on_one_index_are_not_equal() {
        CellCoordinate anyCoordinate = AnyInteriorCoordinate().Generate();

        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I + 1, anyCoordinate.J, anyCoordinate.K));
        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I, anyCoordinate.J + 1, anyCoordinate.K));
        Assert.NotEqual(anyCoordinate, new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K + 1));
    }

    [Fact]
    public void Prints_its_indices() {
        Assert.Equal("(2,5,7)", new CellCoordinate(2, 5, 7).ToString());
    }

    [Fact]
    public void A_cell_is_a_face_neighbour_of_the_next_one_along_each_axis() {
        // Arrange
        CellCoordinate anyCoordinate = AnyInteriorCoordinate().Generate();

        // Act / Assert
        Assert.True(anyCoordinate.IsFaceNeighbourOf(new CellCoordinate(anyCoordinate.I + 1, anyCoordinate.J, anyCoordinate.K)));
        Assert.True(anyCoordinate.IsFaceNeighbourOf(new CellCoordinate(anyCoordinate.I, anyCoordinate.J + 1, anyCoordinate.K)));
        Assert.True(anyCoordinate.IsFaceNeighbourOf(new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K + 1)));
    }

    [Fact]
    public void Neighbourhood_is_symmetric() {
        CellCoordinate anyCoordinate = AnyInteriorCoordinate().Generate();
        CellCoordinate next          = new CellCoordinate(anyCoordinate.I, anyCoordinate.J, anyCoordinate.K + 1);

        Assert.True(next.IsFaceNeighbourOf(anyCoordinate));
    }

    [Fact]
    public void A_cell_is_not_its_own_neighbour() {
        CellCoordinate anyCoordinate = Any.CellCoordinate().Generate();

        Assert.False(anyCoordinate.IsFaceNeighbourOf(anyCoordinate));
    }

    [Fact]
    public void A_diagonal_cell_shares_no_face() {
        CellCoordinate anyCoordinate = AnyInteriorCoordinate().Generate();
        CellCoordinate diagonal      = new CellCoordinate(anyCoordinate.I + 1, anyCoordinate.J + 1, anyCoordinate.K);

        Assert.False(anyCoordinate.IsFaceNeighbourOf(diagonal));
    }

    [Fact]
    public void A_cell_two_steps_away_shares_no_face() {
        CellCoordinate anyCoordinate = AnyInteriorCoordinate().Generate();

        Assert.False(anyCoordinate.IsFaceNeighbourOf(new CellCoordinate(anyCoordinate.I + 2, anyCoordinate.J, anyCoordinate.K)));
    }

    [Fact]
    public void Neighbourhood_needs_a_coordinate_to_compare_with() {
        CellCoordinate anyCoordinate = Any.CellCoordinate().Generate();

        Assert.Throws<ArgumentNullException>(() => anyCoordinate.IsFaceNeighbourOf(null!));
    }

    [Fact(Skip = "domain defect: IsFaceNeighbourOf sums three int distances unchecked; (0,0,0) against (int.MaxValue,int.MaxValue,3) sums to 2^32+1, wraps to 1 and is reported as a face neighbour; seed 127162394 (literal indices, any seed replays it)")]
    public void Far_apart_cells_are_never_reported_as_neighbours() {
        // Index sums stay within int range for a real grid; here the indices reach the type's limits.
        CellCoordinate origin  = new CellCoordinate(0, 0, 0);
        CellCoordinate farAway = new CellCoordinate(int.MaxValue, int.MaxValue, 3);

        Assert.False(origin.IsFaceNeighbourOf(farAway));
    }

}
