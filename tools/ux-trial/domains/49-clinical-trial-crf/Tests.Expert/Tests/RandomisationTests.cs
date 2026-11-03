using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class RandomisationTests {

    private static readonly DateOnly RandomisedOn = new DateOnly(2026, 3, 10);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(27, 28)]
    [InlineData(-1, -1)]
    [InlineData(-2, -2)]
    [InlineData(-28, -28)]
    public void The_randomisation_date_is_day_1_and_the_day_before_it_is_day_minus_1(int daysFromRandomisation, int expectedStudyDay) {
        // Arrange
        Randomisation randomisation = Any.Randomisation().WithDate(RandomisedOn).Generate();

        // Act
        int studyDay = randomisation.StudyDayOf(RandomisedOn.AddDays(daysFromRandomisation));

        // Assert
        Assert.Equal(expectedStudyDay, studyDay);
    }

    [Fact]
    public void Study_days_count_across_a_change_of_year() {
        // Arrange
        Randomisation randomisation = Any.Randomisation().WithDate(new DateOnly(2026, 1, 1)).Generate();

        // Act / Assert
        Assert.Equal(-1, randomisation.StudyDayOf(new DateOnly(2025, 12, 31)));
        Assert.Equal(365, randomisation.StudyDayOf(new DateOnly(2026, 12, 31)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(28, 27)]
    [InlineData(-1, -1)]
    [InlineData(-14, -14)]
    public void A_study_day_maps_back_to_its_calendar_date(int studyDay, int daysFromRandomisation) {
        // Arrange
        Randomisation randomisation = Any.Randomisation().WithDate(RandomisedOn).Generate();

        // Act
        DateOnly date = randomisation.DateOfStudyDay(studyDay);

        // Assert
        Assert.Equal(RandomisedOn.AddDays(daysFromRandomisation), date);
    }

    [Fact]
    public void There_is_no_study_day_0() {
        // Arrange
        Randomisation randomisation = Any.Randomisation().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => randomisation.DateOfStudyDay(0));

        // Assert
        Assert.Equal("studyDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void The_first_and_the_last_randomisation_numbers_are_accepted(int number) {
        // Arrange
        DateOnly      anyDate = Scenarios.AnyDate();
        TreatmentArm  anyArm  = Any.Enum<TreatmentArm>().Generate();

        // Act
        Randomisation randomisation = new Randomisation(number, anyDate, anyArm);

        // Assert
        Assert.Equal(number, randomisation.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void A_randomisation_number_outside_1_to_9999_is_refused(int number) {
        // Arrange
        DateOnly     anyDate = Scenarios.AnyDate();
        TreatmentArm anyArm  = Any.Enum<TreatmentArm>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(number, anyDate, anyArm));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void An_arm_the_protocol_does_not_know_is_refused() {
        // Arrange
        int      anyNumber = Any.Int32().Between(1, 9999).Generate();
        DateOnly anyDate   = Scenarios.AnyDate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Randomisation(anyNumber, anyDate, (TreatmentArm)42));

        // Assert
        Assert.Equal("arm", refusal.ParamName);
    }

    [Fact]
    public void Two_randomisations_are_equal_when_number_date_and_arm_are() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();

        // Act
        Randomisation sameRandomisation = new Randomisation(anyRandomisation.Number, anyRandomisation.Date, anyRandomisation.Arm);

        // Assert
        Assert.Equal(anyRandomisation, sameRandomisation);
        Assert.Equal(anyRandomisation.GetHashCode(), sameRandomisation.GetHashCode());
    }

    [Fact]
    public void A_randomisation_in_another_arm_is_not_the_same_randomisation() {
        // Arrange
        Randomisation anyRandomisation = Any.Randomisation().Generate();
        TreatmentArm  anyOtherArm      = Any.Enum<TreatmentArm>().DifferentFrom(anyRandomisation.Arm).Generate();

        // Act
        Randomisation other = new Randomisation(anyRandomisation.Number, anyRandomisation.Date, anyOtherArm);

        // Assert
        Assert.NotEqual(anyRandomisation, other);
    }

}
