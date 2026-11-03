using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class ToleranceTests {

    [Theory]
    [InlineData(Tolerance.MinPercent)]
    [InlineData(Tolerance.MaxPercent)]
    public void The_two_ends_of_the_range_are_valid(double percent) {
        // Act
        Tolerance tolerance = new Tolerance(percent);

        // Assert
        Assert.Equal(percent, tolerance.Percent);
    }

    [Fact]
    public void Just_outside_the_range_is_refused_on_both_sides() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(double.BitDecrement(Tolerance.MinPercent)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(double.BitIncrement(Tolerance.MaxPercent)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_tolerance_that_is_not_in_the_range_is_refused(double percent) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tolerance(percent));
    }

    [Fact]
    public void The_fraction_is_the_percentage_divided_by_one_hundred() {
        Assert.Equal(0.05, new Tolerance(5).Fraction, 12);
    }

    [Fact]
    public void A_five_percent_tolerance_spreads_a_hundred_ohm_nominal_between_95_and_105() {
        // Arrange
        Tolerance fivePercent = new Tolerance(5);

        // Act & Assert
        Assert.Equal(95, fivePercent.LowerBound(100), 9);
        Assert.Equal(105, fivePercent.UpperBound(100), 9);
    }

    [Fact]
    public void The_bounds_scale_with_the_nominal_at_any_magnitude() {
        // Arrange
        Tolerance tenPercent = new Tolerance(10);

        // Act & Assert: 2.2 nF
        Assert.Equal(1.98e-9, tenPercent.LowerBound(2.2e-9), 1e-18);
        Assert.Equal(2.42e-9, tenPercent.UpperBound(2.2e-9), 1e-18);
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
    public void An_undefined_series_has_no_tolerance() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tolerance.ForSeries((ESeries)99));
    }

    [Fact]
    public void A_one_percent_tolerance_is_at_least_as_tight_as_five_percent_but_not_the_reverse() {
        // Arrange
        Tolerance onePercent  = new Tolerance(1);
        Tolerance fivePercent = new Tolerance(5);

        // Act & Assert
        Assert.True(onePercent.IsAtLeastAsTightAs(fivePercent));
        Assert.False(fivePercent.IsAtLeastAsTightAs(onePercent));
    }

    [Fact]
    public void A_tolerance_is_at_least_as_tight_as_itself() {
        Assert.True(new Tolerance(5).IsAtLeastAsTightAs(new Tolerance(5)));
    }

    [Fact]
    public void Tightness_is_judged_with_a_relative_slack_of_a_billionth() {
        // Arrange
        Tolerance fivePercent = new Tolerance(5);

        // Act & Assert
        Assert.True(new Tolerance(5 * (1 + 5e-10)).IsAtLeastAsTightAs(fivePercent));
        Assert.False(new Tolerance(5 * (1 + 3e-9)).IsAtLeastAsTightAs(fivePercent));
    }

    [Fact]
    public void A_tolerance_cannot_be_compared_with_nothing() {
        // Arrange
        Tolerance anyTolerance = Any.Tolerance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyTolerance.IsAtLeastAsTightAs(null!));
    }

    [Fact]
    public void Equality_is_exact_and_ordering_follows_the_percentage() {
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
    public void A_tolerance_reads_as_a_signed_percentage(double percent, string expected) {
        Assert.Equal(expected, new Tolerance(percent).ToString());
    }

}
