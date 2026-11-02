using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class EnergyPerformanceDiagnosisTests {

    [Theory]
    [InlineData(0, 0, EnergyRating.A)]
    [InlineData(69, 5, EnergyRating.A)]
    [InlineData(70, 5, EnergyRating.B)]
    [InlineData(109, 10, EnergyRating.B)]
    [InlineData(110, 10, EnergyRating.C)]
    [InlineData(179, 29, EnergyRating.C)]
    [InlineData(180, 29, EnergyRating.D)]
    [InlineData(249, 49, EnergyRating.D)]
    [InlineData(250, 49, EnergyRating.E)]
    [InlineData(329, 69, EnergyRating.E)]
    [InlineData(330, 69, EnergyRating.F)]
    [InlineData(419, 99, EnergyRating.F)]
    [InlineData(420, 99, EnergyRating.G)]
    [InlineData(5_000, 0, EnergyRating.G)]
    public void The_energy_band_alone_sets_the_rating_when_emissions_are_cleaner(int energy, int emissions, EnergyRating expected) {
        Assert.Equal(expected, EnergyPerformanceDiagnosis.Rate(energy, emissions));
    }

    [Theory]
    [InlineData(0, 5, EnergyRating.A)]
    [InlineData(0, 6, EnergyRating.B)]
    [InlineData(0, 10, EnergyRating.B)]
    [InlineData(0, 11, EnergyRating.C)]
    [InlineData(0, 29, EnergyRating.C)]
    [InlineData(0, 30, EnergyRating.D)]
    [InlineData(0, 49, EnergyRating.D)]
    [InlineData(0, 50, EnergyRating.E)]
    [InlineData(0, 69, EnergyRating.E)]
    [InlineData(0, 70, EnergyRating.F)]
    [InlineData(0, 99, EnergyRating.F)]
    [InlineData(0, 100, EnergyRating.G)]
    [InlineData(0, 5_000, EnergyRating.G)]
    public void The_emissions_band_alone_sets_the_rating_when_energy_is_cleaner(int energy, int emissions, EnergyRating expected) {
        Assert.Equal(expected, EnergyPerformanceDiagnosis.Rate(energy, emissions));
    }

    [Theory]
    [InlineData(300, 8, EnergyRating.E)]    // energy worse
    [InlineData(60, 45, EnergyRating.D)]    // emissions worse
    [InlineData(100, 100, EnergyRating.G)]
    public void The_rating_is_the_worse_of_the_two_bands(int energy, int emissions, EnergyRating expected) {
        Assert.Equal(expected, EnergyPerformanceDiagnosis.Rate(energy, emissions));
    }

    [Fact]
    public void A_diagnosis_keeps_its_figures_its_rating_and_its_date() {
        // Act
        EnergyPerformanceDiagnosis diagnosis = new EnergyPerformanceDiagnosis(215, 38, EnergyRating.D, new DateOnly(2023, 5, 17));

        // Assert
        Assert.Equal(215, diagnosis.PrimaryEnergy);
        Assert.Equal(38, diagnosis.Emissions);
        Assert.Equal(EnergyRating.D, diagnosis.Rating);
        Assert.Equal(new DateOnly(2023, 5, 17), diagnosis.IssuedOn);
    }

    [Fact]
    public void Negative_energy_is_refused() {
        // Arrange
        DateOnly anyIssueDate = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(-1, 0, EnergyRating.A, anyIssueDate));
    }

    [Fact]
    public void Negative_emissions_are_refused() {
        // Arrange
        DateOnly anyIssueDate = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(0, -1, EnergyRating.A, anyIssueDate));
    }

    [Fact]
    public void A_diagnosis_issued_before_the_reform_is_refused() {
        // Arrange
        DateOnly dayBeforeTheReform = new DateOnly(2021, 6, 30);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(50, 3, EnergyRating.A, dayBeforeTheReform));
    }

    [Fact]
    public void A_diagnosis_issued_on_the_day_of_the_reform_is_accepted() {
        Assert.Equal(new DateOnly(2021, 7, 1), new EnergyPerformanceDiagnosis(50, 3, EnergyRating.A, new DateOnly(2021, 7, 1)).IssuedOn);
    }

    [Fact]
    public void The_latest_issue_date_is_accepted_and_the_day_after_is_refused() {
        // Act
        EnergyPerformanceDiagnosis latest = new EnergyPerformanceDiagnosis(50, 3, EnergyRating.A, EnergyPerformanceDiagnosis.LatestIssueDate);

        // Assert
        Assert.Equal(EnergyPerformanceDiagnosis.LatestIssueDate.AddYears(10), latest.ValidUntil);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergyPerformanceDiagnosis(50, 3, EnergyRating.A, EnergyPerformanceDiagnosis.LatestIssueDate.AddDays(1)));
    }

    [Fact]
    public void A_declared_rating_that_the_figures_do_not_give_is_refused() {
        // Arrange: 100 kWh and 5 kg give B; any other declared rating is wrong.
        EnergyRating anyWrongRating = Any.Enum<EnergyRating>().DifferentFrom(EnergyRating.B).Generate();
        DateOnly     anyIssueDate   = Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new EnergyPerformanceDiagnosis(100, 5, anyWrongRating, anyIssueDate));
    }

    [Fact]
    public void A_diagnosis_is_valid_for_ten_years() {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 15)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2032, 3, 15), diagnosis.ValidUntil);
    }

    [Fact]
    public void A_diagnosis_issued_on_a_leap_day_ends_on_the_28th_ten_years_later() {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2024, 2, 29)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2034, 2, 28), diagnosis.ValidUntil);
    }

    [Theory]
    [InlineData("2022-03-14", false)]   // the day before it was issued
    [InlineData("2022-03-15", true)]    // the day it was issued
    [InlineData("2027-01-01", true)]
    [InlineData("2032-03-14", true)]    // the last valid day
    [InlineData("2032-03-15", false)]   // the day it expires
    [InlineData("2040-01-01", false)]
    public void A_diagnosis_is_valid_from_its_issue_day_up_to_but_excluding_its_expiry(string day, bool expected) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 15)).Generate();

        // Assert
        Assert.Equal(expected, diagnosis.IsValidOn(DateOnly.Parse(day)));
    }

    [Theory]
    [InlineData(EnergyRating.A, false)]
    [InlineData(EnergyRating.B, false)]
    [InlineData(EnergyRating.C, false)]
    [InlineData(EnergyRating.D, false)]
    [InlineData(EnergyRating.E, false)]
    [InlineData(EnergyRating.F, true)]
    [InlineData(EnergyRating.G, true)]
    public void Only_a_rating_of_F_or_G_is_an_energy_sieve(EnergyRating rating, bool isSieve) {
        // Arrange
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().OfRating(rating).Generate();

        // Assert
        Assert.Equal(rating, diagnosis.Rating);
        Assert.Equal(isSieve, diagnosis.IsEnergySieve);
    }

}
