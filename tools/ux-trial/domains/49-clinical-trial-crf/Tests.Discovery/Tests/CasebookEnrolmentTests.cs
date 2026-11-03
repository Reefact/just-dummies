using JustDummies;

using Xunit;

using static ClinicalTrial.Domain.Tests.Protocol;

namespace ClinicalTrial.Domain.Tests;

public sealed class CasebookEnrolmentTests {

    [Fact]
    public void A_new_casebook_is_neither_randomised_nor_locked() {
        // Arrange
        ScreeningNumber number = new ScreeningNumber("FR012-0042");

        // Act
        SubjectCasebook casebook = new SubjectCasebook(number, "JD", Consent, Schedule());

        // Assert
        Assert.Same(number, casebook.ScreeningNumber);
        Assert.Equal("JD", casebook.Initials);
        Assert.Equal(Consent, casebook.ConsentDate);
        Assert.False(casebook.IsRandomised);
        Assert.Null(casebook.Randomisation);
        Assert.False(casebook.IsLocked);
        Assert.False(casebook.IsLostToFollowUp);
        Assert.Null(casebook.DeathDate);
        Assert.Empty(casebook.Forms);
        Assert.Empty(casebook.AdverseEvents);
        Assert.Equal(0, casebook.OpenQueryCount);
    }

    [Fact]
    public void Initials_are_optional() {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();

        // Act
        SubjectCasebook casebook = new SubjectCasebook(anyNumber, null, Consent, Schedule());

        // Assert
        Assert.Null(casebook.Initials);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABC")]
    public void Initials_are_two_or_three_upper_case_letters(string initials) {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();

        // Act
        SubjectCasebook casebook = new SubjectCasebook(anyNumber, initials, Consent, Schedule());

        // Assert
        Assert.Equal(initials, casebook.Initials);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCD")]
    [InlineData("ab")]
    [InlineData("A1")]
    [InlineData("A B")]
    public void Anything_else_as_initials_is_refused(string initials) {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new SubjectCasebook(anyNumber, initials, Consent, Schedule()));
    }

    [Fact]
    public void A_casebook_needs_a_screening_number_and_a_schedule() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = TrialCalendar.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(null!, null, anyConsent, anySchedule));
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(anyNumber, null, anyConsent, null!));
    }

    [Fact]
    public void Randomising_records_the_randomisation() {
        // Arrange
        SubjectCasebook casebook      = Casebook();
        Randomisation   randomisation = Any.Randomisation().WithDate(RandomisationDay).Generate();

        // Act
        casebook.Randomise(randomisation);

        // Assert
        Assert.True(casebook.IsRandomised);
        Assert.Same(randomisation, casebook.Randomisation);
    }

    [Fact]
    public void A_subject_is_randomised_once() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        Randomisation   another  = Any.Randomisation().WithDate(RandomisationDay.AddDays(1)).Generate();

        // Act + Assert
        AssertRefused(CasebookRefusal.AlreadyRandomised, () => casebook.Randomise(another));
    }

    [Fact]
    public void Randomisation_cannot_precede_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        Randomisation   tooEarly = Any.Randomisation().WithDate(Consent.AddDays(-1)).Generate();

        // Act + Assert
        AssertRefused(CasebookRefusal.RandomisationBeforeConsent, () => casebook.Randomise(tooEarly));
        Assert.False(casebook.IsRandomised);
    }

    [Fact]
    public void A_subject_may_be_randomised_the_day_of_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.Randomise(Any.Randomisation().WithDate(Consent).Generate());

        // Assert
        Assert.True(casebook.IsRandomised);
    }

    [Fact]
    public void A_subject_may_be_randomised_42_days_after_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.Randomise(Any.Randomisation().WithDate(Consent.AddDays(SubjectCasebook.MaxScreeningDays)).Generate());

        // Assert
        Assert.True(casebook.IsRandomised);
    }

    [Fact]
    public void A_subject_cannot_be_randomised_43_days_after_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        Randomisation   tooLate  = Any.Randomisation().WithDate(Consent.AddDays(43)).Generate();

        // Act + Assert
        AssertRefused(CasebookRefusal.ScreeningPeriodExceeded, () => casebook.Randomise(tooLate));
        Assert.False(casebook.IsRandomised);
    }

    [Fact]
    public void Randomising_requires_a_randomisation() {
        Assert.Throws<ArgumentNullException>(() => Casebook().Randomise(null!));
    }

    [Fact]
    public void The_window_of_a_visit_is_given_in_calendar_dates() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        (DateOnly earliest, DateOnly latest) = casebook.WindowOf("D29");

        // Assert: days 26 to 32, day 1 being March 11
        Assert.Equal(new DateOnly(2024, 4, 5), earliest);
        Assert.Equal(new DateOnly(2024, 4, 11), latest);
    }

    [Fact]
    public void The_window_of_a_visit_needs_a_randomised_subject() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act + Assert
        AssertRefused(CasebookRefusal.NotRandomised, () => casebook.WindowOf("D29"));
    }

    [Fact]
    public void The_window_of_an_unscheduled_visit_is_refused() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act + Assert
        AssertRefused(CasebookRefusal.UnknownVisit, () => casebook.WindowOf("D99"));
    }

    [Fact]
    public void Randomisation_is_checked_before_the_visit_is_looked_up() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act + Assert
        AssertRefused(CasebookRefusal.NotRandomised, () => casebook.WindowOf("D99"));
    }

}
