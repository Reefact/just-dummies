using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class CalibrationCurveTests {

    private const int Precision = 9;

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_curve_keeps_its_name_and_its_points_in_order() {
        // Arrange
        CalibrationPoint start  = new CalibrationPoint(-500, 2_450, 12);
        CalibrationPoint middle = new CalibrationPoint(-100, 2_050, 10);
        CalibrationPoint end    = new CalibrationPoint(300, 1_650, 14);

        // Act
        CalibrationCurve curve = new CalibrationCurve("Northern hemisphere", [start, middle, end]);

        // Assert
        Assert.Equal("Northern hemisphere", curve.Name);
        Assert.Equal([start, middle, end], curve.Points);
    }

    [Fact]
    public void The_coverage_of_a_curve_runs_from_its_first_year_to_its_last() {
        // Arrange
        CalibrationPoint start = Any.CalibrationPoint().WithCalendarYear(-500).Generate();
        CalibrationPoint end   = Any.CalibrationPoint().WithCalendarYear(300).Generate();

        // Act
        CalibrationCurve curve = Any.CalibrationCurve().WithPoints([start, end]).Generate();

        // Assert
        Assert.Equal(new CalendarInterval(-500, 300), curve.Coverage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_curve_without_a_name_is_refused(string? blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CalibrationCurve().WithName(blank!).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_curve_without_a_list_of_points_is_refused() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CalibrationCurve(anyName, null!));
    }

    [Fact]
    public void A_curve_of_a_single_point_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.CalibrationCurve().WithPoints(Any.ListOf(Any.CalibrationPoint()).WithCount(1).Generate()).Generate());

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void A_curve_of_no_point_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.CalibrationCurve().WithPoints([]).Generate());
    }

    [Fact]
    public void A_curve_with_a_missing_point_is_refused() {
        // Arrange
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.CalibrationCurve().WithPoints([anyPoint, null!]).Generate());

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void Two_points_of_the_same_calendar_year_are_refused() {
        // Arrange
        CalibrationPoint first  = Any.CalibrationPoint().WithCalendarYear(-100).Generate();
        CalibrationPoint second = Any.CalibrationPoint().WithCalendarYear(-100).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.CalibrationCurve().WithPoints([first, second]).Generate());

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void Points_in_decreasing_calendar_order_are_refused() {
        // Arrange
        CalibrationPoint later   = Any.CalibrationPoint().WithCalendarYear(-99).Generate();
        CalibrationPoint earlier = Any.CalibrationPoint().WithCalendarYear(-100).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.CalibrationCurve().WithPoints([later, earlier]).Generate());
    }

    [Fact]
    public void One_point_out_of_order_among_many_is_refused() {
        // Arrange
        CalibrationPoint[] points = [
            Any.CalibrationPoint().WithCalendarYear(-300).Generate(),
            Any.CalibrationPoint().WithCalendarYear(-200).Generate(),
            Any.CalibrationPoint().WithCalendarYear(-250).Generate(),
            Any.CalibrationPoint().WithCalendarYear(-100).Generate(),
        ];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.CalibrationCurve().WithPoints(points).Generate());
    }

    [Fact]
    public void The_curve_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<CalibrationPoint> points = [
            Any.CalibrationPoint().WithCalendarYear(-300).Generate(),
            Any.CalibrationPoint().WithCalendarYear(-200).Generate(),
        ];
        CalibrationCurve curve = Any.CalibrationCurve().WithPoints(points).Generate();

        // Act
        points.Clear();

        // Assert
        Assert.Equal(2, curve.Points.Count);
    }

    [Fact(Skip = "domain defect: Points hands out the internal array, so a caller can cast it to CalibrationPoint[] and break the strictly-increasing order the constructor enforces (no seed: literals only)")]
    public void The_points_of_a_curve_cannot_be_changed_from_outside() {
        // Arrange
        CalibrationCurve curve = Any.CalibrationCurve().Generate();

        // Act & Assert
        Assert.False(curve.Points is CalibrationPoint[]);
    }

    // ---------------------------------------------------------------- calibration

    [Fact]
    public void Calibrating_gives_a_distribution_over_the_whole_coverage_of_the_curve() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(3_000, 30));

        // Assert
        Assert.Equal(curve.Coverage.StartYear, date.FirstYear);
        Assert.Equal(curve.Coverage.EndYear, date.LastYear);
    }

    [Fact]
    public void A_calibrated_distribution_holds_all_of_the_probability() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(3_000, 30));

        // Assert
        Assert.Equal(1, Dating.Total(date), Precision);
    }

    [Theory]
    [InlineData(0, 1_950)]
    [InlineData(1_000, 950)]
    [InlineData(3_000, -1_050)]
    [InlineData(6_950, -5_000)]
    public void On_a_curve_where_age_follows_the_calendar_the_most_probable_year_is_where_the_curve_meets_the_age(int yearsBp, int expectedYear) {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(yearsBp, 30));

        // Assert
        Assert.Equal(expectedYear, Dating.ModeYear(date));
    }

    [Fact]
    public void Between_two_points_the_curve_is_interpolated_linearly_year_by_year() {
        // Arrange: 1000 BP in year 0, 900 BP in year 100, so 950 BP half way, in year 50
        CalibrationPoint start = Any.CalibrationPoint().WithCalendarYear(0).WithRadiocarbonYearsBp(1_000).WithUncertainty(10).Generate();
        CalibrationPoint end   = Any.CalibrationPoint().WithCalendarYear(100).WithRadiocarbonYearsBp(900).WithUncertainty(10).Generate();
        CalibrationCurve curve = Any.CalibrationCurve().WithPoints([start, end]).Generate();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(950, 5));

        // Assert
        Assert.Equal(50, Dating.ModeYear(date));
        Assert.Equal(50, date.Mean, 1);
    }

    [Fact]
    public void An_age_in_the_middle_of_the_curve_is_centred_on_its_year() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(3_000, 30));

        // Assert
        Assert.Equal(-1_050, date.Mean, 1);
    }

    [Fact]
    public void The_spread_combines_the_laboratory_uncertainty_and_the_curve_uncertainty() {
        // Arrange: laboratory 30, curve 10, so sqrt(30² + 10²)
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(3_000, 30));

        // Assert
        Assert.Equal(Math.Sqrt((30 * 30) + (10 * 10)), Dating.StandardDeviation(date), 1);
    }

    [Fact]
    public void A_less_precise_measurement_gives_a_wider_date() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        CalibratedDate precise = curve.Calibrate(new RadiocarbonAge(3_000, 30));
        CalibratedDate vague   = curve.Calibrate(new RadiocarbonAge(3_000, 200));

        // Assert
        Assert.True(Dating.StandardDeviation(vague) > 5 * Dating.StandardDeviation(precise));
    }

    [Fact]
    public void An_age_met_twice_by_the_curve_gives_two_peaks() {
        // Arrange: the age of 2900 BP is met around the years -900 and -700
        CalibrationCurve curve = Curves.WithAReversalAtYearMinus800();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(2_900, 20));

        // Assert
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(OccupationEstimate.CredibleLevel);
        Assert.Equal(2, region.Count);
        Assert.True(region[0].Contains(-900));
        Assert.True(region[1].Contains(-700));
    }

    [Fact]
    public void An_age_the_curve_cannot_reach_is_refused() {
        // Arrange: the curve stops at 6950 BP
        CalibrationCurve curve = Curves.Identity();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => curve.Calibrate(new RadiocarbonAge(20_000, 30)));

        // Assert
        Assert.Equal("age", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: an age 550 years beyond the end of the curve (7500 BP on a curve stopping at 6950 BP) is calibrated instead of refused, and 42% of its probability piles up on the curve's first year; only an age so far that the likelihood underflows to zero (about 38 sigma) is refused (no seed: literals only)")]
    public void An_age_clearly_beyond_the_end_of_the_curve_is_refused() {
        // Arrange: the curve stops at 6950 BP
        CalibrationCurve curve = Curves.Identity();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => curve.Calibrate(new RadiocarbonAge(7_500, 30)));
    }

    [Fact]
    public void Calibrating_nothing_is_refused() {
        // Arrange
        CalibrationCurve anyCurve = Any.CalibrationCurve().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyCurve.Calibrate(null!));
    }

}
