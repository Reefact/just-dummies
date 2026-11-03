using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class RadiocarbonAgeTests {

    [Theory]
    [InlineData(0, 1)]
    [InlineData(55_000, 5_000)]
    [InlineData(0, 5_000)]
    [InlineData(55_000, 1)]
    public void An_age_at_the_limits_of_the_scale_is_accepted(int yearsBp, int uncertainty) {
        // Act
        RadiocarbonAge age = new RadiocarbonAge(yearsBp, uncertainty);

        // Assert
        Assert.Equal(yearsBp, age.YearsBp);
        Assert.Equal(uncertainty, age.Uncertainty);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(55_001)]
    public void An_age_outside_the_scale_is_refused(int yearsBp) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.RadiocarbonAge().WithYearsBp(yearsBp).Generate());

        // Assert
        Assert.Equal("yearsBp", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5_001)]
    public void An_uncertainty_outside_one_to_five_thousand_years_is_refused(int uncertainty) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.RadiocarbonAge().WithUncertainty(uncertainty).Generate());

        // Assert
        Assert.Equal("uncertainty", refusal.ParamName);
    }

    [Fact]
    public void Correcting_for_a_reservoir_offset_makes_the_age_younger_and_keeps_the_uncertainty() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(3_400, 40);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(400);

        // Assert
        Assert.Equal(new RadiocarbonAge(3_000, 40), corrected);
    }

    [Fact]
    public void A_negative_offset_makes_the_age_older() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(3_000, 40);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(-200);

        // Assert
        Assert.Equal(new RadiocarbonAge(3_200, 40), corrected);
    }

    [Fact]
    public void An_offset_equal_to_the_age_brings_it_to_zero() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(300, 25);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(300);

        // Assert
        Assert.Equal(0, corrected.YearsBp);
    }

    [Fact]
    public void An_offset_one_year_beyond_the_age_is_refused() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(300, 25);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => measured.CorrectedFor(301));
    }

    [Fact]
    public void An_offset_that_takes_the_age_past_the_end_of_the_scale_is_refused() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(55_000, 25);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => measured.CorrectedFor(-1));
    }

    [Fact]
    public void Correcting_an_age_leaves_the_measured_one_unchanged() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(3_400, 40);

        // Act
        measured.CorrectedFor(400);

        // Assert
        Assert.Equal(3_400, measured.YearsBp);
    }

    [Fact]
    public void Two_ages_with_the_same_value_and_uncertainty_are_equal() {
        // Arrange
        RadiocarbonAge age     = new RadiocarbonAge(3_000, 30);
        RadiocarbonAge sameAge = new RadiocarbonAge(3_000, 30);

        // Assert
        Assert.Equal(age, sameAge);
        Assert.Equal(age.GetHashCode(), sameAge.GetHashCode());
    }

    [Theory]
    [InlineData(3_001, 30)]
    [InlineData(3_000, 31)]
    public void Ages_differing_by_value_or_by_uncertainty_are_not_equal(int yearsBp, int uncertainty) {
        // Arrange
        RadiocarbonAge age = new RadiocarbonAge(3_000, 30);

        // Assert
        Assert.NotEqual(age, new RadiocarbonAge(yearsBp, uncertainty));
    }

    [Fact]
    public void An_age_is_never_equal_to_nothing() {
        // Arrange
        RadiocarbonAge age = new RadiocarbonAge(3_000, 30);

        // Assert
        Assert.False(age.Equals(null));
    }

    [Fact]
    public void An_age_reads_as_value_plus_or_minus_uncertainty() {
        // Assert
        Assert.Equal("3000 ± 30 BP", new RadiocarbonAge(3_000, 30).ToString());
    }

}
