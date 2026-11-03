using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class ExposureTests {

    [Fact]
    public void A_deductible_equal_to_the_insured_value_is_refused_because_such_a_policy_could_never_pay() {
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(1000m).Generate();
        PolicyTerms     terms       = new(1000m, 500m);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Exposure(anyProperty, terms));

        Assert.Equal("terms", error.ParamName);
    }

    [Fact]
    public void A_deductible_one_cent_below_the_insured_value_is_accepted() {
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(1000m).Generate();
        PolicyTerms     terms       = new(999.99m, 0.01m);

        Exposure exposure = new(anyProperty, terms);

        Assert.Same(terms, exposure.Terms);
    }

    [Fact]
    public void A_limit_above_the_insured_value_is_refused_because_over_insurance_is_not_written() {
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(1000m).Generate();
        PolicyTerms     terms       = new(100m, 1000.01m);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Exposure(anyProperty, terms));

        Assert.Equal("terms", error.ParamName);
    }

    [Fact]
    public void A_limit_equal_to_the_insured_value_is_accepted() {
        InsuredProperty anyProperty = Any.InsuredProperty().WithInsuredValue(1000m).Generate();
        PolicyTerms     terms       = new(100m, 1000m);

        Exposure exposure = new(anyProperty, terms);

        Assert.Same(anyProperty, exposure.Property);
    }

    [Fact]
    public void Missing_parts_are_refused() {
        InsuredProperty anyProperty = Any.InsuredProperty().Generate();
        PolicyTerms     anyTerms    = Any.PolicyTerms().Generate();

        Assert.Throws<ArgumentNullException>(() => new Exposure(null!, anyTerms));
        Assert.Throws<ArgumentNullException>(() => new Exposure(anyProperty, null!));
    }

    [Fact]
    public void The_insured_loss_goes_through_the_curve_then_the_terms() {
        // Peak 10, full damage at 10: the property at the centre suffers value x severity before the terms.
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Exposure  exposure  = Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 100m, limit: 300m);

        decimal loss = exposure.InsuredLoss(footprint, severityFactor: 0.5);

        // ground-up 500.00 - 100 deductible = 400, capped at the 300 limit
        Assert.Equal(300m, loss);
    }

    [Fact]
    public void A_loss_below_the_limit_is_paid_net_of_the_deductible() {
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Exposure  exposure  = Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 100m, limit: 900m);

        decimal loss = exposure.InsuredLoss(footprint, severityFactor: 0.5);

        Assert.Equal(400m, loss);
    }

    [Fact]
    public void A_property_outside_the_footprint_loses_nothing() {
        Footprint footprint = Any.Footprint().WithCenter(new GeoPoint(0, 0)).WithRadiusKm(100.0).Generate();
        Exposure  exposure  = Any.Exposure()
                                 .WithProperty(p => p.WithLocation(new GeoPoint(45, 90)))
                                 .Generate();

        decimal loss = exposure.InsuredLoss(footprint, severityFactor: 1.0);

        Assert.Equal(0m, loss);
    }

    [Fact]
    public void A_zero_severity_factor_causes_no_loss() {
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Exposure  exposure  = Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 0m, limit: 1000m);

        decimal loss = exposure.InsuredLoss(footprint, severityFactor: 0.0);

        Assert.Equal(0m, loss);
    }

    [Fact]
    public void A_severity_so_high_that_the_whole_value_is_destroyed_pays_the_limit() {
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Exposure  exposure  = Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 100m, limit: 800m);

        decimal loss = exposure.InsuredLoss(footprint, severityFactor: 50.0);

        Assert.Equal(800m, loss);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_severity_factor_must_be_finite_and_non_negative(double severityFactor) {
        Footprint anyFootprint = Any.Footprint().Generate();
        Exposure  anyExposure  = Any.Exposure().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyExposure.InsuredLoss(anyFootprint, severityFactor));

        Assert.Equal("severityFactor", error.ParamName);
    }

    [Fact]
    public void A_missing_footprint_is_refused() {
        Exposure anyExposure = Any.Exposure().Generate();

        Assert.Throws<ArgumentNullException>(() => anyExposure.InsuredLoss(null!, 1.0));
    }

}
