using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class GeoCoordinateTests {

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-90.0, -180.0)]          // both lower bounds are inclusive
    [InlineData(90.0, -180.0)]           // latitude is closed on both ends
    [InlineData(-90.0, 179.9999999)]     // the largest longitude is just below 180
    [InlineData(48.8566, 2.3522)]
    public void A_position_inside_the_ranges_is_kept_as_given(double latitude, double longitude) {
        GeoCoordinate position = new GeoCoordinate(latitude, longitude);

        Assert.Equal(latitude, position.Latitude);
        Assert.Equal(longitude, position.Longitude);
    }

    [Theory]
    [InlineData(-90.0000001)]
    [InlineData(90.0000001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_latitude_outside_minus_90_to_90_is_refused(double latitude) {
        // Arrange
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, anyLongitude));

        // Assert
        Assert.Equal("latitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-180.0000001)]
    [InlineData(180.0)]                    // half-open: 180 itself is written -180
    [InlineData(180.0000001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void A_longitude_outside_minus_180_to_180_excluded_is_refused(double longitude) {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(anyLatitude, longitude));

        // Assert
        Assert.Equal("longitude", exception.ParamName);
    }

    [Fact]
    public void A_longitude_that_is_not_a_number_is_refused_as_an_argument_error_not_a_range_error() {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new GeoCoordinate(anyLatitude, double.NaN));

        // Assert
        Assert.Equal("longitude", exception.ParamName);
    }

    [Fact]
    public void Two_positions_with_the_same_coordinates_are_equal() {
        // Arrange
        double anyLatitude  = Any.Double().Between(-90d, 90d).Generate();
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d).Generate();

        // Act
        GeoCoordinate first  = new GeoCoordinate(anyLatitude, anyLongitude);
        GeoCoordinate second = new GeoCoordinate(anyLatitude, anyLongitude);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Positions_differing_by_latitude_only_are_not_equal() {
        // Arrange
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d).Generate();

        // Assert
        Assert.NotEqual(new GeoCoordinate(10.0, anyLongitude), new GeoCoordinate(10.5, anyLongitude));
    }

    [Fact]
    public void Positions_differing_by_longitude_only_are_not_equal() {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Assert
        Assert.NotEqual(new GeoCoordinate(anyLatitude, 20.0), new GeoCoordinate(anyLatitude, 20.5));
    }

    [Fact]
    public void A_position_is_not_equal_to_nothing_nor_to_another_type() {
        GeoCoordinate anyPosition = Any.GeoCoordinate().Generate();

        Assert.False(anyPosition.Equals(null));
        Assert.False(anyPosition.Equals("not a position"));
    }

}
