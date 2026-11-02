using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class GeofenceTests {

    // A field 10 to 20 degrees north and 30 to 40 degrees east: these corners are what the Contains tests are about.
    private static Geofence TheField() {
        return new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(20.0, 40.0));
    }

    [Fact]
    public void A_geofence_keeps_the_two_corners_it_was_given() {
        // Arrange
        GeoCoordinate southWest = new GeoCoordinate(10.0, 30.0);
        GeoCoordinate northEast = new GeoCoordinate(20.0, 40.0);

        // Act
        Geofence fence = new Geofence(southWest, northEast);

        // Assert
        Assert.Same(southWest, fence.SouthWest);
        Assert.Same(northEast, fence.NorthEast);
    }

    [Fact]
    public void A_missing_south_west_corner_is_refused() {
        // Arrange
        GeoCoordinate anyCorner = Any.GeoCoordinate().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Geofence(null!, anyCorner));

        // Assert
        Assert.Equal("southWest", refusal.ParamName);
    }

    [Fact]
    public void A_missing_north_east_corner_is_refused() {
        // Arrange
        GeoCoordinate anyCorner = Any.GeoCoordinate().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Geofence(anyCorner, null!));

        // Assert
        Assert.Equal("northEast", refusal.ParamName);
    }

    [Fact]
    public void A_south_west_corner_north_of_the_north_east_corner_is_refused() {
        // Arrange
        double        anyLongitude = AnyGeoCoordinate.ValidLongitude().Generate();
        GeoCoordinate northerly    = new GeoCoordinate(20.0, anyLongitude);
        GeoCoordinate southerly    = new GeoCoordinate(19.999999999999996, anyLongitude);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Geofence(northerly, southerly));

        // Assert
        Assert.Equal("southWest", refusal.ParamName);
    }

    [Fact]
    public void A_south_west_corner_east_of_the_north_east_corner_is_refused() {
        // Arrange
        double        anyLatitude = AnyGeoCoordinate.ValidLatitude().Generate();
        GeoCoordinate easterly    = new GeoCoordinate(anyLatitude, 40.0);
        GeoCoordinate westerly    = new GeoCoordinate(anyLatitude, 39.99999999999999);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Geofence(easterly, westerly));

        // Assert
        Assert.Equal("southWest", refusal.ParamName);
    }

    [Fact]
    public void A_fence_that_would_cross_the_antimeridian_is_refused() {
        // Arrange: from 170 east round to -170 (that is 190 east) is the case the model leaves out.
        double        anyLatitude = AnyGeoCoordinate.ValidLatitude().Generate();
        GeoCoordinate west        = new GeoCoordinate(anyLatitude, 170.0);
        GeoCoordinate east        = new GeoCoordinate(anyLatitude, -170.0);

        // Assert
        Assert.Throws<ArgumentException>(() => new Geofence(west, east));
    }

    [Fact]
    public void Two_equal_corners_make_a_valid_degenerate_fence() {
        // Arrange
        GeoCoordinate anyCorner      = Any.GeoCoordinate().Generate();
        GeoCoordinate sameCoordinate = new GeoCoordinate(anyCorner.Latitude, anyCorner.Longitude);

        // Act
        Geofence fence = new Geofence(anyCorner, sameCoordinate);

        // Assert
        Assert.True(fence.Contains(anyCorner));
    }

    [Fact]
    public void A_degenerate_fence_contains_only_its_own_point() {
        // Arrange
        Geofence fence = new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(10.0, 30.0));

        // Assert
        Assert.True(fence.Contains(new GeoCoordinate(10.0, 30.0)));
        Assert.False(fence.Contains(new GeoCoordinate(10.0, 30.000000000000004)));
        Assert.False(fence.Contains(new GeoCoordinate(10.000000000000002, 30.0)));
    }

    [Fact]
    public void A_fence_may_be_a_line_of_constant_latitude_or_constant_longitude() {
        Geofence parallel = new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(10.0, 40.0));
        Geofence meridian = new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(20.0, 30.0));

        Assert.True(parallel.Contains(new GeoCoordinate(10.0, 35.0)));
        Assert.True(meridian.Contains(new GeoCoordinate(15.0, 30.0)));
    }

    [Theory]
    [InlineData(15.0, 35.0)]
    [InlineData(10.5, 30.5)]
    [InlineData(19.5, 39.5)]
    public void A_point_inside_the_field_is_contained(double latitude, double longitude) {
        Assert.True(TheField().Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(10.0, 30.0)]
    [InlineData(10.0, 40.0)]
    [InlineData(20.0, 30.0)]
    [InlineData(20.0, 40.0)]
    public void A_point_on_a_corner_is_contained(double latitude, double longitude) {
        Assert.True(TheField().Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(10.0, 35.0)]
    [InlineData(20.0, 35.0)]
    [InlineData(15.0, 30.0)]
    [InlineData(15.0, 40.0)]
    public void A_point_on_an_edge_is_contained(double latitude, double longitude) {
        Assert.True(TheField().Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(9.999999999999998, 35.0)]
    [InlineData(20.000000000000004, 35.0)]
    [InlineData(15.0, 29.999999999999996)]
    [InlineData(15.0, 40.00000000000001)]
    [InlineData(0.0, 0.0)]
    [InlineData(90.0, -180.0)]
    public void A_point_just_outside_any_side_is_not_contained(double latitude, double longitude) {
        Assert.False(TheField().Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Fact]
    public void A_point_at_the_right_latitude_but_the_wrong_longitude_is_not_contained() {
        Assert.False(TheField().Contains(new GeoCoordinate(15.0, -35.0)));
        Assert.False(TheField().Contains(new GeoCoordinate(-15.0, 35.0)));
    }

}
