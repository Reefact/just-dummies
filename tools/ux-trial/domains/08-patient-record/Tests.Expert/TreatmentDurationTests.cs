using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class TreatmentDurationTests {

    [Fact]
    public void A_treatment_lasts_at_most_a_year() {
        // Assert
        Assert.Equal(365, TreatmentDuration.MaxDays);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void A_duration_from_one_day_to_a_year_is_kept_as_given(int days) {
        // Act
        TreatmentDuration duration = new TreatmentDuration(days);

        // Assert
        Assert.Equal(days, duration.Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-1)]
    public void A_duration_outside_one_day_to_a_year_is_refused(int days) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TreatmentDuration(days));

        // Assert
        Assert.Equal("days", refusal.ParamName);
    }

    [Fact]
    public void A_negative_number_of_days_is_refused() {
        // Arrange
        int anyNegativeDays = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TreatmentDuration(anyNegativeDays));

        // Assert
        Assert.Equal("days", refusal.ParamName);
    }

    [Fact]
    public void More_than_a_year_is_refused() {
        // Arrange
        int anyTooManyDays = Any.Int32().GreaterThan(TreatmentDuration.MaxDays).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TreatmentDuration(anyTooManyDays));

        // Assert
        Assert.Equal("days", refusal.ParamName);
    }

    [Fact]
    public void A_one_day_treatment_ends_the_day_it_starts() {
        // Arrange
        DateOnly start = new DateOnly(2025, 3, 10);

        // Act
        DateOnly lastDay = new TreatmentDuration(1).LastDayFrom(start);

        // Assert
        Assert.Equal(new DateOnly(2025, 3, 10), lastDay);
    }

    [Theory]
    [InlineData(10, "2025-02-25", "2025-03-06")]    // crosses a month end in a common year
    [InlineData(10, "2024-02-25", "2024-03-05")]    // the same, in a leap year: the 29th counts
    [InlineData(365, "2025-01-01", "2025-12-31")]   // a full year starting on January 1st
    [InlineData(7, "2025-12-28", "2026-01-03")]     // crosses a year end
    public void The_last_day_is_the_start_plus_the_duration_minus_one_day(int days, string start, string expectedLastDay) {
        // Act
        DateOnly lastDay = new TreatmentDuration(days).LastDayFrom(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(DateOnly.Parse(expectedLastDay, System.Globalization.CultureInfo.InvariantCulture), lastDay);
    }

}
