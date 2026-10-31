using JustDummies.Xunit;
using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class VoxelSpacingTests {

    [Fact]
    public void The_voxel_volume_is_the_product_of_the_three_spacings() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.7, 0.7, 1.25);

        // Act
        double voxelVolume = spacing.VoxelVolumeMm3;

        // Assert
        Assert.Equal(0.6125, voxelVolume, 10);
    }

    [Theory]
    [InlineData(0.625, true)]
    [InlineData(1.25, true)]
    [InlineData(2.5, false)]
    [InlineData(5.0, false)]
    public void Only_a_thin_slice_spacing_is_suitable_for_volumetry(double sliceMm, bool expected) {
        // Arrange
        VoxelSpacing anySpacing = Arbitrary.VoxelSpacing().WithSliceMm(sliceMm).Generate();

        // Act
        bool suitable = anySpacing.IsSuitableForVolumetry;

        // Assert
        Assert.Equal(expected, suitable);
    }

}
