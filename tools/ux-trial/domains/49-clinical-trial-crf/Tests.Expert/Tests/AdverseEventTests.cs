using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class AdverseEventTests {

    private static readonly DateOnly Onset = new DateOnly(2026, 4, 1);

    private static string AnyTerm() {
        return Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength).Generate();
    }

    // An event of grade 1, not serious, with nothing else entered: the base every refusal below starts from.
    private static AdverseEvent MildEvent(string? term = null, DateOnly? endDate = null, AdverseEventOutcome? outcome = null, string? comment = null,
                                          Causality? relationship = null) {
        return new AdverseEvent(term ?? AnyTerm(), Onset, null, endDate, 1, false, relationship, outcome, comment);
    }

    // ---------------------------------------------------------------- term and comment

    [Fact]
    public void A_term_may_have_200_characters_but_not_201() {
        // Arrange
        string atTheLimit   = Any.String().NotBlank().WithLength(200).Generate();
        string overTheLimit = Any.String().NotBlank().WithLength(201).Generate();

        // Act
        AdverseEvent accepted = MildEvent(term: atTheLimit);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(term: overTheLimit));

        // Assert
        Assert.Equal(200, accepted.Term.Length);
        Assert.Equal("term", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \n")]
    public void A_blank_term_is_refused(string term) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(term: term));

        // Assert
        Assert.Equal("term", refusal.ParamName);
    }

    [Fact]
    public void A_missing_term_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new AdverseEvent(null!, Onset, null, null, 1, false, null, null, null));
    }

    [Fact]
    public void A_comment_is_absent_or_between_1_and_500_characters_of_text() {
        // Arrange
        string atTheLimit = Any.String().NotBlank().WithLength(500).Generate();

        // Act
        AdverseEvent absent   = MildEvent(comment: null);
        AdverseEvent atLimit  = MildEvent(comment: atTheLimit);

        // Assert
        Assert.Null(absent.Comment);
        Assert.Equal(500, atLimit.Comment!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n")]
    public void A_blank_comment_is_refused(string comment) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(comment: comment));

        // Assert
        Assert.Equal("comment", refusal.ParamName);
    }

    [Fact]
    public void A_comment_of_501_characters_is_refused() {
        // Arrange
        string overTheLimit = Any.String().NotBlank().WithLength(501).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(comment: overTheLimit));

        // Assert
        Assert.Equal("comment", refusal.ParamName);
    }

    // ---------------------------------------------------------------- grade, dates, enums

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Grades_1_to_4_are_accepted_without_seriousness(int grade) {
        // Act
        AdverseEvent adverseEvent = Any.AdverseEvent().WithGrade(grade).WithSerious(false).Generate();

        // Assert
        Assert.Equal(grade, adverseEvent.Grade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void A_grade_outside_1_to_5_is_refused(int grade) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdverseEvent(AnyTerm(), Onset, null, null, grade, false, null, null, null));

        // Assert
        Assert.Equal("grade", refusal.ParamName);
    }

    [Fact]
    public void An_event_cannot_end_before_it_began() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(endDate: Onset.AddDays(-1)));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Fact]
    public void An_event_may_end_on_the_day_it_began() {
        // Act
        AdverseEvent adverseEvent = MildEvent(endDate: Onset);

        // Assert
        Assert.Equal(1, adverseEvent.DurationDays);
    }

    [Fact]
    public void A_causality_the_investigator_cannot_choose_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => MildEvent(relationship: (Causality)17));

        // Assert
        Assert.Equal("relationship", refusal.ParamName);
    }

    [Fact]
    public void An_outcome_the_log_does_not_know_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => MildEvent(outcome: (AdverseEventOutcome)17));

        // Assert
        Assert.Equal("outcome", refusal.ParamName);
    }

    // ---------------------------------------------------------------- grade 5 and the fatal outcome

    [Fact]
    public void A_grade_5_event_is_always_serious() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AdverseEvent().WithGrade(5).WithSerious(false).Generate());

        // Assert
        Assert.Equal("isSerious", refusal.ParamName);
    }

    [Fact]
    public void A_grade_5_event_may_leave_its_outcome_to_enter_later() {
        // Act
        AdverseEvent adverseEvent = Any.AdverseEvent().WithGrade(5).WithSerious(true).WithOutcome(null).WithEndDate(null).Generate();

        // Assert
        Assert.Null(adverseEvent.Outcome);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovered)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae)]
    [InlineData(AdverseEventOutcome.Recovering)]
    [InlineData(AdverseEventOutcome.NotRecovered)]
    [InlineData(AdverseEventOutcome.Unknown)]
    public void A_grade_5_event_has_a_fatal_outcome_or_none(AdverseEventOutcome outcome) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.AdverseEvent().WithGrade(5).WithSerious(true).WithOutcome(outcome).Generate());

        // Assert
        Assert.Equal("outcome", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void A_fatal_outcome_is_graded_5(int grade) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.AdverseEvent().WithGrade(grade).WithOutcome(AdverseEventOutcome.Fatal).WithEndDate(Onset).WithOnsetDate(Onset).Generate());

        // Assert
        Assert.Equal("grade", refusal.ParamName);
    }

    [Fact]
    public void A_fatal_event_is_grade_5_serious_and_ends_on_the_day_of_death() {
        // Arrange
        DateOnly deathDate = Onset.AddDays(9);

        // Act
        AdverseEvent adverseEvent = Any.AdverseEvent().WithOnsetDate(Onset).Fatal(deathDate).Generate();

        // Assert
        Assert.Equal(AdverseEventOutcome.Fatal, adverseEvent.Outcome);
        Assert.Equal(deathDate, adverseEvent.EndDate);
        Assert.Equal(10, adverseEvent.DurationDays);
    }

    // ---------------------------------------------------------------- outcome and end date

    [Theory]
    [InlineData(AdverseEventOutcome.Recovered)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae)]
    public void An_event_that_has_recovered_needs_its_end_date(AdverseEventOutcome outcome) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(outcome: outcome, endDate: null));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Fact]
    public void A_fatal_event_needs_its_end_date() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.AdverseEvent().WithGrade(5).WithSerious(true).WithOutcome(AdverseEventOutcome.Fatal).WithEndDate(null).Generate());

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovering)]
    [InlineData(AdverseEventOutcome.NotRecovered)]
    public void An_event_still_going_on_has_no_end_date(AdverseEventOutcome outcome) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MildEvent(outcome: outcome, endDate: Onset.AddDays(3)));

        // Assert
        Assert.Equal("endDate", refusal.ParamName);
    }

    [Theory]
    [InlineData(AdverseEventOutcome.Recovering, false)]
    [InlineData(AdverseEventOutcome.NotRecovered, false)]
    [InlineData(AdverseEventOutcome.Recovered, true)]
    [InlineData(AdverseEventOutcome.RecoveredWithSequelae, true)]
    public void An_event_is_ongoing_exactly_when_it_has_no_end_date(AdverseEventOutcome outcome, bool hasEnded) {
        // Act
        AdverseEvent adverseEvent = MildEvent(outcome: outcome, endDate: hasEnded ? Onset.AddDays(4) : null);

        // Assert
        Assert.Equal(!hasEnded, adverseEvent.IsOngoing);
        Assert.Equal(hasEnded ? 5 : null, adverseEvent.DurationDays);
    }

    [Fact]
    public void An_unknown_or_absent_outcome_accepts_an_end_date_or_none() {
        // Act
        AdverseEvent unknownEnded    = MildEvent(outcome: AdverseEventOutcome.Unknown, endDate: Onset.AddDays(2));
        AdverseEvent unknownOngoing  = MildEvent(outcome: AdverseEventOutcome.Unknown);
        AdverseEvent absentEnded     = MildEvent(outcome: null, endDate: Onset.AddDays(2));
        AdverseEvent absentOngoing   = MildEvent(outcome: null);

        // Assert
        Assert.False(unknownEnded.IsOngoing);
        Assert.True(unknownOngoing.IsOngoing);
        Assert.False(absentEnded.IsOngoing);
        Assert.True(absentOngoing.IsOngoing);
    }

    [Fact]
    public void A_duration_counts_both_its_first_and_its_last_day() {
        // Act
        AdverseEvent adverseEvent = MildEvent(endDate: new DateOnly(2026, 4, 3));

        // Assert
        Assert.Equal(3, adverseEvent.DurationDays);
    }

    // ---------------------------------------------------------------- relationship

    [Theory]
    [InlineData(Causality.NotRelated, false)]
    [InlineData(Causality.Unlikely, false)]
    [InlineData(Causality.Possible, true)]
    [InlineData(Causality.Probable, true)]
    [InlineData(Causality.Definite, true)]
    public void An_event_is_related_from_a_possible_causality_up(Causality relationship, bool expected) {
        // Act
        AdverseEvent adverseEvent = MildEvent(relationship: relationship);

        // Assert
        Assert.Equal(expected, adverseEvent.IsRelated);
    }

    [Fact]
    public void An_event_whose_relationship_is_not_assessed_yet_is_not_related() {
        // Act
        AdverseEvent adverseEvent = MildEvent(relationship: null);

        // Assert
        Assert.False(adverseEvent.IsRelated);
    }

    // ---------------------------------------------------------------- what is left to enter

    [Fact]
    public void The_relationship_is_left_to_enter_until_assessed() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().WithRelationship(null).Generate();

        // Assert
        Assert.Equal(["Relationship"], adverseEvent.UnresolvedFields());
    }

    [Fact]
    public void The_outcome_of_a_serious_event_is_left_to_enter() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().WithGrade(3).WithSerious(true).WithOutcome(null).WithEndDate(null).Generate();

        // Assert
        Assert.Equal(["Outcome"], adverseEvent.UnresolvedFields());
    }

    [Fact]
    public void The_outcome_of_an_event_that_is_not_serious_is_not_required() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().WithGrade(2).WithSerious(false).WithOutcome(null).WithEndDate(null).Generate();

        // Assert
        Assert.Empty(adverseEvent.UnresolvedFields());
    }

    [Fact]
    public void The_date_of_death_of_a_grade_5_event_is_left_to_enter() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().WithGrade(5).WithSerious(true).WithOutcome(null).WithEndDate(null).Generate();

        // Assert
        Assert.Equal(["Outcome", "EndDate"], adverseEvent.UnresolvedFields());
    }

    [Fact]
    public void An_event_with_everything_entered_has_nothing_left() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().Generate();

        // Assert
        Assert.Empty(adverseEvent.UnresolvedFields());
    }

    [Fact]
    public void A_fatal_event_with_a_relationship_has_nothing_left() {
        // Arrange
        AdverseEvent adverseEvent = Any.AdverseEvent().Resolved().Fatal(Onset.AddDays(5)).WithOnsetDate(Onset).Generate();

        // Assert
        Assert.Empty(adverseEvent.UnresolvedFields());
    }

    // ---------------------------------------------------------------- equality

    [Fact]
    public void Two_events_are_equal_when_every_field_is() {
        // Arrange
        AdverseEvent anyEvent = Any.AdverseEvent().Generate();

        // Act
        AdverseEvent sameEvent = new AdverseEvent(anyEvent.Term, anyEvent.OnsetDate, anyEvent.OnsetTime, anyEvent.EndDate, anyEvent.Grade,
                                                  anyEvent.IsSerious, anyEvent.Relationship, anyEvent.Outcome, anyEvent.Comment);

        // Assert
        Assert.Equal(anyEvent, sameEvent);
        Assert.Equal(anyEvent.GetHashCode(), sameEvent.GetHashCode());
    }

    [Fact]
    public void An_event_with_another_comment_is_another_event() {
        // Arrange
        AdverseEvent first  = MildEvent(term: "Headache", comment: "First");
        AdverseEvent second = MildEvent(term: "Headache", comment: "Second");

        // Assert
        Assert.NotEqual(first, second);
    }

}
