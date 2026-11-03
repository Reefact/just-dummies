using JustDummies;

using Xunit;

using static ClinicalTrial.Domain.Tests.Protocol;

namespace ClinicalTrial.Domain.Tests;

public sealed class CasebookAdverseEventTests {

    private static readonly DateOnly April1 = new DateOnly(2024, 4, 1);

    [Fact]
    public void Reported_events_are_numbered_from_1() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        int first  = casebook.ReportAdverseEvent(AnyEvent());
        int second = casebook.ReportAdverseEvent(AnyEvent());

        // Assert
        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, casebook.AdverseEvents.Count);
    }

    [Fact]
    public void An_event_can_be_reported_before_randomisation() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.ReportAdverseEvent(AnyEvent());

        // Assert
        Assert.False(casebook.IsRandomised);
        Assert.Single(casebook.AdverseEvents);
    }

    [Fact]
    public void An_event_cannot_start_before_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        AdverseEvent    tooEarly = NonFatalOn(Consent.AddDays(-1));

        // Act + Assert
        AssertRefused(CasebookRefusal.EventBeforeConsent, () => casebook.ReportAdverseEvent(tooEarly));
        Assert.Empty(casebook.AdverseEvents);
    }

    [Fact]
    public void An_event_may_start_the_day_of_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        int number = casebook.ReportAdverseEvent(NonFatalOn(Consent));

        // Assert
        Assert.Equal(1, number);
    }

    [Fact]
    public void A_fatal_event_gives_the_date_of_death() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(4)));

        // Assert
        Assert.Equal(April1.AddDays(4), casebook.DeathDate);
    }

    [Fact]
    public void The_date_of_death_is_the_earliest_end_of_a_fatal_event() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(9)));
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(4)));

        // Assert
        Assert.Equal(April1.AddDays(4), casebook.DeathDate);
    }

    [Fact]
    public void A_non_fatal_event_gives_no_date_of_death() {
        // Arrange
        SubjectCasebook casebook = Casebook();

        // Act
        casebook.ReportAdverseEvent(NonFatalOn(April1));

        // Assert
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void No_event_can_start_after_the_death() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(4)));
        AdverseEvent afterDeath = NonFatalOn(April1.AddDays(5));

        // Act + Assert
        AssertRefused(CasebookRefusal.EventAfterDeath, () => casebook.ReportAdverseEvent(afterDeath));
        Assert.Single(casebook.AdverseEvents);
    }

    [Fact]
    public void An_event_may_start_the_day_of_the_death() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(4)));

        // Act
        int number = casebook.ReportAdverseEvent(NonFatalOn(April1.AddDays(4)));

        // Assert
        Assert.Equal(2, number);
    }

    [Fact]
    public void A_fatal_event_cannot_end_before_an_event_already_started() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(NonFatalOn(April1.AddDays(10)));
        AdverseEvent earlyDeath = Fatal(April1, April1.AddDays(4));

        // Act + Assert
        AssertRefused(CasebookRefusal.EventAfterDeath, () => casebook.ReportAdverseEvent(earlyDeath));
        Assert.Single(casebook.AdverseEvents);
    }

    [Fact]
    public void Reporting_requires_an_event() {
        Assert.Throws<ArgumentNullException>(() => Casebook().ReportAdverseEvent(null!));
    }

    [Fact]
    public void Revising_an_event_replaces_it_in_the_log() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(NonFatalOn(April1));
        casebook.ReportAdverseEvent(NonFatalOn(April1.AddDays(3)));
        AdverseEvent revised = NonFatalOn(April1.AddDays(1));

        // Act
        casebook.ReviseAdverseEvent(1, revised);

        // Assert
        Assert.Same(revised, casebook.AdverseEvents[0]);
        Assert.Equal(2, casebook.AdverseEvents.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void An_event_that_does_not_exist_cannot_be_revised(int number) {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(AnyEvent());
        AdverseEvent anyRevision = AnyEvent();

        // Act + Assert
        AssertRefused(CasebookRefusal.UnknownAdverseEvent, () => casebook.ReviseAdverseEvent(number, anyRevision));
    }

    [Fact]
    public void A_revision_cannot_start_before_consent() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(AnyEvent());
        AdverseEvent tooEarly = NonFatalOn(Consent.AddDays(-5));

        // Act + Assert
        AssertRefused(CasebookRefusal.EventBeforeConsent, () => casebook.ReviseAdverseEvent(1, tooEarly));
    }

    [Fact]
    public void A_revision_to_a_fatal_event_cannot_precede_the_events_that_follow() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        AdverseEvent    original = NonFatalOn(April1);
        casebook.ReportAdverseEvent(original);
        casebook.ReportAdverseEvent(NonFatalOn(April1.AddDays(10)));
        AdverseEvent earlyDeath = Fatal(April1, April1.AddDays(5));

        // Act + Assert
        AssertRefused(CasebookRefusal.EventAfterDeath, () => casebook.ReviseAdverseEvent(1, earlyDeath));
        Assert.Same(original, casebook.AdverseEvents[0]);
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void A_fatal_event_revised_to_a_non_fatal_one_lifts_the_date_of_death() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(Fatal(April1, April1.AddDays(4)));

        // Act
        casebook.ReviseAdverseEvent(1, NonFatalOn(April1));

        // Assert
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void Revising_requires_an_event() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        casebook.ReportAdverseEvent(AnyEvent());

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => casebook.ReviseAdverseEvent(1, null!));
    }

    [Fact]
    public void Unresolved_adverse_event_fields_are_listed_with_the_number_of_their_event() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        AdverseEvent    assessed = Any.AdverseEvent().Resolved().WithOnsetDate(April1).Generate();
        AdverseEvent    pending  = Any.AdverseEvent().NonFatal().WithSerious(true).WithGrade(3).WithRelationship(null).WithOutcome(null).WithOnsetDate(April1).Generate();

        // Act
        casebook.ReportAdverseEvent(assessed);
        casebook.ReportAdverseEvent(pending);

        // Assert
        Assert.Equal(new[] { "AE2.Relationship", "AE2.Outcome" }, casebook.UnresolvedFields());
    }

}
