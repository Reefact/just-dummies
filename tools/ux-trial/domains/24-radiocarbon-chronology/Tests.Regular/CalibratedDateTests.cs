using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalibratedDateTests {

    [Fact]
    public void A_generated_date_spans_one_year_per_probability() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        int span = anyDate.LastYear - anyDate.FirstYear + 1;

        // Assert
        Assert.Equal(anyDate.Probabilities.Count, span);
    }

    [Fact]
    public void The_last_year_is_the_first_year_plus_the_span_less_one() {
        // Arrange
        int            anyFirstYear = Any.Int32().Between(CalendarInterval.EarliestYear, 0).Generate();
        CalibratedDate date         = new CalibratedDate(anyFirstYear, new[] { 0.2, 0.3, 0.5 });

        // Act
        int lastYear = date.LastYear;

        // Assert
        Assert.Equal(anyFirstYear + 2, lastYear);
    }

    [Fact]
    public void A_date_without_probabilities_is_refused() {
        // Arrange
        int anyFirstYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, Array.Empty<double>()));
    }

    [Fact]
    public void A_missing_distribution_is_refused() {
        // Arrange
        int anyFirstYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CalibratedDate(anyFirstYear, null!));
    }

    [Fact]
    public void A_first_year_before_the_calendar_range_is_refused() {
        // Arrange
        int anyFirstYear = Any.Int32().LessThan(CalendarInterval.EarliestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(anyFirstYear, new[] { 1d }));

        // Assert
        Assert.Equal("firstYear", error.ParamName);
    }

    [Fact]
    public void A_span_running_past_the_last_calendar_year_is_refused() {
        // Arrange / Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(CalendarInterval.LatestYear, new[] { 0.5, 0.5 }));

        // Assert
        Assert.Equal("firstYear", error.ParamName);
    }

    [Fact]
    public void A_span_ending_exactly_on_the_last_calendar_year_is_accepted() {
        // Arrange / Act
        CalibratedDate date = new CalibratedDate(CalendarInterval.LatestYear - 1, new[] { 0.5, 0.5 });

        // Assert
        Assert.Equal(CalendarInterval.LatestYear, date.LastYear);
    }

    [Fact]
    public void A_negative_probability_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(0, new[] { anyNegative, 1d - anyNegative }));

        // Assert
        Assert.Equal("probabilities", error.ParamName);
    }

    [Fact]
    public void A_probability_above_one_is_refused() {
        // Arrange
        double anyAboveOne = Any.Double().GreaterThan(1d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(0, new[] { anyAboveOne }));

        // Assert
        Assert.Equal("probabilities", error.ParamName);
    }

    [Fact]
    public void A_not_a_number_probability_is_refused() {
        // Arrange / Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibratedDate(0, new[] { double.NaN, 1d }));

        // Assert
        Assert.Equal("probabilities", error.ParamName);
    }

    [Fact]
    public void Probabilities_that_do_not_sum_to_one_are_refused() {
        // Arrange
        int anyFirstYear = Any.Int32().Between(CalendarInterval.EarliestYear, 0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, new[] { 0.5, 0.4 }));
        Assert.Throws<ArgumentException>(() => new CalibratedDate(anyFirstYear, new[] { 0.6, 0.6 }));
    }

    [Fact]
    public void A_sum_within_the_tolerance_of_one_is_accepted() {
        // Arrange
        double slightlyOff = 1d - (CalibratedDate.SumTolerance / 2);

        // Act
        CalibratedDate date = new CalibratedDate(0, new[] { slightlyOff });

        // Assert
        Assert.Equal(slightlyOff, date.ProbabilityOf(0));
    }

    [Fact]
    public void A_sum_beyond_the_tolerance_of_one_is_refused() {
        // Arrange
        double tooFarOff = 1d - (CalibratedDate.SumTolerance * 2);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibratedDate(0, new[] { tooFarOff }));
    }

    [Fact]
    public void The_date_is_not_changed_by_a_later_change_of_the_list_it_was_built_from() {
        // Arrange
        double[]       source = { 0.4, 0.6 };
        CalibratedDate date   = new CalibratedDate(100, source);

        // Act
        source[0] = 0.9;

        // Assert
        Assert.Equal(0.4, date.ProbabilityOf(100));
    }

    [Fact]
    public void The_probability_of_a_year_is_read_from_its_position() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-50, new[] { 0.1, 0.2, 0.7 });

        // Act / Assert
        Assert.Equal(0.1, date.ProbabilityOf(-50));
        Assert.Equal(0.2, date.ProbabilityOf(-49));
        Assert.Equal(0.7, date.ProbabilityOf(-48));
    }

    [Fact]
    public void A_year_outside_the_span_has_no_probability() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act / Assert
        Assert.Equal(0d, anyDate.ProbabilityOf(anyDate.FirstYear - 1));
        Assert.Equal(0d, anyDate.ProbabilityOf(anyDate.LastYear + 1));
    }

    [Fact]
    public void The_mean_of_a_certain_year_is_that_year() {
        // Arrange
        int            anyYear = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();
        CalibratedDate date    = new CalibratedDate(anyYear, new[] { 1d });

        // Act
        double mean = date.Mean;

        // Assert
        Assert.Equal(anyYear, mean);
    }

    [Fact]
    public void The_mean_weighs_each_year_by_its_probability() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-100, new[] { 0.25, 0.25, 0.5 });

        // Act
        double mean = date.Mean;

        // Assert
        Assert.Equal(-98.75, mean, 9);
    }

    [Fact]
    public void The_mean_of_a_generated_date_lies_within_its_span() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        double mean = anyDate.Mean;

        // Assert
        Assert.InRange(mean, anyDate.FirstYear - 1e-6, anyDate.LastYear + 1e-6);
    }

    [Fact]
    public void The_highest_density_region_of_a_single_peak_is_one_interval() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, new[] { 0.1, 0.2, 0.4, 0.3 });

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.6);

        // Assert
        CalendarInterval only = Assert.Single(region);
        Assert.Equal(new CalendarInterval(2, 3), only);
    }

    [Fact]
    public void A_multimodal_date_gives_disjoint_intervals_in_calendar_order() {
        // Arrange
        CalibratedDate date = new CalibratedDate(1000, new[] { 0.4, 0d, 0d, 0.2, 0d, 0.4 });

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.8);

        // Assert
        Assert.Equal(new[] { new CalendarInterval(1000, 1000), new CalendarInterval(1005, 1005) }, region);
    }

    [Fact]
    public void Raising_the_level_widens_the_region_to_include_the_smaller_modes() {
        // Arrange
        CalibratedDate date = new CalibratedDate(1000, new[] { 0.4, 0d, 0d, 0.2, 0d, 0.4 });

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.95);

        // Assert
        Assert.Equal(3, region.Count);
        Assert.Equal(new CalendarInterval(1003, 1003), region[1]);
    }

    [Fact]
    public void Neighbouring_years_are_merged_into_one_interval() {
        // Arrange
        CalibratedDate date = new CalibratedDate(10, new[] { 0.05, 0.3, 0.3, 0.3, 0.05 });

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.85);

        // Assert
        Assert.Equal(new[] { new CalendarInterval(11, 13) }, region);
    }

    [Fact]
    public void The_region_of_a_generated_date_holds_at_least_the_requested_probability() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();
        double         anyLevel = Any.Double().Between(0.01, 0.99).Generate();

        // Act
        IReadOnlyList<CalendarInterval> region = anyDate.HighestDensityRegion(anyLevel);

        // Assert
        double held = region.Sum(interval => Enumerable.Range(interval.StartYear, interval.LengthInYears).Sum(anyDate.ProbabilityOf));
        Assert.True(held >= anyLevel - 1e-9, $"{held} < {anyLevel}");
    }

    [Fact]
    public void A_zero_probability_year_never_enters_the_region() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, new[] { 0.5, 0d, 0.5 });

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.999);

        // Assert
        Assert.DoesNotContain(region, interval => interval.Contains(1));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_level_outside_the_open_unit_interval_is_refused(double level) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyDate.HighestDensityRegion(level));

        // Assert
        Assert.Equal("level", error.ParamName);
    }

    [Fact]
    public void Truncating_keeps_the_bounded_years_and_renormalises_them() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.1, 0.2, 0.3, 0.4 });

        // Act
        CalibratedDate truncated = date.Truncate(101, 102);

        // Assert
        Assert.Equal(0d, truncated.ProbabilityOf(100));
        Assert.Equal(0.4, truncated.ProbabilityOf(101), 9);
        Assert.Equal(0.6, truncated.ProbabilityOf(102), 9);
        Assert.Equal(0d, truncated.ProbabilityOf(103));
    }

    [Fact]
    public void Truncating_keeps_the_span_of_the_date() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();
        int            anyYearInside = Any.Int32().Between(anyDate.FirstYear, anyDate.LastYear).Generate();

        // Act
        CalibratedDate truncated = anyDate.Truncate(anyYearInside, CalendarInterval.LatestYear);

        // Assert
        Assert.Equal(anyDate.FirstYear, truncated.FirstYear);
        Assert.Equal(anyDate.LastYear, truncated.LastYear);
    }

    [Fact]
    public void A_truncated_date_still_sums_to_one() {
        // Arrange
        CalibratedDate date = new CalibratedDate(0, new[] { 0.1, 0.2, 0.3, 0.4 });

        // Act
        CalibratedDate truncated = date.Truncate(0, 2);

        // Assert
        Assert.Equal(1d, truncated.Probabilities.Sum(), 9);
    }

    [Fact]
    public void Truncating_to_a_range_wider_than_the_span_changes_nothing() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.1, 0.2, 0.3, 0.4 });

        // Act
        CalibratedDate truncated = date.Truncate(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);

        // Assert
        Assert.Equal(date.Probabilities, truncated.Probabilities);
    }

    [Fact]
    public void Truncating_where_no_probability_remains_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.5, 0.5 });

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => date.Truncate(200, 300));
        Assert.Throws<InvalidOperationException>(() => date.Truncate(0, 99));
    }

    [Fact]
    public void Truncating_to_a_zero_probability_window_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.5, 0d, 0.5 });

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => date.Truncate(101, 101));
    }

    [Fact]
    public void Truncating_with_the_bounds_reversed_is_refused() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();
        int            anyBound = Any.Int32().Between(CalendarInterval.EarliestYear + 1, CalendarInterval.LatestYear).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyDate.Truncate(anyBound, anyBound - 1));
    }

    [Fact]
    public void Combining_two_dates_gives_each_half_the_weight() {
        // Arrange
        CalibratedDate early = new CalibratedDate(0, new[] { 1d });
        CalibratedDate late  = new CalibratedDate(2, new[] { 1d });

        // Act
        CalibratedDate mixture = CalibratedDate.Combine(new[] { early, late });

        // Assert
        Assert.Equal(0, mixture.FirstYear);
        Assert.Equal(2, mixture.LastYear);
        Assert.Equal(new[] { 0.5, 0d, 0.5 }, mixture.Probabilities);
    }

    [Fact]
    public void Combining_a_single_date_gives_the_same_distribution() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        CalibratedDate mixture = CalibratedDate.Combine(new[] { anyDate });

        // Assert
        Assert.Equal(anyDate.FirstYear, mixture.FirstYear);
        Assert.Equal(anyDate.Probabilities, mixture.Probabilities);
    }

    [Fact]
    public void Combined_dates_cover_the_span_of_all_of_them() {
        // Arrange
        CalibratedDate anyFirst  = Any.CalibratedDate().Generate();
        CalibratedDate anySecond = Any.CalibratedDate().Generate();

        // Act
        CalibratedDate mixture = CalibratedDate.Combine(new[] { anyFirst, anySecond });

        // Assert
        Assert.Equal(Math.Min(anyFirst.FirstYear, anySecond.FirstYear), mixture.FirstYear);
        Assert.Equal(Math.Max(anyFirst.LastYear, anySecond.LastYear), mixture.LastYear);
    }

    [Fact]
    public void Two_dates_apart_from_each_other_give_a_multimodal_mixture() {
        // Arrange
        CalibratedDate early = new CalibratedDate(-3000, new[] { 0.25, 0.5, 0.25 });
        CalibratedDate late  = new CalibratedDate(-1000, new[] { 0.25, 0.5, 0.25 });

        // Act
        CalibratedDate mixture = CalibratedDate.Combine(new[] { early, late });
        IReadOnlyList<CalendarInterval> region = mixture.HighestDensityRegion(0.9);

        // Assert
        Assert.Equal(2, region.Count);
        Assert.Equal(-1999d, mixture.Mean, 9);
    }

    [Fact]
    public void Combining_nothing_is_refused() {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => CalibratedDate.Combine(Array.Empty<CalibratedDate>()));
        Assert.Throws<ArgumentNullException>(() => CalibratedDate.Combine(null!));
    }

}
