using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Recipes;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class ExposureTests {

    [Fact]
    public void A_deductible_reaching_the_insured_value_is_refused_since_it_could_never_pay() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new(deductible: 100_000m, limit: 50_000m);

        // Act
        Exception? refusal = Record.Exception(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("could never pay", refusal.Message);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused_as_over_insurance() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new(deductible: 0m, limit: 100_000.01m);

        // Act
        Exception? refusal = Record.Exception(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("over-insurance", refusal.Message);
    }

    [Theory]
    [InlineData(1.0, 90_000)]  // intensity 50 -> 50 % of 200 000, less the 10 000 deductible
    [InlineData(1.6, 150_000)] // intensity 80 -> 160 000 ground-up, capped at the 150 000 limit
    public void The_insured_loss_runs_the_scaled_intensity_through_the_curve_then_the_terms(double severityFactor, int expectedLoss) {
        // Arrange
        Footprint anyFootprint = Any.Footprint().WithPeakIntensity(50.0).Generate();
        Exposure anyExposure = Any.Exposure()
                                  .WithProperty(p => p.WithLocation(anyFootprint.Center)
                                                      .WithInsuredValue(200_000m)
                                                      .WithVulnerability(Scenarios.LinearCurve))
                                  .WithTerms(new PolicyTerms(deductible: 10_000m, limit: 150_000m))
                                  .Generate();

        // Act
        decimal loss = anyExposure.InsuredLoss(anyFootprint, severityFactor);

        // Assert
        Assert.Equal(expectedLoss, loss);
    }

    [Fact]
    public void A_property_outside_the_footprint_loses_nothing_however_severe_the_occurrence() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().WithCenter(new GeoPoint(0.0, 0.0)).WithRadiusKm(100.0).Generate();
        Exposure  anyExposure  = Any.Exposure().WithProperty(p => p.WithLocation(new GeoPoint(10.0, 10.0))).Generate();
        double    anySeverity  = Any.Double().Between(0.0, 10.0).Generate();

        // Act
        decimal loss = anyExposure.InsuredLoss(anyFootprint, anySeverity);

        // Assert
        Assert.Equal(0m, loss);
    }

}
