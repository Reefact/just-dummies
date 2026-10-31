using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class HazardTests {

    // One degree of longitude along the equator, in km, on the sphere GeoPoint uses.
    private const double KmPerDegreeAtEquator = 6371.0 * Math.PI / 180.0;

    [Fact]
    public void Paris_and_London_lie_about_344_km_apart() {
        // Arrange
        GeoPoint paris  = new GeoPoint(48.8566, 2.3522);
        GeoPoint london = new GeoPoint(51.5074, -0.1278);

        // Act
        double distance = paris.DistanceKmTo(london);

        // Assert
        Assert.Equal(343.5, distance, 0.5);
    }

    [Fact]
    public void A_footprint_strikes_its_centre_at_peak_intensity() {
        // Arrange
        Footprint footprint = Any.Footprint().WithPeakIntensity(55.0).Generate();

        // Act
        double intensity = footprint.IntensityAt(footprint.Center);

        // Assert
        Assert.Equal(55.0, intensity);
    }

    [Fact]
    public void Intensity_halves_halfway_between_the_centre_and_the_edge() {
        // Arrange
        Footprint footprint = Any.Footprint()
                                 .WithCenter(new GeoPoint(0.0, 0.0))
                                 .WithRadiusKm(200.0)
                                 .WithPeakIntensity(40.0)
                                 .Generate();
        GeoPoint hundredKmEast = new GeoPoint(0.0, 100.0 / KmPerDegreeAtEquator);

        // Act
        double intensity = footprint.IntensityAt(hundredKmEast);

        // Assert
        Assert.Equal(20.0, intensity, 1e-9);
    }

    [Fact]
    public void A_location_beyond_the_radius_is_neither_covered_nor_struck() {
        // Arrange
        Footprint footprint = Any.Footprint()
                                 .WithCenter(new GeoPoint(0.0, 0.0))
                                 .WithRadiusKm(150.0)
                                 .Generate();
        GeoPoint twoHundredKmEast = new GeoPoint(0.0, 200.0 / KmPerDegreeAtEquator);

        // Act
        bool   covered   = footprint.Covers(twoHundredKmEast);
        double intensity = footprint.IntensityAt(twoHundredKmEast);

        // Assert
        Assert.False(covered);
        Assert.Equal(0.0, intensity);
    }

    [Fact]
    public void An_event_occurring_once_in_250_years_has_a_250_year_return_period() {
        // Arrange
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithAnnualRate(0.004).Generate();

        // Act
        double returnPeriod = catastrophe.ReturnPeriodYears;

        // Assert
        Assert.Equal(250.0, returnPeriod, 1e-9);
    }

    [Fact]
    public void An_event_id_without_four_digits_is_refused() {
        // Arrange
        AnyCatastropheEvent badlyIdentified = Any.CatastropheEvent().WithId("EV-42");

        // Act
        Exception? error = Record.Exception(() => badlyIdentified.Generate());

        // Assert
        Assert.IsType<FormatException>(error);
    }

}
