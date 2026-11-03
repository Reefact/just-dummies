using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class SpatialPointTests {

    [Fact]
    public void Distance_is_the_euclidean_norm_of_the_coordinate_differences() {
        // Arrange
        SpatialPoint origin = new SpatialPoint(0, 0, 0);
        SpatialPoint other  = new SpatialPoint(3, 4, 12);

        // Act
        double distance = origin.DistanceTo(other);

        // Assert
        Assert.Equal(13.0, distance, 9);
    }

    [Fact]
    public void Distance_to_null_is_refused() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        Action act = () => anyPoint.DistanceTo(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(2000.0)]
    [InlineData(-2000.0)]
    public void A_coordinate_on_the_field_of_view_limit_is_accepted(double limit) {
        // Act
        SpatialPoint point = new SpatialPoint(limit, 0, 0);

        // Assert
        Assert.Equal(limit, point.XMm);
    }

    [Fact]
    public void A_coordinate_beyond_the_field_of_view_is_refused_on_each_axis() {
        // Arrange
        double anyTooFar = Any.Double().GreaterThan(SpatialPoint.MaxAbsoluteCoordinateMm).LessThan(100_000).Generate();
        double anyInside = Any.Double().Between(-SpatialPoint.MaxAbsoluteCoordinateMm, SpatialPoint.MaxAbsoluteCoordinateMm).Generate();

        // Act
        Action onX = () => new SpatialPoint(anyTooFar, anyInside, anyInside);
        Action onY = () => new SpatialPoint(anyInside, -anyTooFar, anyInside);
        Action onZ = () => new SpatialPoint(anyInside, anyInside, anyTooFar);

        // Assert
        Assert.Equal("xMm", Assert.Throws<ArgumentOutOfRangeException>(onX).ParamName);
        Assert.Equal("yMm", Assert.Throws<ArgumentOutOfRangeException>(onY).ParamName);
        Assert.Equal("zMm", Assert.Throws<ArgumentOutOfRangeException>(onZ).ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_coordinate_that_is_not_finite_is_refused(double notFinite) {
        // Act
        Action act = () => new SpatialPoint(0, notFinite, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Two_points_with_the_same_coordinates_are_equal() {
        // Arrange
        double anyX = Any.Double().Between(-2000, 2000).Generate();
        double anyY = Any.Double().Between(-2000, 2000).Generate();
        double anyZ = Any.Double().Between(-2000, 2000).Generate();

        // Act
        SpatialPoint first  = new SpatialPoint(anyX, anyY, anyZ);
        SpatialPoint second = new SpatialPoint(anyX, anyY, anyZ);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_point_differing_on_one_axis_is_not_equal() {
        // Arrange
        SpatialPoint point = new SpatialPoint(1, 2, 3);

        // Act / Assert
        Assert.NotEqual(point, new SpatialPoint(1, 2, 4));
        Assert.False(point.Equals(null));
    }

}

public sealed class VoxelSpacingTests {

    [Fact]
    public void A_voxel_volume_is_the_product_of_its_three_spacings() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.5, 0.5, 1.0);

        // Act
        double volume = spacing.VoxelVolumeMm3;

        // Assert
        Assert.Equal(0.25, volume, 9);
    }

    [Theory]
    [InlineData(0.1, true)]
    [InlineData(1.25, true)]
    [InlineData(1.26, false)]
    [InlineData(10.0, false)]
    public void Volumetry_needs_a_slice_spacing_of_at_most_1_25_mm(double sliceMm, bool suitable) {
        // Arrange
        double      anyInPlane = Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm).Generate();
        VoxelSpacing spacing   = new VoxelSpacing(anyInPlane, anyInPlane, sliceMm);

        // Act
        bool isSuitable = spacing.IsSuitableForVolumetry;

        // Assert
        Assert.Equal(suitable, isSuitable);
    }

    [Theory]
    [InlineData(0.1, 0.1, 0.1)]
    [InlineData(2.0, 2.0, 10.0)]
    public void The_limits_of_each_axis_are_accepted(double row, double column, double slice) {
        // Act
        VoxelSpacing spacing = new VoxelSpacing(row, column, slice);

        // Assert
        Assert.Equal(row, spacing.RowMm);
        Assert.Equal(column, spacing.ColumnMm);
        Assert.Equal(slice, spacing.SliceMm);
    }

    [Theory]
    [InlineData(0.099, 1.0, 1.0, "rowMm")]
    [InlineData(2.001, 1.0, 1.0, "rowMm")]
    [InlineData(1.0, 0.099, 1.0, "columnMm")]
    [InlineData(1.0, 2.001, 1.0, "columnMm")]
    [InlineData(1.0, 1.0, 0.099, "sliceMm")]
    [InlineData(1.0, 1.0, 10.001, "sliceMm")]
    [InlineData(double.NaN, 1.0, 1.0, "rowMm")]
    public void A_spacing_outside_its_axis_range_is_refused(double row, double column, double slice, string parameter) {
        // Act
        Action act = () => new VoxelSpacing(row, column, slice);

        // Assert
        Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_slice_spacing_above_the_in_plane_maximum_is_allowed_on_the_slice_axis_only() {
        // Arrange
        double anySlice = Any.Double().GreaterThan(VoxelSpacing.MaxInPlaneSpacingMm).LessThanOrEqualTo(VoxelSpacing.MaxSliceSpacingMm).Generate();

        // Act
        VoxelSpacing spacing = new VoxelSpacing(1.0, 1.0, anySlice);
        Action       act     = () => new VoxelSpacing(anySlice, 1.0, 1.0);

        // Assert
        Assert.Equal(anySlice, spacing.SliceMm);
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Two_spacings_with_the_same_values_are_equal_and_a_different_one_is_not() {
        // Arrange
        VoxelSpacing first     = new VoxelSpacing(0.7, 0.7, 1.0);
        VoxelSpacing same      = new VoxelSpacing(0.7, 0.7, 1.0);
        VoxelSpacing different = new VoxelSpacing(0.7, 0.7, 1.5);

        // Assert
        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, different);
    }

}

public sealed class AnatomicalLandmarkTests {

    [Fact]
    public void A_landmark_keeps_its_kind_and_position() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();

        // Act
        AnatomicalLandmark landmark = new AnatomicalLandmark(LandmarkKind.SternalAngle, anyPosition);

        // Assert
        Assert.Equal(LandmarkKind.SternalAngle, landmark.Kind);
        Assert.Same(anyPosition, landmark.Position);
    }

    [Fact]
    public void A_kind_that_is_not_a_declared_member_is_refused() {
        // Arrange
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();

        // Act
        Action act = () => new AnatomicalLandmark((LandmarkKind)99, anyPosition);

        // Assert
        Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_landmark_without_a_position_is_refused() {
        // Arrange
        LandmarkKind anyKind = Any.Enum<LandmarkKind>().Generate();

        // Act
        Action act = () => new AnatomicalLandmark(anyKind, null!);

        // Assert
        Assert.Equal("position", Assert.Throws<ArgumentNullException>(act).ParamName);
    }

}
