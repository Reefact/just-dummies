using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalibrationPointTests {

    [Fact]
    public void A_point_keeps_the_year_the_age_and_the_uncertainty_it_was_given() {
        // Act
        CalibrationPoint point = new CalibrationPoint(-1_050, 3_000.5, 12.25);

        // Assert
        Assert.Equal(-1_050, point.CalendarYear);
        Assert.Equal(3_000.5, point.RadiocarbonYearsBp);
        Assert.Equal(12.25, point.Uncertainty);
    }

    [Theory]
    [InlineData(-55_000, 0, 5_000)]
    [InlineData(1_950, 55_000, 5_000)]
    [InlineData(0, 0, 0.000_001)]
    public void A_point_at_the_limits_is_accepted(int calendarYear, double yearsBp, double uncertainty) {
        // Act
        CalibrationPoint point = new CalibrationPoint(calendarYear, yearsBp, uncertainty);

        // Assert
        Assert.Equal(calendarYear, point.CalendarYear);
        Assert.Equal(yearsBp, point.RadiocarbonYearsBp);
        Assert.Equal(uncertainty, point.Uncertainty);
    }

    [Theory]
    [InlineData(-55_001)]
    [InlineData(1_951)]
    public void A_calendar_year_outside_the_calendar_is_refused(int calendarYear) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CalibrationPoint().WithCalendarYear(calendarYear).Generate());

        // Assert
        Assert.Equal("calendarYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.000_001)]
    [InlineData(55_000.000_001)]
    [InlineData(double.NaN)]
    public void A_curve_age_outside_the_scale_is_refused(double yearsBp) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CalibrationPoint().WithRadiocarbonYearsBp(yearsBp).Generate());

        // Assert
        Assert.Equal("radiocarbonYearsBp", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5_000.000_001)]
    [InlineData(double.NaN)]
    public void A_curve_uncertainty_that_is_not_in_zero_exclusive_to_five_thousand_is_refused(double uncertainty) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CalibrationPoint().WithUncertainty(uncertainty).Generate());

        // Assert
        Assert.Equal("uncertainty", refusal.ParamName);
    }

}
