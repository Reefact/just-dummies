using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class RandomisationTests {

    private static readonly DateOnly RandomisedOn = new DateOnly(2024, 3, 11);

    private static Randomisation OnMarch11() {
        return Any.Randomisation().WithDate(RandomisedOn).Generate();
    }

    [Fact]
    public void The_randomisation_date_is_study_day_1() {
        Assert.Equal(1, OnMarch11().StudyDayOf(RandomisedOn));
    }

    [Fact]
    public void The_day_after_randomisation_is_study_day_2() {
        Assert.Equal(2, OnMarch11().StudyDayOf(RandomisedOn.AddDays(1)));
    }

    [Fact]
    public void The_day_before_randomisation_is_study_day_minus_1_because_there_is_no_day_0() {
        Assert.Equal(-1, OnMarch11().StudyDayOf(RandomisedOn.AddDays(-1)));
        Assert.Equal(-2, OnMarch11().StudyDayOf(RandomisedOn.AddDays(-2)));
    }

    [Fact]
    public void A_study_day_across_a_month_boundary_counts_calendar_days() {
        // 2024 is a leap year: March 11 + 28 days is April 8, study day 29.
        Assert.Equal(29, OnMarch11().StudyDayOf(new DateOnly(2024, 4, 8)));
    }

    [Theory]
    [InlineData(1, 2024, 3, 11)]
    [InlineData(2, 2024, 3, 12)]
    [InlineData(29, 2024, 4, 8)]
    [InlineData(-1, 2024, 3, 10)]
    [InlineData(-10, 2024, 3, 1)]
    public void A_study_day_maps_to_its_calendar_date(int studyDay, int year, int month, int day) {
        Assert.Equal(new DateOnly(year, month, day), OnMarch11().DateOfStudyDay(studyDay));
    }

    [Fact]
    public void There_is_no_study_day_0() {
        Assert.Throws<ArgumentOutOfRangeException>(() => OnMarch11().DateOfStudyDay(0));
    }

    [Theory]
    [InlineData(-400)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(365)]
    [InlineData(1095)]
    public void A_study_day_survives_the_round_trip_through_its_date(int studyDay) {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        int roundTrip = anyRandomisation.StudyDayOf(anyRandomisation.DateOfStudyDay(studyDay));

        // Assert
        Assert.Equal(studyDay, roundTrip);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void The_first_and_last_randomisation_numbers_are_accepted(int number) {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().WithNumber(number).Generate();

        // Assert
        Assert.Equal(number, anyRandomisation.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void A_randomisation_number_outside_1_to_9999_is_refused(int number) {
        // Arrange
        DateOnly       anyDate = TrialCalendar.Date().Generate();
        TreatmentArm   anyArm  = Any.Enum<TreatmentArm>().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(number, anyDate, anyArm));
    }

    [Fact]
    public void An_undeclared_treatment_arm_is_refused() {
        // Arrange
        int      anyNumber = Any.Int32().Between(1, 9999).Generate();
        DateOnly anyDate   = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(anyNumber, anyDate, (TreatmentArm)42));
    }

    [Fact]
    public void Two_randomisations_with_the_same_number_date_and_arm_are_equal() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        Randomisation same = new Randomisation(anyRandomisation.Number, anyRandomisation.Date, anyRandomisation.Arm);

        // Assert
        Assert.Equal(anyRandomisation, same);
        Assert.Equal(anyRandomisation.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Randomisations_that_differ_by_their_arm_are_not_equal() {
        // Arrange
        Randomisation placebo  = Any.Randomisation().WithArm(TreatmentArm.Placebo).Generate();
        Randomisation highDose = new Randomisation(placebo.Number, placebo.Date, TreatmentArm.HighDose);

        // Assert
        Assert.NotEqual(placebo, highDose);
    }

}
