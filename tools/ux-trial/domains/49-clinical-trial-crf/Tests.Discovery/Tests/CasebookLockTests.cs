using JustDummies;

using Xunit;

using static ClinicalTrial.Domain.Tests.Protocol;

namespace ClinicalTrial.Domain.Tests;

public sealed class CasebookLockTests {

    private static readonly DateOnly April1 = new DateOnly(2024, 4, 1);

    [Fact]
    public void A_casebook_that_is_not_randomised_cannot_be_locked() {
        AssertRefused(CasebookRefusal.NotRandomised, () => Casebook().Lock());
    }

    [Fact]
    public void A_casebook_with_no_visit_recorded_cannot_be_locked_and_names_every_visit() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.FormsNotSigned, refusal.Reason);
        Assert.Contains("D8, D29, D85", refusal.Message);
    }

    [Fact]
    public void A_casebook_with_a_visit_still_to_sign_cannot_be_locked() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.RaiseQuery("D29", "Vitals", Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate(), DateOfDay(30));

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.FormsNotSigned, refusal.Reason);
        Assert.Contains("D29", refusal.Message);
        Assert.DoesNotContain("D8", refusal.Message);
        Assert.False(casebook.IsLocked);
    }

    [Fact]
    public void A_casebook_with_every_visit_signed_is_locked_and_so_are_its_forms() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
        Assert.All(casebook.Forms, form => Assert.Equal(FormStatus.Locked, form.Status));
        Assert.Equal(3, casebook.Forms.Count);
    }

    [Fact]
    public void A_casebook_with_an_unresolved_adverse_event_cannot_be_locked() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithGrade(2).WithSerious(false).WithRelationship(null).WithOnsetDate(April1).Generate());

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("AE1.Relationship", refusal.Message);
        Assert.False(casebook.IsLocked);
    }

    [Fact]
    public void A_casebook_whose_adverse_events_are_resolved_is_locked() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithOnsetDate(April1).Generate());

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
    }

    [Fact]
    public void A_subject_who_died_is_locked_once_the_fatal_event_is_resolved() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.ReportAdverseEvent(Fatal(DateOfDay(86), DateOfDay(88)));

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
    }

    [Fact]
    public void A_grade_5_event_without_its_date_of_death_blocks_the_lock() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        AdverseEvent    death    = new AdverseEvent("Cardiac arrest", DateOfDay(86), null, null, 5, true, Causality.Probable, null, null);
        casebook.ReportAdverseEvent(death);

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("AE1.Outcome, AE1.EndDate", refusal.Message);
    }

    [Theory]
    [InlineData("Randomise")]
    [InlineData("RecordVisit")]
    [InlineData("SubmitVisit")]
    [InlineData("RaiseQuery")]
    [InlineData("AnswerQuery")]
    [InlineData("CloseQuery")]
    [InlineData("SignVisit")]
    [InlineData("ReportAdverseEvent")]
    [InlineData("ReviseAdverseEvent")]
    [InlineData("Lock")]
    public void Every_change_on_a_locked_casebook_is_refused(string operation) {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.Lock();
        Randomisation anyRandomisation = Any.Randomisation().Generate();
        VisitEntry    anyEntry         = Entry(DateOfDay(8));
        AdverseEvent  anyEvent         = AnyEvent();
        string        anyText          = Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();

        Action change = operation switch {
            "Randomise"          => () => casebook.Randomise(anyRandomisation),
            "RecordVisit"        => () => casebook.RecordVisit("D8", anyEntry),
            "SubmitVisit"        => () => casebook.SubmitVisit("D8"),
            "RaiseQuery"         => () => casebook.RaiseQuery("D8", "Vitals", anyText, DateOfDay(12)),
            "AnswerQuery"        => () => casebook.AnswerQuery("D8", 1, anyText, DateOfDay(12)),
            "CloseQuery"         => () => casebook.CloseQuery("D8", 1, DateOfDay(12)),
            "SignVisit"          => () => casebook.SignVisit("D8"),
            "ReportAdverseEvent" => () => casebook.ReportAdverseEvent(anyEvent),
            "ReviseAdverseEvent" => () => casebook.ReviseAdverseEvent(1, anyEvent),
            _                    => () => casebook.Lock()
        };

        // Act + Assert
        AssertRefused(CasebookRefusal.CasebookLocked, change);
    }

    [Fact]
    public void Reading_a_locked_casebook_is_still_possible() {
        // Arrange
        SubjectCasebook casebook = SignedCasebook();
        casebook.Lock();

        // Assert
        Assert.Empty(casebook.UnresolvedFields());
        Assert.Equal(new DateOnly(2024, 4, 5), casebook.WindowOf("D29").Earliest);
    }

}
