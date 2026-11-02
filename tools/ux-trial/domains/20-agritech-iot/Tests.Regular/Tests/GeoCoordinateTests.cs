using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class GeoCoordinateTests {

    [Fact]
    public void A_position_inside_the_valid_ranges_keeps_its_coordinates() {
        // Arrange
        double anyLatitude  = Any.Double().Between(-90d, 90d).Generate();
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d).Generate();

        // Act
        GeoCoordinate anyPosition = new GeoCoordinate(anyLatitude, anyLongitude);

        // Assert
        Assert.Equal(anyLatitude, anyPosition.Latitude);
        Assert.Equal(anyLongitude, anyPosition.Longitude);
    }

    [Theory]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, -180.0)]
    [InlineData(-90.0, 179.99999999999997)]
    [InlineData(90.0, 179.99999999999997)]
    [InlineData(0.0, 0.0)]
    public void Both_ends_of_latitude_and_the_lower_end_of_longitude_are_accepted(double latitude, double longitude) {
        // Act
        GeoCoordinate position = new GeoCoordinate(latitude, longitude);

        // Assert
        Assert.Equal(latitude, position.Latitude);
        Assert.Equal(longitude, position.Longitude);
    }

    [Fact]
    public void A_latitude_above_90_is_refused() {
        // Arrange
        double anyLatitude = Any.Double().GreaterThan(90d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(anyLatitude, 0.0));

        // Assert
        Assert.Equal("latitude", exception.ParamName);
    }

    [Fact]
    public void A_latitude_below_minus_90_is_refused() {
        // Arrange
        double anyLatitude = Any.Double().LessThan(-90d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(anyLatitude, 0.0));

        // Assert
        Assert.Equal("latitude", exception.ParamName);
    }

    [Fact]
    public void A_latitude_just_beyond_either_pole_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(double.BitIncrement(90.0), 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(double.BitDecrement(-90.0), 0.0));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void A_latitude_that_is_not_a_finite_number_is_refused(double latitude) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, 0.0));

        // Assert
        Assert.Equal("latitude", exception.ParamName);
    }

    [Fact]
    public void A_longitude_of_180_or_more_is_refused() {
        // Arrange
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(180d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0.0, anyLongitude));

        // Assert
        Assert.Equal("longitude", exception.ParamName);
    }

    [Fact]
    public void A_longitude_of_exactly_180_is_refused_because_the_range_is_half_open() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0.0, 180.0));
    }

    [Fact]
    public void A_longitude_below_minus_180_is_refused() {
        // Arrange
        double anyLongitude = Any.Double().LessThan(-180d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0.0, anyLongitude));

        // Assert
        Assert.Equal("longitude", exception.ParamName);
    }

    [Fact]
    public void A_longitude_just_below_minus_180_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0.0, double.BitDecrement(-180.0)));
    }

    [Fact]
    public void A_longitude_that_is_not_a_number_is_refused_with_an_argument_exception() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new GeoCoordinate(0.0, double.NaN));

        // Assert
        Assert.Equal("longitude", exception.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinite_longitude_is_refused(double longitude) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0.0, longitude));
    }

    [Fact]
    public void Two_positions_with_the_same_coordinates_are_equal_and_hash_alike() {
        // Arrange
        GeoCoordinate anyPosition = Any.GeoCoordinate().Generate();
        GeoCoordinate anySame     = new GeoCoordinate(anyPosition.Latitude, anyPosition.Longitude);

        // Act / Assert
        Assert.True(anyPosition.Equals(anySame));
        Assert.True(anyPosition.Equals((object)anySame));
        Assert.Equal(anyPosition.GetHashCode(), anySame.GetHashCode());
    }

    [Fact]
    public void Positions_differing_by_latitude_only_are_not_equal() {
        // Arrange
        GeoCoordinate anyPosition  = Any.GeoCoordinate().WithLatitude(l => l.Between(-89d, 89d)).Generate();
        GeoCoordinate anyNeighbour = new GeoCoordinate(anyPosition.Latitude + 1.0, anyPosition.Longitude);

        // Act / Assert
        Assert.False(anyPosition.Equals(anyNeighbour));
    }

    [Fact]
    public void Positions_differing_by_longitude_only_are_not_equal() {
        // Arrange
        GeoCoordinate anyPosition  = Any.GeoCoordinate().WithLongitude(l => l.Between(-179d, 179d)).Generate();
        GeoCoordinate anyNeighbour = new GeoCoordinate(anyPosition.Latitude, anyPosition.Longitude + 1.0);

        // Act / Assert
        Assert.False(anyPosition.Equals(anyNeighbour));
    }

    [Fact]
    public void A_position_is_not_equal_to_null_nor_to_another_type() {
        // Arrange
        GeoCoordinate anyPosition = Any.GeoCoordinate().Generate();

        // Act / Assert
        Assert.False(anyPosition.Equals((GeoCoordinate?)null));
        Assert.False(anyPosition.Equals((object?)null));
        Assert.False(anyPosition.Equals(anyPosition.ToString()));
    }

}
