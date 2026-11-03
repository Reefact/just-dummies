using JustDummies;

using Manufacturing.Domain;

using Xunit;

namespace Manufacturing.Tests.Regular;

public sealed class ToleranceTests {

    [Fact]
    public void The_limits_are_the_nominal_minus_and_plus_its_deviations() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);

        // Assert
        Assert.Equal(10m, tolerance.NominalMm);
        Assert.Equal(0.1m, tolerance.MinusMm);
        Assert.Equal(0.25m, tolerance.PlusMm);
        Assert.Equal(9.9m, tolerance.LowerLimitMm);
        Assert.Equal(10.25m, tolerance.UpperLimitMm);
    }

    [Theory]
    [InlineData("9.9", true)]       // lower limit, inclusive
    [InlineData("10.25", true)]     // upper limit, inclusive
    [InlineData("10", true)]
    [InlineData("9.899", false)]
    [InlineData("10.251", false)]
    [InlineData("0", false)]
    [InlineData("-10", false)]
    public void A_measure_is_accepted_between_the_limits_inclusive(string measured, bool expected) {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);

        // Act
        bool accepted = tolerance.Accepts(decimal.Parse(measured, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, accepted);
    }

    [Fact]
    public void A_measure_between_the_limits_is_accepted() {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);
        decimal   anyInside = Any.Decimal().Between(tolerance.LowerLimitMm, tolerance.UpperLimitMm).WithScale(3).Generate();

        // Act
        bool accepted = tolerance.Accepts(anyInside);

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void A_measure_below_the_lower_limit_is_rejected() {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);
        decimal   anyBelow  = Any.Decimal().LessThan(tolerance.LowerLimitMm).Generate();

        // Act
        bool accepted = tolerance.Accepts(anyBelow);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void A_measure_above_the_upper_limit_is_rejected() {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);
        decimal   anyAbove  = Any.Decimal().GreaterThan(tolerance.UpperLimitMm).Generate();

        // Act
        bool accepted = tolerance.Accepts(anyAbove);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void A_generated_tolerance_accepts_its_own_nominal() {
        // Arrange
        Tolerance anyTolerance = new AnyTolerance().Generate();

        // Act
        bool accepted = anyTolerance.Accepts(anyTolerance.NominalMm);

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void A_one_sided_tolerance_is_accepted() {
        // Act
        Tolerance minusOnly = new Tolerance(10m, 0.5m, 0m);
        Tolerance plusOnly  = new Tolerance(10m, 0m, 0.5m);

        // Assert
        Assert.Equal(10m, minusOnly.UpperLimitMm);
        Assert.Equal(10m, plusOnly.LowerLimitMm);
        Assert.True(minusOnly.Accepts(10m));
        Assert.False(minusOnly.Accepts(10.001m));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("5000")]
    public void The_limits_of_the_nominal_range_are_accepted(string nominal) {
        decimal nominalMm = decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture);

        Tolerance tolerance = new Tolerance(nominalMm, 0m, 0.001m);

        Assert.Equal(nominalMm, tolerance.NominalMm);
    }

    [Fact]
    public void A_nominal_that_is_zero_or_negative_is_refused() {
        // Arrange
        decimal anyNonPositive = Any.Decimal().LessThanOrEqualTo(0m).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(anyNonPositive, 0.1m, 0.1m));

        // Assert
        Assert.Equal("nominalMm", error.ParamName);
    }

    [Fact]
    public void A_nominal_over_5000_is_refused() {
        // Arrange
        decimal anyTooBig = Any.Decimal().GreaterThan(5_000m).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(anyTooBig, 0.1m, 0.1m));

        // Assert
        Assert.Equal("nominalMm", error.ParamName);
    }

    [Fact]
    public void A_negative_minus_deviation_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, anyNegative, 0.1m));

        // Assert
        Assert.Equal("minusMm", error.ParamName);
    }

    [Fact]
    public void A_negative_plus_deviation_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, 0.1m, anyNegative));

        // Assert
        Assert.Equal("plusMm", error.ParamName);
    }

    [Fact]
    public void The_nominal_is_checked_before_the_deviations() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(0m, anyNegative, anyNegative));

        // Assert
        Assert.Equal("nominalMm", error.ParamName);
    }

    [Fact]
    public void The_minus_deviation_is_checked_before_the_plus_one() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, anyNegative, anyNegative));

        // Assert
        Assert.Equal("minusMm", error.ParamName);
    }

    [Fact]
    public void An_empty_band_is_refused() {
        // Arrange
        decimal anyNominal = Any.Decimal().Between(0.001m, 5_000m).WithScale(3).Generate();

        // Act
        Action act = () => new Tolerance(anyNominal, 0m, 0m);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_minus_deviation_equal_to_the_nominal_is_refused() {
        // Arrange
        decimal anyNominal = Any.Decimal().Between(0.001m, 5_000m).WithScale(3).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Tolerance(anyNominal, anyNominal, 0.1m));

        // Assert
        Assert.Contains("lower limit", error.Message);
    }

    [Fact]
    public void A_minus_deviation_above_the_nominal_is_refused() {
        // Arrange
        decimal anyNominal = Any.Decimal().Between(0.001m, 1_000m).WithScale(3).Generate();
        decimal anyBigger  = Any.Decimal().Between(anyNominal + 0.001m, 2_000m).WithScale(3).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Tolerance(anyNominal, anyBigger, 0.1m));

        // Assert
        Assert.Contains("lower limit", error.Message);
    }

    [Fact]
    public void A_lower_limit_just_above_zero_is_accepted() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 9.999m, 0.001m);

        // Assert
        Assert.Equal(0.001m, tolerance.LowerLimitMm);
    }

    [Fact]
    public void A_nominal_with_four_decimals_is_refused() {
        Assert.Throws<ArgumentException>(() => new Tolerance(1.2500m, 0.1m, 0.1m));
    }

    [Fact]
    public void A_minus_deviation_with_four_decimals_is_refused() {
        Assert.Throws<ArgumentException>(() => new Tolerance(10m, 0.1000m, 0.1m));
    }

    [Fact]
    public void A_plus_deviation_with_four_decimals_is_refused() {
        Assert.Throws<ArgumentException>(() => new Tolerance(10m, 0.1m, 0.0010m));
    }

    [Fact]
    public void Three_decimals_are_accepted_even_with_a_trailing_zero() {
        // Act
        Tolerance tolerance = new Tolerance(1.250m, 0.050m, 0.100m);

        // Assert
        Assert.Equal(1.2m, tolerance.LowerLimitMm);
        Assert.Equal(1.35m, tolerance.UpperLimitMm);
    }

    [Fact]
    public void The_range_checks_come_before_the_scale_check() {
        // A nominal over 5000 with four decimals: the range is reported, not the scale.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(5_000.0001m, 0.1m, 0.1m));
    }

}
