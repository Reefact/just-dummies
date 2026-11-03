using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class GeoPointTests {

    [Theory]
    [InlineData(-90.0001d)]
    [InlineData(90.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_latitude_outside_the_poles_is_refused(double latitude) {
        // Arrange
        double anyLongitude = Any.Double().Between(-180d, 180d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, anyLongitude));

        // Assert
        Assert.Equal("latitude", error.ParamName);
    }

    [Theory]
    [InlineData(-180.0001d)]
    [InlineData(180.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_longitude_outside_the_antimeridian_is_refused(double longitude) {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(anyLatitude, longitude));

        // Assert
        Assert.Equal("longitude", error.ParamName);
    }

    [Theory]
    [InlineData(-90d, -180d)]
    [InlineData(90d, 180d)]
    [InlineData(0d, 0d)]
    public void The_extreme_coordinates_are_accepted(double latitude, double longitude) {
        // Act
        GeoPoint point = new GeoPoint(latitude, longitude);

        // Assert
        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
    }

    [Fact]
    public void One_degree_of_longitude_on_the_equator_is_about_111_kilometres() {
        // Arrange
        GeoPoint origin = new GeoPoint(0d, 0d);
        GeoPoint east   = new GeoPoint(0d, 1d);

        // Act
        double distance = origin.DistanceKmTo(east);

        // Assert
        Assert.Equal(111.19d, distance, 2);
    }

    [Fact]
    public void Paris_and_London_are_about_344_kilometres_apart() {
        // Arrange
        GeoPoint paris  = new GeoPoint(48.8566d, 2.3522d);
        GeoPoint london = new GeoPoint(51.5074d, -0.1278d);

        // Act
        double distance = paris.DistanceKmTo(london);

        // Assert
        Assert.InRange(distance, 343d, 345d);
    }

    [Fact]
    public void The_two_poles_are_half_the_earth_circumference_apart() {
        // Arrange
        GeoPoint north = new GeoPoint(90d, 0d);
        GeoPoint south = new GeoPoint(-90d, 0d);

        // Act
        double distance = north.DistanceKmTo(south);

        // Assert
        Assert.Equal(Math.PI * 6371d, distance, 3);
    }

    [Fact]
    public void A_point_is_at_distance_zero_from_a_point_with_the_same_coordinates() {
        // Arrange
        double   anyLatitude  = Any.Double().Between(-90d, 90d).Generate();
        double   anyLongitude = Any.Double().Between(-180d, 180d).Generate();
        GeoPoint point        = new GeoPoint(anyLatitude, anyLongitude);
        GeoPoint twin         = new GeoPoint(anyLatitude, anyLongitude);

        // Act
        double distance = point.DistanceKmTo(twin);

        // Assert
        Assert.Equal(0d, distance, 6);
    }

    [Fact]
    public void Measuring_towards_nothing_is_refused() {
        // Arrange
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPoint.DistanceKmTo(null!));
    }

    [Fact]
    public void Two_points_with_the_same_coordinates_are_equal() {
        // Arrange
        GeoPoint point = new GeoPoint(12.5d, -40.25d);
        GeoPoint twin  = new GeoPoint(12.5d, -40.25d);

        // Act & Assert
        Assert.Equal(point, twin);
        Assert.Equal(point.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void Two_points_with_different_coordinates_are_not_equal() {
        // Arrange
        GeoPoint point = new GeoPoint(12.5d, -40.25d);
        GeoPoint other = new GeoPoint(12.5d, -40.26d);

        // Act & Assert
        Assert.NotEqual(point, other);
    }

}
