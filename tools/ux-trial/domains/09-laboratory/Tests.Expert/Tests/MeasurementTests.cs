namespace Laboratory.Tests;

public sealed class MeasurementTests {

    [Fact]
    public void A_measurement_exposes_its_value_and_its_unit() {
        // Arrange
        MeasurementUnit millimolesPerLitre = new MeasurementUnit("mmol/L");

        // Act
        Measurement measurement = new Measurement(4.2, millimolesPerLitre);

        // Assert
        Assert.Equal(4.2, measurement.Value);
        Assert.Same(millimolesPerLitre, measurement.Unit);
        Assert.Equal("4.2 mmol/L", measurement.ToString());
    }

    [Fact]
    public void Zero_is_a_measurement() {
        // An absent analyte reads zero; it is not an error.
        Measurement measurement = new Measurement(0d, Any.MeasurementUnit().Generate());

        Assert.Equal(0d, measurement.Value);
    }

    [Fact]
    public void NaN_is_refused_as_malformed() {
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
    public void An_infinity_is_refused_as_malformed_not_as_out_of_range(double infinity) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Measurement(infinity, anyUnit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.000001)]
    [InlineData(-1d)]
    [InlineData(double.MinValue)]
    public void A_negative_value_is_refused_as_out_of_range(double negative) {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(negative, anyUnit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_missing_unit_is_refused() {
        // Arrange
        double anyValue = Any.Double().Between(0d, 100_000d).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Measurement(anyValue, null!));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void The_value_is_judged_before_the_unit() {
        // Both are wrong: the value is the one reported.
        Assert.Throws<ArgumentException>(() => new Measurement(double.NaN, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(-1d, null!));
    }

    [Fact]
    public void Undiluting_multiplies_the_value_by_the_dilution() {
        // Arrange
        Measurement    measured = Any.Measurement().WithValue(2.5).Generate();
        DilutionFactor tenfold  = new DilutionFactor(10m);

        // Act
        Measurement undiluted = measured.Undiluted(tenfold);

        // Assert
        Assert.Equal(25d, undiluted.Value);
    }

    [Fact]
    public void Undiluting_keeps_the_unit() {
        // Arrange
        MeasurementUnit grammesPerDecilitre = new MeasurementUnit("g/dL");
        Measurement     measured            = new Measurement(2.5, grammesPerDecilitre);

        // Act
        Measurement undiluted = measured.Undiluted(Any.DilutionFactor().Generate());

        // Assert
        Assert.Equal(grammesPerDecilitre, undiluted.Unit);
    }

    [Fact]
    public void Undiluting_by_a_neat_sample_changes_nothing() {
        // Arrange
        Measurement measured = Any.Measurement().WithValue(7.5).Generate();

        // Act
        Measurement undiluted = measured.Undiluted(new DilutionFactor(1m));

        // Assert
        Assert.Equal(7.5, undiluted.Value);
    }

    [Fact]
    public void Undiluting_leaves_the_measurement_it_was_called_on_untouched() {
        // Arrange
        Measurement measured = Any.Measurement().WithValue(2.5).Generate();

        // Act
        Measurement undiluted = measured.Undiluted(new DilutionFactor(4m));

        // Assert
        Assert.NotSame(measured, undiluted);
        Assert.Equal(2.5, measured.Value);
    }

    [Fact]
    public void Undiluting_without_a_dilution_is_refused() {
        // Arrange
        Measurement anyMeasurement = Any.Measurement().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyMeasurement.Undiluted(null!));

        // Assert
        Assert.Equal("dilution", refusal.ParamName);
    }

    [Fact]
    public void Undiluting_past_the_largest_double_is_refused_rather_than_stored_as_infinity() {
        // Arrange
        Measurement hugeMeasurement = Any.Measurement().WithValue(double.MaxValue).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => hugeMeasurement.Undiluted(new DilutionFactor(2m)));
    }

}
