using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalibrationTests {

    [Fact]
    public void An_age_is_read_at_the_year_the_curve_records_it() {
        // Arrange
        RadiocarbonAge anyAgeOf3000Bp = Arbitrary.RadiocarbonAge()
                                                 .WithYearsBp(3000)
                                                 .WithUncertainty(u => u.Between(10, 100))
                                                 .Generate();

        // Act
        CalibratedDate date = Curves.Linear().Calibrate(anyAgeOf3000Bp);

        // Assert
        Assert.Equal(-1050, date.Mean, 1.0);
    }

    [Fact]
    public void A_calibrated_date_covers_the_whole_curve_year_by_year() {
        // Arrange
        RadiocarbonAge anyAge = Arbitrary.RadiocarbonAge().WithYearsBp(y => y.Between(500, 4500)).WithUncertainty(u => u.Between(10, 100)).Generate();

        // Act
        CalibratedDate date = Curves.Linear().Calibrate(anyAge);

        // Assert
        Assert.Equal(-3000, date.FirstYear);
        Assert.Equal(1950, date.LastYear);
    }

    [Fact]
    public void An_age_the_curve_never_reaches_is_refused() {
        // Arrange
        RadiocarbonAge beyondTheCurve = new RadiocarbonAge(50_000, 10);

        // Act
        Action calibrating = () => Curves.Linear().Calibrate(beyondTheCurve);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(calibrating);
    }

    [Fact]
    public void A_wiggle_in_the_curve_gives_a_date_with_two_plausible_intervals() {
        // Arrange
        RadiocarbonAge measurement = new RadiocarbonAge(1950, 10);
        CalibratedDate date        = Curves.Wiggle().Calibrate(measurement);

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(2, estimate.PlausibleIntervals.Count);
        Assert.True(estimate.PlausibleIntervals[0].EndYear < 100);
        Assert.True(estimate.PlausibleIntervals[1].StartYear > 100);
    }

    [Fact]
    public void Curve_points_out_of_calendar_order_are_refused() {
        // Arrange
        CalibrationPoint later   = Arbitrary.CalibrationPoint().WithCalendarYear(500).Generate();
        CalibrationPoint earlier = Arbitrary.CalibrationPoint().WithCalendarYear(-500).Generate();
        AnyCalibrationCurve anyCurveOutOfOrder = Arbitrary.CalibrationCurve().WithPoints([later, earlier]);

        // Act
        Action creating = () => anyCurveOutOfOrder.Generate();

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

}
