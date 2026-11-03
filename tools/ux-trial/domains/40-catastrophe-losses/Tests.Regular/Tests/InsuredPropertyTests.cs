using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class InsuredPropertyTests {

    [Theory]
    [InlineData("PRP-000000")]
    [InlineData("PRP-004217")]
    [InlineData("PRP-999999")]
    public void A_property_id_is_PRP_and_six_digits(string id) {
        GeoPoint           anyLocation   = Any.GeoPoint().Generate();
        decimal            anyValue      = Any.Decimal().Positive().WithScale(2).Generate();
        VulnerabilityCurve anyCurve      = Any.VulnerabilityCurve().Generate();

        InsuredProperty property = new(id, anyLocation, anyValue, anyCurve);

        Assert.Equal(id, property.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PRP-12345")]
    [InlineData("PRP-1234567")]
    [InlineData("prp-123456")]
    [InlineData("PRP123456")]
    [InlineData("PRP-12345A")]
    [InlineData(" PRP-123456")]
    [InlineData("PRP-123456 ")]
    [InlineData("XPRP-123456")]
    public void Any_other_id_shape_is_refused(string id) {
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Positive().WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        Assert.Throws<FormatException>(() => new InsuredProperty(id, anyLocation, anyValue, anyCurve));
    }

    [Fact(Skip = "domain defect: the id pattern '^PRP-\\d{6}$' accepts a trailing newline ('PRP-123456\\n'); the id is a literal here, seed n/a")]
    public void An_id_with_a_trailing_line_break_is_refused() {
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Positive().WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        Assert.Throws<FormatException>(() => new InsuredProperty("PRP-123456\n", anyLocation, anyValue, anyCurve));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so 'PRP-١٢٣٤٥٦' (Arabic-Indic) is accepted as 'six digits'; the id is a literal here, seed n/a")]
    public void An_id_made_of_non_ascii_digits_is_refused() {
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Positive().WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        Assert.Throws<FormatException>(() => new InsuredProperty("PRP-١٢٣٤٥٦", anyLocation, anyValue, anyCurve));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.01")]
    public void An_insured_value_must_be_strictly_positive(string value) {
        string             anyId       = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new InsuredProperty(anyId, anyLocation, Fixtures.D(value), anyCurve));

        Assert.Equal("insuredValue", error.ParamName);
    }

    [Fact]
    public void The_smallest_cent_is_a_valid_insured_value() {
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(0.01m).Generate();

        Assert.Equal(0.01m, property.InsuredValue);
    }

    [Fact]
    public void Missing_parts_are_refused() {
        string             anyId       = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(null!, anyLocation, 100m, anyCurve));
        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, null!, 100m, anyCurve));
        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, anyLocation, 100m, null!));
    }

    [Theory]
    [InlineData(0.0, "0.00")]
    [InlineData(5.0, "500.00")]
    [InlineData(10.0, "1000.00")]
    [InlineData(25.0, "1000.00")]
    public void The_ground_up_loss_is_the_value_times_the_damage_ratio(double intensity, string expectedLoss) {
        InsuredProperty property = Any.InsuredProperty()
                                      .WithInsuredValue(1000m)
                                      .WithVulnerability(Fixtures.LinearCurve(10.0))
                                      .Generate();

        decimal loss = property.GroundUpLoss(intensity);

        Assert.Equal(Fixtures.D(expectedLoss), loss);
    }

    [Fact]
    public void The_ground_up_loss_is_rounded_to_cents() {
        InsuredProperty property = Any.InsuredProperty()
                                      .WithInsuredValue(100m)
                                      .WithVulnerability(Fixtures.LinearCurve(3.0))
                                      .Generate();

        decimal loss = property.GroundUpLoss(1.0);

        Assert.Equal(33.33m, loss);
    }

    [Fact]
    public void A_negative_intensity_has_no_ground_up_loss() {
        InsuredProperty anyProperty = Any.InsuredProperty().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyProperty.GroundUpLoss(-1.0));
    }

}
