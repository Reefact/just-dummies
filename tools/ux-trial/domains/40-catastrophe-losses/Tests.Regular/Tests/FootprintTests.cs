using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class FootprintTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(2000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_radius_outside_zero_to_two_thousand_kilometres_is_refused(double radiusKm) {
        GeoPoint anyCenter        = Any.GeoPoint().Generate();
        double   anyPeakIntensity = Any.Double().Positive().LessThanOrEqualTo(1e6).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCenter, radiusKm, anyPeakIntensity));

        Assert.Equal("radiusKm", error.ParamName);
    }

    [Theory]
    [InlineData(0.000001)]
    [InlineData(2000.0)]
    public void The_extreme_valid_radii_are_accepted(double radiusKm) {
        GeoPoint anyCenter        = Any.GeoPoint().Generate();
        double   anyPeakIntensity = Any.Double().Positive().LessThanOrEqualTo(1e6).Generate();

        Footprint footprint = new(anyCenter, radiusKm, anyPeakIntensity);

        Assert.Equal(radiusKm, footprint.RadiusKm);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_peak_intensity_that_is_not_a_finite_positive_number_is_refused(double peakIntensity) {
        GeoPoint anyCenter = Any.GeoPoint().Generate();
        double   anyRadius = Any.Double().Positive().LessThanOrEqualTo(Footprint.MaxRadiusKm).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCenter, anyRadius, peakIntensity));

        Assert.Equal("peakIntensity", error.ParamName);
    }

    [Fact]
    public void A_footprint_without_a_center_is_refused() {
        double anyRadius        = Any.Double().Positive().LessThanOrEqualTo(Footprint.MaxRadiusKm).Generate();
        double anyPeakIntensity = Any.Double().Positive().LessThanOrEqualTo(1e6).Generate();

        Assert.Throws<ArgumentNullException>(() => new Footprint(null!, anyRadius, anyPeakIntensity));
    }

    [Fact]
    public void Intensity_is_the_peak_at_the_center() {
        Footprint anyFootprint = Any.Footprint().WithPeakIntensity(8.5).Generate();

        double intensity = anyFootprint.IntensityAt(anyFootprint.Center);

        Assert.Equal(8.5, intensity);
    }

    [Fact]
    public void Intensity_decays_linearly_to_half_the_peak_at_half_the_radius() {
        GeoPoint  center    = new(0, 0);
        GeoPoint  halfway   = new(0, 1);
        double    radiusKm  = 2 * center.DistanceKmTo(halfway);
        Footprint footprint = new(center, radiusKm, 10.0);

        double intensity = footprint.IntensityAt(halfway);

        Assert.Equal(5.0, intensity, 9);
    }

    [Fact]
    public void Intensity_has_dropped_to_zero_at_the_edge_of_the_radius_which_still_counts_as_covered() {
        GeoPoint  center    = new(0, 0);
        GeoPoint  edge      = new(0, 1);
        Footprint footprint = new(center, center.DistanceKmTo(edge), 10.0);

        double intensity = footprint.IntensityAt(edge);

        Assert.Equal(0.0, intensity);
        Assert.True(footprint.Covers(edge));
    }

    [Fact]
    public void A_location_beyond_the_radius_is_neither_covered_nor_felt() {
        GeoPoint  center    = new(0, 0);
        GeoPoint  outside   = new(0, 1.01);
        Footprint footprint = new(center, center.DistanceKmTo(new GeoPoint(0, 1)), 10.0);

        double intensity = footprint.IntensityAt(outside);

        Assert.Equal(0.0, intensity);
        Assert.False(footprint.Covers(outside));
    }

    [Fact]
    public void A_missing_location_is_refused() {
        Footprint anyFootprint = Any.Footprint().Generate();

        Assert.Throws<ArgumentNullException>(() => anyFootprint.IntensityAt(null!));
        Assert.Throws<ArgumentNullException>(() => anyFootprint.Covers(null!));
    }

}
