using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

public sealed class SurfaceAndDiagnosisTests {

    [Fact]
    public void A_lot_just_under_eight_square_metres_is_not_counted_under_the_Carrez_law() {
        // Arrange
        Surface justUnderEight = new Surface(7.99);

        // Act
        bool counted = justUnderEight.CountsUnderCarrezLaw;

        // Assert
        Assert.False(counted);
    }

    [Fact]
    public void Adding_surfaces_stays_on_the_hundredth() {
        // Arrange
        Surface tenCentiares   = new Surface(0.10);
        Surface twentyCentiares = new Surface(0.20);

        // Act
        Surface total = tenCentiares.Add(twentyCentiares);

        // Assert
        Assert.Equal(0.30, total.SquareMeters);
    }

    [Fact]
    public void The_rating_is_the_worse_of_the_energy_and_the_emissions_bands() {
        // Act: 100 kWh is band B, 55 kg CO2 is band E.
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(100, 55);

        // Assert
        Assert.Equal(EnergyRating.E, rating);
    }

    [Fact]
    public void A_diagnosis_is_valid_ten_years_to_the_day() {
        // Arrange
        DateOnly                   issuedOn     = new DateOnly(2022, 3, 15);
        EnergyPerformanceDiagnosis anyDiagnosis = Arbitrary.EnergyPerformanceDiagnosis().WithIssuedOn(issuedOn).Generate();

        // Act
        bool validOnLastDay  = anyDiagnosis.IsValidOn(new DateOnly(2032, 3, 14));
        bool validOnNextDay  = anyDiagnosis.IsValidOn(new DateOnly(2032, 3, 15));

        // Assert
        Assert.True(validOnLastDay);
        Assert.False(validOnNextDay);
    }

    [Fact]
    public void An_F_rated_home_is_an_energy_sieve() {
        // Arrange
        EnergyPerformanceDiagnosis anyFDiagnosis = Arbitrary.EnergyPerformanceDiagnosis().WithDeclaredRating(EnergyRating.F).Generate();

        // Act
        bool sieve = anyFDiagnosis.IsEnergySieve;

        // Assert
        Assert.True(sieve);
    }
}
