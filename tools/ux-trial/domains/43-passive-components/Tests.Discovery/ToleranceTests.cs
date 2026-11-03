using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class ToleranceTests {

    [Theory]
    [InlineData(0.1)]
    [InlineData(20)]
    [InlineData(5)]
    public void Accepts_a_percentage_within_the_closed_range(double percent) {
        Assert.Equal(percent, new Tolerance(percent).Percent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.09)]
    [InlineData(-5)]
    [InlineData(20.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_percentage_outside_the_range_or_not_a_number(double percent) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(percent));
    }

    [Fact]
    public void The_fraction_is_the_percentage_over_a_hundred() {
        Assert.Equal(0.05, new Tolerance(5).Fraction, 12);
    }

    [Theory]
    [InlineData(ESeries.E6, 20)]
    [InlineData(ESeries.E12, 10)]
    [InlineData(ESeries.E24, 5)]
    [InlineData(ESeries.E96, 1)]
    public void Each_series_has_the_tolerance_it_is_built_for(ESeries series, double expectedPercent) {
        Assert.Equal(expectedPercent, Tolerance.ForSeries(series).Percent);
    }

    [Fact]
    public void An_unknown_series_has_no_tolerance() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tolerance.ForSeries((ESeries)99));
    }

    [Fact]
    public void The_band_around_a_nominal_is_symmetric() {
        Tolerance fivePercent = new Tolerance(5);

        Assert.Equal(95, fivePercent.LowerBound(100), 9);
        Assert.Equal(105, fivePercent.UpperBound(100), 9);
    }

    [Fact]
    public void Is_at_least_as_tight_as_a_wider_or_equal_tolerance() {
        Tolerance onePercent  = new Tolerance(1);
        Tolerance fivePercent = new Tolerance(5);

        Assert.True(onePercent.IsAtLeastAsTightAs(fivePercent));
        Assert.True(fivePercent.IsAtLeastAsTightAs(new Tolerance(5)));
        Assert.False(fivePercent.IsAtLeastAsTightAs(onePercent));
    }

    [Fact]
    public void Tightness_is_relative_to_the_percentage() {
        Tolerance fivePercent = new Tolerance(5);

        Assert.True(new Tolerance(5 * (1 + 1e-10)).IsAtLeastAsTightAs(fivePercent));
    }

    [Fact]
    public void Tightness_refuses_null() {
        Tolerance anyTolerance = Any.Tolerance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyTolerance.IsAtLeastAsTightAs(null!));
    }

    [Fact]
    public void Equality_and_ordering_follow_the_percentage() {
        Assert.Equal(new Tolerance(5), new Tolerance(5));
        Assert.Equal(new Tolerance(5).GetHashCode(), new Tolerance(5).GetHashCode());
        Assert.NotEqual(new Tolerance(5), new Tolerance(10));
        Assert.True(new Tolerance(1).CompareTo(new Tolerance(5)) < 0);
        Assert.True(new Tolerance(1).CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(5, "±5 %")]
    [InlineData(0.1, "±0.1 %")]
    [InlineData(2.5, "±2.5 %")]
    public void Is_shown_as_a_signed_percentage(double percent, string expected) {
        Assert.Equal(expected, new Tolerance(percent).ToString());
    }

}
