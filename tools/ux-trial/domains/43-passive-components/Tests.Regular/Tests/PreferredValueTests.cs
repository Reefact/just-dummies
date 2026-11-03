namespace PassiveComponents.Tests.Regular;

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
    public void The_E6_mantissas_are_the_classic_six() {
        Assert.Equal(new double[] { 1.0, 1.5, 2.2, 3.3, 4.7, 6.8 }, PreferredValue.Mantissas(ESeries.E6));
    }

    [Fact]
    public void The_E12_mantissas_are_the_classic_twelve() {
        Assert.Equal(new double[] { 1.0, 1.2, 1.5, 1.8, 2.2, 2.7, 3.3, 3.9, 4.7, 5.6, 6.8, 8.2 }, PreferredValue.Mantissas(ESeries.E12));
    }

    [Fact]
    public void The_E96_mantissas_run_from_1_00_to_9_76() {
        IReadOnlyList<double> mantissas = PreferredValue.Mantissas(ESeries.E96);

        Assert.Equal(1.00, mantissas[0]);
        Assert.Equal(4.99, mantissas[67]);
        Assert.Equal(9.76, mantissas[95]);
    }

    [Fact]
    public void Every_series_is_strictly_increasing_within_a_decade() {
        foreach (ESeries series in Enum.GetValues<ESeries>()) {
            IReadOnlyList<double> mantissas = PreferredValue.Mantissas(series);

            for (int i = 1; i < mantissas.Count; i++) {
                Assert.True(mantissas[i - 1] < mantissas[i], $"{series} at {i}");
            }

            Assert.True(mantissas[0] >= 1.0 && mantissas[^1] < 10.0);
        }
    }

    [Fact]
    public void The_E24_value_of_decade_minus_nine_at_index_eight_is_2_2_nano() {
        PreferredValue value = new PreferredValue(ESeries.E24, -9, 8);

        Assert.Equal(2.2, value.Mantissa);
        Assert.Equal(2.2e-9, value.Value);
    }

    [Fact]
    public void An_E96_value_is_the_closest_double_to_its_decimal_reading() {
        PreferredValue value = new PreferredValue(ESeries.E96, 3, 67);

        Assert.Equal(4.99, value.Mantissa);
        Assert.Equal(4990.0, value.Value);
    }

    [Theory]
    [InlineData(-13)]
    [InlineData(12)]
    public void The_extreme_decades_are_accepted(int decade) {
        PreferredValue value = new PreferredValue(ESeries.E12, decade, 0);

        Assert.Equal(decade, value.Decade);
    }

    [Theory]
    [InlineData(-14)]
    [InlineData(13)]
    [InlineData(int.MinValue)]
    public void A_decade_outside_the_range_is_refused(int decade) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(ESeries.E12, decade, 0));
    }

    [Theory]
    [InlineData(ESeries.E6, 6)]
    [InlineData(ESeries.E12, 12)]
    [InlineData(ESeries.E24, 24)]
    [InlineData(ESeries.E96, 96)]
    public void An_index_past_the_last_value_of_the_series_is_refused(ESeries series, int firstInvalidIndex) {
        int anyDecade = Any.Int32().Between(PreferredValue.MinDecade, PreferredValue.MaxDecade).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, anyDecade, firstInvalidIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, anyDecade, -1));
        Assert.NotNull(new PreferredValue(series, anyDecade, firstInvalidIndex - 1));
    }

    [Fact]
    public void An_unknown_series_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue((ESeries)7, 0, 0));
    }

    [Fact]
    public void Nearest_snaps_9_9_kilo_to_ten_kilo_in_E12() {
        PreferredValue nearest = PreferredValue.Nearest(9.9e3, ESeries.E12);

        Assert.Equal(new PreferredValue(ESeries.E12, 4, 0), nearest);
        Assert.Equal(1e4, nearest.Value);
    }

    [Theory]
    [InlineData(4.6, ESeries.E6, 0, 4)]
    [InlineData(4.8, ESeries.E24, 0, 16)]
    [InlineData(1.0, ESeries.E96, 0, 0)]
    [InlineData(1.03, ESeries.E96, 0, 1)]
    [InlineData(1.04, ESeries.E96, 0, 2)]
    [InlineData(2.0, ESeries.E12, 0, 4)]
    public void Nearest_picks_the_neighbour_closest_on_a_logarithmic_scale(double value, ESeries series, int decade, int index) {
        PreferredValue nearest = PreferredValue.Nearest(value, series);

        Assert.Equal(new PreferredValue(series, decade, index), nearest);
    }

    [Fact]
    public void Nearest_chooses_by_ratio_not_by_difference() {
        // E6 neighbours 2.2 and 3.3: the arithmetic midpoint 2.75 is closer (in ratio) to 3.3, the geometric one is 2.69
        Assert.Equal(new PreferredValue(ESeries.E6, 0, 3), PreferredValue.Nearest(2.75, ESeries.E6));
        Assert.Equal(new PreferredValue(ESeries.E6, 0, 2), PreferredValue.Nearest(2.65, ESeries.E6));
    }

    [Fact]
    public void Nearest_goes_down_to_the_decade_floor() {
        PreferredValue nearest = PreferredValue.Nearest(1e-13, ESeries.E96);

        Assert.Equal(new PreferredValue(ESeries.E96, -13, 0), nearest);
    }

    [Fact]
    public void Nearest_may_round_up_into_the_lowest_decade() {
        // 9.9e-14 is in decade -14 but its nearest neighbour is 1.00e-13
        PreferredValue nearest = PreferredValue.Nearest(9.9e-14, ESeries.E12);

        Assert.Equal(new PreferredValue(ESeries.E12, -13, 0), nearest);
    }

    [Fact]
    public void Nearest_refuses_a_value_whose_neighbour_is_below_the_lowest_decade() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(5e-14, ESeries.E24));
    }

    [Fact]
    public void Nearest_refuses_a_value_whose_neighbour_is_past_the_highest_decade() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(9.9e12, ESeries.E96));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(1e13, ESeries.E6));
    }

    [Fact]
    public void Nearest_accepts_the_highest_preferred_value() {
        PreferredValue nearest = PreferredValue.Nearest(9.76e12, ESeries.E96);

        Assert.Equal(new PreferredValue(ESeries.E96, 12, 95), nearest);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_value_that_is_not_finite_and_positive_has_no_neighbour(double value) {
        ESeries anySeries = Any.Enum<ESeries>().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, anySeries));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.IsPreferred(value, anySeries));
    }

    [Fact]
    public void Nearest_of_an_unknown_series_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(1.0, (ESeries)42));
    }

    [Fact]
    public void A_value_on_the_grid_is_preferred() {
        Assert.True(PreferredValue.IsPreferred(4.7e-9, ESeries.E24));
        Assert.True(PreferredValue.IsPreferred(4990, ESeries.E96));
        Assert.True(PreferredValue.IsPreferred(1e-13, ESeries.E6));
    }

    [Fact]
    public void A_value_off_the_grid_is_not_preferred() {
        Assert.False(PreferredValue.IsPreferred(4.8e-9, ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(4.7e-9, ESeries.E96));
    }

    [Fact]
    public void Preferred_means_within_a_relative_billionth() {
        Assert.True(PreferredValue.IsPreferred(4.7e-9 * (1 + 5e-10), ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(4.7e-9 * (1 + 2e-9), ESeries.E24));
    }

    [Fact]
    public void A_value_outside_the_decades_is_not_preferred_rather_than_refused() {
        Assert.False(PreferredValue.IsPreferred(1e13, ESeries.E12));
        Assert.False(PreferredValue.IsPreferred(1e-14, ESeries.E12));
    }

    [Fact]
    public void Next_moves_up_the_series() {
        PreferredValue next = new PreferredValue(ESeries.E12, 3, 4).Next();

        Assert.Equal(new PreferredValue(ESeries.E12, 3, 5), next);
    }

    [Fact]
    public void Next_crosses_into_the_next_decade() {
        PreferredValue next = new PreferredValue(ESeries.E24, 3, 23).Next();

        Assert.Equal(new PreferredValue(ESeries.E24, 4, 0), next);
    }

    [Fact]
    public void Previous_crosses_into_the_previous_decade() {
        PreferredValue previous = new PreferredValue(ESeries.E96, 3, 0).Previous();

        Assert.Equal(new PreferredValue(ESeries.E96, 2, 95), previous);
    }

    [Fact]
    public void Previous_moves_down_the_series() {
        Assert.Equal(new PreferredValue(ESeries.E6, -2, 1), new PreferredValue(ESeries.E6, -2, 2).Previous());
    }

    [Fact]
    public void Nothing_is_above_the_last_value_of_the_top_decade() {
        PreferredValue top = new PreferredValue(ESeries.E96, 12, 95);

        Assert.Throws<InvalidOperationException>(() => top.Next());
    }

    [Fact]
    public void Nothing_is_below_the_first_value_of_the_bottom_decade() {
        PreferredValue bottom = new PreferredValue(ESeries.E96, -13, 0);

        Assert.Throws<InvalidOperationException>(() => bottom.Previous());
    }

    [Fact]
    public void Next_then_previous_comes_back() {
        PreferredValue anyValue = Any.PreferredValue().WithDecade(d => d.Between(-12, 11)).Generate();

        Assert.Equal(anyValue, anyValue.Next().Previous());
        Assert.Equal(anyValue, anyValue.Previous().Next());
    }

    [Fact]
    public void Equality_is_by_series_decade_and_index() {
        PreferredValue e12 = new PreferredValue(ESeries.E12, 0, 0);
        PreferredValue e24 = new PreferredValue(ESeries.E24, 0, 0);

        Assert.NotEqual(e12, e24);
        Assert.Equal(e12, new PreferredValue(ESeries.E12, 0, 0));
        Assert.Equal(e12.GetHashCode(), new PreferredValue(ESeries.E12, 0, 0).GetHashCode());
        Assert.False(e12.Equals(null));
    }

    [Fact]
    public void Ordering_is_by_value_across_series() {
        PreferredValue e12OnePointTwo = new PreferredValue(ESeries.E12, 0, 1);
        PreferredValue e24OnePointOne = new PreferredValue(ESeries.E24, 0, 1);

        Assert.True(e24OnePointOne.CompareTo(e12OnePointTwo) < 0);
        Assert.Equal(0, new PreferredValue(ESeries.E12, 0, 0).CompareTo(new PreferredValue(ESeries.E24, 0, 0)));
        Assert.True(e12OnePointTwo.CompareTo(null) > 0);
    }

    [Fact]
    public void It_is_written_with_its_series_mantissa_and_decade() {
        Assert.Equal("E24 2.2e-9", new PreferredValue(ESeries.E24, -9, 8).ToString());
    }

}
