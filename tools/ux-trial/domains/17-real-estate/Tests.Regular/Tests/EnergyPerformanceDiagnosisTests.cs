namespace RealEstate.Tests.Regular;

public sealed class EnergyPerformanceDiagnosisTests {

    [Theory]
    [InlineData(0, EnergyRating.A)]
    [InlineData(69, EnergyRating.A)]
    [InlineData(70, EnergyRating.B)]
    [InlineData(109, EnergyRating.B)]
    [InlineData(110, EnergyRating.C)]
    [InlineData(179, EnergyRating.C)]
    [InlineData(180, EnergyRating.D)]
    [InlineData(249, EnergyRating.D)]
    [InlineData(250, EnergyRating.E)]
    [InlineData(329, EnergyRating.E)]
    [InlineData(330, EnergyRating.F)]
    [InlineData(419, EnergyRating.F)]
    [InlineData(420, EnergyRating.G)]
    [InlineData(10_000, EnergyRating.G)]
    public void The_energy_band_alone_decides_the_rating_when_emissions_are_low(int energy, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(energy, 0);

        // Assert
        Assert.Equal(expected, rating);
    }

    [Theory]
    [InlineData(0, EnergyRating.A)]
    [InlineData(5, EnergyRating.A)]
    [InlineData(6, EnergyRating.B)]
    [InlineData(10, EnergyRating.B)]
    [InlineData(11, EnergyRating.C)]
    [InlineData(29, EnergyRating.C)]
    [InlineData(30, EnergyRating.D)]
    [InlineData(49, EnergyRating.D)]
    [InlineData(50, EnergyRating.E)]
    [InlineData(69, EnergyRating.E)]
    [InlineData(70, EnergyRating.F)]
    [InlineData(99, EnergyRating.F)]
    [InlineData(100, EnergyRating.G)]
    [InlineData(10_000, EnergyRating.G)]
    public void The_emissions_band_alone_decides_the_rating_when_energy_is_low(int emissions, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(0, emissions);

        // Assert
        Assert.Equal(expected, rating);
    }

    [Theory]
    [InlineData(100, 40, EnergyRating.D)]    // energy B, emissions D: the worse wins
    [InlineData(300, 8, EnergyRating.E)]     // energy E, emissions B
    [InlineData(200, 20, EnergyRating.D)]    // energy D, emissions C
    [InlineData(500, 120, EnergyRating.G)]
    [InlineData(69, 5, EnergyRating.A)]
    public void The_rating_is_the_worse_of_the_two_bands(int energy, int emissions, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(energy, emissions);

        // Assert
        Assert.Equal(expected, rating);
    }

    [Fact]
    public void A_diagnosis_exposes_its_figures_its_rating_and_its_issue_date() {
        // Arrange
        DateOnly issuedOn = new DateOnly(2023, 3, 15);

        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(200, 20, EnergyRating.D, issuedOn);

        // Assert
        Assert.Equal(200, diagnosis.PrimaryEnergy);
        Assert.Equal(20, diagnosis.Emissions);
        Assert.Equal(EnergyRating.D, diagnosis.Rating);
        Assert.Equal(issuedOn, diagnosis.IssuedOn);
    }

    [Fact]
    public void A_declared_rating_that_differs_from_the_computed_one_is_refused() {
        // Arrange
        EnergyRating anyOtherRating = Any.Enum<EnergyRating>().DifferentFrom(EnergyRating.D).Generate();
        DateOnly     anyIssueDate   = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EnergyPerformanceDiagnosis(200, 20, anyOtherRating, anyIssueDate));

