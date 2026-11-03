using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class WeightTests {

    [Fact]
    public void Metric_tons_are_the_kilograms_divided_by_one_thousand() {
        // Act
        Weight weight = new Weight(12_500);

        // Assert
        Assert.Equal(12.5, weight.MetricTons);
        Assert.Equal(12_500, weight.Kilograms);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(32_500)]   // the maximum is accepted
    public void A_weight_on_the_edge_of_the_range_is_accepted(double kilograms) {
        Assert.Equal(kilograms, new Weight(kilograms).Kilograms);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32_500.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_weight_outside_the_range_or_not_finite_is_refused(double kilograms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(kilograms));
    }

    [Fact]
    public void Two_weights_of_the_same_kilograms_are_equal() {
        // Arrange
        Weight anyWeight = Any.Weight().Generate();

        // Act
        Weight sameWeight = new Weight(anyWeight.Kilograms);

        // Assert
        Assert.Equal(anyWeight, sameWeight);
        Assert.Equal(anyWeight.GetHashCode(), sameWeight.GetHashCode());
    }

    [Fact]
    public void Two_weights_of_different_kilograms_are_not_equal() {
        Assert.NotEqual(new Weight(100), new Weight(101));
    }

}

public sealed class VolumeTests {

    [Theory]
    [InlineData(0.001)]
    [InlineData(76.4)]   // the maximum is accepted
    public void A_volume_on_the_edge_of_the_range_is_accepted(double cubicMetres) {
        Assert.Equal(cubicMetres, new Volume(cubicMetres).CubicMetres);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(76.41)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_volume_outside_the_range_or_not_a_number_is_refused(double cubicMetres) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(cubicMetres));
    }

    [Fact]
    public void Two_volumes_of_the_same_cubic_metres_are_equal() {
        // Arrange
        Volume anyVolume = Any.Volume().Generate();

        // Act
        Volume sameVolume = new Volume(anyVolume.CubicMetres);

        // Assert
        Assert.Equal(anyVolume, sameVolume);
        Assert.Equal(anyVolume.GetHashCode(), sameVolume.GetHashCode());
    }

    [Fact]
    public void Two_volumes_of_different_cubic_metres_are_not_equal() {
        Assert.NotEqual(new Volume(10), new Volume(10.5));
    }

}
