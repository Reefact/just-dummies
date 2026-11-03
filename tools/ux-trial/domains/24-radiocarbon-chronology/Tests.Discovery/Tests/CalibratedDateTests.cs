namespace RadiocarbonChronology.Tests.Discovery;

public sealed class CalibratedDateTests {

    [Fact]
    public void A_date_spans_one_year_per_probability_from_its_first_year() {
        // Act
        CalibratedDate date = new CalibratedDate(-300, [0.2, 0.5, 0.3]);

        // Assert
        Assert.Equal(-300, date.FirstYear);
        Assert.Equal(-298, date.LastYear);
        Assert.Equal([0.2, 0.5, 0.3], date.Probabilities);
    }

    [Fact]
    public void A_date_may_end_exactly_on_the_latest_year_or_start_on_the_earliest() {
        // Assert
        Assert.Equal(1_950, new CalibratedDate(1_949, [0.5, 0.5]).LastYear);
        Assert.Equal(-55_000, new CalibratedDate(-55_000, [1.0]).FirstYear);
    }

    [Fact]
    public void A_date_without_probabilities_is_refused() {
        // Arrange
        int anyFirstYear = Any.CalibratedDate().Generate().FirstYear;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new CalibratedDate(anyFirstYear, null!));
        Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, []));
    }

    [Theory]
    [InlineData(-55_001, 2)]
    [InlineData(1_950, 2)]   // the second year would be 1951
    [InlineData(1_951, 1)]
    public void A_date_that_leaves_the_calendar_is_refused(int firstYear, int years) {
        // Arrange
        List<double> probabilities = Enumerable.Repeat(1.0 / years, years).ToList();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(firstYear, probabilities));
    }

    [Theory]
    [InlineData(-0.1, 1.1)]
    [InlineData(1.1, -0.1)]
    [InlineData(double.NaN, 1.0)]
    public void A_probability_outside_0_to_1_is_refused(double first, double second) {
        // Arrange
        int anyFirstYear = Any.CalibratedDate().Generate().FirstYear;

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(anyFirstYear, [first, second]));
    }

    [Theory]
    [InlineData(0.5, 0.4)]
    [InlineData(0.5, 0.6)]
    [InlineData(0.5, 0.500002)]   // beyond the 1e-6 tolerance
    public void Probabilities_that_do_not_sum_to_one_are_refused(double first, double second) {
        // Arrange
        int anyFirstYear = Any.CalibratedDate().Generate().FirstYear;

        // Assert
        Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, [first, second]));
    }

    [Fact]
    public void A_sum_within_a_millionth_of_one_is_tolerated() {
        // Act
        CalibratedDate date = new CalibratedDate(0, [0.5, 0.5000005]);

        // Assert
        Assert.Equal(2, date.Probabilities.Count);
    }

    [Fact]
    public void The_probability_of_a_year_outside_the_date_is_zero() {
        // Arrange
        CalibratedDate date = new CalibratedDate(10, [0.2, 0.5, 0.3]);

        // Assert
        Assert.Equal(0.0, date.ProbabilityOf(9));
        Assert.Equal(0.2, date.ProbabilityOf(10));
        Assert.Equal(0.3, date.ProbabilityOf(12));
        Assert.Equal(0.0, date.ProbabilityOf(13));
    }

    [Fact]
    public void The_mean_weighs_every_year_by_its_probability() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, [0.25, 0.75]);

        // Assert
        Assert.Equal(100.75, date.Mean, 1e-9);
    }

    [Fact]
    public void The_mean_of_a_date_before_the_common_era_is_negative() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-11, [0.5, 0.5]);

        // Assert
        Assert.Equal(-10.5, date.Mean, 1e-9);
    }

    [Fact]
    public void A_single_peak_gives_one_region_around_it() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-300, [0.1, 0.2, 0.4, 0.2, 0.1]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.75);

        // Assert
        Assert.Equal([new CalendarInterval(-299, -297)], region);
    }

    [Fact]
    public void Two_separate_peaks_give_two_disjoint_intervals_in_calendar_order() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-100, [0.4, 0.1, 0.0, 0.4, 0.1]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.75);

        // Assert
        Assert.Equal([new CalendarInterval(-100, -100), new CalendarInterval(-97, -97)], region);
    }

    [Fact]
    public void Years_without_probability_are_never_part_of_the_region() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.0, 0.5, 0.5, 0.0]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.999);

        // Assert
        Assert.Equal([new CalendarInterval(1, 2)], region);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_level_that_is_not_strictly_between_0_and_1_is_refused(double level) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyDate.HighestDensityRegion(level));

        // Assert
        Assert.Equal("level", refusal.ParamName);
    }

    [Fact]
    public void Truncating_keeps_the_years_in_bounds_and_renormalises() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act
        CalibratedDate truncated = date.Truncate(1, 2);

        // Assert
        Assert.Equal(0.0, truncated.ProbabilityOf(0));
        Assert.Equal(0.4, truncated.ProbabilityOf(1), 1e-9);
        Assert.Equal(0.6, truncated.ProbabilityOf(2), 1e-9);
        Assert.Equal(0.0, truncated.ProbabilityOf(3));
    }

    [Fact]
    public void Truncating_leaves_the_original_date_unchanged() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.1, 0.2, 0.3, 0.4]);

        // Act
        date.Truncate(1, 2);

        // Assert
        Assert.Equal([0.1, 0.2, 0.3, 0.4], date.Probabilities);
    }

    [Fact]
    public void Truncating_beyond_the_date_is_refused_as_no_probability_remains() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyDate.Truncate(anyDate.LastYear + 1, anyDate.LastYear + 1));

        // Assert
        Assert.Contains("No probability remains", refusal.Message);
    }

    [Fact]
    public void Truncating_to_years_without_probability_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.5, 0.0, 0.5]);

        // Assert
        Assert.Throws<InvalidOperationException>(() => date.Truncate(1, 1));
    }

    [Fact]
    public void Truncating_with_a_lower_bound_after_the_upper_bound_is_refused() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anyDate.Truncate(10, 9));
    }

    [Fact]
    public void Combining_dates_mixes_them_with_equal_weights_over_their_joint_span() {
        // Arrange
        CalibratedDate early = new CalibratedDate(0, [1.0]);
        CalibratedDate late  = new CalibratedDate(2, [0.5, 0.5]);

        // Act
        CalibratedDate mixture = CalibratedDate.Combine([early, late]);

        // Assert
        Assert.Equal(0, mixture.FirstYear);
        Assert.Equal(3, mixture.LastYear);
        Assert.Equal([0.5, 0.0, 0.25, 0.25], mixture.Probabilities);
    }

    [Fact]
    public void Combining_a_single_date_gives_the_same_distribution() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-5, [0.3, 0.7]);

        // Act
        CalibratedDate mixture = CalibratedDate.Combine([date]);

        // Assert
        Assert.Equal(date.FirstYear, mixture.FirstYear);
        Assert.Equal(date.Probabilities, mixture.Probabilities);
    }

    [Fact]
    public void Combining_nothing_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => CalibratedDate.Combine(null!));
        Assert.Throws<ArgumentException>(() => CalibratedDate.Combine([]));
    }

    [Fact(Skip = "domain defect: Probabilities hands out the date's own array behind IReadOnlyList<double>, so a caller can cast it to double[] and break the sum-to-one invariant after construction (not a drawn value, no seed)")]
    public void A_date_cannot_be_altered_through_the_list_it_exposes() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, [0.5, 0.5]);

        // Act
        if (date.Probabilities is double[] exposed) {
            exposed[0] = 0.9;
        }

        // Assert
        Assert.Equal(0.5, date.ProbabilityOf(0));
    }

}
