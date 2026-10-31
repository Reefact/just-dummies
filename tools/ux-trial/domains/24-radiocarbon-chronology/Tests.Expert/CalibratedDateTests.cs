using JustDummies;

using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalibratedDateTests {

    [Fact]
    public void A_date_with_two_separate_peaks_has_two_disjoint_plausible_intervals() {
        // Arrange
        CalibratedDate twoPeaks = new(100, [0.3, 0.3, 0.0, 0.0, 0.2, 0.2]);

        // Act
        IReadOnlyList<CalendarInterval> region = twoPeaks.HighestDensityRegion(0.9);

        // Assert
        Assert.Equal([new CalendarInterval(100, 101), new CalendarInterval(104, 105)], region);
    }

    [Fact]
    public void Truncating_a_date_renormalises_the_years_it_keeps() {
        // Arrange
        CalibratedDate flat = new(10, [0.25, 0.25, 0.25, 0.25]);

        // Act
        CalibratedDate truncated = flat.Truncate(11, 12);

        // Assert
        Assert.Equal([0.0, 0.5, 0.5, 0.0], truncated.Probabilities);
    }

    [Fact]
    public void Truncating_away_all_the_probability_is_refused() {
        // Arrange
        CalibratedDate tenToThirteen = new(10, [0.25, 0.25, 0.25, 0.25]);

        // Act
        Action truncating = () => tenToThirteen.Truncate(20, 30);

        // Assert
        Assert.Throws<InvalidOperationException>(truncating);
    }

    [Fact]
    public void Combining_two_dates_mixes_them_in_equal_shares_over_both_spans() {
        // Arrange
        CalibratedDate yearZero = new(0, [1.0]);
        CalibratedDate yearTwo  = new(2, [1.0]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([yearZero, yearTwo]);

        // Assert
        Assert.Equal(0, combined.FirstYear);
        Assert.Equal([0.5, 0.0, 0.5], combined.Probabilities);
    }

    [Fact]
    public void The_mean_of_a_date_weighs_each_year_by_its_probability() {
        // Arrange
        CalibratedDate skewed = new(-10, [0.75, 0.0, 0.25]);

        // Act
        double mean = skewed.Mean;

        // Assert
        Assert.Equal(-9.5, mean, precision: 9);
    }

    [Fact]
    public void Probabilities_that_do_not_sum_to_one_are_refused() {
        // Arrange
        int anyFirstYear = Any.Int32().Between(CalendarInterval.EarliestYear, 0).Generate();

        // Act
        Action dating = () => new CalibratedDate(anyFirstYear, [0.5, 0.4]);

        // Assert
        Assert.Throws<ArgumentException>(dating);
    }

    [Fact]
    public void An_occupation_estimate_needs_the_code_of_its_layer() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        Action estimating = () => new OccupationEstimate("  ", anyDate);

        // Assert
        Assert.Throws<ArgumentException>(estimating);
    }

}
