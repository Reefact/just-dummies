namespace PassiveComponents.Tests.Regular;

public sealed class TemperatureCoefficientTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(5000.0)]
    public void Both_ends_of_the_range_are_accepted(double ppm) {
        TemperatureCoefficient coefficient = new TemperatureCoefficient(ppm);

        Assert.Equal(ppm, coefficient.PpmPerKelvin);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5000.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_coefficient_outside_the_range_or_not_finite_is_refused(double ppm) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureCoefficient(ppm));
    }

    [Fact]
    public void There_is_no_drift_at_the_reference_temperature() {
        TemperatureCoefficient coefficient = new TemperatureCoefficient(100);

        Assert.Equal(0.0, coefficient.DriftAt(25));
    }

    [Fact]
    public void A_hundred_ppm_drifts_one_percent_over_a_hundred_kelvin() {
        TemperatureCoefficient coefficient = new TemperatureCoefficient(100);

        Approx.Equal(0.01, coefficient.DriftAt(125));
        Approx.Equal(0.008, coefficient.DriftAt(-55));
    }

    [Theory]
    [InlineData(-55.0)]
    [InlineData(155.0)]
    public void Both_ends_of_the_operating_range_are_accepted(double celsius) {
        TemperatureCoefficient coefficient = new TemperatureCoefficient(50);

        Approx.Equal(50e-6 * Math.Abs(celsius - 25), coefficient.DriftAt(celsius));
    }

    [Theory]
    [InlineData(-55.1)]
    [InlineData(155.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_temperature_outside_the_operating_range_is_refused(double celsius) {
        TemperatureCoefficient anyCoefficient = Any.TemperatureCoefficient().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCoefficient.DriftAt(celsius));
    }

    [Fact]
    public void Equality_follows_the_value() {
        TemperatureCoefficient anyCoefficient = Any.TemperatureCoefficient().Generate();

        Assert.Equal(anyCoefficient, new TemperatureCoefficient(anyCoefficient.PpmPerKelvin));
        Assert.NotEqual(new TemperatureCoefficient(25), new TemperatureCoefficient(30));
    }

    [Fact]
    public void It_is_written_in_ppm_per_kelvin() {
        Assert.Equal("±25 ppm/K", new TemperatureCoefficient(25).ToString());
        Assert.Equal("±0.5 ppm/K", new TemperatureCoefficient(0.5).ToString());
    }

}
