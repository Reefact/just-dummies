using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

public sealed class CellCoordinateTests {

    [Fact]
    public void The_cell_just_below_shares_a_face() {
        // Arrange
        CellCoordinate anyCell = Any.CellCoordinate().WithK(k => k.LessThan(int.MaxValue)).Generate();
        CellCoordinate below   = new CellCoordinate(anyCell.I, anyCell.J, anyCell.K + 1);

        // Act
        bool shareAFace = anyCell.IsFaceNeighbourOf(below);

        // Assert
        Assert.True(shareAFace);
    }

    [Fact]
    public void A_diagonal_cell_shares_only_an_edge() {
        // Arrange
        CellCoordinate anyCell  = Any.CellCoordinate()
                                     .WithI(i => i.LessThan(int.MaxValue))
                                     .WithJ(j => j.LessThan(int.MaxValue))
                                     .Generate();
        CellCoordinate diagonal = new CellCoordinate(anyCell.I + 1, anyCell.J + 1, anyCell.K);

        // Act
        bool shareAFace = anyCell.IsFaceNeighbourOf(diagonal);

        // Assert
        Assert.False(shareAFace);
    }

}
