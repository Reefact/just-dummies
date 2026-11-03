using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class ToleranceTests {

    [Fact]
    public void The_limits_are_the_nominal_less_the_minus_and_plus_the_plus() {
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
    [InlineData("9.9", true)]      // the lower limit itself
    [InlineData("10.25", true)]    // the upper limit itself
    [InlineData("10", true)]
    [InlineData("10.1", true)]
    [InlineData("9.899", false)]   // a micrometre under
    [InlineData("10.251", false)]  // a micrometre over
    [InlineData("0", false)]
    [InlineData("-10", false)]
    [InlineData("1000", false)]
    public void A_measure_is_accepted_between_the_limits_both_included(string measured, bool expected) {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.25m);

        // Act
        bool accepted = tolerance.Accepts(decimal.Parse(measured, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, accepted);
    }

    [Fact]
    public void A_one_sided_tolerance_accepts_only_its_own_side() {
        // Arrange
        Tolerance onlyPlus  = new Tolerance(10m, 0m, 0.5m);
        Tolerance onlyMinus = new Tolerance(10m, 0.5m, 0m);

        // Assert
        Assert.True(onlyPlus.Accepts(10m));
        Assert.True(onlyPlus.Accepts(10.5m));
        Assert.False(onlyPlus.Accepts(9.999m));
        Assert.True(onlyMinus.Accepts(9.5m));
        Assert.False(onlyMinus.Accepts(10.001m));
    }

    [Fact]
    public void A_measure_with_more_places_than_the_tolerance_is_still_compared_exactly() {
        // Arrange
        Tolerance tolerance = new Tolerance(10m, 0.1m, 0.1m);

        // Assert
        Assert.True(tolerance.Accepts(10.1m));
        Assert.False(tolerance.Accepts(10.1000001m));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1")]
    [InlineData("5000")]
    public void A_nominal_in_the_open_closed_range_is_accepted(string nominal) {
        // Act
        Tolerance tolerance = new Tolerance(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture), 0m, 0.001m);

        // Assert
        Assert.Equal(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture), tolerance.NominalMm);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("5000.001")]
    [InlineData("5001")]
    public void A_nominal_outside_the_range_is_refused(string nominal) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture), 0.1m, 0.1m));
    }

    [Fact]
    public void A_negative_deviation_is_refused() {
        ArgumentOutOfRangeException minus = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, -0.1m, 0.1m));
        ArgumentOutOfRangeException plus  = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, 0.1m, -0.1m));

        Assert.Equal("minusMm", minus.ParamName);
        Assert.Equal("plusMm", plus.ParamName);
    }

    [Fact]
    public void The_nominal_is_checked_before_the_deviations_and_the_minus_before_the_plus() {
        ArgumentOutOfRangeException nominalFirst = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(0m, -1m, -1m));
        ArgumentOutOfRangeException minusFirst   = Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(10m, -1m, -1m));

        Assert.Equal("nominalMm", nominalFirst.ParamName);
        Assert.Equal("minusMm", minusFirst.ParamName);
    }

    [Fact]
    public void An_empty_band_is_refused() {
        Assert.Throws<ArgumentException>(() => new Tolerance(10m, 0m, 0m));
    }

    [Theory]
    [InlineData("10", "10")]      // the lower limit would be zero
    [InlineData("10", "10.001")]  // and below zero
    [InlineData("0.5", "0.5")]
    [InlineData("0.001", "0.001")]
    public void A_minus_that_brings_the_lower_limit_to_zero_or_below_is_refused(string nominal, string minus) {
        Assert.Throws<ArgumentException>(() => new Tolerance(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture),
                                                              decimal.Parse(minus, System.Globalization.CultureInfo.InvariantCulture),
                                                              1m));
    }

    [Fact]
    public void A_lower_limit_a_micrometre_above_zero_is_accepted() {
        // Act
        Tolerance tolerance = new Tolerance(10m, 9.999m, 0m);

        // Assert
        Assert.Equal(0.001m, tolerance.LowerLimitMm);
    }

    [Fact]
    public void The_minus_check_comes_before_the_scale_check() {
        // Both rules are broken: the exception type alone cannot tell, but the message names the lower limit.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Tolerance(1.2345m, 2m, 0m));

        Assert.Contains("lower limit", exception.Message);
    }

    [Theory]
    [InlineData("1.2500", "0.1", "0.1")]
    [InlineData("1.25", "0.1000", "0.1")]
    [InlineData("1.25", "0.1", "0.1000")]
    [InlineData("1.2345", "0.1", "0.1")]
    [InlineData("1.25", "0.0001", "0.1")]
    [InlineData("1.25", "0.1", "0.0001")]
    public void More_than_three_decimal_places_are_refused_whatever_the_value(string nominal, string minus, string plus) {
        Assert.Throws<ArgumentException>(() => new Tolerance(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture),
                                                              decimal.Parse(minus, System.Globalization.CultureInfo.InvariantCulture),
                                                              decimal.Parse(plus, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1.25", "0.1", "0.1")]
    [InlineData("1.250", "0.100", "0.100")]
    [InlineData("1.001", "0.001", "0.001")]
    public void Up_to_three_decimal_places_are_accepted(string nominal, string minus, string plus) {
        // Act
        Tolerance tolerance = new Tolerance(decimal.Parse(nominal, System.Globalization.CultureInfo.InvariantCulture),
                                            decimal.Parse(minus, System.Globalization.CultureInfo.InvariantCulture),
                                            decimal.Parse(plus, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.True(tolerance.Accepts(tolerance.NominalMm));
    }

}
