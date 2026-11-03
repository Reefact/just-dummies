using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookQueryLifecycleTests {

    private static (SubjectCasebook Casebook, string Code) CasebookWithADraft() {
        SubjectCasebook casebook = Scenarios.Randomised();
        string          code     = casebook.CodeOf(0);

        casebook.RecordComplete(code);

        return (casebook, code);
    }

    private static (SubjectCasebook Casebook, string Code) CasebookWithASubmittedForm() {
        (SubjectCasebook casebook, string code) = CasebookWithADraft();

        casebook.SubmitVisit(code);

        return (casebook, code);
    }

    // ---------------------------------------------------------------- submitting

    [Fact]
    public void A_draft_is_submitted() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithADraft();

        // Act
        casebook.SubmitVisit(code);

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_submitted_form_cannot_be_submitted_again() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SubmitVisit(code));

        // Assert
        Assert.Contains("Submitted", refusal.Message);
    }

    [Fact]
    public void A_signed_form_cannot_be_submitted_again() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        casebook.SignVisit(code);

        // Act / Assert
        Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.SubmitVisit(code));
    }

    [Fact]
    public void A_visit_never_recorded_cannot_be_submitted() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          aCode    = casebook.CodeOf(1);

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitNotRecorded, () => casebook.SubmitVisit(aCode));
    }

    [Fact]
    public void A_visit_the_protocol_does_not_schedule_cannot_be_submitted() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          unknown  = AnyVisitCodes.Code().Except(casebook.Schedule.Visits.Select(v => v.Code)).Generate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.UnknownVisit, () => casebook.SubmitVisit(unknown));
    }

    [Fact]
    public void A_queried_form_with_an_open_query_cannot_be_resubmitted() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate());

        // Act
        Refusal.Of(CasebookRefusal.OpenQueries, () => casebook.SubmitVisit(code));

        // Assert
        Assert.Equal(FormStatus.Queried, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_queried_form_is_resubmitted_once_every_query_is_answered() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());

        // Act
        casebook.SubmitVisit(code);

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form(code)!.Status);
    }

    // ---------------------------------------------------------------- raising queries

    [Fact]
    public void A_draft_has_nothing_to_query_yet() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithADraft();

        // Act
        Refusal.Of(CasebookRefusal.InvalidTransition, () => casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate()));

        // Assert
        Assert.Empty(casebook.Form(code)!.Queries);
        Assert.Equal(FormStatus.Draft, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_query_on_a_submitted_form_opens_it_and_queries_the_form() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly anyDate = Scenarios.AnyDate();
        string   anyText = Scenarios.AnyQueryText();

        // Act
        Query query = casebook.RaiseQuery(code, "Vitals.HeartRateBpm", anyText, anyDate);

        // Assert
        Assert.Equal(1, query.Number);
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Equal(anyText, query.Text);
        Assert.Equal(anyDate, query.RaisedOn);
        Assert.Equal(FormStatus.Queried, casebook.Form(code)!.Status);
        Assert.Equal(1, casebook.OpenQueryCount);
    }

    [Fact]
    public void Queries_are_numbered_from_1_on_each_form() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised(2);
        string          first    = casebook.CodeOf(0);
        string          second   = casebook.CodeOf(1);
        casebook.RecordAndSubmit(first);
        casebook.RecordAndSubmit(second);

        // Act
        int firstOnFirst  = casebook.RaiseQuery(first, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate()).Number;
        int secondOnFirst = casebook.RaiseQuery(first, "Lab", Scenarios.AnyQueryText(), Scenarios.AnyDate()).Number;
        int firstOnSecond = casebook.RaiseQuery(second, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate()).Number;

        // Assert
        Assert.Equal([1, 2, 1], new[] { firstOnFirst, secondOnFirst, firstOnSecond });
        Assert.Equal(3, casebook.OpenQueryCount);
    }

    [Fact]
    public void A_query_on_a_signed_form_takes_the_signature_back() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        casebook.SignVisit(code);

        // Act
        casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate());

        // Assert
        Assert.Equal(FormStatus.Queried, casebook.Form(code)!.Status);
    }

    [Fact]
    public void A_query_about_an_invalid_field_is_refused_and_changes_nothing() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();

        // Act
        Assert.Throws<FormatException>(() => casebook.RaiseQuery(code, "vitals.heartRate", Scenarios.AnyQueryText(), Scenarios.AnyDate()));

        // Assert
        Assert.Equal(FormStatus.Submitted, casebook.Form(code)!.Status);
        Assert.Empty(casebook.Form(code)!.Queries);
    }

    [Fact]
    public void A_query_on_a_visit_never_recorded_is_refused() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          aCode    = casebook.CodeOf(0);

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitNotRecorded, () => casebook.RaiseQuery(aCode, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate()));
    }

    // ---------------------------------------------------------------- answering and closing

    [Fact]
    public void A_site_answers_an_open_query() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), raisedOn);
        string   response = Scenarios.AnyQueryText();

        // Act
        casebook.AnswerQuery(code, query.Number, response, raisedOn.AddDays(2));

        // Assert
        Assert.Equal(QueryStatus.Answered, query.Status);
        Assert.Equal(response, query.Response);
        Assert.Equal(raisedOn.AddDays(2), query.AnsweredOn);
        Assert.Equal(0, casebook.OpenQueryCount);
    }

    [Fact]
    public void A_query_is_answered_once() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseAnsweredQuery(code, raisedOn);
        string   first    = query.Response!;

        // Act
        Refusal.Of(CasebookRefusal.QueryNotOpen, () => casebook.AnswerQuery(code, query.Number, Scenarios.AnyQueryText(), raisedOn.AddDays(1)));

        // Assert
        Assert.Equal(first, query.Response);
    }

    [Fact]
    public void A_query_cannot_be_answered_before_it_was_raised() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), raisedOn);

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => casebook.AnswerQuery(code, query.Number, Scenarios.AnyQueryText(), raisedOn.AddDays(-1)));

        // Assert
        Assert.Equal(QueryStatus.Open, query.Status);
    }

    [Fact]
    public void A_query_may_be_answered_the_day_it_was_raised() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();

        // Act
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());

        // Assert
        Assert.Equal(QueryStatus.Answered, query.Status);
        Assert.Equal(query.RaisedOn, query.AnsweredOn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_response_is_refused_and_leaves_the_query_open(string response) {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), raisedOn);

        // Act
        Assert.Throws<ArgumentException>(() => casebook.AnswerQuery(code, query.Number, response, raisedOn));

        // Assert
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Null(query.Response);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void A_query_the_form_never_had_is_unknown(int number) {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), raisedOn);

        // Act
        CasebookRefusedException answering = Refusal.Of(CasebookRefusal.UnknownQuery, () => casebook.AnswerQuery(code, number, Scenarios.AnyQueryText(), raisedOn));
        CasebookRefusedException closing   = Refusal.Of(CasebookRefusal.UnknownQuery, () => casebook.CloseQuery(code, number, raisedOn));

        // Assert
        Assert.Contains(code, answering.Message);
        Assert.Contains(code, closing.Message);
    }

    [Fact]
    public void An_answered_query_is_closed_by_the_monitor() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseAnsweredQuery(code, raisedOn);

        // Act
        casebook.CloseQuery(code, query.Number, raisedOn.AddDays(3));

        // Assert
        Assert.Equal(QueryStatus.Closed, query.Status);
        Assert.Equal(raisedOn.AddDays(3), query.ClosedOn);
    }

    [Fact]
    public void A_query_may_be_closed_the_day_it_was_answered() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());

        // Act
        casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value);

        // Assert
        Assert.Equal(QueryStatus.Closed, query.Status);
    }

    [Fact]
    public void A_query_cannot_be_closed_before_it_was_answered() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value.AddDays(-1)));

        // Assert
        Assert.Equal(QueryStatus.Answered, query.Status);
    }

    [Fact]
    public void An_open_query_cannot_be_closed() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        DateOnly raisedOn = Scenarios.AnyDate();
        Query    query    = casebook.RaiseQuery(code, "Vitals", Scenarios.AnyQueryText(), raisedOn);

        // Act
        Refusal.Of(CasebookRefusal.QueryNotAnswered, () => casebook.CloseQuery(code, query.Number, raisedOn));

        // Assert
        Assert.Equal(QueryStatus.Open, query.Status);
    }

    [Fact]
    public void A_closed_query_cannot_be_closed_again() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());
        casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value);

        // Act / Assert
        Refusal.Of(CasebookRefusal.QueryNotAnswered, () => casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value));
    }

    [Fact]
    public void A_closed_query_cannot_be_answered_again() {
        // Arrange
        (SubjectCasebook casebook, string code) = CasebookWithASubmittedForm();
        Query query = casebook.RaiseAnsweredQuery(code, Scenarios.AnyDate());
        casebook.CloseQuery(code, query.Number, query.AnsweredOn!.Value);

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.QueryNotOpen, () => casebook.AnswerQuery(code, query.Number, Scenarios.AnyQueryText(), query.AnsweredOn!.Value));

        // Assert
        Assert.Contains("Closed", refusal.Message);
    }

    [Fact]
    public void Answering_or_closing_on_a_visit_never_recorded_is_refused() {
        // Arrange
        SubjectCasebook casebook = Scenarios.Randomised();
        string          aCode    = casebook.CodeOf(2);
        DateOnly        anyDate  = Scenarios.AnyDate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitNotRecorded, () => casebook.AnswerQuery(aCode, 1, Scenarios.AnyQueryText(), anyDate));
        Refusal.Of(CasebookRefusal.VisitNotRecorded, () => casebook.CloseQuery(aCode, 1, anyDate));
    }

}
