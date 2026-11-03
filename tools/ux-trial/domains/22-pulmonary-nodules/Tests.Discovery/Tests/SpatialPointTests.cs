
namespace PulmonaryNodules.Tests.Discovery;

public sealed class SpatialPointTests {

    [Fact]
    public void A_point_keeps_the_coordinates_it_was_built_with() {
        // Act
        SpatialPoint point = new SpatialPoint(12.5, -40.25, 1999.75);

        // Assert
        Assert.Equal(12.5, point.XMm);
        Assert.Equal(-40.25, point.YMm);
        Assert.Equal(1999.75, point.ZMm);
    }

    [Theory]
    [InlineData(2000.0)]
    [InlineData(-2000.0)]
    [InlineData(0.0)]
    public void A_coordinate_on_the_limit_of_the_field_of_view_is_accepted(double limitMm) {
        // Arrange
        double anyOtherMm = Any.Double().Between(-2000d, 2000d).Generate();

        // Act
        Exception? onX = Record.Exception(() => new SpatialPoint(limitMm, anyOtherMm, anyOtherMm));
        Exception? onY = Record.Exception(() => new SpatialPoint(anyOtherMm, limitMm, anyOtherMm));
        Exception? onZ = Record.Exception(() => new SpatialPoint(anyOtherMm, anyOtherMm, limitMm));

        // Assert
        Assert.Null(onX);
        Assert.Null(onY);
        Assert.Null(onZ);
    }

    [Theory]
    [InlineData(2000.0001)]
    [InlineData(-2000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_coordinate_outside_the_field_of_view_is_refused_on_each_axis(double badMm) {
        // Arrange
        double anyValidMm = Any.Double().Between(-2000d, 2000d).Generate();

        // Act
        ArgumentOutOfRangeException onX = Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialPoint(badMm, anyValidMm, anyValidMm));
        ArgumentOutOfRangeException onY = Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialPoint(anyValidMm, badMm, anyValidMm));
        ArgumentOutOfRangeException onZ = Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialPoint(anyValidMm, anyValidMm, badMm));

        // Assert
        Assert.Equal("xMm", onX.ParamName);
        Assert.Equal("yMm", onY.ParamName);
        Assert.Equal("zMm", onZ.ParamName);
    }

    [Fact]
    public void The_distance_between_two_points_is_the_euclidean_one() {
        // Arrange
        SpatialPoint origin = new SpatialPoint(0, 0, 0);
        SpatialPoint other  = new SpatialPoint(3, 4, 12);

        // Act
        double distance = origin.DistanceTo(other);

        // Assert
        Assert.Equal(13.0, distance, 9);
    }

    [Fact]
    public void A_point_is_at_no_distance_from_itself() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        double distance = anyPoint.DistanceTo(anyPoint);

        // Assert
        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void Measuring_a_distance_to_nothing_is_refused() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPoint.DistanceTo(null!));
    }

    [Fact]
    public void Two_points_with_the_same_coordinates_are_equal_and_hash_alike() {
        // Arrange
        double anyXMm = Any.Double().Between(-2000d, 2000d).Generate();
        double anyYMm = Any.Double().Between(-2000d, 2000d).Generate();
        double anyZMm = Any.Double().Between(-2000d, 2000d).Generate();

        SpatialPoint first  = new SpatialPoint(anyXMm, anyYMm, anyZMm);
        SpatialPoint second = new SpatialPoint(anyXMm, anyYMm, anyZMm);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_points_that_differ_by_one_coordinate_are_not_equal() {
        // Arrange
        SpatialPoint point     = new SpatialPoint(1, 2, 3);
        SpatialPoint different = new SpatialPoint(1, 2, 3.5);

        // Act / Assert
        Assert.NotEqual(point, different);
    }

}
