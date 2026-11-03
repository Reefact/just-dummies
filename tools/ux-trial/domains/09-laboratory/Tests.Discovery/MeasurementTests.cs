using JustDummies;

using Laboratory.Domain;

using Xunit;

namespace Laboratory.Tests.Discovery;

public sealed class MeasurementTests {

    [Fact]
    public void A_measurement_keeps_its_value_and_its_unit() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        Measurement measurement = new Measurement(5.4, anyUnit);

        // Assert
        Assert.Equal(5.4, measurement.Value);
        Assert.Same(anyUnit, measurement.Unit);
    }

    [Fact]
    public void Zero_is_a_valid_measurement() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Equal(0d, new Measurement(0d, anyUnit).Value);
    }

    [Fact]
    public void Any_finite_non_negative_value_makes_a_measurement() {
        // Arrange
        double          anyValue = Any.Double().GreaterThanOrEqualTo(0d).Generate();
        MeasurementUnit anyUnit  = Any.MeasurementUnit().Generate();

        // Act
        Measurement measurement = new Measurement(anyValue, anyUnit);

        // Assert
        Assert.Equal(anyValue, measurement.Value);
    }

    [Fact]
    public void NaN_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Measurement(double.NaN, anyUnit));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinite_value_is_refused(double infinite) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Measurement(infinite, anyUnit));
    }

    [Fact]
    public void A_negative_value_is_refused() {
        // Arrange
        double          anyNegative = Any.Double().Negative().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(anyNegative, anyUnit));
    }

    [Fact]
    public void Negative_zero_is_accepted_as_zero() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Equal(0d, new Measurement(-0d, anyUnit).Value);
    }

    [Fact]
    public void A_missing_unit_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Measurement(1d, null!));
    }

    [Fact]
    public void The_value_is_checked_before_the_unit() {
        // Assert: NaN with no unit reports the NaN, not the missing unit
        Assert.Throws<ArgumentException>(() => new Measurement(double.NaN, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(-1d, null!));
    }

    [Fact]
    public void Undiluting_multiplies_the_value_by_the_dilution_and_keeps_the_unit() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();
        Measurement     diluted = new Measurement(2.5, anyUnit);
        DilutionFactor  tenfold = new DilutionFactor(10m);

        // Act
        Measurement undiluted = diluted.Undiluted(tenfold);

        // Assert
        Assert.Equal(25d, undiluted.Value);
        Assert.Equal(anyUnit, undiluted.Unit);
    }

    [Fact]
    public void Undiluting_returns_a_new_measurement_and_leaves_the_original_alone() {
        // Arrange
        Measurement    diluted = Any.Measurement().WithValue(v => v.Between(1d, 100d)).Generate();
        double         before  = diluted.Value;
        DilutionFactor anyDilution = Any.DilutionFactor().Generate();

        // Act
        Measurement undiluted = diluted.Undiluted(anyDilution);

        // Assert
        Assert.NotSame(diluted, undiluted);
        Assert.Equal(before, diluted.Value);
    }

    [Fact]
    public void A_neat_sample_is_unchanged_by_undiluting() {
        // Arrange
        Measurement diluted = Any.Measurement().WithValue(v => v.Between(0d, 1e6)).Generate();

        // Act
        Measurement undiluted = diluted.Undiluted(new DilutionFactor(1m));

        // Assert
        Assert.Equal(diluted.Value, undiluted.Value);
    }

    [Fact]
    public void Undiluting_a_value_that_overflows_is_refused() {
        // Arrange
        Measurement hugeMeasurement = new Measurement(double.MaxValue, Any.MeasurementUnit().Generate());

        // Assert
        Assert.Throws<ArgumentException>(() => hugeMeasurement.Undiluted(new DilutionFactor(2m)));
    }

    [Fact]
    public void Undiluting_without_a_dilution_is_refused() {
        // Arrange
        Measurement anyMeasurement = Any.Measurement().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyMeasurement.Undiluted(null!));
    }

    [Fact]
    public void A_measurement_reads_as_its_value_and_its_unit() {
        Assert.Equal("5.4 mmol/L", new Measurement(5.4, new MeasurementUnit("mmol/L")).ToString());
    }

}

public sealed class ReferenceRangeTests {

    // The range these tests are about: normal from 10 to 20, critical at or below 5 and at or above 30.
    private static ReferenceRange FullRange(MeasurementUnit unit) => new ReferenceRange(10d, 20d, unit, 5d, 30d);

    [Fact]
    public void A_range_keeps_what_it_was_given() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ReferenceRange range = FullRange(anyUnit);

