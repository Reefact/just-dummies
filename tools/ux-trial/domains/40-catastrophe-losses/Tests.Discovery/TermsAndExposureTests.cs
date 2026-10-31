using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class TermsAndExposureTests {

    [Fact]
    public void A_ground_up_loss_within_the_deductible_costs_the_insurer_nothing() {
        // Arrange
        PolicyTerms terms = Any.PolicyTerms().WithDeductible(1_000m).Generate();

        // Act
        decimal insured = terms.Apply(800m);

        // Assert
        Assert.Equal(0m, insured);
    }

    [Fact]
    public void The_insurer_pays_the_ground_up_loss_beyond_the_deductible() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(deductible: 1_000m, limit: 50_000m);

        // Act
        decimal insured = terms.Apply(11_000m);

        // Assert
        Assert.Equal(10_000m, insured);
    }

    [Fact]
    public void The_insurer_never_pays_more_than_the_limit() {
        // Arrange
        PolicyTerms terms = new PolicyTerms(deductible: 1_000m, limit: 50_000m);

        // Act
        decimal insured = terms.Apply(200_000m);

        // Assert
        Assert.Equal(50_000m, insured);
    }

    [Fact]
    public void A_deductible_as_large_as_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms    = Any.PolicyTerms().WithDeductible(100_000m).WithLimit(l => l.LessThanOrEqualTo(100_000m)).Generate();

        // Act
        Exception? error = Record.Exception(() => new Exposure(property, terms));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused_as_over_insurance() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms    = Any.PolicyTerms().WithDeductible(0m).WithLimit(100_000.01m).Generate();

        // Act
        Exception? error = Record.Exception(() => new Exposure(property, terms));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void An_exposure_outside_the_footprint_suffers_no_insured_loss() {
        // Arrange
        Footprint footprint = Any.Footprint()
                                 .WithCenter(new GeoPoint(0.0, 0.0))
                                 .WithRadiusKm(500.0)
                                 .Generate();
        Exposure anyFarAwayExposure = Any.Exposure()
                                         .WithProperty(p => p.WithLocation(new GeoPoint(60.0, 100.0)))
                                         .Generate();
        double anySeverity = Any.Double().Positive().Generate();

        // Act
        decimal insured = anyFarAwayExposure.InsuredLoss(footprint, anySeverity);

        // Assert
        Assert.Equal(0m, insured);
    }

    [Fact]
    public void Severity_scales_the_hazard_before_the_curve_and_the_terms_apply() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().WithPeakIntensity(40.0).Generate();
        Exposure exposure = Any.Exposure()
                               .WithProperty(p => p.WithLocation(anyFootprint.Center)
                                                   .WithInsuredValue(500_000m)
                                                   .WithVulnerability(Curves.Linear()))
                               .WithTerms(new PolicyTerms(deductible: 10_000m, limit: 250_000m))
                               .Generate();

        // Act
        decimal insured = exposure.InsuredLoss(anyFootprint, severityFactor: 1.5);

        // Assert
        // intensity 40 x 1.5 = 60 -> 60 % of 500 000 = 300 000 -> minus 10 000, capped at 250 000
        Assert.Equal(250_000m, insured);
    }

}
