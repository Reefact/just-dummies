using JustDummies;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

public sealed class ReferenceRangeTests {

    // Potassium, adult serum: the bounds are what these tests are about, so they stay literals.
    private const double PotassiumLower = 3.5;
    private const double PotassiumUpper = 5.1;

    [Fact]
    public void A_value_below_the_lower_bound_is_flagged_low() {
        // Arrange
        MeasurementUnit anyUnit  = new AnyMeasurementUnit().Generate();
        ReferenceRange  range    = new(PotassiumLower, PotassiumUpper, anyUnit);

        // Act
        ResultFlag flag = range.Classify(3.2);

        // Assert
        Assert.Equal(ResultFlag.Low, flag);
    }

    [Fact]
    public void Both_bounds_belong_to_the_normal_interval() {
        // Arrange
        ReferenceRange range = new(PotassiumLower, PotassiumUpper, new AnyMeasurementUnit().Generate());

        // Act
        ResultFlag atLower = range.Classify(PotassiumLower);
        ResultFlag atUpper = range.Classify(PotassiumUpper);

        // Assert
        Assert.Equal(ResultFlag.Normal, atLower);
        Assert.Equal(ResultFlag.Normal, atUpper);
    }

    [Fact]
    public void A_value_above_the_upper_bound_is_flagged_high() {
        // Arrange
        ReferenceRange range = new(PotassiumLower, PotassiumUpper, new AnyMeasurementUnit().Generate());

        // Act
        ResultFlag flag = range.Classify(5.4);

        // Assert
        Assert.Equal(ResultFlag.High, flag);
    }

    [Fact]
    public void A_value_at_the_critical_high_limit_is_critical_rather_than_merely_high() {
        // Arrange
        ReferenceRange range = new(PotassiumLower, PotassiumUpper, new AnyMeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);

        // Act
        ResultFlag flag = range.Classify(6.5);

        // Assert
        Assert.Equal(ResultFlag.CriticalHigh, flag);
    }

    [Fact]
    public void A_range_whose_bounds_cross_is_refused() {
        // Arrange
        MeasurementUnit anyUnit = new AnyMeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceRange(5.1, 3.5, anyUnit));

        // Assert
        Assert.Equal("lower", refusal.ParamName);
    }

}
