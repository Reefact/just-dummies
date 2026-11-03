namespace Manufacturing.Domain.Tests.Expert;

public sealed class ToleranceTests {

    [Fact]
    public void A_tolerance_keeps_its_three_values_and_derives_its_limits() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);

        // Assert
        Assert.Equal(10m, tolerance.NominalMm);
        Assert.Equal(0.1m, tolerance.MinusMm);
        Assert.Equal(0.25m, tolerance.PlusMm);
        Assert.Equal(9.9m, tolerance.LowerLimitMm);
        Assert.Equal(10.25m, tolerance.UpperLimitMm);
    }

    public static TheoryData<decimal, bool> Measurements => new TheoryData<decimal, bool> {
        { 10m, true },
        { 9.9m, true },        // the lower limit itself
        { 10.2m, true },       // the upper limit itself
        { 9.901m, true },
        { 10.199m, true },
        { 9.899m, false },     // one micrometre under the lower limit
        { 10.201m, false },    // one micrometre over the upper limit
        { 0m, false },
        { -10m, false },
        { 1_000m, false }
    };

    [Theory]
    [MemberData(nameof(Measurements))]
    public void A_measure_is_accepted_between_the_limits_both_included(decimal measuredMm, bool expected) {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.2m);

        // Act
        bool accepted = tolerance.Accepts(measuredMm);

        // Assert
        Assert.Equal(expected, accepted);
    }

    [Fact]
    public void A_one_sided_tolerance_accepts_the_nominal_and_its_open_side_only() {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0m, 0.5m);

        // Assert
        Assert.True(tolerance.Accepts(10m));
        Assert.True(tolerance.Accepts(10.5m));
        Assert.False(tolerance.Accepts(9.999m));
        Assert.False(tolerance.Accepts(10.501m));
    }

    [Fact]
    public void A_tolerance_with_only_a_lower_deviation_is_accepted() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 0.5m, 0m);

        // Assert
        Assert.Equal(9.5m, tolerance.LowerLimitMm);
        Assert.Equal(10m, tolerance.UpperLimitMm);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1")]
    [InlineData("5000")]
    [InlineData("5000.000")]
    public void A_nominal_in_zero_exclusive_to_five_thousand_inclusive_is_accepted(string nominal) {
        // Act
        Tolerance tolerance = new Tolerance(decimal.Parse(nominal), 0m, 0.001m);

        // Assert
        Assert.Equal(decimal.Parse(nominal), tolerance.NominalMm);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("5000.001")]
    [InlineData("5001")]
    public void A_nominal_outside_zero_exclusive_to_five_thousand_inclusive_is_refused(string nominal) {
        // Act
        Action create = () => new Tolerance(decimal.Parse(nominal), 0m, 0.001m);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("nominalMm", error.ParamName);
    }

    [Fact]
    public void A_negative_lower_deviation_is_refused() {
        // Act
        Action create = () => new Tolerance(10m, -0.1m, 0.2m);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("minusMm", error.ParamName);
    }

    [Fact]
    public void A_negative_upper_deviation_is_refused() {
        // Act
        Action create = () => new Tolerance(10m, 0.1m, -0.2m);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("plusMm", error.ParamName);
    }

    [Fact]
    public void A_negative_deviation_is_reported_before_the_band_is_found_empty() {
        // Act: -1 + 1 would be an empty band, but the sign is looked at first
        Action create = () => new Tolerance(10m, -1m, 1m);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("minusMm", error.ParamName);
    }

    [Fact]
    public void An_empty_band_is_refused() {
        // Act
        Action create = () => new Tolerance(10m, 0m, 0m);

        // Assert: an argument error, not its out-of-range subtype
        Assert.Throws<ArgumentException>(create);
    }

    [Theory]
    [InlineData("10", "10")]       // the lower limit would be zero
    [InlineData("10", "10.5")]     // the lower limit would be negative
    [InlineData("0.5", "0.5")]
    public void A_lower_deviation_reaching_the_nominal_is_refused(string nominal, string minus) {
        // Act
        Action create = () => new Tolerance(decimal.Parse(nominal), decimal.Parse(minus), 0.1m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_lower_deviation_just_under_the_nominal_is_accepted() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 9.999m, 0m);

        // Assert
        Assert.Equal(0.001m, tolerance.LowerLimitMm);
    }

    [Fact]
    public void The_micrometre_is_the_finest_resolution_of_the_nominal() {
        // Act
        Action create = () => new Tolerance(10.0001m, 0.1m, 0.1m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void The_micrometre_is_the_finest_resolution_of_the_lower_deviation() {
        // Act
        Action create = () => new Tolerance(10m, 0.0001m, 0.1m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void The_micrometre_is_the_finest_resolution_of_the_upper_deviation() {
        // Act
        Action create = () => new Tolerance(10m, 0.1m, 0.0001m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_value_with_trailing_zeros_beyond_the_micrometre_is_refused_because_the_scale_is_the_representations() {
        // Act: 10.5000 is numerically 10.5
        Action create = () => new Tolerance(10.5000m, 0.1m, 0.1m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_value_with_trailing_zeros_within_the_micrometre_is_accepted() {
        // Act
        Tolerance tolerance = new Tolerance(10.500m, 0.100m, 0.100m);

        // Assert
        Assert.Equal(10.5m, tolerance.NominalMm);
    }

}
