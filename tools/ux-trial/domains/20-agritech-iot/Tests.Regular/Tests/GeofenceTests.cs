using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class GeofenceTests {

    // The field every Contains test is about: 10..30 degrees north, 20..40 degrees east.
    private static Geofence Field() {
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
        // Arrange
        GeoCoordinate anyNorthEast = Any.GeoCoordinate().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Geofence(null!, anyNorthEast));

        // Assert
        Assert.Equal("southWest", exception.ParamName);
    }

    [Fact]
    public void A_missing_north_east_corner_is_refused() {
        // Arrange
        GeoCoordinate anySouthWest = Any.GeoCoordinate().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Geofence(anySouthWest, null!));

        // Assert
        Assert.Equal("northEast", exception.ParamName);
    }

    [Fact]
    public void A_south_west_corner_north_of_the_north_east_one_is_refused() {
        // Arrange
        GeoCoordinate anySouthWest = Any.GeoCoordinate().WithLatitude(l => l.GreaterThan(-90d)).Generate();
        GeoCoordinate anyNorthEast = Any.GeoCoordinate().WithLatitude(l => l.LessThan(anySouthWest.Latitude)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Geofence(anySouthWest, anyNorthEast));
    }

    [Fact]
    public void A_south_west_corner_east_of_the_north_east_one_is_refused() {
        // Arrange
        GeoCoordinate anySouthWest = Any.GeoCoordinate().WithLongitude(l => l.GreaterThan(-180d)).Generate();
        GeoCoordinate anyNorthEast = Any.GeoCoordinate().WithLongitude(l => l.LessThan(anySouthWest.Longitude)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Geofence(anySouthWest, anyNorthEast));
    }

    [Fact]
    public void A_fence_crossing_the_antimeridian_is_refused() {
        // Arrange: from 170 degrees east to 170 degrees west would wrap around the globe.
        GeoCoordinate southWest = new GeoCoordinate(0.0, 170.0);
        GeoCoordinate northEast = new GeoCoordinate(10.0, -170.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Geofence(southWest, northEast));

        // Assert
        Assert.Equal("southWest", exception.ParamName);
    }

    [Fact]
    public void A_degenerate_fence_reduced_to_a_single_point_is_accepted() {
        // Arrange
        GeoCoordinate anyPoint = Any.GeoCoordinate().Generate();

        // Act
        Geofence anyDegenerateFence = new Geofence(anyPoint, anyPoint);

        // Assert
        Assert.True(anyDegenerateFence.Contains(anyPoint));
    }

    [Fact]
    public void A_fence_reduced_to_a_line_is_accepted() {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Act
        Geofence anyLineFence = new Geofence(new GeoCoordinate(anyLatitude, -10.0), new GeoCoordinate(anyLatitude, 10.0));

        // Assert
        Assert.True(anyLineFence.Contains(new GeoCoordinate(anyLatitude, 0.0)));
    }

    [Fact]
    public void A_point_strictly_inside_the_fence_is_contained() {
        // Arrange
        Geofence      fence      = Field();
        GeoCoordinate anyInsider = Any.GeoCoordinate()
                                      .WithLatitude(l => l.GreaterThan(10d).LessThan(30d))
                                      .WithLongitude(l => l.GreaterThan(20d).LessThan(40d))
                                      .Generate();

        // Act
        bool contained = fence.Contains(anyInsider);

        // Assert
        Assert.True(contained);
    }

    [Theory]
    [InlineData(10.0, 20.0)]
    [InlineData(10.0, 40.0)]
    [InlineData(30.0, 20.0)]
    [InlineData(30.0, 40.0)]
    [InlineData(10.0, 30.0)]
    [InlineData(30.0, 30.0)]
    [InlineData(20.0, 20.0)]
    [InlineData(20.0, 40.0)]
    public void The_corners_and_the_edges_of_the_fence_are_contained(double latitude, double longitude) {
        // Arrange
        Geofence fence = Field();

        // Act
        bool contained = fence.Contains(new GeoCoordinate(latitude, longitude));

        // Assert
        Assert.True(contained);
    }

    [Fact]
    public void A_point_north_of_the_fence_is_not_contained() {
        // Arrange
        Geofence      fence           = Field();
        GeoCoordinate anyNorthernPoint = Any.GeoCoordinate().WithLatitude(l => l.GreaterThan(30d)).Generate();

        // Act
        bool contained = fence.Contains(anyNorthernPoint);

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void A_point_south_of_the_fence_is_not_contained() {
        // Arrange
        Geofence      fence           = Field();
        GeoCoordinate anySouthernPoint = Any.GeoCoordinate().WithLatitude(l => l.LessThan(10d)).Generate();

        // Act
        bool contained = fence.Contains(anySouthernPoint);

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void A_point_east_of_the_fence_is_not_contained() {
        // Arrange
        Geofence      fence          = Field();
        GeoCoordinate anyEasternPoint = Any.GeoCoordinate().WithLongitude(l => l.GreaterThan(40d)).Generate();

        // Act
        bool contained = fence.Contains(anyEasternPoint);

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void A_point_west_of_the_fence_is_not_contained() {
        // Arrange
        Geofence      fence          = Field();
        GeoCoordinate anyWesternPoint = Any.GeoCoordinate().WithLongitude(l => l.LessThan(20d)).Generate();

        // Act
        bool contained = fence.Contains(anyWesternPoint);

        // Assert
        Assert.False(contained);
    }

    [Theory]
    [InlineData(9.999999999999998, 30.0)]
    [InlineData(30.000000000000004, 30.0)]
    [InlineData(20.0, 19.999999999999996)]
    [InlineData(20.0, 40.00000000000001)]
    public void A_point_one_step_outside_an_edge_is_not_contained(double latitude, double longitude) {
        // Arrange
        Geofence fence = Field();

        // Act
        bool contained = fence.Contains(new GeoCoordinate(latitude, longitude));

        // Assert
        Assert.False(contained);
    }

    [Fact]
    public void A_fence_of_the_generator_always_contains_its_own_corners() {
        // Arrange
        Geofence anyFence = Any.Geofence().Generate();

        // Act / Assert
        Assert.True(anyFence.Contains(anyFence.SouthWest));
        Assert.True(anyFence.Contains(anyFence.NorthEast));
    }

}
