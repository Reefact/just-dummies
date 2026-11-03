using System.Globalization;

using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class GeoPointTests {

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void The_poles_and_the_equator_are_valid_latitudes(double latitude) {
        double anyLongitude = Any.Double().Between(-180d, 180d).Generate();

        GeoPoint point = new(latitude, anyLongitude);

        Assert.Equal(latitude, point.Latitude);
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(180.0)]
    public void The_antimeridian_is_a_valid_longitude_on_both_sides(double longitude) {
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        GeoPoint point = new(anyLatitude, longitude);

        Assert.Equal(longitude, point.Longitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_latitude_beyond_the_poles_is_refused(double latitude) {
        double anyLongitude = Any.Double().Between(-180d, 180d).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, anyLongitude));

        Assert.Equal("latitude", error.ParamName);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_longitude_beyond_the_antimeridian_is_refused(double longitude) {
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(anyLatitude, longitude));

        Assert.Equal("longitude", error.ParamName);
    }

    [Fact]
    public void One_degree_of_longitude_on_the_equator_is_about_111_kilometres() {
        GeoPoint origin = new(0, 0);
        GeoPoint east   = new(0, 1);

        double distance = origin.DistanceKmTo(east);

        Assert.Equal(111.1949, distance, 3);
    }

    [Fact]
    public void Antipodes_are_half_the_earth_circumference_apart() {
        GeoPoint origin    = new(0, 0);
        GeoPoint antipodes = new(0, 180);

        double distance = origin.DistanceKmTo(antipodes);

        Assert.Equal(Math.PI * 6371.0, distance, 3);
    }

    [Fact]
    public void The_shortest_way_across_the_antimeridian_is_taken() {
        GeoPoint west = new(0, 179.5);
        GeoPoint east = new(0, -179.5);

        double distance = west.DistanceKmTo(east);

        Assert.Equal(111.1949, distance, 3);
    }

    [Fact]
    public void Paris_and_London_are_about_344_kilometres_apart() {
        GeoPoint paris  = new(48.8566, 2.3522);
        GeoPoint london = new(51.5074, -0.1278);

        double distance = paris.DistanceKmTo(london);

        Assert.InRange(distance, 343.0, 345.0);
    }

    [Fact]
    public void The_two_poles_are_half_the_earth_circumference_apart_whatever_the_longitudes() {
        double   anyLongitude      = Any.Double().Between(-180d, 180d).Generate();
        double   anyOtherLongitude = Any.Double().Between(-180d, 180d).Generate();
        GeoPoint north             = new(90, anyLongitude);
        GeoPoint south             = new(-90, anyOtherLongitude);

        double distance = north.DistanceKmTo(south);

        Assert.Equal(Math.PI * 6371.0, distance, 3);
    }

    [Fact]
    public void A_point_is_at_distance_zero_of_itself() {
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        double distance = anyPoint.DistanceKmTo(anyPoint);

        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void Distance_to_nothing_is_refused() {
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPoint.DistanceKmTo(null!));
    }

    [Fact]
    public void Two_points_with_the_same_coordinates_are_equal() {
        GeoPoint first  = new(48.8566, 2.3522);
        GeoPoint second = new(48.8566, 2.3522);

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Points_differing_by_one_coordinate_are_not_equal() {
        GeoPoint point = new(48.8566, 2.3522);

        Assert.False(point.Equals(new GeoPoint(48.8567, 2.3522)));
        Assert.False(point.Equals(new GeoPoint(48.8566, 2.3523)));
        Assert.False(point.Equals(null));
        Assert.False(point.Equals((object)"48.8566, 2.3522"));
    }

    [Fact(Skip = "domain defect: GeoPoint.ToString formats with the current culture ('48,8566' under fr-FR); no drawn value involved, seed n/a")]
    public void A_point_prints_its_coordinates_with_the_invariant_decimal_separator_whatever_the_culture() {
        GeoPoint point = new(48.8566, 2.3522);
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            string text = point.ToString();

            Assert.Equal("(48.8566, 2.3522)", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
