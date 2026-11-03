using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookVisitRecordingTests {

    private static VisitEntry Took(int studyDay) {
        return Any.VisitEntry().OnDate(Protocol.DayOf(studyDay)).Generate();
    }

    private static VisitEntry Missed(MissingReason reason) {
        return Any.VisitEntry().Missing(reason).Generate();
    }

    // ---------------------------------------------------------------- what is recorded

    [Fact]
    public void A_visit_cannot_be_recorded_before_the_subject_is_randomised() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        string          aCode    = casebook.CodeOf(0);
        VisitEntry      anyEntry = Any.VisitEntry().Generate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.NotRandomised, () => casebook.RecordVisit(aCode, anyEntry));
    }

    [Fact]
    public void Randomisation_is_looked_for_before_the_visit_code() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        SubjectCasebook notYet   = Any.SubjectCasebook().WithSchedule(Protocol.Schedule()).Generate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.NotRandomised, () => notYet.RecordVisit("V9", Took(8)));
        Refusal.Of(CasebookRefusal.UnknownVisit, () => casebook.RecordVisit("V9", Took(8)));
    }

    [Fact]
    public void A_visit_the_protocol_does_not_schedule_is_refused() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnknownVisit, () => casebook.RecordVisit("V9", Took(8)));

        // Assert
        Assert.Contains("V9", refusal.Message);
        Assert.Empty(casebook.Forms);
    }

    [Fact]
    public void Missing_arguments_are_refused() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => casebook.RecordVisit(null!, Took(8)));
        Assert.Throws<ArgumentNullException>(() => casebook.RecordVisit("V2", null!));
    }

    [Fact]
    public void A_recorded_visit_is_a_draft_dated_in_study_days() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        VisitEntry      entry    = Took(8);

        // Act
        VisitForm form = casebook.RecordVisit("V2", entry);

        // Assert
        Assert.Equal(FormStatus.Draft, form.Status);
        Assert.Equal(8, form.StudyDay);
        Assert.Equal("V2", form.Definition.Code);
        Assert.Same(entry, form.Entry);
        Assert.Same(form, casebook.Form("V2"));
        Assert.Empty(form.Queries);
    }

    [Fact]
    public void The_forms_are_listed_in_the_order_of_the_protocol_not_in_the_order_of_entry() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        casebook.RecordVisit("V3", Took(15));
        casebook.RecordVisit("V2", Took(8));

        // Assert
        Assert.Equal(["V2", "V3"], casebook.Forms.Select(f => f.Definition.Code));
    }

    [Fact]
    public void A_visit_not_recorded_has_no_form() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Assert
        Assert.Null(casebook.Form("V2"));
        Assert.Throws<ArgumentNullException>(() => casebook.Form(null!));
    }

    [Fact]
    public void A_visit_that_did_not_take_place_has_no_study_day() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("V2", Missed(MissingReason.NotDone));

        // Assert
        Assert.Null(form.StudyDay);
        Assert.False(form.IsOutOfWindow);
    }

    // ---------------------------------------------------------------- the tolerance window

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(8, false)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    [InlineData(40, true)]
    public void A_visit_outside_its_window_is_accepted_and_reported(int studyDay, bool outOfWindow) {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("V2", Took(studyDay));

        // Assert
        Assert.Equal(outOfWindow, form.IsOutOfWindow);
        Assert.Equal(outOfWindow ? ["V2"] : [], casebook.OutOfWindowVisits());
    }

    [Fact]
    public void Only_the_visits_that_took_place_outside_their_window_are_reported_in_protocol_order() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        casebook.RecordVisit("V3", Took(20));                          // too late
        casebook.RecordVisit("V2", Took(8));                           // in the window
        casebook.RecordVisit("V4", Missed(MissingReason.NotDone));     // did not take place
        casebook.RecordVisit("V1", Took(2));                           // too late (window is day 1)

        // Assert
        Assert.Equal(["V1", "V3"], casebook.OutOfWindowVisits());
    }

    [Fact]
    public void The_randomisation_day_itself_is_the_first_day_a_visit_can_be_recorded() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("V2", Took(1));

        // Assert
        Assert.Equal(1, form.StudyDay);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void A_visit_cannot_precede_randomisation(int studyDay) {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        Refusal.Of(CasebookRefusal.VisitBeforeRandomisation, () => casebook.RecordVisit("V2", Took(studyDay)));

        // Assert
        Assert.Empty(casebook.Forms);
    }

    // ---------------------------------------------------------------- entering a visit again

    [Fact]
    public void Entering_a_draft_again_replaces_the_entry_and_keeps_the_form() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        VisitForm       form     = casebook.RecordVisit("V2", Took(8));
        VisitEntry      revised  = Took(9);

        // Act
        VisitForm again = casebook.RecordVisit("V2", revised);

        // Assert
        Assert.Same(form, again);
        Assert.Same(revised, again.Entry);
        Assert.Equal(9, again.StudyDay);
        Assert.Equal(FormStatus.Draft, again.Status);
    }

    [Fact]
    public void Entering_a_queried_form_again_keeps_its_status_and_its_queries() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(8));
        casebook.SubmitVisit("V2");
        casebook.RaiseQuery("V2", "Vitals", Scenarios.AnyQueryText(), Protocol.DayOf(12));

        // Act
        VisitForm again = casebook.RecordVisit("V2", Took(9));

        // Assert
        Assert.Equal(FormStatus.Queried, again.Status);
        Assert.Single(again.Queries);
        Assert.Equal(9, again.StudyDay);
    }

    [Theory]
    [InlineData(FormStatus.Submitted)]
    [InlineData(FormStatus.Signed)]
    public void A_submitted_or_signed_form_cannot_be_edited(FormStatus status) {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Any.VisitEntry().OnDate(Protocol.DayOf(8)).CompleteFor(casebook.Schedule.Find("V2")!).Generate());
        casebook.SubmitVisit("V2");
        if (status == FormStatus.Signed) { casebook.SignVisit("V2"); }

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.FormNotEditable, () => casebook.RecordVisit("V2", Took(9)));

        // Assert
        Assert.Contains(status.ToString(), refusal.Message);
        Assert.Equal(8, casebook.Form("V2")!.StudyDay);
    }

    [Fact]
    public void A_form_that_cannot_be_edited_is_refused_as_such_before_its_date_is_looked_at() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(8));
        casebook.SubmitVisit("V2");

        // Act / Assert
        Refusal.Of(CasebookRefusal.FormNotEditable, () => casebook.RecordVisit("V2", Took(-5)));
    }

    // ---------------------------------------------------------------- loss to follow-up

    [Fact]
    public void A_subject_lost_to_follow_up_is_flagged_once_a_visit_is_recorded_as_such() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(8));

        // Act
        bool before = casebook.IsLostToFollowUp;
        casebook.RecordVisit("V3", Missed(MissingReason.LostToFollowUp));

        // Assert
        Assert.False(before);
        Assert.True(casebook.IsLostToFollowUp);
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_the_next_ones_can_be_recorded_as_lost_too() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Missed(MissingReason.LostToFollowUp));

        // Act
        VisitForm form = casebook.RecordVisit("V3", Missed(MissingReason.LostToFollowUp));

        // Assert
        Assert.Equal(FormStatus.Draft, form.Status);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.Refused)]
    public void After_a_visit_lost_to_follow_up_a_next_one_cannot_be_missed_for_another_reason(MissingReason reason) {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Missed(MissingReason.LostToFollowUp));

        // Act / Assert
        Refusal.Of(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("V3", Missed(reason)));
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_cannot_take_place() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Missed(MissingReason.LostToFollowUp));

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("V4", Took(28)));

        // Assert
        Assert.Contains("V2", refusal.Message);
        Assert.Null(casebook.Form("V4"));
    }

    [Fact]
    public void A_visit_cannot_be_lost_to_follow_up_when_a_later_one_took_place() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V3", Took(15));

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("V2", Missed(MissingReason.LostToFollowUp)));

        // Assert
        Assert.Contains("V3", refusal.Message);
    }

    [Fact]
    public void A_visit_can_be_lost_to_follow_up_when_the_later_ones_did_not_take_place_either() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V3", Missed(MissingReason.NotDone));

        // Act
        casebook.RecordVisit("V2", Missed(MissingReason.LostToFollowUp));

        // Assert
        Assert.True(casebook.IsLostToFollowUp);
    }

    [Fact]
    public void Loss_to_follow_up_at_a_visit_does_not_stop_the_earlier_ones_from_being_recorded() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V3", Missed(MissingReason.LostToFollowUp));

        // Act
        casebook.RecordVisit("V2", Took(8));

        // Assert
        Assert.Equal(["V2", "V3"], casebook.Forms.Select(f => f.Definition.Code));
    }

    // ---------------------------------------------------------------- the order of the dates

    [Fact]
    public void Visit_dates_follow_the_order_of_the_protocol() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V3", Took(15));

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("V2", Took(16)));

        // Assert
        Assert.Contains("V3", refusal.Message);
        Assert.Null(casebook.Form("V2"));
    }

    [Fact]
    public void Two_visits_cannot_share_a_date() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(9));

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("V3", Took(9)));
    }

    [Fact]
    public void A_visit_the_day_after_the_previous_one_is_in_order_even_far_outside_its_window() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(9));

        // Act
        VisitForm form = casebook.RecordVisit("V3", Took(10));

        // Assert
        Assert.True(form.IsOutOfWindow);
    }

    [Fact]
    public void A_later_visit_cannot_be_dated_before_an_earlier_one_already_recorded() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(8));

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("V4", Took(7)));
    }

    [Fact]
    public void A_visit_that_did_not_take_place_has_no_date_to_put_out_of_order() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Missed(MissingReason.NotDone));

        // Act
        casebook.RecordVisit("V3", Took(15));
        casebook.RecordVisit("V1", Took(1));

        // Assert
        Assert.Equal(3, casebook.Forms.Count);
    }

    [Fact]
    public void Re_entering_a_visit_is_checked_against_the_other_recorded_visits() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();
        casebook.RecordVisit("V2", Took(8));
        casebook.RecordVisit("V3", Took(15));

        // Act / Assert
        Refusal.Of(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("V2", Took(16)));
        Assert.Equal(8, casebook.Form("V2")!.StudyDay);
    }

}
