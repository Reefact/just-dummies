using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class MeasurementTests {

    [Fact]
    public void A_non_negative_finite_value_is_accepted_and_kept_with_its_unit() {
        // Arrange
        double          anyValue = Any.Double().GreaterThanOrEqualTo(0d).Generate();
        MeasurementUnit anyUnit  = Any.MeasurementUnit().Generate();

        // Act
        Measurement measurement = new Measurement(anyValue, anyUnit);

        // Assert
        Assert.Equal(anyValue, measurement.Value);
        Assert.Same(anyUnit, measurement.Unit);
    }

    [Fact]
    public void Zero_is_a_valid_measurement() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        Measurement measurement = new Measurement(0d, anyUnit);

        // Assert
        Assert.Equal(0d, measurement.Value);
    }

    [Fact]
    public void The_largest_finite_value_is_accepted() {
        Assert.Equal(double.MaxValue, new Measurement(double.MaxValue, Any.MeasurementUnit().Generate()).Value);
    }

    [Fact]
    public void A_negative_value_is_refused_as_out_of_range() {
        // Arrange
        double          anyNegative = Any.Double().Negative().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(anyNegative, anyUnit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void NaN_is_refused_as_an_argument_error() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Measurement(double.NaN, anyUnit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinite_value_is_refused_as_an_argument_error_not_as_out_of_range(double infinite) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Measurement(infinite, anyUnit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_missing_unit_is_refused_as_a_null_argument() {
        // Arrange
        double anyValue = Any.Double().GreaterThanOrEqualTo(0d).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Measurement(anyValue, null!));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void The_value_is_judged_before_the_unit() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Measurement(double.NaN, null!));
    }

    [Fact]
    public void It_prints_its_value_and_its_unit() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        string text = new Measurement(5d, anyUnit).ToString();

        // Assert
        Assert.Equal($"5 {anyUnit.Symbol}", text);
    }

    [Fact]
    public void Undiluting_multiplies_the_value_by_the_dilution_and_keeps_the_unit() {
        // Arrange
        MeasurementUnit anyUnit      = Any.MeasurementUnit().Generate();
        Measurement     measurement  = new Measurement(2.5d, anyUnit);
        DilutionFactor  fourfold     = new DilutionFactor(4m);

        // Act
        Measurement undiluted = measurement.Undiluted(fourfold);

        // Assert
        Assert.Equal(10d, undiluted.Value);
        Assert.Equal(anyUnit, undiluted.Unit);
    }

    [Fact]
    public void Undiluting_by_a_neat_sample_changes_nothing() {
        // Arrange
        Measurement anyMeasurement = Any.Measurement().WithValue(v => v.LessThanOrEqualTo(1_000_000d)).Generate();

        // Act
        Measurement undiluted = anyMeasurement.Undiluted(new DilutionFactor(1m));

        // Assert
        Assert.Equal(anyMeasurement.Value, undiluted.Value);
        Assert.Equal(anyMeasurement.Unit, undiluted.Unit);
    }

    [Fact]
    public void Undiluting_never_gives_less_than_the_measured_value() {
        // Arrange
        Measurement    anyMeasurement = Any.Measurement().WithValue(v => v.LessThanOrEqualTo(1_000_000d)).Generate();
        DilutionFactor anyDilution    = Any.DilutionFactor().Generate();

        // Act
        Measurement undiluted = anyMeasurement.Undiluted(anyDilution);

        // Assert
        Assert.True(undiluted.Value >= anyMeasurement.Value);
    }

    [Fact]
    public void Undiluting_leaves_the_measurement_itself_untouched() {
        // Arrange
        Measurement anyMeasurement = Any.Measurement().WithValue(v => v.LessThanOrEqualTo(1_000_000d)).Generate();
        double      before         = anyMeasurement.Value;

        // Act
        Measurement undiluted = anyMeasurement.Undiluted(Any.DilutionFactor().Generate());

        // Assert
        Assert.NotSame(anyMeasurement, undiluted);
        Assert.Equal(before, anyMeasurement.Value);
    }

    [Fact]
    public void Undiluting_by_a_missing_dilution_is_refused_as_a_null_argument() {
        // Arrange
        Measurement anyMeasurement = Any.Measurement().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyMeasurement.Undiluted(null!));

        // Assert
        Assert.Equal("dilution", refusal.ParamName);
    }

    [Fact]
    public void Undiluting_past_the_largest_double_is_refused_as_an_argument_error() {
        // Arrange
        Measurement hugeMeasurement = new Measurement(double.MaxValue, Any.MeasurementUnit().Generate());
        DilutionFactor anyDilution  = Any.DilutionFactor().WithValue(v => v.GreaterThanOrEqualTo(2m)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => hugeMeasurement.Undiluted(anyDilution));
    }

}
