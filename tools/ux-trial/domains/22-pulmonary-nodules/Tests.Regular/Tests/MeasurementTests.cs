using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class MeasuredVolumeTests {

    [Fact]
    public void The_bounds_are_the_volume_minus_and_plus_the_uncertainty() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(500, 50);

        // Act / Assert
        Assert.Equal(450, volume.LowerBoundMm3, 9);
        Assert.Equal(550, volume.UpperBoundMm3, 9);
    }

    [Fact]
    public void The_equivalent_diameter_is_that_of_the_sphere_of_the_same_volume() {
        // Arrange: a 10 mm sphere holds pi/6 * 1000 mm3
        double         sphereOf10Mm = Math.PI / 6.0 * 1000.0;
        double         anyUncertainty = Any.Double().Between(0, 100).Generate();
        MeasuredVolume volume         = new MeasuredVolume(sphereOf10Mm, anyUncertainty);

        // Act
        double diameter = volume.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, 9);
    }

    [Fact]
    public void A_volume_of_exactly_the_maximum_is_accepted() {
        // Arrange
        double anyUncertainty = Any.Double().Between(0, 1000).Generate();

        // Act
        MeasuredVolume volume = new MeasuredVolume(20_000, anyUncertainty);

        // Assert
        Assert.Equal(20_000, volume.VolumeMm3);
    }

    [Fact]
    public void An_uncertainty_of_zero_is_accepted() {
        // Arrange
        double anyVolume = Any.Double().Positive().LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3).Generate();

        // Act
        MeasuredVolume volume = new MeasuredVolume(anyVolume, 0);

        // Assert
        Assert.Equal(volume.VolumeMm3, volume.LowerBoundMm3);
        Assert.Equal(volume.VolumeMm3, volume.UpperBoundMm3);
    }

    [Fact]
    public void A_volume_that_is_not_positive_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act / Assert
        Assert.Equal("volumeMm3", Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(0, 0)).ParamName);
        Assert.Equal("volumeMm3", Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(anyNegative, 0)).ParamName);
        Assert.Equal("volumeMm3", Assert.Throws<ArgumentOutOfRangeException>(() => new MeasuredVolume(double.NaN, 0)).ParamName);
    }

    [Fact]
    public void A_volume_above_20000_mm3_is_a_mass_not_a_nodule() {
        // Arrange
        double anyMass = Any.Double().GreaterThan(MeasuredVolume.MaxVolumeMm3).LessThan(1_000_000).Generate();

        // Act
        Action act = () => new MeasuredVolume(anyMass, 0);

        // Assert
        Assert.Equal("volumeMm3", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_negative_uncertainty_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        Action act = () => new MeasuredVolume(100, anyNegative);

        // Assert
        Assert.Equal("uncertaintyMm3", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Theory]
    [InlineData(100.0, 100.0)]
    [InlineData(100.0, 100.5)]
    public void An_uncertainty_that_reaches_the_volume_is_refused(double volume, double uncertainty) {
        // Act
        Action act = () => new MeasuredVolume(volume, uncertainty);

        // Assert
        Assert.Equal("uncertaintyMm3", Assert.Throws<ArgumentException>(act).ParamName);
    }

}

public sealed class SegmentationTests {

    [Fact]
    public void Measuring_counts_every_voxel_and_halves_the_boundary_voxels_for_the_uncertainty() {
        // Arrange: a voxel holds 0.25 mm3; 100 voxels, 20 of them on the boundary
        VoxelSpacing spacing      = new VoxelSpacing(0.5, 0.5, 1.0);
        SpatialPoint anyCentroid  = Any.SpatialPoint().Generate();
        Segmentation segmentation = new Segmentation(100, 20, spacing, anyCentroid);

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(25.0, volume.VolumeMm3, 9);
        Assert.Equal(2.5, volume.UncertaintyMm3, 9);
    }

    [Fact]
    public void A_segmentation_without_boundary_voxels_has_no_uncertainty() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();
        Segmentation segmentation = new Segmentation(1, 0, anySpacing, anyCentroid);

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(0.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_segmentation_made_only_of_boundary_voxels_still_measures() {
        // Arrange
        VoxelSpacing spacing      = new VoxelSpacing(1.0, 1.0, 1.0);
        SpatialPoint anyCentroid  = Any.SpatialPoint().Generate();
        Segmentation segmentation = new Segmentation(10, 10, spacing, anyCentroid);

        // Act
        MeasuredVolume volume = segmentation.Measure();

        // Assert
        Assert.Equal(10.0, volume.VolumeMm3, 9);
        Assert.Equal(5.0, volume.UncertaintyMm3, 9);
    }

    [Fact]
    public void A_segmentation_filling_exactly_the_nodule_volume_limit_is_accepted() {
        // Arrange: 20000 voxels of 1 mm3
        VoxelSpacing spacing     = new VoxelSpacing(1.0, 1.0, 1.0);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Segmentation segmentation = new Segmentation(20_000, 0, spacing, anyCentroid);

        // Assert
        Assert.Equal(MeasuredVolume.MaxVolumeMm3, segmentation.Measure().VolumeMm3, 9);
    }

    [Fact]
    public void A_segmentation_beyond_the_nodule_volume_limit_is_refused() {
        // Arrange
        VoxelSpacing spacing     = new VoxelSpacing(1.0, 1.0, 1.0);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Action act = () => new Segmentation(20_001, 0, spacing, anyCentroid);

        // Assert
        Assert.Equal("voxelCount", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(2_000_001, 0)]
    public void A_voxel_count_outside_the_supported_range_is_refused(int voxelCount, int boundary) {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act
        Action act = () => new Segmentation(voxelCount, boundary, anySpacing, anyCentroid);

        // Assert
        Assert.Equal("voxelCount", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_negative_boundary_is_refused() {
        // Arrange
        VoxelSpacing anySpacing   = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid  = Any.SpatialPoint().Generate();
        int          anyNegative  = Any.Int32().Negative().Generate();

        // Act
        Action act = () => new Segmentation(1, anyNegative, anySpacing, anyCentroid);

        // Assert
        Assert.Equal("boundaryVoxelCount", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_boundary_larger_than_the_segmentation_is_refused() {
        // Arrange
        VoxelSpacing spacing     = new VoxelSpacing(1.0, 1.0, 1.0);
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();
        int          anyCount    = Any.Int32().Between(1, 1000).Generate();

        // Act
        Action act = () => new Segmentation(anyCount, anyCount + 1, spacing, anyCentroid);

        // Assert
        Assert.Equal("boundaryVoxelCount", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_missing_spacing_or_centroid_is_refused() {
        // Arrange
        VoxelSpacing anySpacing  = Any.VoxelSpacing().Generate();
        SpatialPoint anyCentroid = Any.SpatialPoint().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Segmentation(1, 0, null!, anyCentroid));
        Assert.Throws<ArgumentNullException>(() => new Segmentation(1, 0, anySpacing, null!));
    }

    [Fact]
    public void The_generator_only_draws_segmentations_a_nodule_can_have() {
        // Arrange
        AnySegmentation anySegmentation = Any.Segmentation();

        // Act
        List<Segmentation> segmentations = Enumerable.Range(0, 200).Select(_ => anySegmentation.Generate()).ToList();

        // Assert
        Assert.All(segmentations, s => Assert.InRange(s.Measure().VolumeMm3, 0.0, MeasuredVolume.MaxVolumeMm3));
    }

}
