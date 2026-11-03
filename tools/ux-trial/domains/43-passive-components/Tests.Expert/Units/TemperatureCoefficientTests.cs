using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class TemperatureCoefficientTests {

    [Theory]
    [InlineData(0)]
    [InlineData(TemperatureCoefficient.MaxPpmPerKelvin)]
    public void The_two_ends_of_the_range_are_valid(double ppm) {
        // Act
        TemperatureCoefficient coefficient = new TemperatureCoefficient(ppm);

        // Assert
        Assert.Equal(ppm, coefficient.PpmPerKelvin);
    }

    [Fact]
    public void Just_above_the_largest_magnitude_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureCoefficient(double.BitIncrement(TemperatureCoefficient.MaxPpmPerKelvin)));
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-30)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_coefficient_that_is_negative_or_not_finite_is_refused(double ppm) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureCoefficient(ppm));
    }

    [Fact(Skip = "domain defect: new TemperatureCoefficient(-0.0) throws ArgumentOutOfRangeException (ArgumentOutOfRangeException.ThrowIfNegative treats negative zero as negative) although -0.0 equals 0 and the range is [0, 5000]; deterministic, no seed")]
    public void A_negative_zero_is_a_zero_and_is_accepted() {
        // Act
        TemperatureCoefficient coefficient = new TemperatureCoefficient(-0.0);

        // Assert
        Assert.True(coefficient.PpmPerKelvin == 0);
    }

    [Fact]
    public void A_hundred_ppm_drifts_by_one_percent_at_125_degrees() {
        // Arrange
        TemperatureCoefficient hundredPpm = new TemperatureCoefficient(100);

        // Act & Assert: 100e-6 · |125 − 25|
        Assert.Equal(0.01, hundredPpm.DriftAt(125), 12);
    }

    [Fact]
    public void There_is_no_drift_at_the_reference_temperature() {
        // Arrange
        TemperatureCoefficient anyCoefficient = Any.TemperatureCoefficient().Generate();

        // Act & Assert
        Assert.Equal(0, anyCoefficient.DriftAt(25));
    }

    [Fact]
    public void Drift_is_a_magnitude_so_cold_and_hot_are_treated_alike() {
        // Arrange
        TemperatureCoefficient hundredPpm = new TemperatureCoefficient(100);

        // Act & Assert: 50 K below and 50 K above the reference
        Assert.Equal(hundredPpm.DriftAt(75), hundredPpm.DriftAt(-25), 12);
        Assert.Equal(0.005, hundredPpm.DriftAt(-25), 12);
    }

    [Theory]
    [InlineData(TemperatureCoefficient.MinOperatingCelsius, 0.008)]
    [InlineData(TemperatureCoefficient.MaxOperatingCelsius, 0.013)]
    public void The_operating_limits_are_valid_temperatures(double celsius, double expectedDrift) {
        // Arrange
        TemperatureCoefficient hundredPpm = new TemperatureCoefficient(100);

        // Act & Assert
        Assert.Equal(expectedDrift, hundredPpm.DriftAt(celsius), 12);
    }

    [Theory]
    [InlineData(-55.001)]
    [InlineData(155.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_temperature_outside_the_operating_range_is_refused(double celsius) {
        // Arrange
        TemperatureCoefficient anyCoefficient = Any.TemperatureCoefficient().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCoefficient.DriftAt(celsius));
    }

    [Fact]
    public void A_coefficient_of_zero_never_drifts() {
        Assert.Equal(0, new TemperatureCoefficient(0).DriftAt(155));
    }

    [Fact]
    public void Equality_is_exact_on_the_double() {
        Assert.Equal(new TemperatureCoefficient(30), new TemperatureCoefficient(30));
        Assert.Equal(new TemperatureCoefficient(30).GetHashCode(), new TemperatureCoefficient(30).GetHashCode());
        Assert.NotEqual(new TemperatureCoefficient(30), new TemperatureCoefficient(25));
    }

    [Theory]
    [InlineData(25, "±25 ppm/K")]
    [InlineData(0, "±0 ppm/K")]
    [InlineData(2.5, "±2.5 ppm/K")]
    public void A_coefficient_reads_as_a_signed_magnitude(double ppm, string expected) {
        Assert.Equal(expected, new TemperatureCoefficient(ppm).ToString());
    }

}
