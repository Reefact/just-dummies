using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class SegmentationTests {

    private static readonly VoxelSpacing UnitCube = new VoxelSpacing(1.0, 1.0, 1.0);

    [Fact]
    public void Measuring_counts_every_voxel_and_takes_half_the_boundary_volume_as_uncertainty() {
        // Arrange
        // A voxel of 0.5 x 0.5 x 1 mm holds 0.25 mm3: 1000 voxels make 250 mm3, 200 boundary voxels leave 25 mm3 of doubt.
        VoxelSpacing spacing = new VoxelSpacing(0.5, 0.5, 1.0);
        Segmentation segmentation = Any.Segmentation().WithVoxelCount(1000).WithBoundaryVoxelCount(200).WithSpacing(spacing).Generate();

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(250.0, volume.VolumeMm3);
        Assert.Equal(25.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_segmentation_without_boundary_voxels_has_no_uncertainty() {
        // Arrange
        Segmentation segmentation = Any.Segmentation().WithVoxelCount(40).WithBoundaryVoxelCount(0).WithSpacing(UnitCube).Generate();

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(40.0, volume.VolumeMm3);
        Assert.Equal(0.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_segmentation_made_only_of_boundary_voxels_is_uncertain_by_half_its_volume() {
        // Arrange
        Segmentation segmentation = Any.Segmentation().WithVoxelCount(40).WithBoundaryVoxelCount(40).WithSpacing(UnitCube).Generate();

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(40.0, volume.VolumeMm3);
        Assert.Equal(20.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void The_segmentation_keeps_where_the_nodule_was_found() {
        // Arrange
        SpatialPoint centroid = new SpatialPoint(-12.5, 40.0, 7.25);

        // Act
        Segmentation segmentation = Any.Segmentation().WithCentroid(centroid).Generate();

        // Assert
        Assert.Same(centroid, segmentation.Centroid);
    }

    [Fact]
    public void A_single_voxel_is_a_valid_segmentation() {
        // Act
        Segmentation single = Any.Segmentation().WithVoxelCount(1).WithBoundaryVoxelCount(1).Generate();

        // Assert
        Assert.Equal(1, single.VoxelCount);
    }

    [Fact]
    public void Two_million_voxels_are_accepted_when_the_voxels_are_fine_enough_to_stay_a_nodule() {
        // Arrange
        VoxelSpacing finest = new VoxelSpacing(0.1, 0.1, 0.1);

        // Act
        Segmentation largestCount = Any.Segmentation().WithVoxelCount(Segmentation.MaxVoxelCount).WithBoundaryVoxelCount(0).WithSpacing(finest).Generate();

        // Assert
        Assert.Equal(2_000_000, largestCount.VoxelCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2_000_001)]
    public void A_voxel_count_outside_1_to_2_million_is_refused(int outside) {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();
        VoxelSpacing finest = new VoxelSpacing(0.1, 0.1, 0.1);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Segmentation(outside, 0, finest, anyCentroid));

        // Assert
        Assert.Equal("voxelCount", refusal.ParamName);
    }

    [Fact]
    public void A_negative_boundary_count_is_refused() {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Segmentation(10, -1, UnitCube, anyCentroid));

        // Assert
        Assert.Equal("boundaryVoxelCount", refusal.ParamName);
    }

    [Fact]
    public void A_boundary_larger_than_the_segmentation_itself_is_refused() {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Segmentation(10, 11, UnitCube, anyCentroid));

        // Assert
        Assert.Equal("boundaryVoxelCount", refusal.ParamName);
    }

    [Fact]
    public void A_segmentation_whose_volume_is_exactly_20000_mm3_is_accepted() {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Segmentation largest = new Segmentation(20_000, 0, UnitCube, anyCentroid);

        // Assert
        Assert.Equal(20_000.0, largest.Measure().VolumeMm3);
    }

    [Fact]
    public void A_segmentation_larger_than_what_a_nodule_can_hold_is_refused() {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Segmentation(20_001, 0, UnitCube, anyCentroid));

        // Assert
        Assert.Equal("voxelCount", refusal.ParamName);
    }

    [Fact]
    public void The_volume_cap_depends_on_the_voxel_size_not_on_the_count_alone() {
        // Arrange
        // 600 voxels of 40 mm3 (2 x 2 x 10 mm) are 24000 mm3; the same 600 voxels of 1 mm3 are fine.
        VoxelSpacing coarsest = new VoxelSpacing(2.0, 2.0, 10.0);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Segmentation fine = new Segmentation(600, 0, UnitCube, anyCentroid);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Segmentation(600, 0, coarsest, anyCentroid));

        // Assert
        Assert.Equal(600, fine.VoxelCount);
        Assert.Equal("voxelCount", refusal.ParamName);
    }

    [Fact]
    public void A_segmentation_needs_a_spacing_and_a_centroid() {
        // Arrange
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Segmentation(10, 0, null!, anyCentroid));
        Assert.Throws<ArgumentNullException>(() => new Segmentation(10, 0, UnitCube, null!));
    }

}
