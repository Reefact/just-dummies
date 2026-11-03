using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class TemperatureCoefficientTests {

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    [InlineData(25)]
    public void Accepts_a_magnitude_within_the_closed_range(double ppm) {
        Assert.Equal(ppm, new TemperatureCoefficient(ppm).PpmPerKelvin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.001)]
    [InlineData(5000.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_magnitude_outside_the_range_or_not_finite(double ppm) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureCoefficient(ppm));
    }

    [Theory]
    [InlineData(25, 0)]
    [InlineData(125, 0.01)]
    [InlineData(-55, 0.008)]
    [InlineData(155, 0.013)]
    public void Drift_is_the_coefficient_times_the_distance_from_25_degrees(double celsius, double expectedDrift) {
        TemperatureCoefficient hundredPpm = new TemperatureCoefficient(100);

        Assert.Equal(expectedDrift, hundredPpm.DriftAt(celsius), 12);
    }

    [Theory]
    [InlineData(-55.1)]
    [InlineData(155.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Drift_refuses_a_temperature_outside_the_operating_range(double celsius) {
        TemperatureCoefficient anyCoefficient = Any.TemperatureCoefficient().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCoefficient.DriftAt(celsius));
    }

    [Fact]
    public void A_zero_coefficient_never_drifts() {
        TemperatureCoefficient none = new TemperatureCoefficient(0);

        Assert.Equal(0, none.DriftAt(155));
    }

    [Fact]
    public void Equality_follows_the_magnitude() {
        Assert.Equal(new TemperatureCoefficient(30), new TemperatureCoefficient(30));
        Assert.Equal(new TemperatureCoefficient(30).GetHashCode(), new TemperatureCoefficient(30).GetHashCode());
        Assert.NotEqual(new TemperatureCoefficient(30), new TemperatureCoefficient(100));
    }

    [Theory]
    [InlineData(30, "±30 ppm/K")]
    [InlineData(0.5, "±0.5 ppm/K")]
    public void Is_shown_with_its_unit(double ppm, string expected) {
        Assert.Equal(expected, new TemperatureCoefficient(ppm).ToString());
    }

}
