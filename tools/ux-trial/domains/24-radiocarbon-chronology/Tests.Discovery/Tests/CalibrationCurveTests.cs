namespace RadiocarbonChronology.Tests.Discovery;

public sealed class CalibrationCurveTests {

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_curve_needs_a_name(string? name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CalibrationCurve().WithName(name!).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_curve_needs_points() {
        // Arrange
        string anyName = Any.CalibrationCurve().Generate().Name;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new CalibrationCurve(anyName, null!));
    }

    [Fact]
    public void A_curve_needs_at_least_two_points() {
        // Arrange
        string           anyName  = Any.CalibrationCurve().Generate().Name;
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, []));
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, [anyPoint]));
    }

    [Fact]
    public void A_curve_refuses_a_missing_point() {
        // Arrange
        string           anyName  = Any.CalibrationCurve().Generate().Name;
        CalibrationPoint anyPoint = Any.CalibrationPoint().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, [anyPoint, null!]));

        // Assert
        Assert.Contains("point 1", refusal.Message);
    }

    [Fact]
    public void Two_points_in_the_same_calendar_year_are_refused() {
        // Arrange
        string           anyName        = Any.CalibrationCurve().Generate().Name;
        CalibrationPoint anyPoint       = Any.CalibrationPoint().WithCalendarYear(-1_200).Generate();
        CalibrationPoint anyOtherPoint  = Any.CalibrationPoint().WithCalendarYear(-1_200).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, [anyPoint, anyOtherPoint]));
    }

    [Fact]
    public void Points_that_go_back_in_time_are_refused() {
        // Arrange
        string           anyName       = Any.CalibrationCurve().Generate().Name;
        CalibrationPoint anyLaterPoint = Any.CalibrationPoint().WithCalendarYear(-1_000).Generate();
        CalibrationPoint anyEarlyPoint = Any.CalibrationPoint().WithCalendarYear(-1_001).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CalibrationCurve(anyName, [anyLaterPoint, anyEarlyPoint]));

        // Assert
        Assert.Contains("strictly increasing", refusal.Message);
    }

    [Fact]
    public void The_coverage_runs_from_the_first_to_the_last_point() {
        // Arrange
        CalibrationCurve curve = Curves.Linear();

        // Assert
        Assert.Equal(new CalendarInterval(-3_000, 1_950), curve.Coverage);
    }

    [Fact]
    public void Calibrating_an_age_gives_a_distribution_over_the_whole_coverage_summing_to_one() {
        // Arrange
        CalibrationCurve curve = Curves.Linear();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(2_000, 30));

        // Assert
        Assert.Equal(-3_000, date.FirstYear);
        Assert.Equal(1_950, date.LastYear);
        Assert.Equal(1.0, date.Probabilities.Sum(), 1e-9);
    }

    [Fact]
    public void On_a_curve_where_age_is_1950_less_the_year_an_age_calibrates_around_that_year() {
        // Arrange
        CalibrationCurve curve = Curves.Linear();

        // Act
        CalibratedDate date = curve.Calibrate(new RadiocarbonAge(2_000, 30));

        // Assert
        Assert.Equal(-50.0, date.Mean, 0.5);
        Assert.Equal(date.ProbabilityOf(-60), date.ProbabilityOf(-40), 1e-9);
        Assert.True(date.ProbabilityOf(-50) > date.ProbabilityOf(-80));
        Assert.True(date.ProbabilityOf(-80) > date.ProbabilityOf(-200));
    }

    [Fact]
    public void A_larger_laboratory_uncertainty_spreads_the_date_over_more_years() {
        // Arrange
        CalibrationCurve curve = Curves.Linear();

        // Act
        int preciseYears = curve.Calibrate(new RadiocarbonAge(2_000, 30)).HighestDensityRegion(0.954).Sum(interval => interval.LengthInYears);
        int vagueYears   = curve.Calibrate(new RadiocarbonAge(2_000, 300)).HighestDensityRegion(0.954).Sum(interval => interval.LengthInYears);

        // Assert
        Assert.True(vagueYears > 5 * preciseYears, $"{vagueYears} years against {preciseYears}");
    }

    [Fact]
    public void An_age_that_the_curve_reaches_twice_calibrates_to_two_separate_intervals() {
        // Arrange
        CalibrationCurve curve = Curves.Wiggle();

        // Act
        IReadOnlyList<CalendarInterval> region = curve.Calibrate(new RadiocarbonAge(2_100, 10)).HighestDensityRegion(0.954);

        // Assert
        Assert.Equal(2, region.Count);
        Assert.True(region[0].Contains(50), region[0].ToString());
        Assert.True(region[1].Contains(150), region[1].ToString());
    }

    [Fact]
    public void An_age_the_curve_cannot_reach_is_refused() {
        // Arrange
        CalibrationCurve curve = Curves.Linear();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => curve.Calibrate(new RadiocarbonAge(50_000, 30)));

        // Assert
        Assert.Equal("age", refusal.ParamName);
        Assert.Contains("Linear", refusal.Message);
    }

    [Fact]
    public void Calibrating_needs_an_age() {
        // Arrange
        CalibrationCurve anyCurve = Any.CalibrationCurve().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyCurve.Calibrate(null!));
    }

}
