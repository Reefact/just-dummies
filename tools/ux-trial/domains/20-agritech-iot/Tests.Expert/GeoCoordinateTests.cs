using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class GeoCoordinateTests {

    [Fact]
    public void A_coordinate_keeps_the_latitude_and_longitude_it_was_given() {
        // Act
        GeoCoordinate coordinate = new GeoCoordinate(48.8566, 2.3522);

        // Assert
        Assert.Equal(48.8566, coordinate.Latitude);
        Assert.Equal(2.3522, coordinate.Longitude);
    }

    [Theory]
    [InlineData(-90.0)]
    [InlineData(90.0)]
    [InlineData(0.0)]
    public void The_poles_and_the_equator_are_valid_latitudes(double latitude) {
        // Arrange
        double anyLongitude = AnyGeoCoordinate.ValidLongitude().Generate();

        // Act
        GeoCoordinate coordinate = new GeoCoordinate(latitude, anyLongitude);

        // Assert
        Assert.Equal(latitude, coordinate.Latitude);
    }

    [Theory]
    [InlineData(-90.00000000000001)]
    [InlineData(90.00000000000001)]
    [InlineData(-91.0)]
    [InlineData(91.0)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void A_latitude_outside_minus_90_to_90_or_not_a_number_is_refused(double latitude) {
        // Arrange
        double anyLongitude = AnyGeoCoordinate.ValidLongitude().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, anyLongitude));

        // Assert
        Assert.Equal("latitude", refusal.ParamName);
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(179.99999999999997)]
    [InlineData(0.0)]
    public void Minus_180_included_and_the_value_just_below_180_are_valid_longitudes(double longitude) {
        // Arrange
        double anyLatitude = AnyGeoCoordinate.ValidLatitude().Generate();

        // Act
        GeoCoordinate coordinate = new GeoCoordinate(anyLatitude, longitude);

        // Assert
        Assert.Equal(longitude, coordinate.Longitude);
    }

    [Theory]
    [InlineData(180.0)]
    [InlineData(181.0)]
    [InlineData(-180.00000000000003)]
    [InlineData(-181.0)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void A_longitude_below_minus_180_or_from_180_up_is_refused(double longitude) {
        // Arrange
        double anyLatitude = AnyGeoCoordinate.ValidLatitude().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(anyLatitude, longitude));

        // Assert
        Assert.Equal("longitude", refusal.ParamName);
    }

    [Fact]
    public void A_longitude_that_is_not_a_number_is_refused_as_an_argument_error_not_a_range_error() {
        // Arrange
        double anyLatitude = AnyGeoCoordinate.ValidLatitude().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GeoCoordinate(anyLatitude, double.NaN));

        // Assert
        Assert.Equal("longitude", refusal.ParamName);
    }

    [Fact]
    public void An_invalid_latitude_is_reported_before_an_invalid_longitude() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(91.0, 180.0));

        // Assert
        Assert.Equal("latitude", refusal.ParamName);
    }

    [Fact]
    public void Two_coordinates_with_the_same_latitude_and_longitude_are_equal() {
        // Arrange
        double anyLatitude  = AnyGeoCoordinate.ValidLatitude().Generate();
        double anyLongitude = AnyGeoCoordinate.ValidLongitude().Generate();

        // Act
        GeoCoordinate first  = new GeoCoordinate(anyLatitude, anyLongitude);
        GeoCoordinate second = new GeoCoordinate(anyLatitude, anyLongitude);

        // Assert
        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_coordinates_that_differ_by_latitude_are_not_equal() {
        // Arrange
        GeoCoordinate anyCoordinate = Any.GeoCoordinate().Generate();
        GeoCoordinate sameLongitudeOtherLatitude = Any.GeoCoordinate()
                                                      .WithLatitude(latitude => latitude.DifferentFrom(anyCoordinate.Latitude))
                                                      .WithLongitude(anyCoordinate.Longitude)
                                                      .Generate();

        // Assert
        Assert.NotEqual(anyCoordinate, sameLongitudeOtherLatitude);
    }

    [Fact]
    public void Two_coordinates_that_differ_by_longitude_are_not_equal() {
        // Arrange
        GeoCoordinate anyCoordinate = Any.GeoCoordinate().Generate();
        GeoCoordinate sameLatitudeOtherLongitude = Any.GeoCoordinate()
                                                      .WithLatitude(anyCoordinate.Latitude)
                                                      .WithLongitude(longitude => longitude.DifferentFrom(anyCoordinate.Longitude))
                                                      .Generate();

        // Assert
        Assert.NotEqual(anyCoordinate, sameLatitudeOtherLongitude);
    }

    [Fact]
    public void A_coordinate_is_equal_to_neither_null_nor_to_something_that_is_not_a_coordinate() {
        // Arrange
        GeoCoordinate anyCoordinate = Any.GeoCoordinate().Generate();

        // Assert
        Assert.False(anyCoordinate.Equals(null));
        Assert.False(anyCoordinate.Equals((object?)null));
        Assert.False(anyCoordinate.Equals((object)"anything else"));
    }

}
