using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class PreferredValueTests {

    private static readonly ESeries[] AllSeries = [ESeries.E6, ESeries.E12, ESeries.E24, ESeries.E96];

    [Fact]
    public void The_twenty_second_value_of_E24_in_the_nano_decade_is_2_2_nanofarads() {
        // Act: E24, decade -9, index 8
        PreferredValue value = new PreferredValue(ESeries.E24, -9, 8);

        // Assert: the value is the closest double to 2.2e-9, not merely close to it
        Assert.Equal(2.2, value.Mantissa);
        Assert.Equal(2.2e-9, value.Value);
        Assert.Equal(-9, value.Decade);
    }

    [Theory]
    [InlineData(ESeries.E6, 12, 5, 6.8, 6.8e12)]
    [InlineData(ESeries.E12, 0, 0, 1.0, 1.0)]
    [InlineData(ESeries.E96, 0, 67, 4.99, 4.99)]
    [InlineData(ESeries.E96, -13, 0, 1.0, 1e-13)]
    [InlineData(ESeries.E96, 12, 95, 9.76, 9.76e12)]
    public void A_value_is_its_mantissa_times_ten_to_its_decade(ESeries series, int decade, int index, double mantissa, double expectedValue) {
        // Act
        PreferredValue value = new PreferredValue(series, decade, index);

        // Assert
        Assert.Equal(mantissa, value.Mantissa);
        Assert.Equal(expectedValue, value.Value);
    }

    [Theory]
    [InlineData(PreferredValue.MinDecade)]
    [InlineData(PreferredValue.MaxDecade)]
    public void The_first_and_last_decades_are_valid(int decade) {
        Assert.Equal(decade, new PreferredValue(ESeries.E12, decade, 0).Decade);
    }

    [Theory]
    [InlineData(PreferredValue.MinDecade - 1)]
    [InlineData(PreferredValue.MaxDecade + 1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_decade_outside_the_range_is_refused(int decade) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(ESeries.E12, decade, 0));
    }

    [Theory]
    [InlineData(ESeries.E6, 6)]
    [InlineData(ESeries.E12, 12)]
    [InlineData(ESeries.E24, 24)]
    [InlineData(ESeries.E96, 96)]
    public void An_index_runs_from_zero_to_one_less_than_the_values_per_decade_of_the_series(ESeries series, int valuesPerDecade) {
        // Act & Assert
        Assert.Equal(valuesPerDecade, PreferredValue.ValuesPerDecade(series));
        Assert.Equal(valuesPerDecade - 1, new PreferredValue(series, 0, valuesPerDecade - 1).Index);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, 0, valuesPerDecade));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue(series, 0, -1));
    }

    [Fact]
    public void An_undefined_series_has_no_values() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreferredValue((ESeries)99, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.ValuesPerDecade((ESeries)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Mantissas((ESeries)99));
    }

    [Theory]
    [InlineData(ESeries.E6, new[] { 1.0, 1.5, 2.2, 3.3, 4.7, 6.8 })]
    [InlineData(ESeries.E12, new[] { 1.0, 1.2, 1.5, 1.8, 2.2, 2.7, 3.3, 3.9, 4.7, 5.6, 6.8, 8.2 })]
    public void The_mantissas_of_the_short_series_are_the_standard_ones(ESeries series, double[] expected) {
        Assert.Equal(expected, PreferredValue.Mantissas(series));
    }

    [Theory]
    [InlineData(ESeries.E24, 2.2)]
    [InlineData(ESeries.E24, 9.1)]
    [InlineData(ESeries.E96, 4.99)]
    [InlineData(ESeries.E96, 9.76)]
    [InlineData(ESeries.E96, 1.02)]
    public void A_series_lists_its_well_known_mantissas(ESeries series, double mantissa) {
        Assert.Contains(mantissa, PreferredValue.Mantissas(series));
    }

    [Fact]
    public void The_mantissas_of_every_series_ascend_strictly_from_one_up_to_below_ten() {
        foreach (ESeries series in AllSeries) {
            IReadOnlyList<double> mantissas = PreferredValue.Mantissas(series);

            Assert.Equal(PreferredValue.ValuesPerDecade(series), mantissas.Count);
            Assert.Equal(1.0, mantissas[0]);
            Assert.True(mantissas[^1] < 10, $"{series}: last mantissa {mantissas[^1]}");
            Assert.Equal(mantissas.OrderBy(m => m).Distinct(), mantissas);
        }
    }

    [Fact]
    public void The_mantissas_cannot_be_changed_by_the_caller() {
        // Arrange
        IReadOnlyList<double> mantissas = PreferredValue.Mantissas(ESeries.E12);

        // Act & Assert
        Assert.True(((System.Collections.IList)mantissas).IsReadOnly);
        Assert.Equal(1.0, PreferredValue.Mantissas(ESeries.E12)[0]);
    }

    [Fact]
    public void Nine_point_nine_kiloohms_snaps_up_to_ten_kiloohms_in_E12() {
        // Act
        PreferredValue nearest = PreferredValue.Nearest(9.9e3, ESeries.E12);

        // Assert
        Assert.Equal(new PreferredValue(ESeries.E12, 4, 0), nearest);
        Assert.Equal(1e4, nearest.Value);
    }

    [Theory]
    [InlineData(2.2e-9, ESeries.E24, 8, -9)]
    [InlineData(2.3e-9, ESeries.E24, 9, -9)]
    [InlineData(4.8, ESeries.E12, 8, 0)]
    [InlineData(4.97e5, ESeries.E96, 67, 5)]
    [InlineData(1.0, ESeries.E6, 0, 0)]
    public void A_value_snaps_to_the_closest_of_the_series_on_a_logarithmic_scale(double value, ESeries series, int expectedIndex, int expectedDecade) {
        // Act
        PreferredValue nearest = PreferredValue.Nearest(value, series);

        // Assert
        Assert.Equal(new PreferredValue(series, expectedDecade, expectedIndex), nearest);
    }

    [Fact]
    public void The_dividing_line_between_two_values_is_their_geometric_mean_not_their_arithmetic_mean() {
        // Arrange: 1.0 and 1.2 in E12. The arithmetic mean is 1.1, the geometric one is 1.0954...
        double justBelowTheGeometricMean = 1.0954451150103321 * (1 - 1e-6);
        double justAboveTheGeometricMean = 1.0954451150103321 * (1 + 1e-6);

        // Act & Assert
        Assert.Equal(1.0, PreferredValue.Nearest(justBelowTheGeometricMean, ESeries.E12).Value);
        Assert.Equal(1.2, PreferredValue.Nearest(justAboveTheGeometricMean, ESeries.E12).Value);
        Assert.Equal(1.2, PreferredValue.Nearest(1.1, ESeries.E12).Value);
    }

    [Fact]
    public void A_value_just_under_ten_snaps_up_into_the_next_decade() {
        // Act
        PreferredValue nearest = PreferredValue.Nearest(9.99e-6, ESeries.E24);

        // Assert
        Assert.Equal(-5, nearest.Decade);
        Assert.Equal(0, nearest.Index);
    }

    [Fact]
    public void The_smallest_and_the_largest_values_snap_to_themselves() {
        Assert.Equal(new PreferredValue(ESeries.E96, -13, 0), PreferredValue.Nearest(1e-13, ESeries.E96));
        Assert.Equal(new PreferredValue(ESeries.E96, 12, 95), PreferredValue.Nearest(9.76e12, ESeries.E96));
    }

    [Fact]
    public void A_value_just_under_the_smallest_snaps_up_to_it_when_that_is_the_nearest() {
        // 9.9e-14 is closer to 1e-13 than to 8.2e-14
        Assert.Equal(new PreferredValue(ESeries.E12, -13, 0), PreferredValue.Nearest(9.9e-14, ESeries.E12));
    }

    [Theory]
    [InlineData(1.1e-14)]
    [InlineData(5e-14)]
    public void A_value_whose_nearest_falls_below_the_first_decade_is_refused(double value) {
        // 1.1e-14 and 5e-14 are closest to 1e-14 and 4.7e-14: both below 1e-13
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, ESeries.E12));
    }

    [Theory]
    [InlineData(9.9e12, ESeries.E12)]
    [InlineData(9.9e12, ESeries.E96)]
    [InlineData(1e13, ESeries.E24)]
    [InlineData(5e15, ESeries.E6)]
    public void A_value_whose_nearest_falls_above_the_last_decade_is_refused(double value, ESeries series) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, series));
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(2.2250738585072014E-308)]
    [InlineData(double.MaxValue)]
    public void The_extremes_of_the_double_range_are_outside_the_decades_rather_than_a_crash(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(value, ESeries.E24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_value_that_is_not_positive_and_finite_has_no_preferred_neighbour(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.Nearest(value, ESeries.E24));
        Assert.Throws<ArgumentOutOfRangeException>(() => PreferredValue.IsPreferred(value, ESeries.E24));
    }

    [Fact]
    public void Every_value_of_every_series_in_every_decade_is_its_own_nearest() {
        foreach (ESeries series in AllSeries) {
            for (int decade = PreferredValue.MinDecade; decade <= PreferredValue.MaxDecade; decade++) {
                for (int index = 0; index < PreferredValue.ValuesPerDecade(series); index++) {
                    PreferredValue value = new PreferredValue(series, decade, index);

                    Assert.True(value.Equals(PreferredValue.Nearest(value.Value, series)), $"{value} (decade {decade}, index {index}) is not its own nearest");
                }
            }
        }
    }

    [Fact]
    public void Every_value_of_every_series_in_every_decade_is_preferred() {
        foreach (ESeries series in AllSeries) {
            for (int decade = PreferredValue.MinDecade; decade <= PreferredValue.MaxDecade; decade++) {
                for (int index = 0; index < PreferredValue.ValuesPerDecade(series); index++) {
                    PreferredValue value = new PreferredValue(series, decade, index);

                    Assert.True(PreferredValue.IsPreferred(value.Value, series), $"{value} (decade {decade}, index {index}) is not preferred");
                }
            }
        }
    }

    [Theory]
    [InlineData(2.2e-9, ESeries.E24, true)]
    [InlineData(2.3e-9, ESeries.E24, false)]
    [InlineData(2.7, ESeries.E12, true)]
    [InlineData(2.4, ESeries.E12, false)]
    [InlineData(2.4, ESeries.E24, true)]
    [InlineData(2.2, ESeries.E6, true)]
    [InlineData(2.7, ESeries.E6, false)]
    [InlineData(4.99, ESeries.E96, true)]
    [InlineData(4.99, ESeries.E24, false)]
    [InlineData(1e-13, ESeries.E6, true)]
    [InlineData(6.8e12, ESeries.E6, true)]
    public void A_value_is_preferred_when_it_sits_on_the_grid_of_the_series(double value, ESeries series, bool expected) {
        Assert.Equal(expected, PreferredValue.IsPreferred(value, series));
    }

    [Theory]
    [InlineData(1e-14)]
    [InlineData(1e13)]
    [InlineData(9.9e12)]
    public void A_value_outside_the_decades_is_not_preferred_rather_than_refused(double value) {
        Assert.False(PreferredValue.IsPreferred(value, ESeries.E12));
    }

    [Fact]
    public void Preferred_is_judged_with_a_relative_slack_of_a_billionth() {
        Assert.True(PreferredValue.IsPreferred(2.2e-9 * (1 + 5e-10), ESeries.E24));
        Assert.True(PreferredValue.IsPreferred(2.2e12 * (1 - 5e-10), ESeries.E24));
        Assert.False(PreferredValue.IsPreferred(2.2e-9 * (1 + 3e-9), ESeries.E24));
    }

    [Fact]
    public void The_next_value_of_the_last_mantissa_is_the_first_of_the_next_decade() {
        // Arrange: 8.2 kΩ is the last of E12 in its decade
        PreferredValue last = new PreferredValue(ESeries.E12, 3, 11);

        // Act
        PreferredValue next = last.Next();

        // Assert
        Assert.Equal(new PreferredValue(ESeries.E12, 4, 0), next);
        Assert.Equal(1e4, next.Value);
    }

    [Fact]
    public void The_previous_value_of_the_first_mantissa_is_the_last_of_the_previous_decade() {
        // Arrange
        PreferredValue first = new PreferredValue(ESeries.E24, -9, 0);

        // Act
        PreferredValue previous = first.Previous();

        // Assert
        Assert.Equal(new PreferredValue(ESeries.E24, -10, 23), previous);
        Assert.Equal(9.1e-10, previous.Value);
    }

    [Fact]
    public void Next_and_previous_move_within_a_decade() {
        // Arrange
        PreferredValue middle = new PreferredValue(ESeries.E12, -6, 4);

        // Act & Assert
        Assert.Equal(new PreferredValue(ESeries.E12, -6, 5), middle.Next());
        Assert.Equal(new PreferredValue(ESeries.E12, -6, 3), middle.Previous());
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void There_is_nothing_above_the_last_value_of_the_top_decade(ESeries series) {
        // Arrange
        PreferredValue top = new PreferredValue(series, PreferredValue.MaxDecade, PreferredValue.ValuesPerDecade(series) - 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => top.Next());
    }

    [Theory]
    [InlineData(ESeries.E6)]
    [InlineData(ESeries.E12)]
    [InlineData(ESeries.E24)]
    [InlineData(ESeries.E96)]
    public void There_is_nothing_below_the_first_value_of_the_bottom_decade(ESeries series) {
        // Arrange
        PreferredValue bottom = new PreferredValue(series, PreferredValue.MinDecade, 0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bottom.Previous());
    }

    [Fact]
    public void Walking_up_from_the_bottom_visits_every_value_once_in_ascending_order() {
        foreach (ESeries series in AllSeries) {
            // Arrange
            PreferredValue current = new PreferredValue(series, PreferredValue.MinDecade, 0);
            int            visited = 1;

            // Act
            while (true) {
                PreferredValue? next = null;

                try {
                    next = current.Next();
                } catch (InvalidOperationException) { }

                if (next is null) { break; }

                Assert.True(next.Value > current.Value, $"{series}: {next} does not follow {current}");
                current = next;
                visited++;
            }

            // Assert
            int decades = PreferredValue.MaxDecade - PreferredValue.MinDecade + 1;
            Assert.Equal(decades * PreferredValue.ValuesPerDecade(series), visited);
            Assert.Equal(new PreferredValue(series, PreferredValue.MaxDecade, PreferredValue.ValuesPerDecade(series) - 1), current);
        }
    }

    [Theory]
    [InlineData(ESeries.E6, 0, 3)]
    [InlineData(ESeries.E12, -13, 11)]
    [InlineData(ESeries.E24, 7, 23)]
    [InlineData(ESeries.E96, 11, 0)]
    public void Previous_undoes_next_including_across_a_decade(ESeries series, int decade, int index) {
        // Arrange
        PreferredValue value = new PreferredValue(series, decade, index);

        // Act & Assert
        Assert.Equal(value, value.Next().Previous());
    }

    [Fact]
    public void A_preferred_value_compared_with_nothing_comes_after_it() {
        // Arrange
        PreferredValue anyValue = Any.PreferredValue().Generate();

        // Act & Assert
        Assert.True(anyValue.CompareTo(null) > 0);
        Assert.False(anyValue.Equals(null));
    }

    [Fact]
    public void Equality_is_by_series_decade_and_index_and_ordering_by_value() {
        // Arrange
        PreferredValue e12 = new PreferredValue(ESeries.E12, -9, 4);
        PreferredValue e24 = new PreferredValue(ESeries.E24, -9, 8);

        // Act & Assert: 2.2 nF in both series, but not the same position in the same series
        Assert.Equal(e12.Value, e24.Value);
        Assert.NotEqual(e12, e24);
        Assert.Equal(new PreferredValue(ESeries.E12, -9, 4), e12);
        Assert.Equal(new PreferredValue(ESeries.E12, -9, 4).GetHashCode(), e12.GetHashCode());
        Assert.True(new PreferredValue(ESeries.E12, -9, 3).CompareTo(e12) < 0);
        Assert.True(e12.CompareTo(new PreferredValue(ESeries.E12, -8, 0)) < 0);
        Assert.True(e12.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_values_of_different_series_with_the_same_value_compare_as_zero_without_being_equal() {
        // Arrange
        PreferredValue e12 = new PreferredValue(ESeries.E12, -9, 4);
        PreferredValue e24 = new PreferredValue(ESeries.E24, -9, 8);

        // Act & Assert
        Assert.Equal(0, e12.CompareTo(e24));
        Assert.False(e12.Equals(e24));
    }

    [Fact]
    public void A_preferred_value_reads_as_its_series_and_its_mantissa_and_decade() {
        Assert.Equal("E24 2.2e-9", new PreferredValue(ESeries.E24, -9, 8).ToString());
        Assert.Equal("E96 4.99e0", new PreferredValue(ESeries.E96, 0, 67).ToString());
    }

}
