using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class PreferredValueTests {

    [Theory]
    [InlineData(ESeries.E6, 6)]
    [InlineData(ESeries.E12, 12)]
    [InlineData(ESeries.E24, 24)]
    [InlineData(ESeries.E96, 96)]
    public void A_series_has_its_number_of_values_per_decade(ESeries series, int expected) {
        Assert.Equal(expected, PreferredValue.ValuesPerDecade(series));
        Assert.Equal(expected, PreferredValue.Mantissas(series).Count);
    }

    [Fact]
    public void The_mantissas_of_a_series_run_from_one_up_to_below_ten() {
        Assert.Equal(1.0, PreferredValue.Mantissas(ESeries.E6)[0]);
        Assert.Equal(6.8, PreferredValue.Mantissas(ESeries.E6)[5]);
        Assert.Equal(1.00, PreferredValue.Mantissas(ESeries.E96)[0]);
        Assert.Equal(9.76, PreferredValue.Mantissas(ESeries.E96)[95]);
        Assert.Equal(2.2, PreferredValue.Mantissas(ESeries.E24)[8]);
    }

    [Fact]
    public void Exposes_its_parts_mantissa_and_value() {
        PreferredValue twoPointTwoNano = new PreferredValue(ESeries.E24, -9, 8);

        Assert.Equal(ESeries.E24, twoPointTwoNano.Series);
        Assert.Equal(-9, twoPointTwoNano.Decade);
        Assert.Equal(8, twoPointTwoNano.Index);
        Assert.Equal(2.2, twoPointTwoNano.Mantissa);
        Assert.Equal(2.2e-9, twoPointTwoNano.Value);
    }

    [Fact]
    public void An_e96_value_carries_three_significant_digits() {
        PreferredValue fourPointNineNine = new PreferredValue(ESeries.E96, 0, 67);

        Assert.Equal(4.99, fourPointNineNine.Mantissa);
        Assert.Equal(4.99, fourPointNineNine.Value);
    }

    [Theory]
    [InlineData(-14)]
    [InlineData(13)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Refuses_a_decade_outside_minus_13_to_12(int decade) {
        ESeries anySeries = Any.Enum<ESeries>().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(anySeries, decade, 0));
    }

    [Theory]
    [InlineData(ESeries.E6, 6)]
    [InlineData(ESeries.E12, 12)]
    [InlineData(ESeries.E24, 24)]
    [InlineData(ESeries.E96, 96)]
    public void Refuses_an_index_beyond_the_values_per_decade_of_its_series(ESeries series, int firstRefusedIndex) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, 0, firstRefusedIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, 0, -1));
        Assert.Equal(firstRefusedIndex - 1, new PreferredValue(series, 0, firstRefusedIndex - 1).Index);
    }

    [Fact]
    public void Refuses_an_unknown_series() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue((ESeries)99, 0, 0));
    }

    [Fact]
    public void The_bounds_of_the_decades_are_accepted() {
        Assert.Equal(1e-13, new PreferredValue(ESeries.E24, -13, 0).Value);
        Assert.Equal(9.76e12, new PreferredValue(ESeries.E96, 12, 95).Value);
    }

    [Fact]
    public void Nearest_snaps_9_9_kilo_to_ten_kilo_in_e12() {
        PreferredValue nearest = PreferredValue.Nearest(9.9e3, ESeries.E12);

        Assert.Equal(new PreferredValue(ESeries.E12, 4, 0), nearest);
    }

    [Theory]
    [InlineData(2.29e-9, ESeries.E24, -9, 8)]
    [InlineData(2.31e-9, ESeries.E24, -9, 9)]
    [InlineData(4.8e3, ESeries.E12, 3, 8)]
    [InlineData(100.4, ESeries.E96, 2, 0)]
    [InlineData(5.1, ESeries.E6, 0, 4)]
    public void Nearest_picks_the_closest_value_of_the_series(double value, ESeries series, int decade, int index) {
        Assert.Equal(new PreferredValue(series, decade, index), PreferredValue.Nearest(value, series));
    }

    [Fact]
    public void Nearest_works_on_a_logarithmic_scale_not_a_linear_one() {
        // Between 1.0 and 1.5 in E6 the arithmetic middle is 1.25 but the geometric one is 1.2247.
        Assert.Equal(new PreferredValue(ESeries.E6, 0, 1), PreferredValue.Nearest(1.23, ESeries.E6));
        Assert.Equal(new PreferredValue(ESeries.E6, 0, 0), PreferredValue.Nearest(1.22, ESeries.E6));
    }

    [Fact]
    public void Nearest_crosses_into_the_next_decade_when_ten_is_closest() {
        Assert.Equal(new PreferredValue(ESeries.E24, 4, 0), PreferredValue.Nearest(9.99e3, ESeries.E24));
    }

    [Fact]
    public void Nearest_is_refused_when_the_closest_value_falls_below_the_lowest_decade() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(4e-14, ESeries.E12));
    }

    [Fact]
    public void Nearest_is_refused_when_the_closest_value_falls_above_the_highest_decade() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(9.95e12, ESeries.E96));
    }

    [Fact]
    public void Nearest_reaches_the_edges_of_the_grid() {
        Assert.Equal(new PreferredValue(ESeries.E96, 12, 95), PreferredValue.Nearest(9.7e12, ESeries.E96));
        Assert.Equal(new PreferredValue(ESeries.E24, -13, 0), PreferredValue.Nearest(1e-13, ESeries.E24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4.7)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Nearest_and_IsPreferred_refuse_a_value_that_is_not_finite_and_positive(double value) {
        ESeries anySeries = Any.Enum<ESeries>().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, anySeries));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.IsPreferred(value, anySeries));
    }

    [Fact]
    public void A_value_on_the_grid_is_preferred() {
        Assert.True(PreferredValue.IsPreferred(2.2e-9, ESeries.E24));
        Assert.True(PreferredValue.IsPreferred(4.99e3, ESeries.E96));
        Assert.True(PreferredValue.IsPreferred(1e-13, ESeries.E6));
    }

    [Fact]
    public void A_value_off_the_grid_is_not_preferred() {
        Assert.False(PreferredValue.IsPreferred(2.3e-9, ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(4.7e3, ESeries.E96));
    }

    [Fact]
    public void Preferred_means_within_a_relative_billionth() {
        Assert.True(PreferredValue.IsPreferred(2.2e-9 * (1 + 5e-10), ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(2.2e-9 * (1 + 3e-9), ESeries.E24));
    }

    [Fact]
    public void A_value_outside_the_decades_is_not_preferred() {
        Assert.False(PreferredValue.IsPreferred(1e-14, ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(1e13, ESeries.E24));
    }

    [Fact]
    public void Next_moves_up_the_series_and_crosses_into_the_next_decade() {
        Assert.Equal(new PreferredValue(ESeries.E12, 3, 1), new PreferredValue(ESeries.E12, 3, 0).Next());
        Assert.Equal(new PreferredValue(ESeries.E12, 4, 0), new PreferredValue(ESeries.E12, 3, 11).Next());
    }

    [Fact]
    public void Previous_moves_down_the_series_and_crosses_into_the_lower_decade() {
        Assert.Equal(new PreferredValue(ESeries.E24, 3, 4), new PreferredValue(ESeries.E24, 3, 5).Previous());
        Assert.Equal(new PreferredValue(ESeries.E24, 2, 23), new PreferredValue(ESeries.E24, 3, 0).Previous());
    }

    [Fact]
    public void Next_is_refused_past_the_top_of_the_last_decade() {
        Assert.Throws<InvalidOperationException>(() => new PreferredValue(ESeries.E96, 12, 95).Next());
    }

    [Fact]
    public void Previous_is_refused_below_the_bottom_of_the_first_decade() {
        Assert.Throws<InvalidOperationException>(() => new PreferredValue(ESeries.E96, -13, 0).Previous());
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void Every_value_of_every_decade_is_its_own_nearest_and_is_preferred(ESeries series) {
        // An exhaustive walk of the whole grid, with no draw: the claim is about every value of the series.
        for (int decade = PreferredValue.MinDecade; decade <= PreferredValue.MaxDecade; decade++) {
            for (int index = 0; index < PreferredValue.ValuesPerDecade(series); index++) {
                PreferredValue expected = new PreferredValue(series, decade, index);

                Assert.True(PreferredValue.IsPreferred(expected.Value, series), $"{expected} (value {expected.Value:R}) should be preferred");
                Assert.True(expected.Equals(PreferredValue.Nearest(expected.Value, series)), $"{expected} (value {expected.Value:R}) should be its own nearest");
            }
        }
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E96)]
    public void Walking_up_then_down_comes_back_to_the_start(ESeries series) {
        PreferredValue start = new PreferredValue(series, -13, 0);
        PreferredValue current = start;
        int steps = 0;
        while (true) {
            PreferredValue next;
            try {
                next = current.Next();
            } catch (InvalidOperationException) {
                break;
            }

            Assert.True(next.CompareTo(current) > 0);
            current = next;
            steps++;
        }

        Assert.Equal(26 * PreferredValue.ValuesPerDecade(series) - 1, steps);
        for (int i = 0; i < steps; i++) {
            current = current.Previous();
        }

        Assert.Equal(start, current);
    }

    [Fact]
    public void Equality_is_by_series_decade_and_index_and_ordering_by_value() {
        PreferredValue e24 = new PreferredValue(ESeries.E24, 3, 8);
        PreferredValue e12 = new PreferredValue(ESeries.E12, 3, 4);

        Assert.Equal(e24, new PreferredValue(ESeries.E24, 3, 8));
        Assert.Equal(e24.GetHashCode(), new PreferredValue(ESeries.E24, 3, 8).GetHashCode());
        Assert.NotEqual(e24, e12);   // same 2.2 kilo, different series
        Assert.Equal(0, e24.CompareTo(e12));
        Assert.True(new PreferredValue(ESeries.E24, 3, 7).CompareTo(e24) < 0);
        Assert.True(e24.CompareTo(null) > 0);
    }

    [Fact]
    public void Is_shown_as_series_mantissa_and_decade() {
        Assert.Equal("E24 2.2e-9", new PreferredValue(ESeries.E24, -9, 8).ToString());
        Assert.Equal("E96 4.99e0", new PreferredValue(ESeries.E96, 0, 67).ToString());
    }

}
