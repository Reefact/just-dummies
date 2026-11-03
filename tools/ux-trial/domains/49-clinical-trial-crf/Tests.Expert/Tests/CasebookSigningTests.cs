using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookSigningTests {

    private static VisitEntry EntryOn(int studyDay, VitalSigns? vitals, params LabResult[] labs) {
        return Any.VisitEntry().OnDate(Protocol.DayOf(studyDay)).WithVitals(vitals).WithLabs(labs).Generate();
    }

    private static LabResult ResolvedLab(string code) {
        return Any.LabResult().WithAnalyteCode(code).Resolved().Generate();
    }

    // ---------------------------------------------------------------- the lifecycle that leads to a signature

    [Fact]
    public void A_submitted_form_whose_queries_are_closed_and_fields_resolved_is_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSubmit(code);

        // Act
        casebook.SignVisit(code);

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_draft_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordComplete(code);

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SignVisit(code));

        // Assert
        Assert.Contains("Draft", refusal.Message);
    }

    [Fact]
    public void A_signed_form_cannot_be_signed_again() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSign(code);

        // Act / Assert
        Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SignVisit(code));
    }

    [Fact]
    public void A_queried_form_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSubmit(code);
        casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate());

        // Act / Assert
        Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SignVisit(code));
    }

    [Fact]
    public void A_form_signed_then_queried_cannot_be_signed_until_it_is_submitted_again() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSign(code);
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());
        casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value);

        // Act
        Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SignVisit(code));
        casebook.SubmitVisit(code);
        casebook.SignVisit(code);

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_visit_never_recorded_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          aCode    = casebook.CodeOf(1);

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitNotRecorded, () => casebook.SignVisit(aCode));
    }

    // ---------------------------------------------------------------- queries and signature

    [Fact]
    public void A_form_with_an_answered_query_not_yet_closed_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSubmit(code);
        casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());
        casebook.SubmitVisit(code);

        // Act
        Refusal.Of(CasebookRefusal.UnclosedQueries, () => casebook.SignVisit(code));

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_form_is_signed_once_its_answered_query_is_closed() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);
        casebook.RecordAndSubmit(code);
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());
        casebook.SubmitVisit(code);
        casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value);

        // Act
        casebook.SignVisit(code);

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form(code)!.Status);
    }

    [Fact]
    public void Unclosed_queries_are_refused_before_unresolved_fields() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, null));
        casebook.SubmitVisit("V2");
        casebook.RaiseAnsweredQuery("V2", Scenarios.AnyDate());
        casebook.SubmitVisit("V2");

        // Act / Assert
        Refusal.Of(CasebookRefusal.UnclosedQueries, () => casebook.SignVisit("V2"));
    }

    // ---------------------------------------------------------------- what must be resolved

    [Fact]
    public void A_visit_that_took_place_without_vital_signs_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, null));
        casebook.SubmitVisit("V2");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V2"));

        // Assert
        Assert.Contains("Vitals", refusal.Message);
        Assert.Equal(FormStatus.Submitted, casebook.Form("V2")!.Status);
    }

    [Fact]
    public void The_signs_not_entered_are_named_in_the_refusal() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(null).WithWeight(null).Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, vitals));
        casebook.SubmitVisit("V2");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V2"));

        // Assert
        Assert.Contains("Vitals.HeartRateBpm, Vitals.WeightKg", refusal.Message);
    }

    [Fact]
    public void A_required_analyte_not_reported_cannot_be_signed() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V4", EntryOn(28, vitals, ResolvedLab("ALT")));
        casebook.SubmitVisit("V4");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V4"));

        // Assert
        Assert.Contains("Lab.HGB", refusal.Message);
        Assert.DoesNotContain("Lab.ALT", refusal.Message);
    }

    [Fact]
    public void A_required_analyte_reported_without_a_result_cannot_be_signed() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V4", EntryOn(28, vitals, ResolvedLab("ALT"), Any.LabResult().WithAnalyteCode("HGB").Blank().Generate()));
        casebook.SubmitVisit("V4");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V4"));

        // Assert
        Assert.Contains("Lab.HGB.Result", refusal.Message);
    }

    [Fact]
    public void A_lab_missing_for_a_reason_counts_as_resolved() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V4", EntryOn(28, vitals, ResolvedLab("ALT"), Any.LabResult().WithAnalyteCode("HGB").Missing().Generate()));
        casebook.SubmitVisit("V4");

        // Act
        casebook.SignVisit("V4");

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form("V4")!.Status);
    }

    [Fact]
    public void A_lab_the_protocol_does_not_require_may_still_block_the_signature_when_its_result_is_blank() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, vitals, Any.LabResult().WithAnalyteCode("CREA").Blank().Generate()));
        casebook.SubmitVisit("V2");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V2"));

        // Assert
        Assert.Contains("Lab.CREA.Result", refusal.Message);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_has_nothing_to_resolve_and_is_signed_as_it_is() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V4", Any.VisitEntry().NotTakenPlace().Generate());
        casebook.SubmitVisit("V4");

        // Act
        casebook.SignVisit("V4");

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form("V4")!.Status);
        Assert.Empty(casebook.Form("V4")!.UnresolvedFields());
    }

    [Fact]
    public void A_measured_sign_without_its_time_cannot_be_signed() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(Observed<int>.Of(60)).WithMeasuredAt(null).Generate();
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, vitals));
        casebook.SubmitVisit("V2");

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnresolvedFields, () => casebook.SignVisit("V2"));

        // Assert
        Assert.Contains("Vitals.MeasuredAt", refusal.Message);
    }

    // ---------------------------------------------------------------- the paths of what is left to resolve

    [Fact]
    public void Unresolved_fields_of_a_form_are_the_vitals_then_the_missing_analytes_then_the_blank_results() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        VisitForm form = casebook.RecordVisit("V4", EntryOn(28, null, Any.LabResult().WithAnalyteCode("ALT").Blank().Generate()));

        // Act
        IReadOnlyList<string> unresolved = form.UnresolvedFields();

        // Assert
        Assert.Equal(["Vitals", "Lab.HGB", "Lab.ALT.Result"], unresolved);
    }

    [Fact]
    public void The_casebook_prefixes_each_field_with_its_visit_and_each_event_number() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", EntryOn(8, null));
        casebook.RecordVisit("V1", EntryOn(1, Any.VitalSigns().Resolved().Generate()));
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithRelationship(null).WithOnsetDate(casebook.ConsentDate).Generate());
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithOnsetDate(casebook.ConsentDate).Generate());
        casebook.ReportAdverseEvent(Any.AdverseEvent().Resolved().WithRelationship(null).WithOnsetDate(casebook.ConsentDate).Generate());

        // Act
        IReadOnlyList<string> unresolved = casebook.UnresolvedFields();

        // Assert
        Assert.Equal(["V2.Vitals", "AE1.Relationship", "AE3.Relationship"], unresolved);
    }

}