        // Assert
        Assert.Equal(10d, range.Lower);
        Assert.Equal(20d, range.Upper);
        Assert.Same(anyUnit, range.Unit);
        Assert.Equal(5d, range.CriticalLow);
        Assert.Equal(30d, range.CriticalHigh);
    }

    [Fact]
    public void The_critical_limits_are_optional() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ReferenceRange range = new ReferenceRange(10d, 20d, anyUnit);

        // Assert
        Assert.Null(range.CriticalLow);
        Assert.Null(range.CriticalHigh);
    }

    [Fact]
    public void A_missing_unit_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ReferenceRange(10d, 20d, null!));
    }

    [Theory]
    [InlineData(double.NaN, 20d)]
    [InlineData(10d, double.NaN)]
    [InlineData(double.NegativeInfinity, 20d)]
    [InlineData(10d, double.PositiveInfinity)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    public void A_non_finite_bound_is_refused(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReferenceRange(lower, upper, anyUnit));
    }

    [Fact]
    public void A_negative_lower_bound_is_refused() {
        // Arrange
        double          anyNegative = Any.Double().Negative().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(anyNegative, 20d, anyUnit));
    }

    [Fact]
    public void A_lower_bound_of_zero_is_accepted() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Equal(0d, new ReferenceRange(0d, 1d, anyUnit).Lower);
    }

    [Theory]
    [InlineData(20d, 20d)]
    [InlineData(21d, 20d)]
    [InlineData(0d, 0d)]
    public void The_lower_bound_must_be_below_the_upper_bound(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReferenceRange(lower, upper, anyUnit));
    }

    [Fact]
    public void The_sign_of_the_lower_bound_is_checked_before_the_order_of_the_bounds() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert: both are wrong, the sign is reported
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(-1d, -2d, anyUnit));
    }

    [Fact]
    public void A_critical_low_above_the_lower_bound_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(10d, 20d, anyUnit, criticalLow: 10.5d));
    }

    [Fact]
    public void A_critical_high_below_the_upper_bound_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(10d, 20d, anyUnit, criticalHigh: 19.5d));
    }

    [Fact]
    public void A_critical_limit_equal_to_its_bound_is_accepted() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ReferenceRange range = new ReferenceRange(10d, 20d, anyUnit, criticalLow: 10d, criticalHigh: 20d);

        // Assert
        Assert.Equal(10d, range.CriticalLow);
        Assert.Equal(20d, range.CriticalHigh);
    }

    [Fact]
    public void The_critical_limits_are_not_required_to_be_non_negative() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.Equal(-3d, new ReferenceRange(10d, 20d, anyUnit, criticalLow: -3d).CriticalLow);
    }

    [Fact]
    public void The_order_of_the_bounds_is_checked_before_the_critical_limits() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert: bounds crossed AND a critical low above the lower bound: the bounds are reported
        Assert.Throws<ArgumentException>(() => new ReferenceRange(30d, 20d, anyUnit, criticalLow: 40d));
    }

    [Theory]
    [InlineData(2d,   ResultFlag.CriticalLow)]
    [InlineData(5d,   ResultFlag.CriticalLow)]   // a critical limit is inclusive
    [InlineData(5.1d, ResultFlag.Low)]
    [InlineData(9.9d, ResultFlag.Low)]
    [InlineData(10d,  ResultFlag.Normal)]        // the normal interval is inclusive
    [InlineData(15d,  ResultFlag.Normal)]
    [InlineData(20d,  ResultFlag.Normal)]
    [InlineData(20.1d, ResultFlag.High)]
    [InlineData(29.9d, ResultFlag.High)]
    [InlineData(30d,  ResultFlag.CriticalHigh)]
    [InlineData(1e9d, ResultFlag.CriticalHigh)]
    public void A_value_is_classified_against_the_normal_interval_and_the_critical_limits(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange range = FullRange(Any.MeasurementUnit().Generate());

        // Assert
        Assert.Equal(expected, range.Classify(value));
    }

    [Theory]
    [InlineData(0d,   ResultFlag.Low)]
    [InlineData(9.9d, ResultFlag.Low)]
    [InlineData(10d,  ResultFlag.Normal)]
    [InlineData(20d,  ResultFlag.Normal)]
    [InlineData(1e12d, ResultFlag.High)]
    public void Without_critical_limits_a_value_is_never_critical(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate());

        // Assert
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
    public void Only_the_critical_low_may_be_given() {
        // Arrange
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: 5d);

        // Assert
        Assert.Equal(ResultFlag.CriticalLow, range.Classify(5d));
        Assert.Equal(ResultFlag.High, range.Classify(1e9d));
    }

    [Fact]
    public void NaN_is_classified_as_normal() {
        // Arrange
        ReferenceRange range = FullRange(Any.MeasurementUnit().Generate());

        // Assert
        Assert.Equal(ResultFlag.Normal, range.Classify(double.NaN));
    }

    [Fact]
    public void A_drawn_range_is_valid_and_classifies_its_own_bounds() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().Generate();

        // Assert
        Assert.True(anyRange.Lower < anyRange.Upper);
        Assert.True(anyRange.CriticalLow is null or <= 0d);
        Assert.NotEqual(ResultFlag.High, anyRange.Classify(anyRange.Upper));
        Assert.NotEqual(ResultFlag.Low, anyRange.Classify(anyRange.Lower));
    }

}
