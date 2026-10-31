namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class FootprintTests {

    [Fact]
    public void The_intensity_at_the_centre_is_the_peak() {
        // Arrange
        Footprint footprint = Arbitrary.Footprint().WithPeakIntensity(60.0).Generate();

        // Act
        double intensity = footprint.IntensityAt(footprint.Center);

        // Assert
        Assert.Equal(60.0, intensity);
    }

    [Fact]
    public void Halfway_to_the_edge_the_intensity_is_half_the_peak() {
        // Arrange
        GeoPoint center   = new GeoPoint(0.0, 0.0);
        GeoPoint location = new GeoPoint(1.0, 0.0);
        Footprint footprint = Arbitrary.Footprint()
                                       .WithCenter(center)
                                       .WithRadiusKm(2.0 * center.DistanceKmTo(location))
                                       .WithPeakIntensity(40.0)
                                       .Generate();

        // Act
        double intensity = footprint.IntensityAt(location);

        // Assert
        Assert.Equal(20.0, intensity, precision: 9);
    }

    [Fact]
    public void A_location_beyond_the_radius_feels_nothing() {
        // Arrange
        Footprint footprint = Arbitrary.Footprint()
                                       .WithCenter(new GeoPoint(14.6, -61.0))      // Martinique
                                       .WithRadiusKm(150.0)
                                       .Generate();
        GeoPoint paris = new GeoPoint(48.86, 2.35);

        // Act
        double intensity = footprint.IntensityAt(paris);

        // Assert
        Assert.Equal(0.0, intensity);
        Assert.False(footprint.Covers(paris));
    }

}
