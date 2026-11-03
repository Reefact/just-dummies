namespace PulmonaryNodules.Tests.Discovery;

public sealed class SegmentationTests {

    [Fact]
    public void A_segmentation_keeps_what_it_was_built_with() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Segmentation segmentation = new Segmentation(40, 12, anySpacing, anyCentroid);

        // Assert
        Assert.Equal(40, segmentation.VoxelCount);
        Assert.Equal(12, segmentation.BoundaryVoxelCount);
        Assert.Same(anySpacing, segmentation.Spacing);
        Assert.Same(anyCentroid, segmentation.Centroid);
    }

    [Fact]
    public void The_measured_volume_counts_every_voxel_and_the_uncertainty_half_the_boundary() {
        // Arrange: a voxel of 0.5 x 0.5 x 2 mm holds 0.5 mm3.
        VoxelSpacing spacing      = new VoxelSpacing(0.5, 0.5, 2.0);
        SpatialPoint anyCentroid  = Any.SpatialPoint().Generate();
        Segmentation segmentation = new Segmentation(1000, 200, spacing, anyCentroid);

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(500.0, volume.VolumeMm3, 9);
        Assert.Equal(50.0, volume.UncertaintyMm3, 9);
    }

    [Fact]
    public void A_segmentation_without_boundary_voxels_is_measured_without_uncertainty() {
        // Arrange
        VoxelSpacing spacing     = new VoxelSpacing(1, 1, 1);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        MeasuredVolume volume = new Segmentation(250, 0, spacing, anyCentroid).Measure();

        // Assert
        Assert.Equal(250.0, volume.VolumeMm3, 9);
        Assert.Equal(0.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_segmentation_made_only_of_boundary_voxels_can_still_be_measured() {
        // Arrange
        VoxelSpacing spacing     = new VoxelSpacing(1, 1, 1);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        MeasuredVolume volume = new Segmentation(100, 100, spacing, anyCentroid).Measure();

        // Assert
        Assert.Equal(100.0, volume.VolumeMm3, 9);
        Assert.Equal(50.0, volume.UncertaintyMm3, 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(2_000_001)]
    public void A_voxel_count_outside_one_to_two_million_is_refused(int badCount) {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Segmentation(badCount, 0, anySpacing, anyCentroid));

        // Assert
        Assert.Equal("voxelCount", exception.ParamName);
    }

    [Fact]
    public void A_negative_boundary_is_refused() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Segmentation(10, -1, anySpacing, anyCentroid));

        // Assert
        Assert.Equal("boundaryVoxelCount", exception.ParamName);
    }

    [Fact]
    public void A_boundary_larger_than_the_segmentation_is_refused() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Segmentation(10, 11, anySpacing, anyCentroid));

        // Assert
        Assert.Equal("boundaryVoxelCount", exception.ParamName);
    }

    [Fact]
    public void A_boundary_as_large_as_the_segmentation_is_accepted() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Exception? exception = Record.Exception(() => new Segmentation(10, 10, anySpacing, anyCentroid));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void A_segmentation_holding_more_than_a_nodule_can_hold_is_refused() {
        // Arrange: 20001 voxels of 1 mm3 are one cubic millimetre over the 20000 a nodule may have.
        VoxelSpacing spacing     = new VoxelSpacing(1, 1, 1);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Segmentation(20_001, 0, spacing, anyCentroid));

        // Assert
        Assert.Equal("voxelCount", exception.ParamName);
    }

    [Fact]
    public void A_segmentation_holding_exactly_the_largest_nodule_is_accepted() {
        // Arrange
        VoxelSpacing spacing     = new VoxelSpacing(1, 1, 1);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        MeasuredVolume volume = new Segmentation(20_000, 0, spacing, anyCentroid).Measure();

        // Assert
        Assert.Equal(MeasuredVolume.MaxVolumeMm3, volume.VolumeMm3, 9);
    }

    [Fact]
    public void A_segmentation_without_spacing_or_centroid_is_refused() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentNullException noSpacing  = Assert.Throws<ArgumentNullException>(() => new Segmentation(10, 1, null!, anyCentroid));
        ArgumentNullException noCentroid = Assert.Throws<ArgumentNullException>(() => new Segmentation(10, 1, anySpacing, null!));

        // Assert
        Assert.Equal("spacing", noSpacing.ParamName);
        Assert.Equal("centroid", noCentroid.ParamName);
    }

    [Fact]
    public void An_arbitrary_segmentation_can_always_be_measured() {
        // Arrange
        Segmentation anySegmentation = Any.Segmentation().Generate();

        // Act
        Exception? exception = Record.Exception(() => anySegmentation.Measure());

        // Assert
        Assert.Null(exception);
    }

}
