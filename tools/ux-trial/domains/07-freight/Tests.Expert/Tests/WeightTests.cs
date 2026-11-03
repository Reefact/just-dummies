using Freight.Domain;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class WeightTests {

    [Fact]
    public void The_heaviest_gross_weight_is_32_500_kilograms() {
        Assert.Equal(32_500d, Weight.MaxGrossKilograms);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(1)]
    [InlineData(32_499.99)]
    [InlineData(32_500)]
    public void A_weight_up_to_the_maximum_is_accepted(double kilograms) {
        Weight weight = new Weight(kilograms);

        Assert.Equal(kilograms, weight.Kilograms);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(-1)]
    [InlineData(32_500.01)]
    [InlineData(1_000_000)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void A_weight_outside_the_open_zero_closed_maximum_interval_is_refused(double kilograms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(kilograms));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_weight_that_is_not_a_finite_number_is_refused(double kilograms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(kilograms));
    }

    [Fact]
    public void A_negative_zero_is_not_a_weight() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(-0.0));
    }

    [Theory]
    [InlineData(12_500, 12.5)]
    [InlineData(1_000, 1)]
    [InlineData(500, 0.5)]
    [InlineData(32_500, 32.5)]
    public void Metric_tons_are_the_kilograms_over_one_thousand(double kilograms, double expectedMetricTons) {
        Assert.Equal(expectedMetricTons, new Weight(kilograms).MetricTons, precision: 9);
    }

    [Fact]
    public void Two_weights_of_the_same_kilograms_are_equal() {
        Weight first  = new Weight(12_500);
        Weight second = new Weight(12_500);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_weights_of_different_kilograms_are_not_equal() {
        Assert.NotEqual(new Weight(12_500), new Weight(12_500.5));
        Assert.False(new Weight(12_500).Equals(null));
    }

}
