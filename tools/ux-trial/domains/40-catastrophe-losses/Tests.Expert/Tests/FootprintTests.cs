using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class FootprintTests {

    private static readonly GeoPoint Equator0 = new(0.0, 0.0);
    private static readonly GeoPoint Equator1 = new(0.0, 1.0); // about 111 km east of Equator0

    [Fact]
    public void The_peak_intensity_is_felt_at_the_centre() {
        // Arrange
        GeoPoint  anyCentre    = Any.GeoPoint().Generate();
        Footprint anyFootprint = Any.Footprint().WithCenter(anyCentre).WithPeakIntensity(60.0).Generate();

        // Act
        double intensity = anyFootprint.IntensityAt(anyCentre);

        // Assert
        Assert.Equal(60.0, intensity);
    }

    [Fact]
    public void Intensity_decays_linearly_to_half_the_peak_half_way_to_the_edge() {
        // Arrange
        double    halfRadiusKm = Equator0.DistanceKmTo(Equator1);
        Footprint footprint    = new(Equator0, radiusKm: 2 * halfRadiusKm, peakIntensity: 80.0);

        // Act
        double intensity = footprint.IntensityAt(Equator1);

        // Assert
        Assert.Equal(40.0, intensity, precision: 9);
    }

    [Fact]
    public void A_location_beyond_the_radius_feels_nothing_and_is_not_covered() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().WithCenter(Equator0).WithRadiusKm(100.0).Generate();

        // Act
        double intensity = anyFootprint.IntensityAt(Equator1);
        bool   covered   = anyFootprint.Covers(Equator1);

        // Assert
        Assert.Equal(0.0, intensity);
        Assert.False(covered);
    }

}
