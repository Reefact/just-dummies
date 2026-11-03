using System;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class RandomisationTests {

    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void The_first_and_last_numbers_are_accepted(int number) {
        // Arrange
        DateOnly       anyDate = TrialFields.Date().Generate();
        TreatmentArm   anyArm  = Any.Enum<TreatmentArm>().Generate();

        // Act
        Randomisation randomisation = new Randomisation(number, anyDate, anyArm);

        // Assert
        Assert.Equal(number, randomisation.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void A_number_outside_1_to_9999_is_refused(int number) {
        // Arrange
        DateOnly     anyDate = TrialFields.Date().Generate();
        TreatmentArm anyArm  = Any.Enum<TreatmentArm>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(number, anyDate, anyArm));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void An_undeclared_arm_is_refused() {
        // Arrange
        int      anyNumber = Any.Randomisation().Generate().Number;
        DateOnly anyDate   = TrialFields.Date().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(anyNumber, anyDate, (TreatmentArm)42));

        // Assert
        Assert.Equal("arm", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(27, 28)]
    [InlineData(-1, -1)]
    [InlineData(-2, -2)]
    [InlineData(-30, -30)]
    public void The_study_day_counts_from_the_randomisation_date_with_no_day_zero(int daysFromRandomisation, int expectedStudyDay) {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();
        DateOnly      date             = anyRandomisation.Date.AddDays(daysFromRandomisation);

        // Act
        int studyDay = anyRandomisation.StudyDayOf(date);

        // Assert
        Assert.Equal(expectedStudyDay, studyDay);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(28, 27)]
    [InlineData(-1, -1)]
    [InlineData(-14, -14)]
    public void A_study_day_is_dated_from_the_randomisation_date(int studyDay, int expectedDaysFromRandomisation) {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        DateOnly date = anyRandomisation.DateOfStudyDay(studyDay);

        // Assert
        Assert.Equal(anyRandomisation.Date.AddDays(expectedDaysFromRandomisation), date);
    }

    [Fact]
    public void There_is_no_study_day_zero() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyRandomisation.DateOfStudyDay(0));

        // Assert
        Assert.Equal("studyDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(365)]
    public void Dating_a_study_day_then_counting_it_again_gives_the_same_day(int studyDay) {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        int roundTrip = anyRandomisation.StudyDayOf(anyRandomisation.DateOfStudyDay(studyDay));

        // Assert
        Assert.Equal(studyDay, roundTrip);
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
    public void Randomisations_differing_by_their_arm_are_not_equal() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().WithArm(TreatmentArm.Placebo).Generate();

        // Act
        Randomisation otherArm = new Randomisation(anyRandomisation.Number, anyRandomisation.Date, TreatmentArm.HighDose);

        // Assert
        Assert.NotEqual(anyRandomisation, otherArm);
    }

    [Fact]
    public void Randomisations_differing_by_their_date_are_not_equal() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        Randomisation otherDate = new Randomisation(anyRandomisation.Number, anyRandomisation.Date.AddDays(1), anyRandomisation.Arm);

        // Assert
        Assert.NotEqual(anyRandomisation, otherDate);
    }

}
