using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalibrationCurveTests {

    [Fact]
    public void An_age_read_on_a_straight_stretch_gives_one_period_around_its_calendar_year() {
        // Arrange
        CalibrationCurve straight = Curves.Straight();

        // Act
        OccupationEstimate estimate = new("US1", straight.Calibrate(new RadiocarbonAge(2_000, 30)));

        // Assert
        CalendarInterval period = Assert.Single(estimate.PlausibleIntervals);
        Assert.True(period.Contains(-50), $"{period} should contain -50.");
    }

    [Fact]
    public void An_age_read_on_a_reversal_of_the_curve_gives_two_plausible_periods() {
        // Arrange
        CalibrationCurve reversed = Curves.ReversedAroundTheEighthCenturyBce();

        // Act
        OccupationEstimate estimate = new("US1", reversed.Calibrate(new RadiocarbonAge(2_800, 20)));

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Contains(estimate.PlausibleIntervals, interval => interval.Contains(-900));
        Assert.Contains(estimate.PlausibleIntervals, interval => interval.Contains(-700));
        Assert.DoesNotContain(estimate.PlausibleIntervals, interval => interval.Contains(-800));
    }

    [Fact]
    public void An_age_the_curve_cannot_reach_is_refused() {
        // Arrange
        CalibrationCurve straight = Curves.Straight();   // reaches back to 6950 BP

        // Act
        Action calibrating = () => straight.Calibrate(new RadiocarbonAge(50_000, 20));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(calibrating);
    }

    [Fact]
    public void A_curve_refuses_points_out_of_calendar_order() {
        // Arrange
        string           anyName   = Any.String().Alpha().WithLengthBetween(3, 20).Generate();
        CalibrationPoint anyLater  = Any.CalibrationPoint().WithCalendarYear(100).Generate();
        CalibrationPoint anyEarlier = Any.CalibrationPoint().WithCalendarYear(50).Generate();

        // Act
        Action building = () => new CalibrationCurve(anyName, [anyLater, anyEarlier]);

        // Assert
        Assert.Throws<ArgumentException>(building);
    }

}
