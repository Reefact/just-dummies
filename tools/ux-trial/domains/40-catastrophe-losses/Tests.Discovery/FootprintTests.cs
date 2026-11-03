using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class FootprintTests {

    [Fact]
    public void A_footprint_without_a_centre_is_refused() {
        // Arrange
        double anyRadius = Any.Double().Between(1d, 2000d).Generate();
        double anyPeak   = Any.Double().Between(0.1d, 12d).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Footprint(null!, anyRadius, anyPeak));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(2000.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_radius_outside_zero_to_two_thousand_kilometres_is_refused(double radiusKm) {
        // Arrange
        GeoPoint anyCenter = Any.GeoPoint().Generate();
        double   anyPeak   = Any.Double().Between(0.1d, 12d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCenter, radiusKm, anyPeak));

        // Assert
        Assert.Equal("radiusKm", error.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.5d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_peak_that_is_not_a_finite_positive_number_is_refused(double peakIntensity) {
        // Arrange
        GeoPoint anyCenter = Any.GeoPoint().Generate();
        double   anyRadius = Any.Double().Between(1d, 2000d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Footprint(anyCenter, anyRadius, peakIntensity));

        // Assert
        Assert.Equal("peakIntensity", error.ParamName);
    }

    [Fact]
    public void A_radius_of_exactly_two_thousand_kilometres_is_accepted() {
        // Arrange
        GeoPoint anyCenter = Any.GeoPoint().Generate();
        double   anyPeak   = Any.Double().Between(0.1d, 12d).Generate();

        // Act
        Footprint footprint = new Footprint(anyCenter, 2000d, anyPeak);

        // Assert
        Assert.Equal(2000d, footprint.RadiusKm);
    }

    [Fact]
    public void The_intensity_at_the_centre_is_the_peak() {
        // Arrange
        Footprint footprint = Samples.PeakEightAtOrigin();

        // Act
        double intensity = footprint.IntensityAt(Samples.Origin());

        // Assert
        Assert.Equal(8d, intensity);
    }

    [Fact]
    public void The_intensity_halfway_to_the_edge_is_half_the_peak() {
        // Arrange: 1 degree of longitude on the equator is about 111.195 km
        Footprint footprint = Samples.PeakEightAtOrigin(radiusKm: 222.39d);
        GeoPoint  halfway   = new GeoPoint(0d, 1d);

        // Act
        double intensity = footprint.IntensityAt(halfway);

        // Assert
        Assert.Equal(4d, intensity, 2);
    }

    [Fact]
    public void The_intensity_at_a_distance_beyond_the_radius_is_zero() {
        // Arrange
        Footprint footprint = Samples.PeakEightAtOrigin(radiusKm: 50d);
        GeoPoint  far       = new GeoPoint(0d, 1d);

        // Act
        double intensity = footprint.IntensityAt(far);

        // Assert
        Assert.Equal(0d, intensity);
    }

    [Fact]
    public void A_location_just_inside_the_radius_is_covered_and_just_outside_is_not() {
        // Arrange
        Footprint footprint = Samples.PeakEightAtOrigin(radiusKm: 111.2d);
        GeoPoint  inside    = new GeoPoint(0d, 0.99d);
        GeoPoint  outside   = new GeoPoint(0d, 1.01d);

        // Act & Assert
        Assert.True(footprint.Covers(inside));
        Assert.False(footprint.Covers(outside));
    }

    [Fact]
    public void Asking_the_intensity_at_nowhere_is_refused() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyFootprint.IntensityAt(null!));
    }

    [Fact]
    public void The_intensity_felt_never_exceeds_the_peak_for_this_location() {
        // Arrange
        Footprint footprint = Any.Footprint().Generate();
        GeoPoint  anyPlace  = Any.GeoPoint().Generate();

        // Act
        double intensity = footprint.IntensityAt(anyPlace);

        // Assert
        Assert.InRange(intensity, 0d, footprint.PeakIntensity);
    }

}
