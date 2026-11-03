using System;
using System.Collections.Generic;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class AdverseEventTests {

    private static readonly DateOnly Onset = new DateOnly(2025, 3, 10);

    /// <summary>An event starting on <see cref="Onset" />: the term and comment are dummies, the rest is what each test is about.</summary>
    private static AdverseEvent Event(int grade = 2, bool isSerious = false, DateOnly? end = null, AdverseEventOutcome? outcome = null,
                                      Causality? relationship = null, string? comment = null, DateOnly? onset = null) {
        string anyTerm = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength).Generate();

        return new AdverseEvent(anyTerm, onset ?? Onset, null, end, grade, isSerious, relationship, outcome, comment);
    }

    [Fact]
    public void An_event_exposes_what_was_recorded() {
        // Arrange
        string   anyTerm    = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength).Generate();
        string   anyComment = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxCommentLength).Generate();
        TimeOnly anyTime    = Any.TimeOnly().Generate();

        // Act
        AdverseEvent ae = new AdverseEvent(anyTerm, Onset, anyTime, new DateOnly(2025, 3, 12), 3, true, Causality.Probable, AdverseEventOutcome.Recovered, anyComment);

        // Assert
        Assert.Equal(anyTerm, ae.Term);
        Assert.Equal(Onset, ae.OnsetDate);
        Assert.Equal(anyTime, ae.OnsetTime);
        Assert.Equal(new DateOnly(2025, 3, 12), ae.EndDate);
        Assert.Equal(3, ae.Grade);
        Assert.True(ae.IsSerious);
        Assert.Equal(Causality.Probable, ae.Relationship);
        Assert.Equal(AdverseEventOutcome.Recovered, ae.Outcome);
        Assert.Equal(anyComment, ae.Comment);
    }

    [Fact]
    public void An_event_may_be_recorded_with_only_what_is_known_at_onset() {
        // Act
        AdverseEvent ae = Event();

        // Assert
        Assert.True(ae.IsOngoing);
        Assert.Null(ae.Relationship);
        Assert.Null(ae.Outcome);
        Assert.Null(ae.DurationDays);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_term_is_refused(string term) {
        Assert.Throws<ArgumentException>(() => new AdverseEvent(term, Onset, null, null, 2, false, null, null, null));
    }

    [Fact]
    public void A_missing_term_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AdverseEvent(null!, Onset, null, null, 2, false, null, null, null));
    }

    [Fact]
    public void A_term_may_have_200_characters_and_not_201() {
        // Act
        AdverseEvent longest = new AdverseEvent(new string('x', 200), Onset, null, null, 2, false, null, null, null);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AdverseEvent(new string('x', 201), Onset, null, null, 2, false, null, null, null));

        // Assert
        Assert.Equal(200, longest.Term.Length);
        Assert.Equal("term", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Grades_1_to_4_are_accepted(int grade) {
        Assert.Equal(grade, Event(grade).Grade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void A_grade_outside_1_to_5_is_refused(int grade) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Event(grade));

        Assert.Equal("grade", refusal.ParamName);
    }

    [Fact]
    public void An_event_cannot_end_before_its_onset() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(end: Onset.AddDays(-1)));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Fact]
    public void An_event_may_end_the_day_it_starts() {
        // Act
        AdverseEvent ae = Event(end: Onset);

        // Assert
        Assert.Equal(1, ae.DurationDays);
        Assert.False(ae.IsOngoing);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(9, 10)]
    public void The_duration_counts_both_ends(int daysAfterOnset, int expectedDuration) {
        Assert.Equal(expectedDuration, Event(end: Onset.AddDays(daysAfterOnset)).DurationDays);
    }

    [Fact]
    public void The_duration_spans_a_change_of_month_and_year() {
        // Act
        AdverseEvent ae = Event(onset: new DateOnly(2024, 12, 30), end: new DateOnly(2025, 1, 2));

        // Assert
        Assert.Equal(4, ae.DurationDays);
    }

    [Fact]
    public void An_undeclared_relationship_or_outcome_is_refused() {
        // Assert
        Assert.Equal("relationship", Assert.Throws<ArgumentOutOfRangeException>(() => Event(relationship: (Causality)42)).ParamName);
        Assert.Equal("outcome", Assert.Throws<ArgumentOutOfRangeException>(() => Event(outcome: (AdverseEventOutcome)42)).ParamName);
    }

    [Fact]
    public void A_fatal_grade_5_event_is_always_serious() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: false));

        // Assert
        Assert.Equal("isSerious", refusal.ParamName);
    }

    [Fact]
    public void A_grade_5_event_has_a_fatal_outcome_when_it_has_one() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: true, end: Onset.AddDays(2), outcome: AdverseEventOutcome.Recovered));

        // Assert
        Assert.Equal("outcome", refusal.ParamName);
    }

    [Fact]
    public void A_grade_5_event_may_wait_for_its_outcome() {
        // Act
        AdverseEvent ae = Event(grade: 5, isSerious: true);

        // Assert
        Assert.Null(ae.Outcome);
    }

    [Fact]
    public void A_grade_5_event_with_a_fatal_outcome_is_accepted() {
        // Act
        AdverseEvent ae = Event(grade: 5, isSerious: true, end: Onset.AddDays(2), outcome: AdverseEventOutcome.Fatal);

        // Assert
        Assert.Equal(AdverseEventOutcome.Fatal, ae.Outcome);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void A_fatal_outcome_is_graded_5(int grade) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(grade: grade, isSerious: true, end: Onset.AddDays(2), outcome: AdverseEventOutcome.Fatal));

        // Assert
        Assert.Equal("grade", refusal.ParamName);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovered)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae)]
    public void An_outcome_that_ended_the_event_needs_an_end_date(AdverseEventOutcome outcome) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(outcome: outcome));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Fact]
    public void A_fatal_outcome_needs_an_end_date() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(grade: 5, isSerious: true, outcome: AdverseEventOutcome.Fatal));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovered)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae)]
    public void An_outcome_that_ended_the_event_is_accepted_with_an_end_date(AdverseEventOutcome outcome) {
        Assert.Equal(outcome, Event(end: Onset.AddDays(4), outcome: outcome).Outcome);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovering)]
    [InlineData(AdverseEventOutcome.NotRecovered)]
    public void An_outcome_of_an_ongoing_event_has_no_end_date(AdverseEventOutcome outcome) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Event(end: Onset.AddDays(4), outcome: outcome));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
        Assert.True(Event(outcome: outcome).IsOngoing);
    }

    [Fact]
    public void An_unknown_outcome_is_compatible_with_an_end_date_or_without() {
        // Assert
        Assert.NotNull(Event(outcome: AdverseEventOutcome.Unknown));
        Assert.NotNull(Event(end: Onset.AddDays(3), outcome: AdverseEventOutcome.Unknown));
    }

    [Fact]
    public void An_event_without_outcome_is_compatible_with_an_end_date_or_without() {
        // Assert
        Assert.NotNull(Event());
        Assert.NotNull(Event(end: Onset.AddDays(3)));
    }

    [Fact]
    public void A_comment_may_have_between_1_and_500_characters() {
        // Assert
        Assert.Equal("x", Event(comment: "x").Comment);
        Assert.Equal(500, Event(comment: new string('x', 500)).Comment!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void A_blank_comment_is_refused_it_is_absent_instead(string comment) {
        Assert.Equal("comment", Assert.Throws<ArgumentException>(() => Event(comment: comment)).ParamName);
    }

    [Fact]
    public void A_comment_of_501_characters_is_refused() {
        Assert.Equal("comment", Assert.Throws<ArgumentException>(() => Event(comment: new string('x', 501))).ParamName);
    }

    [Theory]
    [InlineData(Causality.NotRelated, false)]
    [InlineData(Causality.Unlikely, false)]
    [InlineData(Causality.Possible, true)]
    [InlineData(Causality.Probable, true)]
    [InlineData(Causality.Definite, true)]
    public void An_event_is_related_when_the_relationship_is_possible_probable_or_definite(Causality relationship, bool expected) {
        Assert.Equal(expected, Event(relationship: relationship).IsRelated);
    }

    [Fact]
    public void An_event_whose_relationship_is_not_assessed_is_not_related() {
        Assert.False(Event(relationship: null).IsRelated);
    }

    [Fact]
    public void The_relationship_blocks_signing_until_assessed() {
        // Act
        IReadOnlyList<string> unresolved = Event().UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Relationship" }, unresolved);
    }

    [Fact]
    public void The_outcome_of_a_serious_event_blocks_signing_until_given() {
        // Act
        IReadOnlyList<string> unresolved = Event(isSerious: true, relationship: Causality.Possible).UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Outcome" }, unresolved);
    }

    [Fact]
    public void The_outcome_of_a_non_serious_event_does_not_block_signing() {
        // Act
        IReadOnlyList<string> unresolved = Event(isSerious: false, relationship: Causality.Possible).UnresolvedFields();

        // Assert
        Assert.Empty(unresolved);
    }

    [Fact]
    public void The_date_of_death_of_a_grade_5_event_blocks_signing_until_given() {
        // Act
        IReadOnlyList<string> unresolved = Event(grade: 5, isSerious: true, relationship: Causality.Unlikely).UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Outcome", "EndDate" }, unresolved);
    }

    [Fact]
    public void A_fatal_event_with_its_date_of_death_and_relationship_leaves_nothing_to_resolve() {
        // Act
        AdverseEvent ae = Event(grade: 5, isSerious: true, end: Onset.AddDays(1), outcome: AdverseEventOutcome.Fatal, relationship: Causality.Definite);

        // Assert
        Assert.Empty(ae.UnresolvedFields());
    }

    [Fact]
    public void A_resolved_event_leaves_nothing_to_resolve() {
        // Act
        AdverseEvent ae = Any.AdverseEvent().Resolved().Generate();

        // Assert
        Assert.Empty(ae.UnresolvedFields());
    }

    [Fact]
    public void Events_made_of_the_same_content_are_equal() {
        // Arrange
        AdverseEvent anyEvent = Any.AdverseEvent().Generate();

        // Act
        AdverseEvent same = new AdverseEvent(anyEvent.Term, anyEvent.OnsetDate, anyEvent.OnsetTime, anyEvent.EndDate, anyEvent.Grade, anyEvent.IsSerious,
                                             anyEvent.Relationship, anyEvent.Outcome, anyEvent.Comment);

        // Assert
        Assert.Equal(anyEvent, same);
        Assert.Equal(anyEvent.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Events_differing_by_their_comment_only_are_not_equal() {
        // Arrange
        AdverseEvent first  = new AdverseEvent("Headache", Onset, null, null, 2, false, null, null, "first");
        AdverseEvent second = new AdverseEvent("Headache", Onset, null, null, 2, false, null, null, "second");

        // Assert
        Assert.NotEqual(first, second);
    }

}
