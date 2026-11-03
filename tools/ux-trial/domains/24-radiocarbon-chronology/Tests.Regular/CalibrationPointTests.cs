using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalibrationPointTests {

    [Fact]
    public void A_generated_point_exposes_what_it_was_built_with() {
        // Arrange
        int    anyYear        = Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear).Generate();
        double anyAge         = Any.Double().Between(0d, RadiocarbonAge.MaximumYearsBp).Generate();
        double anyUncertainty = Any.Double().Positive().LessThanOrEqualTo(RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        CalibrationPoint point = new CalibrationPoint(anyYear, anyAge, anyUncertainty);

        // Assert
        Assert.Equal(anyYear, point.CalendarYear);
        Assert.Equal(anyAge, point.RadiocarbonYearsBp);
        Assert.Equal(anyUncertainty, point.Uncertainty);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_valid_points() {
        // Arrange
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act / Assert
        Assert.InRange(anyPoint.CalendarYear, CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
        Assert.InRange(anyPoint.RadiocarbonYearsBp, 0d, RadiocarbonAge.MaximumYearsBp);
        Assert.InRange(anyPoint.Uncertainty, double.Epsilon, RadiocarbonAge.MaximumUncertainty);
    }

    [Fact]
    public void A_year_outside_the_calendar_range_is_refused() {
        // Arrange
        int anyYearAfterTheRange = Any.Int32().GreaterThan(CalendarInterval.LatestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(anyYearAfterTheRange, 100d, 10d));

        // Assert
        Assert.Equal("calendarYear", error.ParamName);
    }

    [Fact]
    public void A_year_before_the_calendar_range_is_refused() {
        // Arrange
        int anyYearBeforeTheRange = Any.Int32().LessThan(CalendarInterval.EarliestYear).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(anyYearBeforeTheRange, 100d, 10d));

        // Assert
        Assert.Equal("calendarYear", error.ParamName);
    }

    [Fact]
    public void A_negative_curve_age_is_refused() {
        // Arrange
        double anyNegativeAge = Any.Double().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyNegativeAge, 10d));

        // Assert
        Assert.Equal("radiocarbonYearsBp", error.ParamName);
    }

    [Fact]
    public void A_curve_age_beyond_the_horizon_is_refused() {
        // Arrange
        double anyTooOldAge = Any.Double().GreaterThan(RadiocarbonAge.MaximumYearsBp).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyTooOldAge, 10d));

        // Assert
        Assert.Equal("radiocarbonYearsBp", error.ParamName);
    }

    [Fact]
    public void A_not_a_number_age_is_refused() {
        // Arrange / Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, double.NaN, 10d));

        // Assert
        Assert.Equal("radiocarbonYearsBp", error.ParamName);
    }

    [Fact]
    public void A_zero_curve_uncertainty_is_refused() {
        // Arrange
        double anyAge = Any.Double().Between(0d, RadiocarbonAge.MaximumYearsBp).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyAge, 0d));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

    [Fact]
    public void A_negative_curve_uncertainty_is_refused() {
        // Arrange
        double anyAge      = Any.Double().Between(0d, RadiocarbonAge.MaximumYearsBp).Generate();
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyAge, anyNegative));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

    [Fact]
    public void A_curve_uncertainty_above_the_ceiling_is_refused() {
        // Arrange
        double anyAge  = Any.Double().Between(0d, RadiocarbonAge.MaximumYearsBp).Generate();
        double anyHuge = Any.Double().GreaterThan(RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyAge, anyHuge));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

    [Fact]
    public void A_not_a_number_uncertainty_is_refused() {
        // Arrange
        double anyAge = Any.Double().Between(0d, RadiocarbonAge.MaximumYearsBp).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CalibrationPoint(0, anyAge, double.NaN));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

}
