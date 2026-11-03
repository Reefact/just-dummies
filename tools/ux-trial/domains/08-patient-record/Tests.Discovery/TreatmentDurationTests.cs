using Clinical.Domain;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class TreatmentDurationTests {

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void A_duration_from_one_to_365_days_is_accepted(int days) {
        // Act
        TreatmentDuration duration = new TreatmentDuration(days);

        // Assert
        Assert.Equal(days, duration.Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MaxValue)]
    public void A_duration_below_one_or_above_365_days_is_out_of_range(int days) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TreatmentDuration(days));

        // Assert
        Assert.Equal("days", refusal.ParamName);
    }

    [Fact]
    public void A_one_day_treatment_ends_on_the_day_it_starts() {
        // Arrange
        TreatmentDuration duration = new TreatmentDuration(1);

        // Act
        DateOnly lastDay = duration.LastDayFrom(new DateOnly(2026, 3, 10));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 10), lastDay);
    }

    [Fact]
    public void A_ten_day_treatment_starting_on_the_first_ends_on_the_tenth() {
        // Arrange
        TreatmentDuration duration = new TreatmentDuration(10);

        // Act
        DateOnly lastDay = duration.LastDayFrom(new DateOnly(2026, 3, 1));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 10), lastDay);
    }

    [Fact]
    public void A_treatment_runs_across_month_ends() {
        // Arrange
        TreatmentDuration duration = new TreatmentDuration(5);

        // Act
        DateOnly lastDay = duration.LastDayFrom(new DateOnly(2026, 2, 26));

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 2), lastDay);
    }

    [Fact]
    public void A_full_year_treatment_started_on_the_first_of_a_leap_year_ends_on_the_30th_of_december() {
        // Arrange
        TreatmentDuration duration = new TreatmentDuration(TreatmentDuration.MaxDays);

        // Act
        DateOnly lastDay = duration.LastDayFrom(new DateOnly(2028, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(2028, 12, 30), lastDay);
    }

}
