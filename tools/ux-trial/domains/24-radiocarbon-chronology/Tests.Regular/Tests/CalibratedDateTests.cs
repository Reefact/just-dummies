using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalibratedDateTests {

    [Fact]
    public void Probabilities_that_do_not_sum_to_one_are_refused() {
        // Arrange
        double[] probabilities = [0.5, 0.4];

        // Act
        Action creating = () => new CalibratedDate(-1000, probabilities);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

    [Fact]
    public void The_highest_density_region_of_a_bimodal_date_is_two_disjoint_intervals() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, [0.3, 0.2, 0.0, 0.0, 0.2, 0.3]);

        // Act
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(0.95);

        // Assert
        Assert.Equal([new CalendarInterval(100, 101), new CalendarInterval(104, 105)], region);
    }

    [Fact]
    public void Truncating_renormalises_what_remains() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, [0.25, 0.25, 0.5]);

        // Act
        CalibratedDate truncated = date.Truncate(101, 102);

        // Assert
        Assert.Equal(0, truncated.ProbabilityOf(100));
        Assert.Equal(1.0 / 3, truncated.ProbabilityOf(101), 9);
        Assert.Equal(2.0 / 3, truncated.ProbabilityOf(102), 9);
    }

    [Fact]
    public void Truncating_away_all_the_probability_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, [0.5, 0.5]);

        // Act
        Action truncating = () => date.Truncate(200, 300);

        // Assert
        Assert.Throws<InvalidOperationException>(truncating);
    }

    [Fact]
    public void Combining_dates_gives_each_an_equal_weight() {
        // Arrange
        CalibratedDate first  = new CalibratedDate(100, [1.0]);
        CalibratedDate second = new CalibratedDate(110, [1.0]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([first, second]);

        // Assert
        Assert.Equal(0.5, combined.ProbabilityOf(100));
        Assert.Equal(0.5, combined.ProbabilityOf(110));
        Assert.Equal(105, combined.Mean, 9);
    }

}
