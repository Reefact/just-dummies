namespace Laboratory.Tests;

public sealed class ReferenceRangeTests {

    // ── construction ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_range_exposes_what_it_was_built_from() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ReferenceRange range = new ReferenceRange(3.5, 5.1, anyUnit, criticalLow: 2.5, criticalHigh: 6.5);

        // Assert
        Assert.Equal(3.5, range.Lower);
        Assert.Equal(5.1, range.Upper);
        Assert.Equal(2.5, range.CriticalLow);
        Assert.Equal(6.5, range.CriticalHigh);
    }

    [Fact]
    public void The_critical_limits_are_optional() {
        // Act
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate());

        // Assert
        Assert.Null(range.CriticalLow);
        Assert.Null(range.CriticalHigh);
    }

    [Fact]
    public void A_range_may_start_at_zero() {
        Assert.Equal(0d, new ReferenceRange(0d, 1d, Any.MeasurementUnit().Generate()).Lower);
    }

    [Fact]
    public void A_range_without_a_unit_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReferenceRange(3.5, 5.1, null!));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void The_unit_is_judged_before_the_bounds() {
        Assert.Throws<ArgumentNullException>(() => new ReferenceRange(double.NaN, double.NaN, null!));
    }

    [Theory]
    [InlineData(double.NaN, 5.1)]
    [InlineData(3.5, double.NaN)]
    [InlineData(double.NegativeInfinity, 5.1)]
    [InlineData(3.5, double.PositiveInfinity)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    public void A_bound_that_is_not_a_finite_number_is_refused_as_malformed(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act / Assert: exactly ArgumentException, not the out-of-range kind
        Assert.Throws<ArgumentException>(() => new ReferenceRange(lower, upper, anyUnit));
    }

    [Theory]
    [InlineData(-0.001, 5.1)]
    [InlineData(-3.5, 5.1)]
    [InlineData(-3.5, -1.0)]     // negative AND consistent: still negative
    [InlineData(-2.0, -3.0)]     // negative AND inverted: the sign is judged first
    public void A_negative_lower_bound_is_refused_as_out_of_range(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(lower, upper, anyUnit));

        // Assert
        Assert.Equal("lower", refusal.ParamName);
    }

    [Theory]
    [InlineData(5.1, 5.1)]       // empty interval
    [InlineData(5.1, 3.5)]       // inverted
    [InlineData(0.0, 0.0)]
    public void A_lower_bound_that_is_not_below_the_upper_one_is_refused(double lower, double upper) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceRange(lower, upper, anyUnit));

        // Assert
        Assert.Equal("lower", refusal.ParamName);
    }

    [Fact]
    public void A_critical_low_above_the_lower_bound_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(3.5, 5.1, anyUnit, criticalLow: 3.6));

        // Assert
        Assert.Equal("criticalLow", refusal.ParamName);
    }

    [Fact]
    public void A_critical_high_below_the_upper_bound_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceRange(3.5, 5.1, anyUnit, criticalHigh: 5.0));

        // Assert
        Assert.Equal("criticalHigh", refusal.ParamName);
    }

    [Fact]
    public void A_critical_limit_may_coincide_with_its_bound() {
        // Act
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 3.5, criticalHigh: 5.1);

        // Assert
        Assert.Equal(3.5, range.CriticalLow);
        Assert.Equal(5.1, range.CriticalHigh);
    }

    [Fact]
    public void The_critical_limits_are_not_required_to_be_positive() {
        // The model checks the critical limits against the bounds only, not against zero.
        ReferenceRange range = new ReferenceRange(1d, 2d, Any.MeasurementUnit().Generate(), criticalLow: -5d);

        Assert.Equal(-5d, range.CriticalLow);
    }

    // ── classification ──────────────────────────────────────────────────────────────────────────
    // Potassium in mmol/L: normal 3.5 – 5.1, critical at 2.5 and below, 6.5 and above.

    [Theory]
    [InlineData(0.0, ResultFlag.CriticalLow)]
    [InlineData(2.4, ResultFlag.CriticalLow)]
    [InlineData(2.5, ResultFlag.CriticalLow)]      // the critical limit itself is critical
    [InlineData(2.6, ResultFlag.Low)]
    [InlineData(3.4, ResultFlag.Low)]
    [InlineData(3.5, ResultFlag.Normal)]           // the lower bound is normal
    [InlineData(4.0, ResultFlag.Normal)]
    [InlineData(5.1, ResultFlag.Normal)]           // the upper bound is normal
    [InlineData(5.2, ResultFlag.High)]
    [InlineData(6.4, ResultFlag.High)]
    [InlineData(6.5, ResultFlag.CriticalHigh)]     // the critical limit itself is critical
    [InlineData(6.6, ResultFlag.CriticalHigh)]
    [InlineData(1_000_000.0, ResultFlag.CriticalHigh)]
    public void A_value_is_classified_against_the_bounds_and_the_critical_limits(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange potassium = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);

        // Act
        ResultFlag flag = potassium.Classify(value);

        // Assert
        Assert.Equal(expected, flag);
    }

    [Theory]
    [InlineData(0.0, ResultFlag.Low)]
    [InlineData(3.4, ResultFlag.Low)]
    [InlineData(3.5, ResultFlag.Normal)]
    [InlineData(5.1, ResultFlag.Normal)]
    [InlineData(5.2, ResultFlag.High)]
    [InlineData(1_000_000.0, ResultFlag.High)]
    public void Without_critical_limits_a_value_is_never_critical(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate());

        // Act / Assert
        Assert.Equal(expected, range.Classify(value));
    }

    [Theory]
    [InlineData(2.0, ResultFlag.Low)]              // no critical low: low stays low
    [InlineData(7.0, ResultFlag.CriticalHigh)]
    public void Each_critical_limit_acts_on_its_own_side_only(double value, ResultFlag expected) {
        // Arrange
        ReferenceRange onlyCriticalHigh = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalHigh: 6.5);

        // Act / Assert
        Assert.Equal(expected, onlyCriticalHigh.Classify(value));
    }

    [Fact]
    public void A_critical_low_equal_to_the_lower_bound_makes_the_lower_bound_itself_critical() {
        // Arrange
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 3.5);

        // Act / Assert
        Assert.Equal(ResultFlag.CriticalLow, range.Classify(3.5));
        Assert.Equal(ResultFlag.Normal,      range.Classify(3.6));
    }

    [Fact]
    public void A_critical_high_equal_to_the_upper_bound_makes_the_upper_bound_itself_critical() {
        // Arrange
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalHigh: 5.1);

        // Act / Assert
        Assert.Equal(ResultFlag.CriticalHigh, range.Classify(5.1));
        Assert.Equal(ResultFlag.Normal,       range.Classify(5.0));
    }

    [Fact]
    public void A_NaN_value_is_neither_low_nor_high_so_it_is_classified_normal() {
        // Arrange: this documents the model's stated behaviour, not a recommendation
        ReferenceRange range = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);

        // Act / Assert
        Assert.Equal(ResultFlag.Normal, range.Classify(double.NaN));
    }

}
