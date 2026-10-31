using JustDummies;
using JustDummies.Xunit;

using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Discovery;

[Reproducible]
public sealed class RadiocarbonAgeTests {

    [Fact]
    public void Correcting_for_a_reservoir_offset_subtracts_it_from_the_measured_age() {
        // Arrange
        RadiocarbonAge age = Any.RadiocarbonAge().WithYearsBp(3000).Generate();

        // Act
        RadiocarbonAge corrected = age.CorrectedFor(400);

        // Assert
        Assert.Equal(2600, corrected.YearsBp);
    }

    [Fact]
    public void Correcting_for_a_reservoir_offset_keeps_the_laboratory_uncertainty() {
        // Arrange
        RadiocarbonAge age = Any.RadiocarbonAge().WithYearsBp(3000).WithUncertainty(35).Generate();

        // Act
        RadiocarbonAge corrected = age.CorrectedFor(400);

        // Assert
        Assert.Equal(35, corrected.Uncertainty);
    }

    [Fact]
    public void A_reservoir_offset_larger_than_the_age_is_refused() {
        // Arrange
        RadiocarbonAge age = Any.RadiocarbonAge().WithYearsBp(100).Generate();

        // Act
        Action correcting = () => age.CorrectedFor(300);

        // Assert
        Assert.Throws<InvalidOperationException>(correcting);
    }

}

public sealed class CalendarIntervalTests {

    [Fact]
    public void Two_intervals_sharing_a_single_year_overlap() {
        // Arrange
        CalendarInterval earlier = new CalendarInterval(-200, -100);
        CalendarInterval later   = new CalendarInterval(-100, 50);

        // Act
        bool overlap = earlier.Overlaps(later);

        // Assert
        Assert.True(overlap);
    }

    [Fact]
    public void Two_consecutive_intervals_do_not_overlap() {
        // Arrange
        CalendarInterval earlier = new CalendarInterval(-200, -101);
        CalendarInterval later   = new CalendarInterval(-100, 50);

        // Act
        bool overlap = earlier.Overlaps(later);

        // Assert
        Assert.False(overlap);
    }

    [Fact]
    public void The_length_of_an_interval_counts_both_ends_across_year_zero() {
        // Arrange
        CalendarInterval interval = new CalendarInterval(-50, 49);

        // Act
        int length = interval.LengthInYears;

        // Assert
        Assert.Equal(100, length);
    }

    [Fact]
    public void An_interval_ending_before_it_starts_is_refused() {
        // Arrange

        // Act
        Action creating = () => new CalendarInterval(100, 99);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

}

public sealed class CalibratedDateTests {

    [Fact]
    public void A_date_with_two_separate_peaks_has_two_disjoint_plausible_intervals() {
        // Arrange
        CalibratedDate bimodal = new CalibratedDate(1000, [0.45, 0.05, 0.0, 0.05, 0.45]);

        // Act
        IReadOnlyList<CalendarInterval> region = bimodal.HighestDensityRegion(0.8);

        // Assert
        Assert.Equal([new CalendarInterval(1000, 1000), new CalendarInterval(1004, 1004)], region);
    }

    [Fact]
    public void Truncating_a_date_renormalises_what_remains() {
        // Arrange
        CalibratedDate date = new CalibratedDate(1000, [0.25, 0.25, 0.5]);

        // Act
        CalibratedDate truncated = date.Truncate(1000, 1001);

        // Assert
        Assert.Equal(0.5, truncated.ProbabilityOf(1000), 9);
        Assert.Equal(0.5, truncated.ProbabilityOf(1001), 9);
        Assert.Equal(0.0, truncated.ProbabilityOf(1002), 9);
    }

    [Fact]
    public void Truncating_a_date_outside_all_its_probability_is_refused() {
        // Arrange
        CalibratedDate date = new CalibratedDate(1000, [0.5, 0.5]);

        // Act
        Action truncating = () => date.Truncate(1500, 1600);

        // Assert
        Assert.Throws<InvalidOperationException>(truncating);
    }

    [Fact]
    public void Combining_two_dates_weighs_them_equally() {
        // Arrange
        CalibratedDate first  = new CalibratedDate(1000, [1.0]);
        CalibratedDate second = new CalibratedDate(1002, [1.0]);

        // Act
        CalibratedDate combined = CalibratedDate.Combine([first, second]);

        // Assert
        Assert.Equal([0.5, 0.0, 0.5], combined.Probabilities);
    }

    [Fact]
    public void Probabilities_that_do_not_sum_to_one_are_refused() {
        // Arrange

        // Act
        Action creating = () => new CalibratedDate(1000, [0.5, 0.4]);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

}

[Reproducible]
public sealed class LayerAndRelationTests {

    [Fact]
    public void A_layer_code_without_its_unit_letters_is_refused() {
        // Arrange
        string anyDescription = Any.String().NotBlank().WithMaxLength(500).Generate();

        // Act
        Action creating = () => new StratigraphicLayer("12", anyDescription);

        // Assert
        Assert.Throws<FormatException>(creating);
    }

    [Fact]
    public void A_layer_cannot_precede_itself() {
        // Arrange
        string anyCode = Any.StratigraphicLayer().Generate().Code;

        // Act
        Action creating = () => new PrecedenceRelation(anyCode, anyCode);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

}
