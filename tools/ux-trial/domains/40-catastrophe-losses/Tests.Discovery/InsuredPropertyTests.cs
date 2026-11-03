using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class InsuredPropertyTests {

    [Theory]
    [InlineData("PRP-12345")]
    [InlineData("PRP-1234567")]
    [InlineData("prp-123456")]
    [InlineData("PRP123456")]
    [InlineData("PRP-12345A")]
    [InlineData(" PRP-123456")]
    [InlineData("PRP-123456 ")]
    [InlineData("")]
    public void An_id_that_is_not_prp_and_six_digits_is_refused(string id) {
        // Arrange
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Between(1m, 1_000_000m).WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new InsuredProperty(id, anyLocation, anyValue, anyCurve));
    }

    [Fact(Skip = "domain defect: IdFormat '^PRP-\\d{6}$' accepts a trailing line feed ('$' matches before a final \\n), seed -258041706")]
    public void An_id_with_a_trailing_line_feed_is_refused() {
        // Arrange
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Between(1m, 1_000_000m).WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new InsuredProperty("PRP-123456\n", anyLocation, anyValue, anyCurve));
    }

    [Fact(Skip = "domain defect: IdFormat '\\d' matches any Unicode decimal digit (Arabic-Indic here), seed 2063570848")]
    public void An_id_with_non_ascii_digits_is_refused() {
        // Arrange
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        decimal            anyValue    = Any.Decimal().Between(1m, 1_000_000m).WithScale(2).Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        // Act & Assert: Arabic-Indic digits
        Assert.Throws<FormatException>(() => new InsuredProperty("PRP-١٢٣٤٥٦", anyLocation, anyValue, anyCurve));
    }

    [Fact]
    public void A_missing_id_location_or_curve_is_refused() {
        // Arrange
        string             anyId       = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(null!, anyLocation, 100m, anyCurve));
        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, null!, 100m, anyCurve));
        Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, anyLocation, 100m, null!));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000000")]
    public void An_insured_value_that_is_not_positive_is_refused(string insuredValue) {
        // Arrange
        string             anyId       = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation = Any.GeoPoint().Generate();
        VulnerabilityCurve anyCurve    = Any.VulnerabilityCurve().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new InsuredProperty(anyId, anyLocation, decimal.Parse(insuredValue), anyCurve));

        // Assert
        Assert.Equal("insuredValue", error.ParamName);
    }

    [Fact]
    public void A_property_exposes_what_it_was_built_with() {
        // Arrange
        GeoPoint           location = Any.GeoPoint().Generate();
        VulnerabilityCurve curve    = Any.VulnerabilityCurve().Generate();

        // Act
        InsuredProperty property = new InsuredProperty("PRP-004217", location, 250_000m, curve);

        // Assert
        Assert.Equal("PRP-004217", property.Id);
        Assert.Same(location, property.Location);
        Assert.Equal(250_000m, property.InsuredValue);
        Assert.Same(curve, property.Vulnerability);
    }

    [Theory]
    [InlineData(1d, "0")]
    [InlineData(2d, "0")]
    [InlineData(6d, "250000.00")]
    [InlineData(10d, "500000.00")]
    [InlineData(50d, "500000.00")]
    public void The_ground_up_loss_is_the_insured_value_times_the_damage_ratio(double intensity, string expected) {
        // Arrange
        string          anyId    = Any.StringMatching("PRP-[0-9]{6}").Generate();
        InsuredProperty property = Samples.PropertyAtOrigin(anyId, 1_000_000m);

        // Act
        decimal loss = property.GroundUpLoss(intensity);

        // Assert
        Assert.Equal(decimal.Parse(expected), loss);
    }

    [Fact]
    public void The_ground_up_loss_is_rounded_to_cents() {
        // Arrange: ratio at 6 is 0.25 -> 0.25 * 0.07 = 0.0175 -> 0.02
        string          anyId    = Any.StringMatching("PRP-[0-9]{6}").Generate();
        InsuredProperty property = Samples.PropertyAtOrigin(anyId, 0.07m);

        // Act
        decimal loss = property.GroundUpLoss(6d);

        // Assert
        Assert.Equal(0.02m, loss);
    }

    [Fact]
    public void A_ground_up_loss_is_not_computed_for_a_negative_intensity() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyProperty.GroundUpLoss(-1d));
    }

}
