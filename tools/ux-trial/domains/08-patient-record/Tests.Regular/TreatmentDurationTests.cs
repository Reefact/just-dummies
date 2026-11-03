using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class TreatmentDurationTests {

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void A_duration_from_one_to_365_days_is_accepted(int days) {
        TreatmentDuration duration = new TreatmentDuration(days);

        Assert.Equal(days, duration.Days);
    }

    [Fact]
    public void The_maximum_is_365_days() {
        Assert.Equal(365, TreatmentDuration.MaxDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-1)]
    public void A_duration_outside_one_to_365_days_is_out_of_range(int days) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreatmentDuration(days));
    }

    [Fact]
    public void Any_duration_below_one_day_is_out_of_range() {
        // Arrange
        int anyNonPositiveDays = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        Action construct = () => new TreatmentDuration(anyNonPositiveDays);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void Any_duration_above_a_year_is_out_of_range() {
        // Arrange
        int anyExcessiveDays = Any.Int32().GreaterThan(TreatmentDuration.MaxDays).Generate();

        // Act
        Action construct = () => new TreatmentDuration(anyExcessiveDays);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_one_day_treatment_ends_the_day_it_starts() {
        // Arrange
        TreatmentDuration oneDay = new TreatmentDuration(1);
        DateOnly          anyStart = Any.DateOnly().Between(new DateOnly(1900, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        DateOnly lastDay = oneDay.LastDayFrom(anyStart);

        // Assert
        Assert.Equal(anyStart, lastDay);
    }

    [Fact]
    public void A_ten_day_treatment_ends_nine_days_after_it_starts_across_a_leap_day() {
        // Arrange
        TreatmentDuration tenDays = new TreatmentDuration(10);

        // Act
        DateOnly lastDay = tenDays.LastDayFrom(new DateOnly(2024, 2, 25));

        // Assert
        Assert.Equal(new DateOnly(2024, 3, 5), lastDay);
    }

    [Fact]
    public void A_full_year_treatment_started_on_the_first_of_january_ends_on_the_31st_of_december_of_a_common_year() {
        // Arrange
        TreatmentDuration fullYear = new TreatmentDuration(365);

        // Act
        DateOnly lastDay = fullYear.LastDayFrom(new DateOnly(2023, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(2023, 12, 31), lastDay);
    }

}
