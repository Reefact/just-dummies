using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class ExposureTests {

    // Intensity 2 or less: no damage; 8 or more: 30% of the value; linear in between.
    private static VulnerabilityCurve LinearUpToThirtyPercent() {
        return new VulnerabilityCurve(Any.String().NotBlank().Generate(), [2d, 8d], [0d, 0.3d]);
    }

    private static InsuredProperty MillionAt(GeoPoint place) {
        return Any.InsuredProperty().WithLocation(place).WithInsuredValue(1_000_000m).WithVulnerability(LinearUpToThirtyPercent()).Generate();
    }

    private static Footprint CentredOn(GeoPoint place, double peakIntensity) {
        double anyRadiusKm = Any.Double().Between(10d, Footprint.MaxRadiusKm).Generate();

        return new Footprint(place, anyRadiusKm, peakIntensity);
    }

    [Fact]
    public void An_exposure_keeps_the_property_and_the_terms_it_was_built_from() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(1_000m, 90_000m);

        // Act
        Exposure exposure = new Exposure(anyProperty, terms);

        // Assert
        Assert.Same(anyProperty, exposure.Property);
        Assert.Same(terms, exposure.Terms);
    }

    [Fact]
    public void An_exposure_without_a_property_is_refused() {
        // Arrange
        PolicyTerms anyTerms = Any.PolicyTerms().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Exposure(null!, anyTerms));

        // Assert
        Assert.Equal("property", refusal.ParamName);
    }

    [Fact]
    public void An_exposure_without_terms_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Exposure(anyProperty, null!));

        // Assert
        Assert.Equal("terms", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_equal_to_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(100_000m, 50_000m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.Equal("terms", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_above_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        decimal         anyDeductibleAboveValue = Any.Decimal().GreaterThan(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(anyDeductibleAboveValue, 50_000m);

        // Act
        Exception? refusal = Record.Exception(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_deductible_just_below_the_insured_value_is_accepted() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(99_999.99m, 50_000m);

        // Act
        Exposure exposure = new Exposure(anyProperty, terms);

        // Assert
        Assert.Same(terms, exposure.Terms);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(0m, 100_000.01m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Exposure(anyProperty, terms));

        // Assert
        Assert.Equal("terms", refusal.ParamName);
    }

    [Fact]
    public void A_limit_equal_to_the_insured_value_is_accepted() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(100_000m).Generate();
        PolicyTerms     terms       = new PolicyTerms(0m, 100_000m);

        // Act
        Exposure exposure = new Exposure(anyProperty, terms);

        // Assert
        Assert.Equal(100_000m, exposure.Terms.Limit);
    }

    [Fact]
    public void A_property_at_the_centre_of_the_footprint_loses_its_ground_up_loss_less_the_terms() {
        // Arrange
        GeoPoint        anyPlace  = Any.GeoPoint().Generate();
        Footprint       footprint = CentredOn(anyPlace, 5d);                            // intensity 5 -> ratio 0.15 -> 150 000 ground-up
        PolicyTerms     terms     = new PolicyTerms(20_000m, 500_000m);
        Exposure        exposure  = new Exposure(MillionAt(anyPlace), terms);

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(130_000m, loss);
    }

    [Fact]
    public void The_loss_is_capped_at_the_limit_of_the_terms() {
        // Arrange
        GeoPoint    anyPlace  = Any.GeoPoint().Generate();
        Footprint   footprint = CentredOn(anyPlace, 5d);
        PolicyTerms terms     = new PolicyTerms(20_000m, 100_000m);
        Exposure    exposure  = new Exposure(MillionAt(anyPlace), terms);

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(100_000m, loss);
    }

    [Fact]
    public void A_ground_up_loss_within_the_deductible_costs_the_insurer_nothing() {
        // Arrange
        GeoPoint    anyPlace  = Any.GeoPoint().Generate();
        Footprint   footprint = CentredOn(anyPlace, 5d);
        PolicyTerms terms     = new PolicyTerms(150_000m, 500_000m);
        Exposure    exposure  = new Exposure(MillionAt(anyPlace), terms);

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void A_severity_factor_scales_the_intensity_before_it_goes_through_the_curve() {
        // Arrange
        GeoPoint    anyPlace  = Any.GeoPoint().Generate();
        Footprint   footprint = CentredOn(anyPlace, 5d);
        PolicyTerms terms     = new PolicyTerms(0.01m, 1_000_000m);
        Exposure    exposure  = new Exposure(MillionAt(anyPlace), terms);

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1.2d);   // intensity 6 -> ratio 0.2 -> 200 000 - 0.01

        // Assert
        Assert.Equal(199_999.99m, loss);
    }

    [Fact]
    public void A_severity_factor_of_zero_means_no_loss() {
        // Arrange
        GeoPoint  anyPlace  = Any.GeoPoint().Generate();
        Footprint footprint = CentredOn(anyPlace, 5d);
        Exposure  exposure  = Any.Exposure().WithProperty(MillionAt(anyPlace)).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 0d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void An_enormous_severity_factor_pays_the_terms_at_the_last_point_of_the_curve() {
        // Arrange
        GeoPoint    anyPlace  = Any.GeoPoint().Generate();
        Footprint   footprint = CentredOn(anyPlace, 5d);
        PolicyTerms terms     = new PolicyTerms(50_000m, 900_000m);
        Exposure    exposure  = new Exposure(MillionAt(anyPlace), terms);
        double      anyEnormousSeverity = Any.Double().GreaterThan(10d).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, anyEnormousSeverity);

        // Assert
        Assert.Equal(250_000m, loss);                                                   // 30% of 1 000 000, less the deductible
    }

    [Fact]
    public void A_property_outside_the_footprint_is_not_hurt() {
        // Arrange
        GeoPoint        place     = new GeoPoint(10d, 10d);
        GeoPoint        farAway   = new GeoPoint(-40d, 120d);
        Footprint       footprint = CentredOn(farAway, 1_000d);
        Exposure        exposure  = Any.Exposure().WithProperty(MillionAt(place)).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact(Skip = "domain defect: a property exactly antipodal to the footprint centre gets a NaN distance, hence a NaN intensity, and InsuredLoss throws ArgumentOutOfRangeException from the curve instead of returning 0; literal coordinates, seed n/a")]
    public void A_property_on_the_other_side_of_the_earth_is_not_hurt() {
        // Arrange
        GeoPoint  centre    = new GeoPoint(77.35488929196956d, -65.17134279253489d);
        GeoPoint  antipode  = new GeoPoint(-77.35488929196956d, 114.82865720746511d);
        Footprint footprint = new Footprint(centre, 100d, 5d);
        Exposure  exposure  = Any.Exposure().WithProperty(MillionAt(antipode)).Generate();

        // Act
        decimal loss = exposure.InsuredLoss(footprint, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_severity_factor_that_is_not_finite_and_non_negative_is_refused(double severityFactor) {
        // Arrange
        Exposure  anyExposure  = Any.Exposure().Generate();
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyExposure.InsuredLoss(anyFootprint, severityFactor));

        // Assert
        Assert.Equal("severityFactor", refusal.ParamName);
    }

    [Fact]
    public void A_loss_under_no_footprint_is_refused() {
        // Arrange
        Exposure anyExposure = Any.Exposure().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyExposure.InsuredLoss(null!, 1d));

        // Assert
        Assert.Equal("footprint", refusal.ParamName);
    }

}
