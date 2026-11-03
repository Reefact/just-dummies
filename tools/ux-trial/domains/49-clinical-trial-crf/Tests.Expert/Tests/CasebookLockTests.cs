using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookLockTests {

    private static SubjectCasebook ASignedCasebook(int visits = 2) {
        SubjectCasebook casebook = Scenarios.Randomised(visits);

        casebook.SignEveryVisit();

        return casebook;
    }

    // ---------------------------------------------------------------- locking

    [Fact]
    public void A_casebook_whose_visits_are_all_signed_is_locked_and_so_are_its_forms() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
        Assert.All(casebook.Forms, form => Assert.Equal(FormStatus.Locked, form.Status));
    }

    [Fact]
    public void A_casebook_not_randomised_cannot_be_locked() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.NotRandomised, () => casebook.Lock());
    }

    [Fact]
    public void Every_scheduled_visit_must_be_recorded_and_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised(3);
        string          signed   = casebook.CodeOf(0);
        string          drafted  = casebook.CodeOf(1);
        string          notYet   = casebook.CodeOf(2);
        casebook.RecordAndSign(signed);
        casebook.RecordComplete(drafted);

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.FormsNotSigned, () => casebook.Lock());

        // Assert
        Assert.Contains($"{drafted}, {notYet}", refusal.Message);
        Assert.DoesNotContain(signed, refusal.Message.Replace(drafted, "").Replace(notYet, ""));
        Assert.False(casebook.IsLocked);
    }

    [Fact]
    public void A_signature_taken_back_by_a_query_keeps_the_casebook_from_locking() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        string          code     = casebook.CodeOf(1);
        casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate());

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.FormsNotSigned, () => casebook.Lock());

        // Assert
        Assert.Contains(code, refusal.Message);
    }

    [Fact]
    public void An_adverse_event_with_fields_left_to_enter_keeps_the_casebook_from_locking() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithRelationship(null).WithOnsetDate(casebook.ConsentDate).Generate());

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.Lock());

        // Assert
        Assert.Contains("AE1.Relationship", refusal.Message);
        Assert.False(casebook.IsLocked);
    }

    [Fact]
    public void Unsigned_visits_are_refused_before_unresolved_events() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised(2);
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithRelationship(null).WithOnsetDate(casebook.ConsentDate).Generate());

        // Act / Assert
        Refusal.Of(CasebookRefusal.FormsNotSigned, () => casebook.Lock());
    }

    [Fact]
    public void A_casebook_with_resolved_events_is_locked() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithOnsetDate(casebook.ConsentDate).Generate());

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
    }

    [Fact]
    public void A_casebook_whose_visits_did_not_all_take_place_is_locked_once_each_is_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised(2);
        string          missed   = casebook.CodeOf(1);
        casebook.RecordAndSign(casebook.CodeOf(0));
        casebook.RecordVisit(missed, Any.VisitEntry().Missing(MissingReason.Refused).Generate());
        casebook.SubmitVisit(missed);
        casebook.SignVisit(missed);

        // Act
        casebook.Lock();

        // Assert
        Assert.True(casebook.IsLocked);
    }

    // ---------------------------------------------------------------- after the lock

    [Fact]
    public void A_locked_casebook_cannot_be_locked_again() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        casebook.Lock();

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.CasebookLocked, () => casebook.Lock());

        // Assert
        Assert.Contains(casebook.ScreeningNumber.Value, refusal.Message);
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
    public void Every_change_on_a_locked_casebook_is_refused(string operation) {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithOnsetDate(casebook.ConsentDate).Generate());
        casebook.Lock();
        string   code   = casebook.CodeOf(0);
        DateOnly anyDay = Scenarios.AnyDate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.CasebookLocked, () => {
            switch (operation) {
                case "Randomise":          casebook.Randomise(Any.Randomisation().Generate()); break;
                case "RecordVisit":        casebook.RecordVisit(code, Any.VisitEntry().Generate()); break;
                case "SubmitVisit":        casebook.SubmitVisit(code); break;
                case "RaiseQuery":         casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), anyDay); break;
                case "AnswerQuery":        casebook.AnswerQuery(code, 1, Scenarios.AnyQueryText(), anyDay); break;
                case "CloseQuery":         casebook.CloseQuery(code, 1, anyDay); break;
                case "SignVisit":          casebook.SignVisit(code); break;
                case "ReportAdverseEvent": casebook.ReportAdverseEvent(Any.AdverseEvent().WithOnsetDate(casebook.ConsentDate).Generate()); break;
                case "ReviseAdverseEvent": casebook.ReviseAdverseEvent(1, Any.AdverseEvent().WithOnsetDate(casebook.ConsentDate).Generate()); break;
                default:                   throw new InvalidOperationException(operation);
            }
        });
    }

    [Fact]
    public void A_locked_casebook_is_still_readable() {
        // Arrange
        SubjectCasebook casebook = ASignedCasebook();
        casebook.Lock();
        string code = casebook.CodeOf(0);

        // Assert
        Assert.True(casebook.WindowOf(code).Earliest <= casebook.WindowOf(code).Latest);
        Assert.Equal(2, casebook.Forms.Count);
        Assert.Empty(casebook.UnresolvedFields());
        Assert.Equal(0, casebook.OpenQueryCount);
    }

}
