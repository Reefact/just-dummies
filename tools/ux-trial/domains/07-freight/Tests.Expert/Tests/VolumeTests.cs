using Freight.Domain;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class VolumeTests {

    [Fact]
    public void The_largest_volume_is_that_of_a_forty_foot_high_cube() {
        Assert.Equal(76.4, Volume.MaxCubicMetres);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(33.2)]
    [InlineData(76.39)]
    [InlineData(76.4)]
    public void A_volume_up_to_the_maximum_is_accepted(double cubicMetres) {
        Volume volume = new Volume(cubicMetres);

        Assert.Equal(cubicMetres, volume.CubicMetres);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(-76.4)]
    [InlineData(76.41)]
    [InlineData(1_000)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_volume_outside_the_open_zero_closed_maximum_interval_is_refused(double cubicMetres) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(cubicMetres));
    }

    [Fact]
    public void A_volume_that_is_not_a_number_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(double.NaN));

        Assert.Equal("cubicMetres", refusal.ParamName);
    }

    [Fact]
    public void Two_volumes_of_the_same_cubic_metres_are_equal() {
        Volume first  = new Volume(33.2);
        Volume second = new Volume(33.2);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal("33.2 m3", first.ToString());
    }

    [Fact]
    public void Two_volumes_of_different_cubic_metres_are_not_equal() {
        Assert.NotEqual(new Volume(33.2), new Volume(33.3));
        Assert.False(new Volume(33.2).Equals(null));
    }

}
