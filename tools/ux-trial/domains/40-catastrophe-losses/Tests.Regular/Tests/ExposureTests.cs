namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class ExposureTests {

    [Fact]
    public void A_property_id_that_is_not_PRP_and_six_digits_is_refused() {
        // Arrange
        GeoPoint           anyLocation      = Arbitrary.GeoPoint().Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Arbitrary.VulnerabilityCurve().Generate();

        // Act
        Exception? error = Record.Exception(() => new InsuredProperty("PRP-42", anyLocation, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void The_ground_up_loss_is_the_insured_value_times_the_damage_ratio_in_cents() {
        // Arrange
        InsuredProperty property = Arbitrary.InsuredProperty()
                                            .WithInsuredValue(333_333.33m)
                                            .WithVulnerability(Curves.Linear())
                                            .Generate();

        // Act
        decimal groundUp = property.GroundUpLoss(10.0);

        // Assert
        Assert.Equal(33_333.33m, groundUp);
    }

    [Fact]
    public void A_deductible_that_swallows_the_whole_insured_value_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Arbitrary.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms       = Arbitrary.PolicyTerms().WithDeductible(500_000m).Generate();

        // Act
        Exception? error = Record.Exception(() => new Exposure(anyProperty, terms));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("terms", argumentError.ParamName);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused_as_over_insurance() {
        // Arrange
        InsuredProperty anyProperty = Arbitrary.InsuredProperty().WithInsuredValue(500_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(deductible: 1_000m, limit: 500_000.01m);

        // Act
        Exception? error = Record.Exception(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.Contains("over-insurance", Assert.IsType<ArgumentException>(error).Message);
    }

    [Fact]
    public void A_property_outside_the_footprint_suffers_no_insured_loss() {
        // Arrange
        Exposure anyExposure = Arbitrary.Exposure()
                                        .WithProperty(p => p.WithLocation(new GeoPoint(-21.1, 55.5)))   // La Réunion
                                        .Generate();
        Footprint anyFootprintOverJapan = Arbitrary.Footprint()
                                                   .WithCenter(new GeoPoint(35.7, 139.7))
                                                   .WithRadiusKm(r => r.LessThanOrEqualTo(500.0))
                                                   .Generate();
        double anySeverity = Any.Double().Between(0.1, 5.0).Generate();

        // Act
        decimal loss = anyExposure.InsuredLoss(anyFootprintOverJapan, anySeverity);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void The_severity_factor_scales_the_hazard_before_the_curve_and_the_terms_apply() {
        // Arrange
        GeoPoint anySite  = Arbitrary.GeoPoint().Generate();
        Exposure exposure = Arbitrary.Exposure()
                                     .WithProperty(p => p.WithLocation(anySite)
                                                         .WithInsuredValue(200_000m)
                                                         .WithVulnerability(Curves.Linear()))
                                     .WithTerms(new PolicyTerms(deductible: 10_000m, limit: 150_000m))
                                     .Generate();
        Footprint footprint = Arbitrary.Footprint().WithCenter(anySite).WithPeakIntensity(25.0).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 2.0);

        // Assert — intensity 25 x 2 = 50, ratio 0.5, ground-up 100 000, less the deductible.
        Assert.Equal(90_000m, loss);
    }

}