        // Assert
        Assert.Equal("declaredRating", exception.ParamName);
        Assert.Contains("D", exception.Message);
    }

    [Fact]
    public void A_negative_energy_is_refused() {
        // Arrange
        int      anyNegative  = Any.Int32().Negative().Generate();
        DateOnly anyIssueDate = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(anyNegative, 0, EnergyRating.A, anyIssueDate));

        // Assert
        Assert.Equal("primaryEnergyKwhPerSquareMeterYear", exception.ParamName);
    }

    [Fact]
    public void Negative_emissions_are_refused() {
        // Arrange
        int      anyNegative  = Any.Int32().Negative().Generate();
        DateOnly anyIssueDate = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, anyNegative, EnergyRating.A, anyIssueDate));

        // Assert
        Assert.Equal("emissionsKgCo2PerSquareMeterYear", exception.ParamName);
    }

    [Fact]
    public void Zero_figures_are_accepted() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, EnergyPerformanceDiagnosis.ReformDate);

        // Assert
        Assert.Equal(EnergyRating.A, diagnosis.Rating);
    }

    [Fact]
    public void A_diagnosis_issued_before_the_reform_is_refused() {
        // Arrange
        DateOnly anyDayBeforeTheReform = Any.DateOnly().Before(EnergyPerformanceDiagnosis.ReformDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, anyDayBeforeTheReform));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Fact]
    public void A_diagnosis_issued_on_the_day_of_the_reform_is_accepted() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, new DateOnly(2021, 7, 1));

        // Assert
        Assert.Equal(new DateOnly(2021, 7, 1), diagnosis.IssuedOn);
    }

    [Fact]
    public void The_day_before_the_reform_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, new DateOnly(2021, 6, 30)));
    }

    [Fact]
    public void A_diagnosis_issued_after_the_latest_issue_date_is_refused() {
        // Arrange
        DateOnly anyLaterDay = Any.DateOnly().After(EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, anyLaterDay));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Fact]
    public void A_diagnosis_issued_on_the_latest_issue_date_is_valid_until_the_last_representable_day() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, EnergyPerformanceDiagnosis.LatestIssueDate);

        // Assert
        Assert.Equal(DateOnly.MaxValue, diagnosis.ValidUntil);
    }

    [Fact]
    public void The_figures_are_checked_before_the_date() {
        // Arrange
        DateOnly anyDayBeforeTheReform = Any.DateOnly().Before(EnergyPerformanceDiagnosis.ReformDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(-1, 0, EnergyRating.A, anyDayBeforeTheReform));

        // Assert
        Assert.Equal("primaryEnergyKwhPerSquareMeterYear", exception.ParamName);
    }

    [Fact]
    public void The_date_is_checked_before_the_rating() {
        // Arrange
        DateOnly anyDayBeforeTheReform = Any.DateOnly().Before(EnergyPerformanceDiagnosis.ReformDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(500, 0, EnergyRating.A, anyDayBeforeTheReform));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Theory]
    [InlineData(2023, 3, 15, 2033, 3, 15)]
    [InlineData(2021, 7, 1, 2031, 7, 1)]
    [InlineData(2024, 2, 29, 2034, 2, 28)]    // no 29 February ten years later
    [InlineData(2024, 12, 31, 2034, 12, 31)]
    public void A_diagnosis_is_valid_for_ten_years(int issuedYear, int issuedMonth, int issuedDay, int untilYear, int untilMonth, int untilDay) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(issuedYear, issuedMonth, issuedDay)).Generate();

        // Act
        DateOnly validUntil = diagnosis.ValidUntil;

        // Assert
        Assert.Equal(new DateOnly(untilYear, untilMonth, untilDay), validUntil);
    }

    [Theory]
    [InlineData("2023-03-14", false)]    // the day before the issue
    [InlineData("2023-03-15", true)]     // the issue day
    [InlineData("2028-06-01", true)]
    [InlineData("2033-03-14", true)]     // the last valid day
    [InlineData("2033-03-15", false)]    // the day validity ends
    [InlineData("2040-01-01", false)]
    public void A_diagnosis_is_valid_from_its_issue_day_until_the_day_before_its_end(string day, bool expected) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 3, 15)).Generate();

        // Act
        bool isValid = diagnosis.IsValidOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData(EnergyRating.A, false)]
    [InlineData(EnergyRating.B, false)]
    [InlineData(EnergyRating.C, false)]
    [InlineData(EnergyRating.D, false)]
    [InlineData(EnergyRating.E, false)]
    [InlineData(EnergyRating.F, true)]
    [InlineData(EnergyRating.G, true)]
    public void Only_a_rating_of_F_or_G_is_an_energy_sieve(EnergyRating rating, bool expected) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithRating(rating).Generate();

        // Act
        bool isEnergySieve = diagnosis.IsEnergySieve;

        // Assert
        Assert.Equal(expected, isEnergySieve);
        Assert.Equal(rating, diagnosis.Rating);
    }

}
