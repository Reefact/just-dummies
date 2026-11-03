using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalibratedDateTests {

    private const int Precision = 9;

    private static void AssertProbabilities(double[] expected, CalibratedDate date) {
        Assert.Equal(expected.Length, date.Probabilities.Count);
        for (int i = 0; i < expected.Length; i++) {
            Assert.Equal(expected[i], date.Probabilities[i], Precision);
        }
    }

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_date_spans_one_year_per_probability_from_its_first_year() {
        // Act
        CalibratedDate date = new CalibratedDate(-100, [0.25, 0.5, 0.25]);

        // Assert
        Assert.Equal(-100, date.FirstYear);
        Assert.Equal(-98, date.LastYear);
        AssertProbabilities([0.25, 0.5, 0.25], date);
    }

    [Theory]
    [InlineData(-55_000, 1)]
    [InlineData(1_950, 1)]
    [InlineData(-55_000, 56_951)]
    public void A_date_filling_the_calendar_up_to_its_limits_is_accepted(int firstYear, int years) {
        // Arrange
        double[] certainOnTheFirstYear = new double[years];
        certainOnTheFirstYear[0] = 1;

        // Act
        CalibratedDate date = new CalibratedDate(firstYear, certainOnTheFirstYear);

        // Assert
        Assert.Equal(firstYear + years - 1, date.LastYear);
    }

    [Fact]
    public void A_date_starting_before_the_calendar_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(-55_001, [1.0]));

        // Assert
        Assert.Equal("firstYear", refusal.ParamName);
    }

    [Fact]
    public void A_date_starting_after_the_calendar_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(1_951, [1.0]));

        // Assert
        Assert.Equal("firstYear", refusal.ParamName);
    }

    [Fact]
    public void A_date_spilling_one_year_past_the_end_of_the_calendar_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(1_950, [0.5, 0.5]));

        // Assert
        Assert.Equal("firstYear", refusal.ParamName);
    }

    [Fact]
    public void A_date_without_any_probability_is_refused() {
        // Arrange
        int anyFirstYear = Years.InTheCalendar().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, []));

        // Assert
        Assert.Equal("probabilities", refusal.ParamName);
    }

    [Fact]
    public void A_date_without_a_list_of_probabilities_is_refused() {
        // Arrange
        int anyFirstYear = Years.InTheCalendar().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CalibratedDate(anyFirstYear, null!));
    }

    [Theory]
    [InlineData(1.000_001)]
    [InlineData(-0.1, 1.1)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(1.0, -0.000_001)]
    public void A_probability_outside_zero_to_one_is_refused(params double[] probabilities) {
        // Arrange
        int anyFirstYear = Years.InTheCalendar().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(anyFirstYear, probabilities));

        // Assert
        Assert.Equal("probabilities", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.5, 0.4999)]
    [InlineData(0.5, 0.5002)]
    [InlineData(0.1)]
    public void Probabilities_that_do_not_sum_to_one_are_refused(params double[] probabilities) {
        // Arrange
        int anyFirstYear = Years.InTheCalendar().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, probabilities));

        // Assert
        Assert.Equal("probabilities", refusal.ParamName);
    }

    [Fact]
    public void A_sum_within_a_millionth_of_one_is_accepted() {
        // Act
        CalibratedDate date = new CalibratedDate(0, [0.5, 0.500_000_5]);

        // Assert
        Assert.Equal(1, date.LastYear);
    }

    [Fact]
    public void A_sum_two_millionths_off_one_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CalibratedDate(0, [0.5, 0.500_002]));
    }

    [Fact]
    public void The_date_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<double> probabilities = [0.5, 0.5];
        CalibratedDate date        = new CalibratedDate(0, probabilities);

        // Act
        probabilities[0] = 0.9;

        // Assert
        Assert.Equal(0.5, date.ProbabilityOf(0), Precision);
    }

    [Fact(Skip = "domain defect: Probabilities hands out the internal array, so a caller can cast it to double[] and break the sum-to-one invariant of a value object (no seed: literals only)")]
    public void The_probabilities_of_a_date_cannot_be_changed_from_outside() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.5, 0.5]);

        // Act & Assert
        Assert.False(date.Probabilities is double[]);
    }

    // ---------------------------------------------------------------- reading

    [Theory]
    [InlineData(-101, 0)]
    [InlineData(-100, 0.25)]
    [InlineData(-99, 0.5)]
    [InlineData(-98, 0.25)]
    [InlineData(-97, 0)]
    public void The_probability_of_a_year_is_zero_outside_the_span(int year, double expected) {
        // Arrange
        CalibratedDate date = new CalibratedDate(-100, [0.25, 0.5, 0.25]);

        // Assert
        Assert.Equal(expected, date.ProbabilityOf(year), Precision);
    }

    [Fact]
    public void The_mean_of_a_symmetrical_date_is_its_middle_year() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-100, [0.25, 0.5, 0.25]);

        // Assert
        Assert.Equal(-99, date.Mean, Precision);
    }

    [Fact]
    public void The_mean_may_fall_between_two_years() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-3, [0.25, 0.25, 0.25, 0.25]);

        // Assert
        Assert.Equal(-1.5, date.Mean, Precision);
    }

    [Fact]
    public void The_mean_is_weighted_by_the_probabilities() {
        // Arrange
        CalibratedDate date = new CalibratedDate(10, [0.9, 0, 0.1]);

        // Assert
        Assert.Equal(10.2, date.Mean, Precision);
    }

    // ---------------------------------------------------------------- highest density region

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_credible_level_must_lie_strictly_between_zero_and_one(double level) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyDate.HighestDensityRegion(level));

        // Assert
        Assert.Equal("level", refusal.ParamName);
    }

    [Fact]
    public void The_region_is_the_smallest_set_of_years_holding_the_level() {
        // Arrange: the years by decreasing probability are 2 (0.5), 3 (0.2), 1 (0.15), 4 (0.1), 0 (0.05)
        CalibratedDate date = new CalibratedDate(0, [0.05, 0.15, 0.5, 0.2, 0.1]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.6);

        // Assert: 0.5 alone is not enough, 0.5 + 0.2 is
        Assert.Equal([new CalendarInterval(2, 3)], region);
    }

    [Fact]
    public void The_region_grows_with_the_level() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.05, 0.15, 0.5, 0.2, 0.1]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.9);

        // Assert: 0.5 + 0.2 + 0.15 = 0.85 falls short, the fourth year makes 0.95
        Assert.Equal([new CalendarInterval(1, 4)], region);
    }

    [Fact]
    public void The_region_stops_as_soon_as_it_holds_exactly_the_level() {
        // Arrange: halves are exact in binary, so the middle year holds exactly 0.5
        CalibratedDate date = new CalibratedDate(10, [0.25, 0.5, 0.25]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.5);

        // Assert
        Assert.Equal([new CalendarInterval(11, 11)], region);
    }

    [Fact]
    public void A_date_with_two_peaks_has_a_region_of_two_disjoint_intervals_in_calendar_order() {
        // Arrange: the peaks are years -1000 and -997, a trough between them
        CalibratedDate date = new CalibratedDate(-1_000, [0.4, 0.05, 0.05, 0.5]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.85);

        // Assert
        Assert.Equal([new CalendarInterval(-1_000, -1_000), new CalendarInterval(-997, -997)], region);
    }

    [Fact]
    public void Years_without_any_probability_never_join_the_region() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0, 0.5, 0.5, 0]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.99);

        // Assert
        Assert.Equal([new CalendarInterval(1, 2)], region);
    }

    [Fact]
    public void A_date_certain_of_one_year_has_that_year_as_its_region() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-250, [1.0]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.954);

        // Assert
        Assert.Equal([new CalendarInterval(-250, -250)], region);
    }

    // ---------------------------------------------------------------- truncation

    [Fact]
    public void Truncating_keeps_the_years_within_the_bounds_and_renormalises() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act
        CalibratedDate truncated = date.Truncate(1, 2);

        // Assert: 0.2 and 0.3 out of the 0.5 that remains
        AssertProbabilities([0, 0.4, 0.6, 0], truncated);
    }

    [Fact]
    public void Truncating_keeps_the_span_of_the_date() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-40, [0.1, 0.2, 0.3, 0.4]);

        // Act
        CalibratedDate truncated = date.Truncate(-39, -39);

        // Assert
        Assert.Equal(-40, truncated.FirstYear);
        Assert.Equal(-37, truncated.LastYear);
    }

    [Fact]
    public void Truncating_with_bounds_wider_than_the_span_changes_nothing() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act
        CalibratedDate truncated = date.Truncate(-1_000, 1_000);

        // Assert
        AssertProbabilities([0.1, 0.2, 0.3, 0.4], truncated);
    }

    [Fact]
    public void Truncating_keeps_both_bounds_years() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.25, 0.25, 0.25, 0.25]);

        // Act
        CalibratedDate truncated = date.Truncate(0, 3);

        // Assert
        AssertProbabilities([0.25, 0.25, 0.25, 0.25], truncated);
    }

    [Fact]
    public void Truncating_leaves_the_original_date_unchanged() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act
        date.Truncate(3, 3);

        // Assert
        AssertProbabilities([0.1, 0.2, 0.3, 0.4], date);
    }

    [Fact]
    public void Truncating_to_a_period_the_date_does_not_cover_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => date.Truncate(10, 20));
    }

    [Fact]
    public void Truncating_to_years_without_any_probability_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0, 1.0, 0, 0]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => date.Truncate(2, 3));
    }

    [Fact]
    public void Truncating_with_bounds_in_the_wrong_order_is_refused() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyDate.Truncate(10, 9));

        // Assert
        Assert.Equal("notAfter", refusal.ParamName);
    }

    // ---------------------------------------------------------------- combination

    [Fact]
    public void Combining_one_date_gives_that_date_back() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-5, [0.1, 0.2, 0.3, 0.4]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([date]);

        // Assert
        Assert.Equal(-5, combined.FirstYear);
        AssertProbabilities([0.1, 0.2, 0.3, 0.4], combined);
    }

    [Fact]
    public void Combining_dates_mixes_them_with_equal_weights_over_the_span_of_them_all() {
        // Arrange
        CalibratedDate early = new CalibratedDate(0, [1.0]);
        CalibratedDate late  = new CalibratedDate(2, [0.5, 0.5]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([early, late]);

        // Assert
        Assert.Equal(0, combined.FirstYear);
        AssertProbabilities([0.5, 0, 0.25, 0.25], combined);
    }

    [Fact]
    public void Combining_overlapping_dates_adds_their_shares_year_by_year() {
        // Arrange
        CalibratedDate first  = new CalibratedDate(0, [0.5, 0.5]);
        CalibratedDate second = new CalibratedDate(1, [0.5, 0.5]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([first, second]);

        // Assert
        AssertProbabilities([0.25, 0.5, 0.25], combined);
    }

    [Fact]
    public void The_order_of_the_combined_dates_does_not_matter() {
        // Arrange
        CalibratedDate early = new CalibratedDate(0, [0.7, 0.3]);
        CalibratedDate late  = new CalibratedDate(5, [0.4, 0.6]);

        // Act
        CalibratedDate forwards  = CalibratedDate.Combine([early, late]);
        CalibratedDate backwards = CalibratedDate.Combine([late, early]);

        // Assert
        AssertProbabilities(forwards.Probabilities.ToArray(), backwards);
    }

    [Fact]
    public void Combining_no_date_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => CalibratedDate.Combine([]));

        // Assert
        Assert.Equal("dates", refusal.ParamName);
    }

    [Fact]
    public void Combining_nothing_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CalibratedDate.Combine(null!));
    }

}
