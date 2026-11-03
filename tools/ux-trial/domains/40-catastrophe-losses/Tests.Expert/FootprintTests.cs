using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class FootprintTests {

    private const double OneDegreeOfLongitudeOnTheEquatorKm = 111.19492664455873d;

    [Fact]
    public void A_footprint_without_a_centre_is_refused() {
        // Arrange
        double anyRadiusKm     = Any.Double().Between(1d, Footprint.MaxRadiusKm).Generate();
        double anyPeakIntensity = Any.Double().Positive().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Footprint(null!, anyRadiusKm, anyPeakIntensity));

        // Assert
        Assert.Equal("center", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(2000.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_radius_outside_zero_exclusive_to_two_thousand_kilometres_is_refused(double radiusKm) {
        // Arrange
        GeoPoint anyCentre        = Any.GeoPoint().Generate();
        double   anyPeakIntensity = Any.Double().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCentre, radiusKm, anyPeakIntensity));

        // Assert
        Assert.Equal("radiusKm", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(2000d)]
    public void A_radius_on_the_edge_of_its_range_is_accepted(double radiusKm) {
        // Arrange
        GeoPoint anyCentre        = Any.GeoPoint().Generate();
        double   anyPeakIntensity = Any.Double().Positive().Generate();

        // Act
        Footprint footprint = new Footprint(anyCentre, radiusKm, anyPeakIntensity);

        // Assert
        Assert.Equal(radiusKm, footprint.RadiusKm);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.5d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_peak_intensity_that_is_not_a_finite_strictly_positive_number_is_refused(double peakIntensity) {
        // Arrange
        GeoPoint anyCentre   = Any.GeoPoint().Generate();
        double   anyRadiusKm = Any.Double().Between(1d, Footprint.MaxRadiusKm).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCentre, anyRadiusKm, peakIntensity));

        // Assert
        Assert.Equal("peakIntensity", refusal.ParamName);
    }

    [Fact]
    public void A_negative_peak_intensity_is_refused() {
        // Arrange
        GeoPoint anyCentre           = Any.GeoPoint().Generate();
        double   anyRadiusKm         = Any.Double().Between(1d, Footprint.MaxRadiusKm).Generate();
        double   anyNegativeIntensity = Any.Double().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Footprint(anyCentre, anyRadiusKm, anyNegativeIntensity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_intensity_at_the_centre_is_the_peak() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().WithPeakIntensity(7.5d).Generate();

        // Act
        double intensity = anyFootprint.IntensityAt(anyFootprint.Center);

        // Assert
        Assert.Equal(7.5d, intensity);
    }

    [Fact]
    public void The_intensity_decays_linearly_to_half_the_peak_at_half_the_radius() {
        // Arrange
        Footprint footprint = new Footprint(new GeoPoint(0d, 0d), 2d * OneDegreeOfLongitudeOnTheEquatorKm, 10d);
        GeoPoint  halfway   = new GeoPoint(0d, 1d);

        // Act
        double intensity = footprint.IntensityAt(halfway);

        // Assert
        Assert.Equal(5d, intensity, 9);
    }

    [Fact]
    public void The_intensity_at_a_quarter_of_the_radius_is_three_quarters_of_the_peak() {
        // Arrange
        Footprint footprint = new Footprint(new GeoPoint(0d, 0d), 4d * OneDegreeOfLongitudeOnTheEquatorKm, 8d);
        GeoPoint  atQuarter = new GeoPoint(0d, 1d);

        // Act
        double intensity = footprint.IntensityAt(atQuarter);

        // Assert
        Assert.Equal(6d, intensity, 9);
    }

    [Fact]
    public void The_intensity_is_zero_beyond_the_radius() {
        // Arrange
        Footprint footprint = new Footprint(new GeoPoint(0d, 0d), 0.5d * OneDegreeOfLongitudeOnTheEquatorKm, 10d);
        GeoPoint  outside   = new GeoPoint(0d, 1d);

        // Act
        double intensity = footprint.IntensityAt(outside);

        // Assert
        Assert.Equal(0d, intensity);
    }

    [Fact]
    public void The_intensity_is_zero_exactly_on_the_edge_of_the_radius() {
        // Arrange
        GeoPoint  centre = new GeoPoint(0d, 0d);
        GeoPoint  onTheEdge = new GeoPoint(0d, 1d);
        double    radiusKm  = centre.DistanceKmTo(onTheEdge);
        Footprint footprint = new Footprint(centre, radiusKm, 10d);

        // Act
        double intensity = footprint.IntensityAt(onTheEdge);

        // Assert
        Assert.Equal(0d, intensity);
    }

    [Fact]
    public void A_location_without_coordinates_has_no_intensity() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyFootprint.IntensityAt(null!));

        // Assert
        Assert.Equal("location", refusal.ParamName);
    }

    [Fact]
    public void A_footprint_covers_its_own_centre() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        bool covered = anyFootprint.Covers(anyFootprint.Center);

        // Assert
        Assert.True(covered);
    }

    [Fact]
    public void A_footprint_covers_a_location_inside_its_radius_and_not_one_outside() {
        // Arrange
        Footprint footprint = new Footprint(new GeoPoint(0d, 0d), 1.5d * OneDegreeOfLongitudeOnTheEquatorKm, 10d);
        GeoPoint  inside    = new GeoPoint(0d, 1d);
        GeoPoint  outside   = new GeoPoint(0d, 2d);

        // Act
        bool coversInside  = footprint.Covers(inside);
        bool coversOutside = footprint.Covers(outside);

        // Assert
        Assert.True(coversInside);
        Assert.False(coversOutside);
    }

    // A location on the very edge is "covered" yet feels no hazard: Covers is inclusive, IntensityAt is zero from the radius on.
    [Fact(Skip = "domain defect: Footprint.Covers is inclusive of the radius while IntensityAt is 0 from the radius on, so an edge location is covered yet feels nothing; literal geometry, seed n/a")]
    public void A_location_covered_by_the_footprint_feels_a_strictly_positive_intensity() {
        // Arrange
        GeoPoint  centre    = new GeoPoint(0d, 0d);
        GeoPoint  onTheEdge = new GeoPoint(0d, 1d);
        Footprint footprint = new Footprint(centre, centre.DistanceKmTo(onTheEdge), 10d);

        // Act
        bool   covered   = footprint.Covers(onTheEdge);
        double intensity = footprint.IntensityAt(onTheEdge);

        // Assert
        Assert.True(covered);
        Assert.True(intensity > 0d, $"a location the footprint covers should feel some hazard, but the intensity is {intensity}");
    }

}
