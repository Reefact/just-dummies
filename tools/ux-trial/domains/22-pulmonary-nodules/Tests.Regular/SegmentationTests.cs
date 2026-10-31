using JustDummies;
using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class SegmentationTests {

    [Fact]
    public void Measuring_counts_every_voxel_and_half_the_boundary_as_uncertainty() {
        // Arrange
        Segmentation anySegmentation = Arbitrary.Segmentation()
                                                .WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0))
                                                .WithVoxelCount(1_000)
                                                .WithBoundaryVoxelCount(200)
                                                .Generate();

        // Act
        MeasuredVolume volume = anySegmentation.Measure();

        // Assert
        Assert.Equal(250.0, volume.VolumeMm3);
        Assert.Equal(25.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_mask_larger_than_a_nodule_can_be_is_rejected() {
        // Arrange
        VoxelSpacing oneCubicMillimetre = new VoxelSpacing(1.0, 1.0, 1.0);
        int          anyBoundary        = Any.Int32().Between(0, 20_001).Generate();
        SpatialPoint anyCentroid        = Arbitrary.SpatialPoint().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new Segmentation(20_001, anyBoundary, oneCubicMillimetre, anyCentroid));

        // Assert
        Assert.Equal("voxelCount", thrown.ParamName);
    }

    [Fact]
    public void A_boundary_larger_than_the_mask_is_rejected() {
        // Arrange
        VoxelSpacing anySpacing  = Arbitrary.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Arbitrary.SpatialPoint().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new Segmentation(10, 11, anySpacing, anyCentroid));

        // Assert
        Assert.Equal("boundaryVoxelCount", thrown.ParamName);
    }

}
