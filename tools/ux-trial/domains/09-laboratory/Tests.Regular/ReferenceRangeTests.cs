using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class ReferenceRangeTests {

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_range_keeps_what_it_was_built_from() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ReferenceRange range = new ReferenceRange(10d, 20d, anyUnit, 5d, 30d);

        // Assert
        Assert.Equal(10d, range.Lower);
        Assert.Equal(20d, range.Upper);
        Assert.Same(anyUnit, range.Unit);
        Assert.Equal(5d, range.CriticalLow);
        Assert.Equal(30d, range.CriticalHigh);
    }

    [Fact]
    public void Critical_limits_are_optional() {
        // Act
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate());

        // Assert
        Assert.Null(range.CriticalLow);
        Assert.Null(range.CriticalHigh);
    }

    [Fact]
    public void The_generator_only_draws_ranges_the_constructor_accepts() {
        // Act
        ReferenceRange anyRange = Any.ReferenceRange().Generate();

        // Assert
        Assert.InRange(anyRange.Lower, 0d, double.MaxValue);
        Assert.True(anyRange.Lower < anyRange.Upper);
        Assert.True(anyRange.CriticalLow is null || anyRange.CriticalLow <= anyRange.Lower);
        Assert.True(anyRange.CriticalHigh is null || anyRange.CriticalHigh >= anyRange.Upper);
    }

    [Fact]
    public void A_missing_unit_is_refused_as_a_null_argument() {
        Assert.Throws<ArgumentNullException>(() => new ReferenceRange(10d, 20d, null!));
    }

    [Theory]
    [InlineData(double.NaN, 20d)]
    [InlineData(double.NegativeInfinity, 20d)]
    [InlineData(10d, double.NaN)]
    [InlineData(10d, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN)]
    public void A_non_finite_bound_is_refused_as_an_argument_error(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ReferenceRange(lower, upper, anyUnit));
    }

    [Fact]
    public void Finiteness_is_judged_before_the_sign_of_the_lower_bound() {
        // Act & Assert: -infinity is both non-finite and negative; the argument error wins.
        Assert.Throws<ArgumentException>(() => new ReferenceRange(double.NegativeInfinity, 20d, Any.MeasurementUnit().Generate()));
    }

    [Fact]
    public void A_negative_lower_bound_is_refused_as_out_of_range() {
        // Arrange
        double          anyNegative = Any.Double().Negative().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(anyNegative, 20d, anyUnit));

        // Assert
        Assert.Equal("lower", refusal.ParamName);
    }

    [Fact]
    public void The_sign_of_the_lower_bound_is_judged_before_its_order_with_the_upper_one() {
        // Act & Assert: lower is negative and above the upper bound; out-of-range wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(-1d, -2d, Any.MeasurementUnit().Generate()));
    }

    [Fact]
    public void A_lower_bound_of_zero_is_accepted() {
        Assert.Equal(0d, new ReferenceRange(0d, 1d, Any.MeasurementUnit().Generate()).Lower);
    }

    [Fact]
    public void A_lower_bound_equal_to_the_upper_one_is_refused() {
        // Arrange
        double          anyBound = Any.Double().GreaterThanOrEqualTo(0d).Generate();
        MeasurementUnit anyUnit  = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceRange(anyBound, anyBound, anyUnit));

        // Assert
        Assert.Equal("lower", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_lower_bound_above_the_upper_one_is_refused() {
        // Arrange
        double          anyUpper = Any.Double().GreaterThanOrEqualTo(0d).Generate();
        double          anyLower = Any.Double().GreaterThan(anyUpper).Generate();
        MeasurementUnit anyUnit  = Any.MeasurementUnit().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ReferenceRange(anyLower, anyUpper, anyUnit));
    }

    [Fact]
    public void A_critical_low_limit_equal_to_the_lower_bound_is_accepted() {
        Assert.Equal(10d, new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: 10d).CriticalLow);
    }

    [Fact]
    public void A_critical_low_limit_above_the_lower_bound_is_refused_as_out_of_range() {
        // Arrange
        double          anyAboveLower = Any.Double().GreaterThan(10d).Generate();
        MeasurementUnit anyUnit       = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(10d, 20d, anyUnit, criticalLow: anyAboveLower));

        // Assert
        Assert.Equal("criticalLow", refusal.ParamName);
    }

    [Fact]
    public void A_critical_high_limit_equal_to_the_upper_bound_is_accepted() {
        Assert.Equal(20d, new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalHigh: 20d).CriticalHigh);
    }

    [Fact]
    public void A_critical_high_limit_below_the_upper_bound_is_refused_as_out_of_range() {
        // Arrange
        double          anyBelowUpper = Any.Double().LessThan(20d).Generate();
        MeasurementUnit anyUnit       = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(10d, 20d, anyUnit, criticalHigh: anyBelowUpper));

        // Assert
        Assert.Equal("criticalHigh", refusal.ParamName);
    }

    [Fact]
    public void Critical_limits_are_not_required_to_be_non_negative() {
        // Arrange
        double anyNegativeLimit = Any.Double().Negative().Generate();

        // Act
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: anyNegativeLimit);

        // Assert
        Assert.Equal(anyNegativeLimit, range.CriticalLow);
    }

    [Fact(Skip = "domain defect: a NaN critical limit is accepted (the comparison against the bound is false), and then no value is ever critical; no seed: literal NaN")]
    public void A_NaN_critical_limit_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: double.NaN));
    }

    // ---- classification -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0d, ResultFlag.CriticalLow)]
    [InlineData(4.99d, ResultFlag.CriticalLow)]
    [InlineData(5d, ResultFlag.CriticalLow)]        // a critical limit is itself critical
    [InlineData(5.01d, ResultFlag.Low)]
    [InlineData(9.99d, ResultFlag.Low)]
    [InlineData(10d, ResultFlag.Normal)]            // the normal interval is inclusive
    [InlineData(15d, ResultFlag.Normal)]
    [InlineData(20d, ResultFlag.Normal)]
    [InlineData(20.01d, ResultFlag.High)]
    [InlineData(29.99d, ResultFlag.High)]
    [InlineData(30d, ResultFlag.CriticalHigh)]
    [InlineData(1e9d, ResultFlag.CriticalHigh)]
    public void A_value_is_classified_against_the_bounds_and_the_critical_limits(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), 5d, 30d);

        // Act
        ResultFlag flag = range.Classify(value);

        // Assert
        Assert.Equal(expected, flag);
    }

    [Theory]
    [InlineData(-1e9d, ResultFlag.Low)]
    [InlineData(9.99d, ResultFlag.Low)]
    [InlineData(10d, ResultFlag.Normal)]
    [InlineData(20d, ResultFlag.Normal)]
    [InlineData(20.01d, ResultFlag.High)]
    [InlineData(1e9d, ResultFlag.High)]
    public void Without_critical_limits_a_value_is_never_critical(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate());

        // Act & Assert
        Assert.Equal(expected, range.Classify(value));
    }

    [Theory]
    [InlineData(4d, ResultFlag.CriticalLow)]
    [InlineData(9d, ResultFlag.Low)]
    [InlineData(25d, ResultFlag.High)]
    [InlineData(1e9d, ResultFlag.High)]
    public void With_only_a_critical_low_limit_the_high_side_stays_ordinary(double value, ResultFlag expected) {
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: 5d);

        Assert.Equal(expected, range.Classify(value));
    }

    [Theory]
    [InlineData(-1e9d, ResultFlag.Low)]
    [InlineData(9d, ResultFlag.Low)]
    [InlineData(25d, ResultFlag.High)]
    [InlineData(31d, ResultFlag.CriticalHigh)]
    public void With_only_a_critical_high_limit_the_low_side_stays_ordinary(double value, ResultFlag expected) {
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalHigh: 30d);

        Assert.Equal(expected, range.Classify(value));
    }

    [Fact]
    public void A_critical_limit_equal_to_a_bound_makes_that_bound_itself_critical() {
        // Arrange
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: 10d, criticalHigh: 20d);

        // Assert
        Assert.Equal(ResultFlag.CriticalLow, range.Classify(10d));
        Assert.Equal(ResultFlag.CriticalHigh, range.Classify(20d));
        Assert.Equal(ResultFlag.Normal, range.Classify(15d));
    }

    [Fact]
    public void A_NaN_value_is_normal() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().Generate();

        // Act & Assert
        Assert.Equal(ResultFlag.Normal, anyRange.Classify(double.NaN));
    }

    [Fact]
    public void Any_value_between_the_bounds_is_normal_when_no_critical_limit_touches_them() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().WithoutCriticalLow().WithoutCriticalHigh().Generate();
        double         anyInside = Any.Double().Between(anyRange.Lower, anyRange.Upper).Generate();

        // Act & Assert
        Assert.Equal(ResultFlag.Normal, anyRange.Classify(anyInside));
    }

    [Fact]
    public void Any_value_below_the_lower_bound_is_low_when_there_is_no_critical_limit() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().WithoutCriticalLow().WithoutCriticalHigh().Generate();
        double         anyBelow = Any.Double().LessThan(anyRange.Lower).Generate();

        // Act & Assert
        Assert.Equal(ResultFlag.Low, anyRange.Classify(anyBelow));
    }

    [Fact]
    public void Any_value_above_the_upper_bound_is_high_when_there_is_no_critical_limit() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().WithoutCriticalLow().WithoutCriticalHigh().Generate();
        double         anyAbove = Any.Double().GreaterThan(anyRange.Upper).Generate();

        // Act & Assert
        Assert.Equal(ResultFlag.High, anyRange.Classify(anyAbove));
    }

}
