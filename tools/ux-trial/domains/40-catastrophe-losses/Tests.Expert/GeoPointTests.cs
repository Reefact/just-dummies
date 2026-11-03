using System.Globalization;

using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class GeoPointTests {

    [Theory]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, 180.0)]
    [InlineData(0.0, 0.0)]
    public void A_point_on_the_edge_of_the_coordinate_ranges_is_accepted(double latitude, double longitude) {
        // Act
        GeoPoint point = new GeoPoint(latitude, longitude);

        // Assert
        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
    }

    [Theory]
    [InlineData(90.0001)]
    [InlineData(-90.0001)]
    [InlineData(double.NaN)]
    public void A_latitude_just_beyond_a_pole_or_not_a_number_is_refused(double latitude) {
        // Arrange
        double anyLongitude = Any.Double().Between(-180d, 180d).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, anyLongitude));

        // Assert
        Assert.Equal("latitude", refusal.ParamName);
    }

    [Theory]
    [InlineData(180.0001)]
    [InlineData(-180.0001)]
    [InlineData(double.NaN)]
    public void A_longitude_just_beyond_the_antimeridian_or_not_a_number_is_refused(double longitude) {
        // Arrange
        double anyLatitude = Any.Double().Between(-90d, 90d).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(anyLatitude, longitude));

        // Assert
        Assert.Equal("longitude", refusal.ParamName);
    }

    [Fact]
    public void A_latitude_far_beyond_the_poles_is_refused() {
        // Arrange
        double anyLatitudeBeyondNorth = Any.Double().GreaterThan(90d).Generate();
        double anyLatitudeBeyondSouth = Any.Double().LessThan(-90d).Generate();
        double anyLongitude           = Any.Double().Between(-180d, 180d).Generate();

        // Act
        Exception? northRefusal = Record.Exception(() => new GeoPoint(anyLatitudeBeyondNorth, anyLongitude));
        Exception? southRefusal = Record.Exception(() => new GeoPoint(anyLatitudeBeyondSouth, anyLongitude));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(northRefusal);
        Assert.IsType<ArgumentOutOfRangeException>(southRefusal);
    }

    [Fact]
    public void A_longitude_far_beyond_the_antimeridian_is_refused() {
        // Arrange
        double anyLatitude           = Any.Double().Between(-90d, 90d).Generate();
        double anyLongitudeBeyondEast = Any.Double().GreaterThan(180d).Generate();
        double anyLongitudeBeyondWest = Any.Double().LessThan(-180d).Generate();

        // Act
        Exception? eastRefusal = Record.Exception(() => new GeoPoint(anyLatitude, anyLongitudeBeyondEast));
        Exception? westRefusal = Record.Exception(() => new GeoPoint(anyLatitude, anyLongitudeBeyondWest));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(eastRefusal);
        Assert.IsType<ArgumentOutOfRangeException>(westRefusal);
    }

    [Fact]
    public void One_degree_of_longitude_on_the_equator_is_about_111_kilometres() {
        // Arrange
        GeoPoint greenwich = new GeoPoint(0d, 0d);
        GeoPoint oneDegreeEast = new GeoPoint(0d, 1d);

        // Act
        double distanceKm = greenwich.DistanceKmTo(oneDegreeEast);

        // Assert
        Assert.Equal(111.1949, distanceKm, 3);
    }

    [Fact]
    public void Paris_and_London_are_about_344_kilometres_apart() {
        // Arrange
        GeoPoint paris  = new GeoPoint(48.8566d, 2.3522d);
        GeoPoint london = new GeoPoint(51.5074d, -0.1278d);

        // Act
        double distanceKm = paris.DistanceKmTo(london);

        // Assert
        Assert.Equal(343.556, distanceKm, 2);
    }

    [Fact]
    public void A_point_is_at_distance_zero_from_itself() {
        // Arrange
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        // Act
        double distanceKm = anyPoint.DistanceKmTo(anyPoint);

        // Assert
        Assert.Equal(0d, distanceKm);
    }

    [Fact]
    public void The_distance_does_not_depend_on_the_direction_it_is_measured_in() {
        // Arrange
        GeoPoint anyDeparture   = Any.GeoPoint().Generate();
        GeoPoint anyDestination = Any.GeoPoint().Generate();

        // Act
        double outward = anyDeparture.DistanceKmTo(anyDestination);
        double back    = anyDestination.DistanceKmTo(anyDeparture);

        // Assert
        Assert.Equal(outward, back, 6);
    }

    [Theory]
    [InlineData(0d, 0d, 0d, 180d)]
    [InlineData(90d, 0d, -90d, 0d)]
    [InlineData(45d, 10d, -45d, -170d)]
    public void Two_antipodal_points_are_half_the_earth_circumference_apart(double latitude, double longitude, double antipodeLatitude, double antipodeLongitude) {
        // Arrange
        GeoPoint point    = new GeoPoint(latitude, longitude);
        GeoPoint antipode = new GeoPoint(antipodeLatitude, antipodeLongitude);

        // Act
        double distanceKm = point.DistanceKmTo(antipode);

        // Assert
        Assert.Equal(20015.09, distanceKm, 1);
    }

    [Fact(Skip = "domain defect: DistanceKmTo returns NaN for some exactly-antipodal points (haversine 'a' rounds above 1, Math.Sqrt(1 - a) is NaN), e.g. (77.35488929196956, -65.17134279253489) and its antipode; literal coordinates, seed n/a")]
    public void The_distance_between_antipodal_points_is_a_number() {
        // Arrange
        GeoPoint point    = new GeoPoint(77.35488929196956d, -65.17134279253489d);
        GeoPoint antipode = new GeoPoint(-77.35488929196956d, 114.82865720746511d);

        // Act
        double distanceKm = point.DistanceKmTo(antipode);

        // Assert
        Assert.False(double.IsNaN(distanceKm), "an antipodal pair is 20 015 km apart, not not-a-number");
    }

    [Fact]
    public void A_distance_to_nothing_is_refused() {
        // Arrange
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPoint.DistanceKmTo(null!));

        // Assert
        Assert.Equal("other", refusal.ParamName);
    }

    [Fact]
    public void Two_points_with_the_same_coordinates_are_equal() {
        // Arrange
        double   anyLatitude  = Any.Double().Between(-90d, 90d).Generate();
        double   anyLongitude = Any.Double().Between(-180d, 180d).Generate();
        GeoPoint original     = new GeoPoint(anyLatitude, anyLongitude);
        GeoPoint twin         = new GeoPoint(anyLatitude, anyLongitude);

        // Act
        bool equal = original.Equals(twin);

        // Assert
        Assert.True(equal);
        Assert.Equal(original.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void Two_points_differing_by_their_longitude_are_not_equal() {
        // Arrange
        GeoPoint anyPoint        = Any.GeoPoint().Generate();
        double   anyOtherLongitude = Any.Double().Between(-180d, 180d).DifferentFrom(anyPoint.Longitude).Generate();
        GeoPoint other           = new GeoPoint(anyPoint.Latitude, anyOtherLongitude);

        // Act
        bool equal = anyPoint.Equals(other);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_point_is_not_equal_to_nothing() {
        // Arrange
        GeoPoint anyPoint = Any.GeoPoint().Generate();

        // Act
        bool equal = anyPoint.Equals(null);

        // Assert
        Assert.False(equal);
    }

    [Fact(Skip = "domain defect: GeoPoint.ToString formats with the current culture, so under fr-FR a point reads '(48,8566, 2,3522)', ambiguous next to the ', ' separator; literal coordinates, seed n/a")]
    public void A_point_is_written_the_same_way_whatever_the_culture_of_the_machine() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try {
            GeoPoint point = new GeoPoint(48.8566d, 2.3522d);

            // Act
            string text = point.ToString();

            // Assert
            Assert.Equal("(48.8566, 2.3522)", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_point_is_written_with_at_most_four_decimals() {
        // Arrange
        GeoPoint point = new GeoPoint(48.856614d, -2.352222d);

        // Act
        string text = point.ToString();

        // Assert
        Assert.Equal("(48.8566, -2.3522)", text);
    }

}
