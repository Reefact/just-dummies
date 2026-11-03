using GeothermalReservoir.Domain;

using Xunit;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class CellCoordinateTests {

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 7, 11)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    public void A_coordinate_with_non_negative_indices_keeps_them(int i, int j, int k) {
        CellCoordinate coordinate = new CellCoordinate(i, j, k);

        Assert.Equal((i, j, k), (coordinate.I, coordinate.J, coordinate.K));
    }

    [Theory]
    [InlineData(-1, 0, 0, "i")]
    [InlineData(0, -1, 0, "j")]
    [InlineData(0, 0, -1, "k")]
    [InlineData(int.MinValue, 0, 0, "i")]
    public void A_negative_index_is_refused_and_named(int i, int j, int k, string parameter) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CellCoordinate(i, j, k));

        Assert.Equal(parameter, refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    public void A_cell_shares_a_face_with_the_cell_one_step_further_along_an_axis(int di, int dj, int dk) {
        CellCoordinate centre = new CellCoordinate(2, 2, 2);

        Assert.True(centre.IsFaceNeighbourOf(new CellCoordinate(2 + di, 2 + dj, 2 + dk)));
        Assert.True(centre.IsFaceNeighbourOf(new CellCoordinate(2 - di, 2 - dj, 2 - dk)));
    }

    [Theory]
    [InlineData(2, 2, 2)] // itself
    [InlineData(3, 3, 2)] // diagonal in a plane
    [InlineData(3, 3, 3)] // diagonal in space
    [InlineData(4, 2, 2)] // two steps along an axis
    [InlineData(1, 2, 3)] // an edge neighbour: one step along two axes
    public void A_cell_does_not_share_a_face_with_itself_a_diagonal_or_a_distant_cell(int i, int j, int k) {
        CellCoordinate centre = new CellCoordinate(2, 2, 2);

        Assert.False(centre.IsFaceNeighbourOf(new CellCoordinate(i, j, k)));
    }

    [Fact]
    public void Sharing_a_face_is_symmetric() {
        CellCoordinate above = new CellCoordinate(0, 0, 0);
        CellCoordinate below = new CellCoordinate(0, 0, 1);

        Assert.True(above.IsFaceNeighbourOf(below));
        Assert.True(below.IsFaceNeighbourOf(above));
    }

    [Fact]
    public void Asking_whether_nothing_shares_a_face_is_refused() {
        CellCoordinate coordinate = new CellCoordinate(0, 0, 0);

        Assert.Throws<ArgumentNullException>(() => coordinate.IsFaceNeighbourOf(null!));
    }

    [Fact]
    public void Two_coordinates_of_the_same_indices_are_equal_and_any_differing_index_makes_them_different() {
        Assert.Equal(new CellCoordinate(1, 2, 3), new CellCoordinate(1, 2, 3));
        Assert.Equal(new CellCoordinate(1, 2, 3).GetHashCode(), new CellCoordinate(1, 2, 3).GetHashCode());
        Assert.NotEqual(new CellCoordinate(1, 2, 3), new CellCoordinate(0, 2, 3));
        Assert.NotEqual(new CellCoordinate(1, 2, 3), new CellCoordinate(1, 0, 3));
        Assert.NotEqual(new CellCoordinate(1, 2, 3), new CellCoordinate(1, 2, 0));
    }

    [Fact]
    public void A_coordinate_prints_its_three_indices() {
        Assert.Equal("(1,2,3)", new CellCoordinate(1, 2, 3).ToString());
    }

}
