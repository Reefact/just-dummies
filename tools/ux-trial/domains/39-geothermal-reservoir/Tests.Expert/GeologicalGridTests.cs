using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

public sealed class GeologicalGridTests {

    [Fact]
    public void A_grid_must_be_filled_completely() {
        // Arrange
        double         anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();
        List<RockCell> threeCells  = [Cell(0, 0, 0), Cell(1, 0, 0), Cell(0, 1, 0)];

        // Act
        Exception? refusal = Record.Exception(() => new GeologicalGrid(2, 2, 1, anyCellSize, threeCells));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_grid_cannot_hold_the_same_cell_twice() {
        // Arrange
        double         anyCellSize = Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres).Generate();
        List<RockCell> twiceTheSame = [Cell(0, 0, 0), Cell(0, 0, 0)];

        // Act
        Exception? refusal = Record.Exception(() => new GeologicalGrid(2, 1, 1, anyCellSize, twiceTheSame));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("twice", argumentException.Message);
    }

    [Fact]
    public void A_corner_cell_of_a_block_has_three_face_neighbours() {
        // Arrange
        GeologicalGrid anyBlock = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();

        // Act
        int neighbours = anyBlock.NeighboursOf(new CellCoordinate(0, 0, 0)).Count();

        // Assert
        Assert.Equal(3, neighbours);
    }

    [Fact]
    public void Only_lateral_faces_touch_the_far_field_aquifer() {
        // Arrange
        GeologicalGrid anyBlock = Any.GeologicalGrid().WithDimensions(3, 3, 3).Generate();

        // Act
        int topCornerFaces    = anyBlock.LateralBoundaryFaceCount(new CellCoordinate(0, 0, 0));
        int topCentreFaces    = anyBlock.LateralBoundaryFaceCount(new CellCoordinate(1, 1, 0));
        int bottomEdgeFaces   = anyBlock.LateralBoundaryFaceCount(new CellCoordinate(1, 2, 2));

        // Assert
        Assert.Equal(2, topCornerFaces);
        Assert.Equal(0, topCentreFaces);
        Assert.Equal(1, bottomEdgeFaces);
    }

    [Fact]
    public void The_thermal_gradient_is_the_temperature_rise_per_metre_of_depth() {
        // Arrange
        GeologicalGrid anyLayeredGrid = Any.GeologicalGrid()
                                           .WithDimensions(2, 2, 3)
                                           .WithCellSizeMetres(100)
                                           .WithRock((coordinate, rock) => rock.WithTemperature(Temperature.FromCelsius(60 + 15 * coordinate.K)))
                                           .Generate();

        // Act
        double gradient = anyLayeredGrid.AverageThermalGradient();

        // Assert
        Assert.Equal(0.15, gradient, 1e-9);
    }

    [Fact]
    public void A_single_layer_has_no_vertical_gradient() {
        // Arrange
        GeologicalGrid anySingleLayer = Any.GeologicalGrid().WithCellsZ(1).Generate();

        // Act
        double gradient = anySingleLayer.AverageThermalGradient();

        // Assert
        Assert.Equal(0.0, gradient);
    }

    private static RockCell Cell(int i, int j, int k) {
        return Any.RockCell().WithCoordinate(new CellCoordinate(i, j, k)).Generate();
    }

}
