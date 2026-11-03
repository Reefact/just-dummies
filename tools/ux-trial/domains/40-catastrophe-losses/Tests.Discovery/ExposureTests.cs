using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class ExposureTests {

    [Fact]
    public void A_deductible_equal_to_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms    = new PolicyTerms(500_000m, 100m);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Exposure(property, terms));

        // Assert
        Assert.Equal("terms", error.ParamName);
    }

    [Fact]
    public void A_deductible_just_below_the_insured_value_is_accepted() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms    = new PolicyTerms(499_999.99m, 100m);

        // Act
        Exposure exposure = new Exposure(property, terms);

        // Assert
        Assert.Same(terms, exposure.Terms);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms    = new PolicyTerms(0m, 500_000.01m);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Exposure(property, terms));

        // Assert
        Assert.Equal("terms", error.ParamName);
    }

    [Fact]
    public void A_limit_equal_to_the_insured_value_is_accepted() {
        // Arrange
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms    = new PolicyTerms(0m, 500_000m);

        // Act
        Exposure exposure = new Exposure(property, terms);

        // Assert
        Assert.Same(property, exposure.Property);
    }

    [Fact]
    public void An_exposure_without_property_or_terms_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().Generate();
        PolicyTerms     anyTerms    = new PolicyTerms(0m, 1m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Exposure(null!, anyTerms));
        Assert.Throws<ArgumentNullException>(() => new Exposure(anyProperty, null!));
    }

    [Fact]
    public void The_insured_loss_goes_through_the_curve_then_the_terms() {
        // Arrange: intensity 8 -> ratio 0.375 -> ground-up 375 000 -> minus 50 000 deductible -> 325 000 -> capped at 200 000
        Exposure  exposure  = Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, deductible: 50_000m, limit: 200_000m);
        Footprint footprint = Samples.PeakEightAtOrigin();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(200_000m, loss);
    }

    [Fact]
    public void The_severity_factor_scales_the_intensity_before_the_curve() {
        // Arrange: intensity 4 -> ratio 0.125 -> ground-up 125 000 -> minus 50 000 -> 75 000
        Exposure  exposure  = Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, deductible: 50_000m, limit: 200_000m);
        Footprint footprint = Samples.PeakEightAtOrigin();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 0.5d);

        // Assert
        Assert.Equal(75_000m, loss);
    }

    [Fact]
    public void A_ground_up_loss_below_the_deductible_costs_the_insurer_nothing() {
        // Arrange: intensity 2.4 -> ratio 0.025 -> ground-up 25 000 < 50 000
        Exposure  exposure  = Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, deductible: 50_000m, limit: 200_000m);
        Footprint footprint = Samples.PeakEightAtOrigin();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 0.3d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void A_property_outside_the_footprint_suffers_no_loss() {
        // Arrange
        Exposure  exposure  = Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, deductible: 0m, limit: 1_000_000m);
        Footprint footprint = new Footprint(new GeoPoint(45d, 45d), 100d, 8d);

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_severity_factor_that_is_negative_or_not_finite_is_refused(double severityFactor) {
        // Arrange
        Exposure  anyExposure  = Any.Exposure().Generate();
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyExposure.InsuredLoss(anyFootprint, severityFactor));

        // Assert
        Assert.Equal("severityFactor", error.ParamName);
    }

    [Fact]
    public void A_loss_without_a_footprint_is_refused() {
        // Arrange
        Exposure anyExposure = Any.Exposure().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyExposure.InsuredLoss(null!, 1d));
    }

    [Fact]
    public void The_insured_loss_never_exceeds_the_limit_for_this_event() {
        // Arrange
        Exposure  exposure  = Any.Exposure().Generate();
        Footprint footprint = Any.Footprint().Generate();
        double    anySeverity = Any.Double().Between(0d, 5d).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, anySeverity);

        // Assert
        Assert.InRange(loss, 0m, exposure.Terms.Limit);
    }

}
