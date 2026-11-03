namespace RadiocarbonChronology.Tests.Discovery;

public sealed class CalibrationPointTests {

    [Theory]
    [InlineData(-55_000, 0.0, 0.5)]
    [InlineData(1_950, 55_000.0, 5_000.0)]
    [InlineData(0, 2_000.5, 12.25)]
    public void A_point_within_the_ranges_is_accepted(int calendarYear, double radiocarbonYearsBp, double uncertainty) {
        // Act
        CalibrationPoint point = new CalibrationPoint(calendarYear, radiocarbonYearsBp, uncertainty);

        // Assert
        Assert.Equal(calendarYear, point.CalendarYear);
        Assert.Equal(radiocarbonYearsBp, point.RadiocarbonYearsBp);
        Assert.Equal(uncertainty, point.Uncertainty);
    }

    [Theory]
    [InlineData(-55_001)]
    [InlineData(1_951)]
    public void A_calendar_year_outside_the_calendar_is_refused(int calendarYear) {
        // Arrange
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(calendarYear, anyPoint.RadiocarbonYearsBp, anyPoint.Uncertainty));

        // Assert
        Assert.Equal("calendarYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(55_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_curve_age_outside_0_to_55000_BP_is_refused(double radiocarbonYearsBp) {
        // Arrange
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(anyPoint.CalendarYear, radiocarbonYearsBp, anyPoint.Uncertainty));

        // Assert
        Assert.Equal("radiocarbonYearsBp", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(5_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_uncertainty_that_is_not_positive_or_exceeds_5000_is_refused(double uncertainty) {
        // Arrange
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(anyPoint.CalendarYear, anyPoint.RadiocarbonYearsBp, uncertainty));

        // Assert
        Assert.Equal("uncertainty", refusal.ParamName);
    }

}
