using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class AdverseEventTests {

    private static readonly DateOnly Onset = new DateOnly(2024, 4, 2);

    private static string AnyTerm() => Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength).Generate();

    // The fields a test leaves alone are dummies; the ones it names are literals.
    private static AdverseEvent Event(int grade = 2,
                                      bool isSerious = false,
                                      DateOnly? endDate = null,
                                      Causality? relationship = null,
                                      AdverseEventOutcome? outcome = null,
                                      string? comment = null,
                                      string? term = null) {
        return new AdverseEvent(term ?? AnyTerm(), Onset, null, endDate, grade, isSerious, relationship, outcome, comment);
    }

    [Fact]
    public void An_adverse_event_exposes_what_was_recorded() {
        // Arrange
        string   term      = "Headache";
        TimeOnly onsetTime = new TimeOnly(14, 30);
        DateOnly end       = Onset.AddDays(2);

        // Act
        AdverseEvent adverseEvent = new AdverseEvent(term, Onset, onsetTime, end, 2, false, Causality.Possible, AdverseEventOutcome.Recovered, "Resolved with paracetamol");

        // Assert
        Assert.Equal("Headache", adverseEvent.Term);
        Assert.Equal(Onset, adverseEvent.OnsetDate);
        Assert.Equal(onsetTime, adverseEvent.OnsetTime);
        Assert.Equal(end, adverseEvent.EndDate);
        Assert.Equal(2, adverseEvent.Grade);
        Assert.False(adverseEvent.IsSerious);
        Assert.Equal(Causality.Possible, adverseEvent.Relationship);
        Assert.Equal(AdverseEventOutcome.Recovered, adverseEvent.Outcome);
        Assert.Equal("Resolved with paracetamol", adverseEvent.Comment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_term_is_not_blank(string term) {
        Assert.Throws<ArgumentException>(() => Event(term: term));
    }

    [Fact]
    public void A_term_is_not_absent() {
        Assert.Throws<ArgumentNullException>(() => new AdverseEvent(null!, Onset, null, null, 2, false, null, null, null));
    }

    [Fact]
    public void A_term_of_200_characters_is_accepted_and_one_of_201_is_refused() {
        Assert.Equal(200, Event(term: new string('x', 200)).Term.Length);
        Assert.Throws<ArgumentException>(() => Event(term: new string('x', 201)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Grades_1_to_4_are_accepted(int grade) {
        Assert.Equal(grade, Event(grade: grade).Grade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void A_grade_outside_1_to_5_is_refused(int grade) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Event(grade: grade, isSerious: true));
    }

    [Fact]
    public void An_adverse_event_cannot_end_before_it_began() {
        Assert.Throws<ArgumentException>(() => Event(endDate: Onset.AddDays(-1)));
    }

    [Fact]
    public void An_adverse_event_may_end_the_day_it_began() {
        // Act
        AdverseEvent adverseEvent = Event(endDate: Onset);

        // Assert
        Assert.Equal(1, adverseEvent.DurationDays);
        Assert.False(adverseEvent.IsOngoing);
    }

    [Fact]
    public void The_duration_counts_both_ends() {
        Assert.Equal(10, Event(endDate: Onset.AddDays(9)).DurationDays);
    }

    [Fact]
    public void An_adverse_event_without_an_end_is_ongoing_and_has_no_duration() {
        // Act
        AdverseEvent adverseEvent = Event();

        // Assert
        Assert.True(adverseEvent.IsOngoing);
        Assert.Null(adverseEvent.DurationDays);
    }

    [Fact]
    public void An_undeclared_causality_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Event(relationship: (Causality)42));
    }

    [Fact]
    public void An_undeclared_outcome_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Event(outcome: (AdverseEventOutcome)42));
    }

    [Fact]
    public void A_grade_5_event_is_always_serious() {
        Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: false));
        Assert.Equal(5, Event(grade: 5, isSerious: true).Grade);
    }

    [Fact]
    public void A_grade_5_event_with_an_outcome_has_a_fatal_one() {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: true, endDate: Onset, outcome: AdverseEventOutcome.Recovered));
        Assert.Equal(AdverseEventOutcome.Fatal, Event(grade: 5, isSerious: true, endDate: Onset.AddDays(1), outcome: AdverseEventOutcome.Fatal).Outcome);
    }

    [Fact]
    public void A_grade_5_event_may_have_no_outcome_yet() {
        Assert.Null(Event(grade: 5, isSerious: true).Outcome);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void A_fatal_outcome_is_graded_5(int grade) {
        Assert.Throws<ArgumentException>(() => Event(grade: grade, isSerious: true, endDate: Onset, outcome: AdverseEventOutcome.Fatal));
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovered)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae)]
    public void A_recovered_event_needs_its_end_date(AdverseEventOutcome outcome) {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => Event(outcome: outcome));
        Assert.Equal(outcome, Event(outcome: outcome, endDate: Onset.AddDays(5)).Outcome);
    }

    [Fact]
    public void A_fatal_event_needs_its_end_date() {
        Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: true, outcome: AdverseEventOutcome.Fatal));
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovering)]
    [InlineData(AdverseEventOutcome.NotRecovered)]
    public void An_event_that_is_still_ongoing_has_no_end_date(AdverseEventOutcome outcome) {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => Event(outcome: outcome, endDate: Onset.AddDays(5)));
        Assert.Equal(outcome, Event(outcome: outcome).Outcome);
    }

    [Fact]
    public void An_unknown_outcome_accepts_an_end_date_or_none() {
        Assert.NotNull(Event(outcome: AdverseEventOutcome.Unknown, endDate: Onset.AddDays(5)).EndDate);
        Assert.Null(Event(outcome: AdverseEventOutcome.Unknown).EndDate);
    }

    [Fact]
    public void A_comment_is_absent_or_between_1_and_500_characters_of_text() {
        // Assert
        Assert.Null(Event(comment: null).Comment);
        Assert.Equal(500, Event(comment: new string('c', 500)).Comment!.Length);
        Assert.Throws<ArgumentException>(() => Event(comment: new string('c', 501)));
        Assert.Throws<ArgumentException>(() => Event(comment: ""));
        Assert.Throws<ArgumentException>(() => Event(comment: "  \t "));
    }

    [Theory]
    [InlineData(Causality.NotRelated, false)]
    [InlineData(Causality.Unlikely, false)]
    [InlineData(Causality.Possible, true)]
    [InlineData(Causality.Probable, true)]
    [InlineData(Causality.Definite, true)]
    public void An_event_is_related_from_possible_up(Causality relationship, bool expected) {
        Assert.Equal(expected, Event(relationship: relationship).IsRelated);
    }

    [Fact]
    public void An_event_with_no_assessment_is_not_related() {
        Assert.False(Event(relationship: null).IsRelated);
    }

    [Fact]
    public void The_relationship_is_unresolved_until_assessed() {
        Assert.Equal(new[] { "Relationship" }, Event(relationship: null).UnresolvedFields());
        Assert.Empty(Event(relationship: Causality.Unlikely).UnresolvedFields());
    }

    [Fact]
    public void The_outcome_of_a_serious_event_is_unresolved_until_given() {
        // Act
        AdverseEvent unresolved = Event(isSerious: true, relationship: Causality.Possible);
        AdverseEvent resolved   = Event(isSerious: true, relationship: Causality.Possible, outcome: AdverseEventOutcome.Recovering);

        // Assert
        Assert.Equal(new[] { "Outcome" }, unresolved.UnresolvedFields());
        Assert.Empty(resolved.UnresolvedFields());
    }

    [Fact]
    public void The_outcome_of_a_non_serious_event_is_never_unresolved() {
        Assert.Empty(Event(isSerious: false, relationship: Causality.Possible, outcome: null).UnresolvedFields());
    }

    [Fact]
    public void The_end_date_of_a_grade_5_event_is_unresolved_until_the_date_of_death_is_given() {
        // Act
        AdverseEvent unresolved = Event(grade: 5, isSerious: true, relationship: Causality.Probable);

        // Assert
        Assert.Equal(new[] { "Outcome", "EndDate" }, unresolved.UnresolvedFields());
    }

    [Fact]
    public void A_fatal_event_with_its_date_is_fully_resolved() {
        // Act
        AdverseEvent fatal = Event(grade: 5, isSerious: true, relationship: Causality.Probable, outcome: AdverseEventOutcome.Fatal, endDate: Onset.AddDays(3));

        // Assert
        Assert.Empty(fatal.UnresolvedFields());
    }

    [Fact]
    public void Adverse_events_with_the_same_content_are_equal() {
        // Arrange
        AdverseEvent anyEvent = Any.AdverseEvent().Generate();

        // Act
        AdverseEvent same = new AdverseEvent(anyEvent.Term, anyEvent.OnsetDate, anyEvent.OnsetTime, anyEvent.EndDate, anyEvent.Grade, anyEvent.IsSerious, anyEvent.Relationship, anyEvent.Outcome, anyEvent.Comment);

        // Assert
        Assert.Equal(anyEvent, same);
        Assert.Equal(anyEvent.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Adverse_events_that_differ_by_their_grade_are_not_equal() {
        // Arrange
        string term = AnyTerm();

        // Assert
        Assert.NotEqual(Event(grade: 2, term: term), Event(grade: 3, term: term));
    }

}
