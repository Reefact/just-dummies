using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class GeofenceTests {

    // The field under test: south-west (10, 20), north-east (30, 40).
    private static Geofence AField() {
        return new Geofence(new GeoCoordinate(10.0, 20.0), new GeoCoordinate(30.0, 40.0));
    }

    [Fact]
    public void A_fence_keeps_its_two_corners() {
        // Arrange
        GeoCoordinate southWest = new GeoCoordinate(10.0, 20.0);
        GeoCoordinate northEast = new GeoCoordinate(30.0, 40.0);

        // Act
        Geofence fence = new Geofence(southWest, northEast);

        // Assert
        Assert.Same(southWest, fence.SouthWest);
        Assert.Same(northEast, fence.NorthEast);
    }

    [Fact]
    public void A_missing_south_west_corner_is_refused() {
        GeoCoordinate anyCorner = Any.GeoCoordinate().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Geofence(null!, anyCorner));

        Assert.Equal("southWest", exception.ParamName);
    }

    [Fact]
    public void A_missing_north_east_corner_is_refused() {
        GeoCoordinate anyCorner = Any.GeoCoordinate().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Geofence(anyCorner, null!));

        Assert.Equal("northEast", exception.ParamName);
    }

    [Fact]
    public void A_south_west_corner_north_of_the_north_east_one_is_refused() {
        // Arrange: the latitudes are inverted; the longitudes are in the right order and do not matter.
        (double anyWestLongitude, double anyEastLongitude) = Any.OrderedPair(Any.Double().Between(-180d, 179d), Any.Double().Between(-180d, 179d)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Geofence(new GeoCoordinate(30.0, anyWestLongitude), new GeoCoordinate(29.9999999, anyEastLongitude)));

        // Assert
        Assert.Equal("southWest", exception.ParamName);
    }

    [Fact]
    public void A_south_west_corner_east_of_the_north_east_one_is_refused() {
        // Arrange: the longitudes are inverted; the latitudes are in the right order and do not matter.
        (double anySouthLatitude, double anyNorthLatitude) = Any.OrderedPair(Any.Double().Between(-90d, 90d), Any.Double().Between(-90d, 90d)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Geofence(new GeoCoordinate(anySouthLatitude, 40.0), new GeoCoordinate(anyNorthLatitude, 39.9999999)));

        // Assert
        Assert.Equal("southWest", exception.ParamName);
    }

    [Fact]
    public void A_fence_may_cross_no_antimeridian() {
        // 170 east to -170 (a rectangle over the date line) is refused rather than reinterpreted.
        Assert.Throws<ArgumentException>(() => new Geofence(new GeoCoordinate(0.0, 170.0), new GeoCoordinate(10.0, -170.0)));
    }

    [Fact]
    public void A_degenerate_fence_reduced_to_one_point_is_valid() {
        // Arrange
        GeoCoordinate anyPoint = Any.GeoCoordinate().Generate();

        // Act
        Geofence fence = new Geofence(anyPoint, anyPoint);

        // Assert
        Assert.True(fence.Contains(anyPoint));
    }

    [Fact]
    public void A_degenerate_fence_reduced_to_a_line_is_valid() {
        Geofence meridian = new Geofence(new GeoCoordinate(10.0, 20.0), new GeoCoordinate(30.0, 20.0));

        Assert.True(meridian.Contains(new GeoCoordinate(15.0, 20.0)));
        Assert.False(meridian.Contains(new GeoCoordinate(15.0, 20.0000001)));
    }

    [Fact]
    public void A_drawn_fence_is_always_buildable() {
        // Arrange
        Geofence anyFence = Any.Geofence().Generate();

        // Assert: the generator's own ordering keeps both corners in the right order.
        Assert.True(anyFence.SouthWest.Latitude <= anyFence.NorthEast.Latitude);
        Assert.True(anyFence.SouthWest.Longitude <= anyFence.NorthEast.Longitude);
    }

    [Theory]
    [InlineData(20.0, 30.0)]    // strictly inside
    [InlineData(10.0, 30.0)]    // on the south edge
    [InlineData(30.0, 30.0)]    // on the north edge
    [InlineData(20.0, 20.0)]    // on the west edge
    [InlineData(20.0, 40.0)]    // on the east edge
    [InlineData(10.0, 20.0)]    // south-west corner
    [InlineData(30.0, 40.0)]    // north-east corner
    [InlineData(10.0, 40.0)]    // south-east corner
    [InlineData(30.0, 20.0)]    // north-west corner
    public void A_point_inside_or_on_the_boundary_is_contained(double latitude, double longitude) {
        Assert.True(AField().Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(9.9999999, 30.0)]     // just south
    [InlineData(30.0000001, 30.0)]    // just north
    [InlineData(20.0, 19.9999999)]    // just west
    [InlineData(20.0, 40.0000001)]    // just east
    [InlineData(0.0, 0.0)]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, 179.0)]
    public void A_point_outside_is_not_contained(double latitude, double longitude) {
        Assert.False(AField().Contains(new GeoCoordinate(latitude, longitude)));
    }

}
