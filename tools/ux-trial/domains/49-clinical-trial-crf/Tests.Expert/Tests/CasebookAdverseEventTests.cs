using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookAdverseEventTests {

    private static AnyAdverseEvent AnEventStartingOn(DateOnly onset) {
        return Any.AdverseEvent().WithOnsetDate(onset);
    }

    private static AdverseEvent ADeathOn(DateOnly onset, DateOnly deathDate) {
        return Any.AdverseEvent().WithOnsetDate(onset).Fatal(deathDate).Generate();
    }

    [Fact]
    public void Reported_events_are_numbered_from_1() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        AdverseEvent    first    = AnEventStartingOn(casebook.ConsentDate).Generate();
        AdverseEvent    second   = AnEventStartingOn(casebook.ConsentDate).Generate();

        // Act
        int firstNumber  = casebook.ReportAdverseEvent(first);
        int secondNumber = casebook.ReportAdverseEvent(second);

        // Assert
        Assert.Equal(1, firstNumber);
        Assert.Equal(2, secondNumber);
        Assert.Same(first, casebook.AdverseEvents[0]);
        Assert.Same(second, casebook.AdverseEvents[1]);
    }

    [Fact]
    public void An_event_can_be_reported_before_the_subject_is_randomised() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act
        int number = casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());

        // Assert
        Assert.False(casebook.IsRandomised);
        Assert.Equal(1, number);
    }

    [Fact]
    public void A_missing_event_is_refused() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => casebook.ReportAdverseEvent(null!));
        Assert.Throws<ArgumentNullException>(() => casebook.ReviseAdverseEvent(1, null!));
    }

    // ---------------------------------------------------------------- consent

    [Fact]
    public void Events_are_collected_from_the_day_of_consent() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act
        int number = casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());

        // Assert
        Assert.Equal(1, number);
    }

    [Fact]
    public void An_event_that_began_before_consent_is_refused_and_not_logged() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        AdverseEvent    tooEarly = AnEventStartingOn(casebook.ConsentDate.AddDays(-1)).Generate();

        // Act
        Refusal.Of(CasebookRefusal.EventBeforeConsent, () => casebook.ReportAdverseEvent(tooEarly));

        // Assert
        Assert.Empty(casebook.AdverseEvents);
    }

    [Fact]
    public void A_revision_cannot_move_an_event_before_consent() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        AdverseEvent    original = AnEventStartingOn(casebook.ConsentDate).Generate();
        int             number   = casebook.ReportAdverseEvent(original);
        AdverseEvent    tooEarly = AnEventStartingOn(casebook.ConsentDate.AddDays(-1)).Generate();

        // Act
        Refusal.Of(CasebookRefusal.EventBeforeConsent, () => casebook.ReviseAdverseEvent(number, tooEarly));

        // Assert
        Assert.Same(original, casebook.AdverseEvents[0]);
    }

    // ---------------------------------------------------------------- death

    [Fact]
    public void The_date_of_death_is_the_end_date_of_the_fatal_event() {
        // Arrange
        SubjectCasebook casebook  = Any.SubjectCasebook().Generate();
        DateOnly        deathDate = casebook.ConsentDate.AddDays(30);

        // Act
        casebook.ReportAdverseEvent(ADeathOn(casebook.ConsentDate.AddDays(20), deathDate));

        // Assert
        Assert.Equal(deathDate, casebook.DeathDate);
    }

    [Fact]
    public void A_grade_5_event_whose_outcome_is_not_yet_entered_is_not_a_death_yet() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        AdverseEvent    pending  = AnEventStartingOn(casebook.ConsentDate).WithGrade(5).WithSerious(true).WithOutcome(null).WithEndDate(casebook.ConsentDate.AddDays(5)).Generate();

        // Act
        casebook.ReportAdverseEvent(pending);

        // Assert
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void The_earliest_death_wins_when_several_events_are_fatal() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        DateOnly        consent  = casebook.ConsentDate;
        casebook.ReportAdverseEvent(ADeathOn(consent, consent.AddDays(40)));

        // Act
        casebook.ReportAdverseEvent(ADeathOn(consent, consent.AddDays(25)));

        // Assert
        Assert.Equal(consent.AddDays(25), casebook.DeathDate);
    }

    [Fact]
    public void No_event_can_begin_after_the_death() {
        // Arrange
        SubjectCasebook casebook  = Any.SubjectCasebook().Generate();
        DateOnly        deathDate = casebook.ConsentDate.AddDays(30);
        casebook.ReportAdverseEvent(ADeathOn(casebook.ConsentDate, deathDate));
        AdverseEvent afterwards = AnEventStartingOn(deathDate.AddDays(1)).Generate();

        // Act
        Refusal.Of(CasebookRefusal.EventAfterDeath, () => casebook.ReportAdverseEvent(afterwards));

        // Assert
        Assert.Single(casebook.AdverseEvents);
    }

    [Fact]
    public void An_event_may_begin_on_the_day_of_death() {
        // Arrange
        SubjectCasebook casebook  = Any.SubjectCasebook().Generate();
        DateOnly        deathDate = casebook.ConsentDate.AddDays(30);
        casebook.ReportAdverseEvent(ADeathOn(casebook.ConsentDate, deathDate));

        // Act
        int number = casebook.ReportAdverseEvent(AnEventStartingOn(deathDate).Generate());

        // Assert
        Assert.Equal(2, number);
    }

    [Fact]
    public void A_death_cannot_be_reported_before_the_onset_of_an_event_already_logged() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        DateOnly        consent  = casebook.ConsentDate;
        casebook.ReportAdverseEvent(AnEventStartingOn(consent.AddDays(20)).WithGrade(3).Generate());
        AdverseEvent earlyDeath = ADeathOn(consent, consent.AddDays(10));

        // Act
        Refusal.Of(CasebookRefusal.EventAfterDeath, () => casebook.ReportAdverseEvent(earlyDeath));

        // Assert
        Assert.Single(casebook.AdverseEvents);
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void A_fatal_event_may_be_revised_without_conflicting_with_its_own_death() {
        // Arrange
        SubjectCasebook casebook  = Any.SubjectCasebook().Generate();
        DateOnly        consent   = casebook.ConsentDate;
        int             number    = casebook.ReportAdverseEvent(ADeathOn(consent, consent.AddDays(30)));
        AdverseEvent    corrected = ADeathOn(consent, consent.AddDays(31));

        // Act
        casebook.ReviseAdverseEvent(number, corrected);

        // Assert
        Assert.Equal(consent.AddDays(31), casebook.DeathDate);
    }

    [Fact]
    public void A_revision_cannot_put_the_death_before_another_event() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        DateOnly        consent  = casebook.ConsentDate;
        casebook.ReportAdverseEvent(AnEventStartingOn(consent.AddDays(20)).Generate());
        AdverseEvent original = AnEventStartingOn(consent).WithGrade(5).WithSerious(true).WithOutcome(null).WithEndDate(null).Generate();
        int          number   = casebook.ReportAdverseEvent(original);
        AdverseEvent earlyDeath = ADeathOn(consent, consent.AddDays(10));

        // Act
        Refusal.Of(CasebookRefusal.EventAfterDeath, () => casebook.ReviseAdverseEvent(number, earlyDeath));

        // Assert
        Assert.Same(original, casebook.AdverseEvents[number - 1]);
    }

    // ---------------------------------------------------------------- revising

    [Fact]
    public void A_revision_replaces_the_event_at_its_number() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        AdverseEvent    first    = AnEventStartingOn(casebook.ConsentDate).Generate();
        AdverseEvent    second   = AnEventStartingOn(casebook.ConsentDate).Generate();
        casebook.ReportAdverseEvent(first);
        casebook.ReportAdverseEvent(second);
        AdverseEvent revised = AnEventStartingOn(casebook.ConsentDate).Generate();

        // Act
        casebook.ReviseAdverseEvent(1, revised);

        // Assert
        Assert.Same(revised, casebook.AdverseEvents[0]);
        Assert.Same(second, casebook.AdverseEvents[1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void An_event_that_is_not_in_the_log_cannot_be_revised(int number) {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());
        casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());
        AdverseEvent revised = AnEventStartingOn(casebook.ConsentDate).Generate();

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnknownAdverseEvent, () => casebook.ReviseAdverseEvent(number, revised));

        // Assert
        Assert.Contains(number.ToString(System.Globalization.CultureInfo.InvariantCulture), refusal.Message);
    }

    [Fact]
    public void The_last_event_of_the_log_can_be_revised() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());
        int          last    = casebook.ReportAdverseEvent(AnEventStartingOn(casebook.ConsentDate).Generate());
        AdverseEvent revised = AnEventStartingOn(casebook.ConsentDate).Generate();

        // Act
        casebook.ReviseAdverseEvent(last, revised);

        // Assert
        Assert.Same(revised, casebook.AdverseEvents[1]);
    }

}
