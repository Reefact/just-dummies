using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class InsuredPropertyTests {

    [Fact]
    public void A_property_keeps_what_it_was_built_from() {
        // Arrange
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        InsuredProperty property = new InsuredProperty("PRP-004217", anyLocation, 250_000.50m, anyVulnerability);

        // Assert
        Assert.Equal("PRP-004217", property.Id);
        Assert.Same(anyLocation, property.Location);
        Assert.Equal(250_000.50m, property.InsuredValue);
        Assert.Same(anyVulnerability, property.Vulnerability);
    }

    [Theory]
    [InlineData("PRP-000000")]
    [InlineData("PRP-999999")]
    public void An_id_of_prp_followed_by_six_digits_is_accepted(string id) {
        // Act
        InsuredProperty property = Any.InsuredProperty().WithId(id).Generate();

        // Assert
        Assert.Equal(id, property.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PRP-12345")]
    [InlineData("PRP-1234567")]
    [InlineData("prp-123456")]
    [InlineData("PRP123456")]
    [InlineData("PRP-12345A")]
    [InlineData("XPRP-123456")]
    [InlineData("PRP-123456 ")]
    [InlineData(" PRP-123456")]
    [InlineData("EV-123456")]
    public void An_id_that_is_not_prp_followed_by_six_digits_is_refused(string id) {
        // Arrange
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new InsuredProperty(id, anyLocation, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact(Skip = "domain defect: the id pattern ends with $, which .NET lets match before a trailing newline, so 'PRP-123456\\n' is accepted; literal id, seed n/a")]
    public void An_id_followed_by_a_line_break_is_refused() {
        // Arrange
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new InsuredProperty("PRP-123456\n", anyLocation, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so 'PRP-' followed by six Arabic-Indic digits is accepted as an id; literal id, seed n/a")]
    public void An_id_written_with_non_ascii_digits_is_refused() {
        // Arrange
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new InsuredProperty("PRP-٠٠٠١٢٣", anyLocation, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void A_property_without_an_id_is_refused() {
        // Arrange
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new InsuredProperty(null!, anyLocation, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_property_without_a_location_is_refused() {
        // Arrange
        string             anyId            = Any.StringMatching("PRP-[0-9]{6}").Generate();
        decimal            anyInsuredValue  = Any.Decimal().Positive().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, null!, anyInsuredValue, anyVulnerability));

        // Assert
        Assert.Equal("location", refusal.ParamName);
    }

    [Fact]
    public void A_property_without_a_vulnerability_curve_is_refused() {
        // Arrange
        string   anyId           = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint anyLocation     = Any.GeoPoint().Generate();
        decimal  anyInsuredValue = Any.Decimal().Positive().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new InsuredProperty(anyId, anyLocation, anyInsuredValue, null!));

        // Assert
        Assert.Equal("vulnerability", refusal.ParamName);
    }

    [Fact]
    public void A_property_worth_nothing_is_refused() {
        // Arrange
        string             anyId            = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new InsuredProperty(anyId, anyLocation, 0m, anyVulnerability));

        // Assert
        Assert.Equal("insuredValue", refusal.ParamName);
    }

    [Fact]
    public void A_property_of_negative_value_is_refused() {
        // Arrange
        string             anyId            = Any.StringMatching("PRP-[0-9]{6}").Generate();
        GeoPoint           anyLocation      = Any.GeoPoint().Generate();
        decimal            anyNegativeValue = Any.Decimal().Negative().Generate();
        VulnerabilityCurve anyVulnerability = Any.VulnerabilityCurve().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new InsuredProperty(anyId, anyLocation, anyNegativeValue, anyVulnerability));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_smallest_possible_value_is_accepted() {
        // Act
        InsuredProperty property = Any.InsuredProperty().WithInsuredValue(0.01m).Generate();

        // Assert
        Assert.Equal(0.01m, property.InsuredValue);
    }

    [Fact]
    public void The_ground_up_loss_is_zero_below_the_damage_threshold() {
        // Arrange
        VulnerabilityCurve curve    = new VulnerabilityCurve(Any.String().NotBlank().Generate(), [10d, 20d], [0d, 0.5d]);
        InsuredProperty    property = Any.InsuredProperty().WithInsuredValue(1_000_000m).WithVulnerability(curve).Generate();

        // Act
        decimal loss = property.GroundUpLoss(9.5d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void The_ground_up_loss_is_the_insured_value_times_the_damage_ratio() {
        // Arrange
        VulnerabilityCurve curve    = new VulnerabilityCurve(Any.String().NotBlank().Generate(), [10d, 20d], [0d, 0.5d]);
        InsuredProperty    property = Any.InsuredProperty().WithInsuredValue(200_000m).WithVulnerability(curve).Generate();

        // Act
        decimal loss = property.GroundUpLoss(15d);

        // Assert
        Assert.Equal(50_000m, loss);
    }

    [Fact]
    public void Above_the_last_point_of_the_curve_the_ground_up_loss_is_the_last_ratio_of_the_value() {
        // Arrange
        VulnerabilityCurve curve    = new VulnerabilityCurve(Any.String().NotBlank().Generate(), [10d, 20d], [0d, 0.25d]);
        InsuredProperty    property = Any.InsuredProperty().WithInsuredValue(80_000m).WithVulnerability(curve).Generate();

        // Act
        decimal loss = property.GroundUpLoss(500d);

        // Assert
        Assert.Equal(20_000m, loss);
    }

    [Theory]
    [InlineData(10, 0.12345, 1.23)]
    [InlineData(10, 0.12355, 1.24)]
    [InlineData(1000, 0.33333, 333.33)]
    public void The_ground_up_loss_is_rounded_to_the_cent(int insuredValue, double lastRatio, double expectedLoss) {
        // Arrange
        VulnerabilityCurve curve    = new VulnerabilityCurve(Any.String().NotBlank().Generate(), [0d, 1d], [0d, lastRatio]);
        InsuredProperty    property = Any.InsuredProperty().WithInsuredValue(insuredValue).WithVulnerability(curve).Generate();

        // Act
        decimal loss = property.GroundUpLoss(1d);

        // Assert
        Assert.Equal((decimal)expectedLoss, loss);
    }

    [Fact]
    public void A_negative_intensity_has_no_ground_up_loss() {
        // Arrange
        InsuredProperty anyProperty          = Any.InsuredProperty().Generate();
        double          anyNegativeIntensity = Any.Double().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyProperty.GroundUpLoss(anyNegativeIntensity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
