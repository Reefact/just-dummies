using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class VoltageLimitsTests {

    [Theory]
    [InlineData(0.5, 1.5)]
    [InlineData(1.0, 1.1)]
    [InlineData(0.9, 1.0)]
    public void Accepts_a_band_that_holds_one_per_unit(double minimumPu, double maximumPu) {
        // Act
        VoltageLimits limits = new VoltageLimits(minimumPu, maximumPu);

        // Assert
        Assert.Equal(minimumPu, limits.MinimumPu);
        Assert.Equal(maximumPu, limits.MaximumPu);
    }

    [Theory]
    [InlineData(0.499)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_minimum_outside_half_to_one(double minimumPu) {
        // Arrange
        double anyMaximumPu = Any.Double().Between(1.0, VoltageLimits.AbsoluteMaximumPu).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new VoltageLimits(minimumPu, anyMaximumPu));

        // Assert
        Assert.Equal("minimumPu", exception.ParamName);
    }

    [Theory]
    [InlineData(0.999)]
    [InlineData(1.501)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_maximum_outside_one_to_one_and_a_half(double maximumPu) {
        // Arrange
        double anyMinimumPu = Any.Double().Between(VoltageLimits.AbsoluteMinimumPu, 0.99).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new VoltageLimits(anyMinimumPu, maximumPu));

        // Assert
        Assert.Equal("maximumPu", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_band_that_collapses_to_the_nominal_voltage() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new VoltageLimits(1.0, 1.0));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("minimumPu", exception.ParamName);
    }

    [Fact]
    public void Contains_a_voltage_inside_the_band_and_both_of_its_edges() {
        // Arrange
        VoltageLimits anyLimits = Any.VoltageLimits().Generate();

        // Assert
        Assert.True(anyLimits.Contains(1.0));
        Assert.True(anyLimits.Contains(anyLimits.MinimumPu));
        Assert.True(anyLimits.Contains(anyLimits.MaximumPu));
    }

    [Fact]
    public void Does_not_contain_a_voltage_just_outside_the_band() {
        // Arrange
        VoltageLimits limits = new VoltageLimits(0.95, 1.05);

        // Assert
        Assert.False(limits.Contains(0.9499));
        Assert.False(limits.Contains(1.0501));
        Assert.False(limits.Contains(double.NaN));
    }

    [Fact]
    public void The_standard_band_is_ninety_five_to_one_oh_five_percent() {
        Assert.Equal(0.95, VoltageLimits.Standard.MinimumPu);
        Assert.Equal(1.05, VoltageLimits.Standard.MaximumPu);
    }

    [Fact]
    public void Two_bands_with_the_same_edges_are_equal() {
        // Arrange
        VoltageLimits anyLimits = Any.VoltageLimits().Generate();
        VoltageLimits copy      = new VoltageLimits(anyLimits.MinimumPu, anyLimits.MaximumPu);

        // Assert
        Assert.Equal(anyLimits, copy);
        Assert.Equal(anyLimits.GetHashCode(), copy.GetHashCode());
        Assert.False(anyLimits.Equals(null));
    }

    [Fact]
    public void Bands_differing_by_either_edge_are_different() {
        // Arrange
        VoltageLimits reference = new VoltageLimits(0.9, 1.1);

        // Assert
        Assert.NotEqual(reference, new VoltageLimits(0.95, 1.1));
        Assert.NotEqual(reference, new VoltageLimits(0.9, 1.05));
    }

    [Fact]
    public void Prints_its_edges() {
        Assert.Equal("[0.95, 1.05] pu", VoltageLimits.Standard.ToString());
    }

}
