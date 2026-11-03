namespace PassiveComponents.Tests.Regular;

public sealed class ToleranceTests {

    [Theory]
    [InlineData(0.1)]
    [InlineData(20.0)]
    public void Both_ends_of_the_range_are_accepted(double percent) {
        Tolerance tolerance = new Tolerance(percent);

        Assert.Equal(percent, tolerance.Percent);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.0999)]
    [InlineData(20.0001)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_percentage_outside_the_range_is_refused(double percent) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(percent));
    }

    [Fact]
    public void The_fraction_is_the_percentage_over_a_hundred() {
        Approx.Equal(0.05, new Tolerance(5).Fraction);
    }

    [Fact]
    public void A_five_percent_band_around_a_hundred_is_95_to_105() {
        Tolerance fivePercent = new Tolerance(5);

        Approx.Equal(95, fivePercent.LowerBound(100));
        Approx.Equal(105, fivePercent.UpperBound(100));
    }

    [Theory]
    [InlineData(ESeries.E6, 20.0)]
    [InlineData(ESeries.E12, 10.0)]
    [InlineData(ESeries.E24, 5.0)]
    [InlineData(ESeries.E96, 1.0)]
    public void Each_series_has_the_tolerance_it_is_built_for(ESeries series, double expectedPercent) {
        Assert.Equal(expectedPercent, Tolerance.ForSeries(series).Percent);
    }

    [Fact]
    public void An_unknown_series_has_no_tolerance() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tolerance.ForSeries((ESeries)99));
    }

    [Fact]
    public void A_tighter_tolerance_is_at_least_as_tight_as_a_looser_one() {
        Tolerance onePercent  = new Tolerance(1);
        Tolerance fivePercent = new Tolerance(5);

        Assert.True(onePercent.IsAtLeastAsTightAs(fivePercent));
        Assert.False(fivePercent.IsAtLeastAsTightAs(onePercent));
        Assert.True(fivePercent.IsAtLeastAsTightAs(new Tolerance(5)));
    }

    [Fact]
    public void Tightness_ignores_a_relative_difference_below_a_billionth() {
        Tolerance fivePercent        = new Tolerance(5);
        Tolerance fractionallyTighter = new Tolerance(5 * (1 + 5e-10));

        Assert.True(fractionallyTighter.IsAtLeastAsTightAs(fivePercent));
    }

    [Fact]
    public void Tightness_with_nothing_is_refused() {
        Tolerance anyTolerance = Any.Tolerance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyTolerance.IsAtLeastAsTightAs(null!));
    }

    [Fact]
    public void Equality_and_order_follow_the_percentage() {
        Tolerance anyTolerance = Any.Tolerance().Generate();

        Assert.Equal(anyTolerance, new Tolerance(anyTolerance.Percent));
        Assert.True(new Tolerance(1).CompareTo(new Tolerance(2)) < 0);
        Assert.True(new Tolerance(1).CompareTo(null) > 0);
    }

    [Fact]
    public void It_is_written_with_a_plus_minus_sign() {
        Assert.Equal("±5 %", new Tolerance(5).ToString());
        Assert.Equal("±0.1 %", new Tolerance(0.1).ToString());
    }

}
