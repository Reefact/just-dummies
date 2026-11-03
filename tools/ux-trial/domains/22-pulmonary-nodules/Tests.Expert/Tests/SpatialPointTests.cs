using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class SpatialPointTests {

    [Fact]
    public void Distance_is_measured_in_a_straight_line_through_the_three_axes() {
        // Arrange
        SpatialPoint origin = new SpatialPoint(0.0, 0.0, 0.0);
        SpatialPoint farCorner = new SpatialPoint(2.0, 3.0, 6.0);

        // Act
        double distance = origin.DistanceTo(farCorner);

        // Assert
        Assert.Equal(7.0, distance);
    }

    [Fact]
    public void Two_points_at_the_same_height_are_apart_by_their_in_plane_gap_alone() {
        // Arrange
        // The height is shared by both points, so it takes no part in the distance.
        double anySharedHeight = Any.Double().Between(-SpatialPoint.MaxAbsoluteCoordinateMm, SpatialPoint.MaxAbsoluteCoordinateMm).Generate();
        SpatialPoint left = new SpatialPoint(0.0, 0.0, anySharedHeight);
        SpatialPoint right = new SpatialPoint(3.0, 4.0, anySharedHeight);

        // Act
        double distance = left.DistanceTo(right);

        // Assert
        Assert.Equal(5.0, distance);
    }

    [Fact]
    public void A_point_exactly_on_the_edge_of_the_field_of_view_is_accepted() {
        // Arrange
        double edge = SpatialPoint.MaxAbsoluteCoordinateMm;

        // Act
        SpatialPoint corner = new SpatialPoint(edge, -edge, edge);

        // Assert
        Assert.Equal(2000.0, corner.XMm);
        Assert.Equal(-2000.0, corner.YMm);
        Assert.Equal(2000.0, corner.ZMm);
    }

    [Theory]
    [InlineData("xMm", 2000.001, 0.0, 0.0)]
    [InlineData("xMm", -2000.001, 0.0, 0.0)]
    [InlineData("yMm", 0.0, 2000.001, 0.0)]
    [InlineData("yMm", 0.0, -2000.001, 0.0)]
    [InlineData("zMm", 0.0, 0.0, 2000.001)]
    [InlineData("zMm", 0.0, 0.0, -2000.001)]
    public void A_coordinate_beyond_2000_mm_is_refused_and_named(string axis, double x, double y, double z) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialPoint(x, y, z));

        // Assert
        Assert.Equal(axis, refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_coordinate_that_is_not_a_finite_number_is_refused(double notFinite) {
        // Arrange
        // The other two coordinates are well-formed; only the first one is wrong.
        SpatialPoint anyOtherPoint = Any.SpatialPoint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialPoint(notFinite, anyOtherPoint.YMm, anyOtherPoint.ZMm));

        // Assert
        Assert.Equal("xMm", refusal.ParamName);
    }

    [Fact]
    public void Points_with_the_same_three_coordinates_are_equal_and_hash_alike() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();
        SpatialPoint sameCoordinates = new SpatialPoint(anyPoint.XMm, anyPoint.YMm, anyPoint.ZMm);

        // Act
        bool equal = anyPoint.Equals(sameCoordinates);
        bool sameHash = anyPoint.GetHashCode() == sameCoordinates.GetHashCode();

        // Assert
        Assert.True(equal);
        Assert.True(sameHash);
    }

    [Theory]
    [InlineData(1.0, 0.0, 0.0)]
    [InlineData(0.0, 1.0, 0.0)]
    [InlineData(0.0, 0.0, 1.0)]
    public void Points_differing_on_one_axis_only_are_not_equal(double extraX, double extraY, double extraZ) {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().WithXMm(x => x.Between(-1000.0, 1000.0))
                                   .WithYMm(y => y.Between(-1000.0, 1000.0))
                                   .WithZMm(z => z.Between(-1000.0, 1000.0))
                                   .Generate();
        SpatialPoint shiftedByOneMillimetre = new SpatialPoint(anyPoint.XMm + extraX, anyPoint.YMm + extraY, anyPoint.ZMm + extraZ);

        // Act
        bool equal = anyPoint.Equals(shiftedByOneMillimetre);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_point_is_never_equal_to_nothing() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        bool equal = anyPoint.Equals(null);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Distance_to_nothing_is_refused() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPoint.DistanceTo(null!));
    }

}
