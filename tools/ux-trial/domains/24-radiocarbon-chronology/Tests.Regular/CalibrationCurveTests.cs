using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class CalibrationCurveTests {

    [Fact]
    public void A_generated_curve_lists_its_points_in_increasing_calendar_order() {
        // Arrange
        CalibrationCurve anyCurve = Any.CalibrationCurve().Generate();

        // Act
        IReadOnlyList<CalibrationPoint> points = anyCurve.Points;

        // Assert
        Assert.True(points.Count >= 2);
        for (int i = 1; i < points.Count; i++) {
            Assert.True(points[i].CalendarYear > points[i - 1].CalendarYear);
        }
    }

    [Fact]
    public void The_coverage_runs_from_the_first_to_the_last_point() {
        // Arrange
        CalibrationCurve anyCurve = Any.CalibrationCurve().Generate();

        // Act
        CalendarInterval coverage = anyCurve.Coverage;

        // Assert
        Assert.Equal(anyCurve.Points[0].CalendarYear, coverage.StartYear);
        Assert.Equal(anyCurve.Points[^1].CalendarYear, coverage.EndYear);
    }

    [Fact]
    public void A_curve_keeps_its_name() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();
        IReadOnlyList<CalibrationPoint> anyPoints = Any.CalibrationCurve().Generate().Points;

        // Act
        CalibrationCurve curve = new CalibrationCurve(anyName, anyPoints);

        // Assert
        Assert.Equal(anyName, curve.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_curve_without_a_name_is_refused(string? name) {
        // Arrange
        IReadOnlyList<CalibrationPoint> anyPoints = Any.CalibrationCurve().Generate().Points;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(name!, anyPoints));
    }

    [Fact]
    public void A_curve_without_points_is_refused() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CalibrationCurve(anyName, null!));
    }

    [Fact]
    public void A_curve_needs_at_least_two_points() {
        // Arrange
        string           anyName  = Any.String().NotBlank().Generate();
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, Array.Empty<CalibrationPoint>()));
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, new[] { anyPoint }));
    }

    [Fact]
    public void A_null_point_is_refused() {
        // Arrange
        string           anyName  = Any.String().NotBlank().Generate();
        CalibrationPoint anyPoint = Any.CalibrationPoint().WithCalendarYear(100).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, new[] { anyPoint, null! }));

        // Assert
        Assert.Contains("1", error.Message);
    }

    [Fact]
    public void Points_in_decreasing_calendar_order_are_refused() {
        // Arrange
        string           anyName  = Any.String().NotBlank().Generate();
        CalibrationPoint later    = Any.CalibrationPoint().WithCalendarYear(200).Generate();
        CalibrationPoint earlier  = Any.CalibrationPoint().WithCalendarYear(100).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, new[] { later, earlier }));
    }

    [Fact]
    public void Two_points_on_the_same_calendar_year_are_refused() {
        // Arrange
        string           anyName = Any.String().NotBlank().Generate();
        CalibrationPoint first   = Any.CalibrationPoint().WithCalendarYear(100).Generate();
        CalibrationPoint second  = Any.CalibrationPoint().WithCalendarYear(100).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, new[] { first, second }));
    }

    [Fact]
    public void Calibrating_gives_one_probability_per_year_of_the_coverage() {
        // Arrange
        CalibrationCurve curve  = TestCurves.Linear();
        RadiocarbonAge   anyAge = Any.RadiocarbonAge().WithYearsBp(3000).Generate();

        // Act
        CalibratedDate date = curve.Calibrate(anyAge);

        // Assert
        Assert.Equal(curve.Coverage.StartYear, date.FirstYear);
        Assert.Equal(curve.Coverage.EndYear, date.LastYear);
        Assert.Equal(curve.Coverage.LengthInYears, date.Probabilities.Count);
    }

    [Fact]
    public void A_calibrated_date_sums_to_one() {
        // Arrange
        CalibrationCurve curve = TestCurves.Linear();
        int              anyYearsBp = Any.Int32().Between(100, 9000).Generate();
        RadiocarbonAge   age = Any.RadiocarbonAge().WithYearsBp(anyYearsBp).Generate();

        // Act
        CalibratedDate date = curve.Calibrate(age);

        // Assert
        Assert.Equal(1d, date.Probabilities.Sum(), 9);
    }

    [Fact]
    public void An_age_peaks_in_the_year_the_curve_gives_it() {
        // Arrange
        CalibrationCurve curve = TestCurves.Linear();
        RadiocarbonAge   age   = new RadiocarbonAge(3000, 30);

        // Act
        CalibratedDate date = curve.Calibrate(age);

        // Assert
        Assert.Equal(-1050d, date.Mean, 0);
        Assert.True(date.ProbabilityOf(-1050) > date.ProbabilityOf(-1100));
        Assert.True(date.ProbabilityOf(-1050) > date.ProbabilityOf(-1000));
    }

    [Fact]
    public void A_larger_laboratory_uncertainty_spreads_the_date() {
        // Arrange
        CalibrationCurve curve = TestCurves.Linear();

        // Act
        CalibratedDate precise = curve.Calibrate(new RadiocarbonAge(3000, 20));
        CalibratedDate vague   = curve.Calibrate(new RadiocarbonAge(3000, 200));

        // Assert
        int preciseLength = precise.HighestDensityRegion(0.954).Sum(interval => interval.LengthInYears);
        int vagueLength   = vague.HighestDensityRegion(0.954).Sum(interval => interval.LengthInYears);
        Assert.True(vagueLength > preciseLength * 3);
    }

    [Fact]
    public void An_older_measurement_gives_an_older_date() {
        // Arrange
        CalibrationCurve curve = TestCurves.Linear();
        int              anyUncertainty = Any.Int32().Between(20, 100).Generate();

        // Act
        CalibratedDate younger = curve.Calibrate(new RadiocarbonAge(2000, anyUncertainty));
        CalibratedDate older   = curve.Calibrate(new RadiocarbonAge(4000, anyUncertainty));

        // Assert
        Assert.True(older.Mean < younger.Mean);
    }

    [Fact]
    public void The_curve_uncertainty_is_combined_with_the_laboratory_uncertainty() {
        // Arrange: curve 40 and laboratory 30 combine into a spread of 50, so one spread from the peak the
        // likelihood falls to exp(-1/2) of the peak.
        List<CalibrationPoint> points = new List<CalibrationPoint> {
            new CalibrationPoint(-1100, 3050d, 40d),
            new CalibrationPoint(-1000, 2950d, 40d),
        };
        CalibrationCurve curve = new CalibrationCurve("Steep", points);

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(3000, 30));

        // Assert
        double ratio = date.ProbabilityOf(-1000) / date.ProbabilityOf(-1050);
        Assert.Equal(Math.Exp(-0.5), ratio, 6);
    }

    [Fact]
    public void An_age_far_beyond_what_the_curve_records_is_refused() {
        // Arrange
        CalibrationCurve curve = TestCurves.Linear();
        RadiocarbonAge   anyTooOldAge = Any.RadiocarbonAge().WithYearsBp(RadiocarbonAge.MaximumYearsBp).WithUncertainty(1).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => curve.Calibrate(anyTooOldAge));

        // Assert
        Assert.Equal("age", error.ParamName);
        Assert.Contains("Linear test curve", error.Message);
    }

    [Fact]
    public void Calibrating_nothing_is_refused() {
        // Arrange
        CalibrationCurve anyCurve = Any.CalibrationCurve().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyCurve.Calibrate(null!));
    }

}
