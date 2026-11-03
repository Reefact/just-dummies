using JustDummies;

using Xunit;

using static ClinicalTrial.Domain.Tests.Protocol;

namespace ClinicalTrial.Domain.Tests;

public sealed class CasebookWorkflowTests {

    private static SubjectCasebook WithSubmittedD8() {
        SubjectCasebook casebook = RandomisedCasebook();

        casebook.RecordVisit("D8", CompleteEntry(DateOfDay(8), "ALT"));
        casebook.SubmitVisit("D8");

        return casebook;
    }

    private static string AnyQueryText() => Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();

    // ---- submitting

    [Fact]
    public void Submitting_a_draft_form_submits_it() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));

        // Act
        casebook.SubmitVisit("D8");

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_visit_that_was_not_recorded_cannot_be_submitted() {
        AssertRefused(CasebookRefusal.VisitNotRecorded, () => RandomisedCasebook().SubmitVisit("D8"));
    }

    [Fact]
    public void An_unscheduled_visit_cannot_be_submitted() {
        AssertRefused(CasebookRefusal.UnknownVisit, () => RandomisedCasebook().SubmitVisit("D9"));
    }

    [Fact]
    public void A_submitted_form_cannot_be_submitted_again() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();

        // Act + Assert
        AssertRefused(CasebookRefusal.InvalidTransition, () => casebook.SubmitVisit("D8"));
    }

    [Fact]
    public void A_queried_form_with_an_open_query_cannot_be_submitted() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act + Assert
        AssertRefused(CasebookRefusal.OpenQueries, () => casebook.SubmitVisit("D8"));
        Assert.Equal(FormStatus.Queried, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_queried_form_is_submitted_again_once_its_queries_are_answered() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));

        // Act
        casebook.SubmitVisit("D8");

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form("D8")!.Status);
    }

    // ---- queries

    [Fact]
    public void A_draft_form_cannot_be_queried() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));
        string anyText = AnyQueryText();

        // Act + Assert
        AssertRefused(CasebookRefusal.InvalidTransition, () => casebook.RaiseQuery("D8", "Vitals", anyText, DateOfDay(12)));
    }

    [Fact]
    public void Raising_a_query_on_a_submitted_form_queries_it() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        DateOnly        raisedOn = DateOfDay(12);

        // Act
        Query query = casebook.RaiseQuery("D8", "Vitals.HeartRateBpm", "Please confirm the heart rate", raisedOn);

        // Assert
        Assert.Equal(1, query.Number);
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Equal("Vitals.HeartRateBpm", query.FieldName);
        Assert.Equal(raisedOn, query.RaisedOn);
        Assert.Equal(FormStatus.Queried, casebook.Form("D8")!.Status);
        Assert.Equal(1, casebook.OpenQueryCount);
    }

    [Fact]
    public void Queries_are_numbered_from_1_on_each_form() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RecordVisit("D29", Entry(DateOfDay(29)));
        casebook.SubmitVisit("D29");

        // Act
        Query firstOnD8   = casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        Query secondOnD8  = casebook.RaiseQuery("D8", "Lab.ALT", AnyQueryText(), DateOfDay(12));
        Query firstOnD29  = casebook.RaiseQuery("D29", "Vitals", AnyQueryText(), DateOfDay(30));

        // Assert
        Assert.Equal(1, firstOnD8.Number);
        Assert.Equal(2, secondOnD8.Number);
        Assert.Equal(1, firstOnD29.Number);
        Assert.Equal(3, casebook.OpenQueryCount);
    }

    [Fact]
    public void A_signed_form_that_is_queried_loses_its_signature() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.SignVisit("D8");

        // Act
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Assert
        Assert.Equal(FormStatus.Queried, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_query_on_an_invalid_field_path_is_refused_and_leaves_the_form_untouched() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        string          anyText  = AnyQueryText();

        // Act + Assert
        Assert.Throws<FormatException>(() => casebook.RaiseQuery("D8", "vitals", anyText, DateOfDay(12)));
        Assert.Equal(FormStatus.Submitted, casebook.Form("D8")!.Status);
        Assert.Empty(casebook.Form("D8")!.Queries);
    }

    [Fact]
    public void Answering_a_query_records_the_response_and_the_date() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act
        casebook.AnswerQuery("D8", 1, "Value confirmed against the source", DateOfDay(14));

        // Assert
        Query query = casebook.Form("D8")!.Queries[0];
        Assert.Equal(QueryStatus.Answered, query.Status);
        Assert.Equal("Value confirmed against the source", query.Response);
        Assert.Equal(DateOfDay(14), query.AnsweredOn);
        Assert.Equal(0, casebook.OpenQueryCount);
    }

    [Fact]
    public void A_query_may_be_answered_the_day_it_was_raised() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(12));

        // Assert
        Assert.Equal(QueryStatus.Answered, casebook.Form("D8")!.Queries[0].Status);
    }

    [Fact]
    public void A_query_cannot_be_answered_before_it_was_raised() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        string anyResponse = AnyQueryText();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => casebook.AnswerQuery("D8", 1, anyResponse, DateOfDay(11)));
        Assert.Equal(QueryStatus.Open, casebook.Form("D8")!.Queries[0].Status);
    }

    [Fact]
    public void A_response_is_not_blank() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act + Assert
        Assert.Throws<ArgumentException>(() => casebook.AnswerQuery("D8", 1, "  ", DateOfDay(13)));
    }

    [Fact]
    public void A_query_is_answered_once() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));
        string anyResponse = AnyQueryText();

        // Act + Assert
        AssertRefused(CasebookRefusal.QueryNotOpen, () => casebook.AnswerQuery("D8", 1, anyResponse, DateOfDay(14)));
    }

    [Fact]
    public void An_unknown_query_cannot_be_answered_or_closed() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        string anyResponse = AnyQueryText();

        // Act + Assert
        AssertRefused(CasebookRefusal.UnknownQuery, () => casebook.AnswerQuery("D8", 2, anyResponse, DateOfDay(13)));
        AssertRefused(CasebookRefusal.UnknownQuery, () => casebook.AnswerQuery("D8", 0, anyResponse, DateOfDay(13)));
        AssertRefused(CasebookRefusal.UnknownQuery, () => casebook.CloseQuery("D8", 2, DateOfDay(13)));
    }

    [Fact]
    public void A_query_on_a_visit_with_no_form_is_refused() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        string          anyText  = AnyQueryText();

        // Act + Assert
        AssertRefused(CasebookRefusal.VisitNotRecorded, () => casebook.RaiseQuery("D8", "Vitals", anyText, DateOfDay(12)));
        AssertRefused(CasebookRefusal.VisitNotRecorded, () => casebook.AnswerQuery("D8", 1, anyText, DateOfDay(12)));
        AssertRefused(CasebookRefusal.VisitNotRecorded, () => casebook.CloseQuery("D8", 1, DateOfDay(12)));
    }

    [Fact]
    public void An_answered_query_is_closed() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));

        // Act
        casebook.CloseQuery("D8", 1, DateOfDay(15));

        // Assert
        Query query = casebook.Form("D8")!.Queries[0];
        Assert.Equal(QueryStatus.Closed, query.Status);
        Assert.Equal(DateOfDay(15), query.ClosedOn);
    }

    [Fact]
    public void A_query_may_be_closed_the_day_it_was_answered() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));

        // Act
        casebook.CloseQuery("D8", 1, DateOfDay(13));

        // Assert
        Assert.Equal(QueryStatus.Closed, casebook.Form("D8")!.Queries[0].Status);
    }

    [Fact]
    public void A_query_cannot_be_closed_before_it_was_answered() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(14));

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => casebook.CloseQuery("D8", 1, DateOfDay(13)));
        Assert.Equal(QueryStatus.Answered, casebook.Form("D8")!.Queries[0].Status);
    }

    [Fact]
    public void An_open_query_cannot_be_closed() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act + Assert
        AssertRefused(CasebookRefusal.QueryNotAnswered, () => casebook.CloseQuery("D8", 1, DateOfDay(13)));
    }

    [Fact]
    public void A_closed_query_cannot_be_closed_again() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));
        casebook.CloseQuery("D8", 1, DateOfDay(14));

        // Act + Assert
        AssertRefused(CasebookRefusal.QueryNotAnswered, () => casebook.CloseQuery("D8", 1, DateOfDay(15)));
    }

    // ---- signing

    [Fact]
    public void Signing_a_complete_submitted_form_signs_it() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();

        // Act
        casebook.SignVisit("D8");

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_draft_form_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", CompleteEntry(DateOfDay(8), "ALT"));

        // Act + Assert
        AssertRefused(CasebookRefusal.InvalidTransition, () => casebook.SignVisit("D8"));
    }

    [Fact]
    public void A_signed_form_cannot_be_signed_again() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.SignVisit("D8");

        // Act + Assert
        AssertRefused(CasebookRefusal.InvalidTransition, () => casebook.SignVisit("D8"));
    }

    [Fact]
    public void A_queried_form_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));

        // Act + Assert
        AssertRefused(CasebookRefusal.InvalidTransition, () => casebook.SignVisit("D8"));
    }

    [Fact]
    public void A_form_whose_queries_are_answered_but_not_closed_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));
        casebook.SubmitVisit("D8");

        // Act + Assert
        AssertRefused(CasebookRefusal.UnclosedQueries, () => casebook.SignVisit("D8"));
    }

    [Fact]
    public void A_form_can_be_signed_again_once_every_query_is_closed_and_it_was_resubmitted() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.SignVisit("D8");
        casebook.RaiseQuery("D8", "Vitals", AnyQueryText(), DateOfDay(12));
        casebook.AnswerQuery("D8", 1, AnyQueryText(), DateOfDay(13));
        casebook.CloseQuery("D8", 1, DateOfDay(14));
        casebook.SubmitVisit("D8");

        // Act
        casebook.SignVisit("D8");

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_form_with_no_vitals_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D85", new VisitEntry(Observed<DateOnly>.Of(DateOfDay(85)), null, Array.Empty<LabResult>()));
        casebook.SubmitVisit("D85");

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.SignVisit("D85"));

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("Vitals", refusal.Message);
    }

    [Fact]
    public void A_form_with_a_blank_vital_sign_names_it_when_refused() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VitalSigns      vitals   = new VitalSigns(null, null, Observed<int>.Missing(MissingReason.NotDone), Observed<decimal>.Missing(MissingReason.NotDone), Observed<decimal>.Missing(MissingReason.NotDone), null);
        casebook.RecordVisit("D85", Any.VisitEntry().WithVisitDate(Observed<DateOnly>.Of(DateOfDay(85))).WithVitals(vitals).WithLabs(Array.Empty<LabResult>()).Generate());
        casebook.SubmitVisit("D85");

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.SignVisit("D85"));

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("Vitals.SystolicMmHg, Vitals.DiastolicMmHg", refusal.Message);
    }

    [Fact]
    public void A_form_missing_a_required_analyte_cannot_be_signed() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", CompleteEntry(DateOfDay(8)));
        casebook.SubmitVisit("D8");

        // Act
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(() => casebook.SignVisit("D8"));

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("Lab.ALT", refusal.Message);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_is_signed_with_nothing_to_resolve() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", DidNotTakePlace(MissingReason.Refused));
        casebook.SubmitVisit("D8");

        // Act
        casebook.SignVisit("D8");

        // Assert
        Assert.Equal(FormStatus.Signed, casebook.Form("D8")!.Status);
    }

    [Fact]
    public void A_signed_form_can_no_longer_be_edited() {
        // Arrange
        SubjectCasebook casebook = WithSubmittedD8();
        casebook.SignVisit("D8");
        VisitEntry anyEntry = Entry(DateOfDay(8));

        // Act + Assert
        AssertRefused(CasebookRefusal.FormNotEditable, () => casebook.RecordVisit("D8", anyEntry));
    }

}
