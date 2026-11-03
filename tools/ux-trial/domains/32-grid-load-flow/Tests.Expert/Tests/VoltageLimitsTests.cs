using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class VoltageLimitsTests {

    [Theory]
    [InlineData(0.5, 1.0 + 1e-12)]
    [InlineData(1.0 - 1e-12, 1.5)]
    [InlineData(0.95, 1.05)]
    [InlineData(1.0, 1.0000001)]   // a band may start or end on 1.0 pu, not collapse on it
    [InlineData(0.9999999, 1.0)]
    public void A_band_holding_nominal_voltage_is_accepted(double minimum, double maximum) {
        VoltageLimits limits = new VoltageLimits(minimum, maximum);

        Assert.Equal(minimum, limits.MinimumPu);
        Assert.Equal(maximum, limits.MaximumPu);
    }

    [Theory]
    [InlineData(0.4999)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_minimum_outside_half_to_one_is_refused(double minimum) {
        double anyMaximum = Any.Double().Between(1.01d, VoltageLimits.AbsoluteMaximumPu).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoltageLimits(minimum, anyMaximum));

        Assert.Equal("minimumPu", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.9999)]
    [InlineData(1.5001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_maximum_outside_one_to_one_and_a_half_is_refused(double maximum) {
        double anyMinimum = Any.Double().Between(VoltageLimits.AbsoluteMinimumPu, 0.99d).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoltageLimits(anyMinimum, maximum));

        Assert.Equal("maximumPu", refusal.ParamName);
    }

    [Fact]
    public void A_band_collapsed_on_nominal_voltage_is_refused() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VoltageLimits(1.0, 1.0));

        Assert.Equal("minimumPu", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(0.95, true)]    // the floor belongs to the band
    [InlineData(1.05, true)]    // so does the ceiling
    [InlineData(1.0, true)]
    [InlineData(0.9499999, false)]
    [InlineData(1.0500001, false)]
    [InlineData(double.NaN, false)]
    public void A_voltage_is_contained_when_it_lies_between_the_limits_inclusive(double magnitude, bool expected) {
        VoltageLimits limits = new VoltageLimits(0.95, 1.05);

        Assert.Equal(expected, limits.Contains(magnitude));
    }

    [Fact]
    public void The_standard_band_is_ninety_five_to_one_oh_five_percent() {
        Assert.Equal(new VoltageLimits(0.95, 1.05), VoltageLimits.Standard);
    }

    [Fact]
    public void Bands_with_the_same_bounds_are_equal_and_others_are_not() {
        VoltageLimits reference = new VoltageLimits(0.9, 1.1);

        Assert.Equal(reference, new VoltageLimits(0.9, 1.1));
        Assert.Equal(reference.GetHashCode(), new VoltageLimits(0.9, 1.1).GetHashCode());
        Assert.NotEqual(reference, new VoltageLimits(0.9, 1.2));
        Assert.NotEqual(reference, new VoltageLimits(0.8, 1.1));
    }

}
