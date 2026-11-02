using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class EnergyPerformanceDiagnosisTests {

    private static readonly DateOnly SomeIssueDate = new DateOnly(2023, 5, 17);

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
    [InlineData(int.MaxValue, EnergyRating.G)]
    public void Rates_the_primary_energy_alone_when_emissions_are_negligible(int primaryEnergy, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(primaryEnergy, 0);

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
    [InlineData(int.MaxValue, EnergyRating.G)]
    public void Rates_the_emissions_alone_when_the_primary_energy_is_negligible(int emissions, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(0, emissions);

        // Assert
        Assert.Equal(expected, rating);
    }

    [Theory]
    [InlineData(69, 100, EnergyRating.G)] // best energy band, worst emissions band
    [InlineData(420, 0, EnergyRating.G)]  // worst energy band, best emissions band
    [InlineData(200, 6, EnergyRating.D)]  // energy D against emissions B
    [InlineData(100, 30, EnergyRating.D)] // energy B against emissions D
    [InlineData(69, 5, EnergyRating.A)]   // both in the best band
    [InlineData(250, 50, EnergyRating.E)] // both in the same band
    public void Gives_the_worse_of_the_energy_and_emissions_bands(int primaryEnergy, int emissions, EnergyRating expected) {
        // Act
        EnergyRating rating = EnergyPerformanceDiagnosis.Rate(primaryEnergy, emissions);

        // Assert
        Assert.Equal(expected, rating);
    }

    [Fact]
    public void Keeps_the_figures_the_rating_and_the_issue_date_it_was_built_from() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(250, 40, EnergyRating.E, SomeIssueDate);

        // Assert
        Assert.Equal(250, diagnosis.PrimaryEnergy);
        Assert.Equal(40, diagnosis.Emissions);
        Assert.Equal(EnergyRating.E, diagnosis.Rating);
        Assert.Equal(SomeIssueDate, diagnosis.IssuedOn);
    }

    [Fact]
    public void Refuses_a_negative_primary_energy() {
        // Arrange
        DateOnly anyIssueDate = AnyEnergyPerformanceDiagnosis.ValidIssuedOn().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(-1, 0, EnergyRating.A, anyIssueDate));

        // Assert
        Assert.Equal("primaryEnergyKwhPerSquareMeterYear", exception.ParamName);
    }

    [Fact]
    public void Refuses_negative_emissions() {
        // Arrange
        DateOnly anyIssueDate = AnyEnergyPerformanceDiagnosis.ValidIssuedOn().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, -1, EnergyRating.A, anyIssueDate));

        // Assert
        Assert.Equal("emissionsKgCo2PerSquareMeterYear", exception.ParamName);
    }

    [Fact]
    public void Accepts_zero_figures() {
        // Arrange
        DateOnly anyIssueDate = AnyEnergyPerformanceDiagnosis.ValidIssuedOn().Generate();

        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, anyIssueDate);

        // Assert
        Assert.Equal(EnergyRating.A, diagnosis.Rating);
    }

    [Fact]
    public void Refuses_a_diagnosis_issued_the_day_before_the_2021_reform() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, new DateOnly(2021, 6, 30)));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_diagnosis_issued_on_the_day_of_the_2021_reform() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, new DateOnly(2021, 7, 1));

        // Assert
        Assert.Equal(new DateOnly(2021, 7, 1), diagnosis.IssuedOn);
    }

    [Fact]
    public void Refuses_an_issue_date_whose_validity_end_cannot_be_represented() {
        // Arrange
        DateOnly dayAfterLatest = EnergyPerformanceDiagnosis.LatestIssueDate.AddDays(1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, dayAfterLatest));
    }

    [Fact]
    public void Accepts_the_latest_issue_date_and_is_then_valid_until_the_last_representable_day() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(0, 0, EnergyRating.A, EnergyPerformanceDiagnosis.LatestIssueDate);

        // Assert
        Assert.Equal(DateOnly.MaxValue, diagnosis.ValidUntil);
    }

    [Theory]
    [InlineData(EnergyRating.A)]
    [InlineData(EnergyRating.B)]
    [InlineData(EnergyRating.C)]
    [InlineData(EnergyRating.D)]
    [InlineData(EnergyRating.F)]
    [InlineData(EnergyRating.G)]
    public void Refuses_a_declared_rating_other_than_the_computed_one(EnergyRating declared) {
        // Arrange: 250 kWh and 40 kg give an E
        DateOnly anyIssueDate = AnyEnergyPerformanceDiagnosis.ValidIssuedOn().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EnergyPerformanceDiagnosis(250, 40, declared, anyIssueDate));

        // Assert
        Assert.Equal("declaredRating", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_declared_rating_that_ignores_the_worse_band() {
        // Arrange: the energy alone gives an A, the emissions give a G
        DateOnly anyIssueDate = AnyEnergyPerformanceDiagnosis.ValidIssuedOn().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EnergyPerformanceDiagnosis(10, 100, EnergyRating.A, anyIssueDate));
    }

    [Fact]
    public void Is_valid_ten_years_after_it_was_issued() {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 15)).Generate();

        // Act
        DateOnly validUntil = diagnosis.ValidUntil;

        // Assert
        Assert.Equal(new DateOnly(2032, 3, 15), validUntil);
    }

    [Fact]
    public void Ends_its_validity_on_the_28th_of_February_when_issued_on_a_leap_day() {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2024, 2, 29)).Generate();

        // Act
        DateOnly validUntil = diagnosis.ValidUntil;

        // Assert
        Assert.Equal(new DateOnly(2034, 2, 28), validUntil);
    }

    [Theory]
    [InlineData("2022-03-14", false)] // the day before it was issued
    [InlineData("2022-03-15", true)]  // the day it was issued
    [InlineData("2022-03-16", true)]
    [InlineData("2032-03-14", true)]  // its last valid day
    [InlineData("2032-03-15", false)] // ten years to the day: expired
    [InlineData("2040-01-01", false)]
    public void Is_valid_from_its_issue_date_up_to_but_excluding_ten_years_later(string day, bool valid) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 15)).Generate();

        // Act
        bool isValid = diagnosis.IsValidOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(valid, isValid);
    }

    [Theory]
    [InlineData(EnergyRating.A, false)]
    [InlineData(EnergyRating.B, false)]
    [InlineData(EnergyRating.C, false)]
    [InlineData(EnergyRating.D, false)]
    [InlineData(EnergyRating.E, false)]
    [InlineData(EnergyRating.F, true)]
    [InlineData(EnergyRating.G, true)]
    public void Is_an_energy_sieve_when_rated_F_or_G(EnergyRating rating, bool sieve) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithRating(rating).Generate();

        // Act
        bool isEnergySieve = diagnosis.IsEnergySieve;

        // Assert
        Assert.Equal(sieve, isEnergySieve);
    }

}
