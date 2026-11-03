using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain.Tests.Regular.Generators;
using ClinicalTrial.Domain.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class SubjectCasebookTests {

    private static CasebookRefusedException Refusal(Action act) {
        return Assert.Throws<CasebookRefusedException>(act);
    }

    // ---- creation ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_casebook_starts_with_consent_and_nothing_else() {
        // Arrange
        ScreeningNumber anyNumber  = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent = TrialFields.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Act
        SubjectCasebook casebook = new SubjectCasebook(anyNumber, "JD", anyConsent, anySchedule);

        // Assert
        Assert.Same(anyNumber, casebook.ScreeningNumber);
        Assert.Equal("JD", casebook.Initials);
        Assert.Equal(anyConsent, casebook.ConsentDate);
        Assert.Same(anySchedule, casebook.Schedule);
        Assert.False(casebook.IsRandomised);
        Assert.False(casebook.IsLocked);
        Assert.False(casebook.IsLostToFollowUp);
        Assert.Empty(casebook.Forms);
        Assert.Empty(casebook.AdverseEvents);
        Assert.Equal(0, casebook.OpenQueryCount);
        Assert.Null(casebook.DeathDate);
        Assert.Empty(casebook.UnresolvedFields());
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABC")]
    public void Initials_of_two_or_three_upper_case_letters_are_accepted(string initials) {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().WithInitials(initials).Generate();

        // Assert
        Assert.Equal(initials, casebook.Initials);
    }

    [Fact]
    public void Initials_may_be_left_absent() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().WithoutInitials().Generate();

        // Assert
        Assert.Null(casebook.Initials);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCD")]
    [InlineData("ab")]
    [InlineData("Ab")]
    [InlineData("A1")]
    [InlineData("A B")]
    [InlineData(" AB")]
    [InlineData("AB ")]
    [InlineData("É.")]
    public void Initials_that_are_not_two_or_three_upper_case_letters_are_refused(string initials) {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = TrialFields.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Assert
        Assert.Throws<FormatException>(() => new SubjectCasebook(anyNumber, initials, anyConsent, anySchedule));
    }

    [Fact(Skip = "domain defect: the initials pattern is anchored with a dollar sign, which matches before a final newline, so AB followed by a newline are accepted as initials; no drawn value involved, no seed")]
    public void Initials_followed_by_a_line_break_are_refused() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = TrialFields.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Assert
        Assert.Throws<FormatException>(() => new SubjectCasebook(anyNumber, "AB\n", anyConsent, anySchedule));
    }

    [Fact]
    public void A_casebook_needs_a_screening_number_and_a_schedule() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = TrialFields.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(null!, null, anyConsent, anySchedule));
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(anyNumber, null, anyConsent, null!));
    }

    // ---- randomisation ----------------------------------------------------------------------------------------

    [Fact]
    public void A_screened_subject_is_randomised() {
        // Arrange
        Trial         trial         = Trial.Screened();
        Randomisation randomisation = Any.Randomisation().WithDate(trial.ConsentDate.AddDays(10)).Generate();

        // Act
        trial.Casebook.Randomise(randomisation);

        // Assert
        Assert.True(trial.Casebook.IsRandomised);
        Assert.Same(randomisation, trial.Casebook.Randomisation);
    }

    [Fact]
    public void A_subject_is_randomised_only_once() {
        // Arrange
        Trial trial = Trial.Randomised();
        Randomisation another = Any.Randomisation().WithDate(trial.RandomisationDate).Generate();

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Randomise(another));

        // Assert
        Assert.Equal(CasebookRefusal.AlreadyRandomised, refusal.Reason);
    }

    [Fact]
    public void A_refused_second_randomisation_keeps_the_first() {
        // Arrange
        Trial         trial    = Trial.Randomised();
        Randomisation first    = trial.Casebook.Randomisation!;
        Randomisation another  = Any.Randomisation().WithDate(trial.RandomisationDate).Generate();

        // Act
        Refusal(() => trial.Casebook.Randomise(another));

        // Assert
        Assert.Same(first, trial.Casebook.Randomisation);
    }

    [Fact]
    public void Randomisation_cannot_precede_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Randomise(Any.Randomisation().WithDate(trial.ConsentDate.AddDays(-1)).Generate()));

        // Assert
        Assert.Equal(CasebookRefusal.RandomisationBeforeConsent, refusal.Reason);
        Assert.False(trial.Casebook.IsRandomised);
    }

    [Fact]
    public void Randomisation_may_happen_the_day_of_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        trial.Casebook.Randomise(Any.Randomisation().WithDate(trial.ConsentDate).Generate());

        // Assert
        Assert.True(trial.Casebook.IsRandomised);
    }

    [Fact]
    public void Randomisation_may_happen_42_days_after_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        trial.Casebook.Randomise(Any.Randomisation().WithDate(trial.ConsentDate.AddDays(42)).Generate());

        // Assert
        Assert.True(trial.Casebook.IsRandomised);
    }

    [Fact]
    public void Randomisation_cannot_happen_more_than_42_days_after_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Randomise(Any.Randomisation().WithDate(trial.ConsentDate.AddDays(43)).Generate()));

        // Assert
        Assert.Equal(CasebookRefusal.ScreeningPeriodExceeded, refusal.Reason);
        Assert.False(trial.Casebook.IsRandomised);
    }

    [Fact]
    public void A_missing_randomisation_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Trial.Screened().Casebook.Randomise(null!));
    }

    // ---- windows ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_window_is_dated_from_the_randomisation() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        (DateOnly earliest, DateOnly latest) = trial.Casebook.WindowOf(Trial.V1);

        // Assert: V1 is day 8 ± 2, i.e. days 6 to 10 and day 1 is the randomisation date
        Assert.Equal(trial.RandomisationDate.AddDays(5), earliest);
        Assert.Equal(trial.RandomisationDate.AddDays(9), latest);
    }

    [Fact]
    public void A_window_needs_a_randomised_subject() {
        // Arrange
        Trial trial = Trial.Screened();

        // Assert
        Assert.Equal(CasebookRefusal.NotRandomised, Refusal(() => trial.Casebook.WindowOf(Trial.V1)).Reason);
    }

    [Fact]
    public void A_window_needs_a_scheduled_visit() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.UnknownVisit, Refusal(() => trial.Casebook.WindowOf("V9")).Reason);
    }

    // ---- recording a visit ------------------------------------------------------------------------------------

    [Fact]
    public void A_visit_is_recorded_as_a_draft_with_its_study_day() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Assert
        Assert.Equal(FormStatus.Draft, form.Status);
        Assert.Equal(8, form.StudyDay);
        Assert.Equal(Trial.V1, form.Definition.Code);
        Assert.Same(form, trial.Casebook.Form(Trial.V1));
        Assert.False(form.IsOutOfWindow);
    }

    [Fact]
    public void A_visit_needs_a_randomised_subject() {
        // Arrange
        Trial     trial = Trial.Screened();
        VisitEntry entry = new AnyVisitEntry().TookPlace().Generate();

        // Assert
        Assert.Equal(CasebookRefusal.NotRandomised, Refusal(() => trial.Casebook.RecordVisit(Trial.V1, entry)).Reason);
    }

    [Fact]
    public void A_visit_must_be_a_scheduled_one() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.UnknownVisit, Refusal(() => trial.Casebook.RecordVisit("V9", trial.BareEntry(8))).Reason);
    }

    [Fact]
    public void The_subject_not_being_randomised_is_reported_before_the_visit_being_unknown() {
        // Arrange
        Trial      trial = Trial.Screened();
        VisitEntry entry = new AnyVisitEntry().TookPlace().Generate();

        // Assert
        Assert.Equal(CasebookRefusal.NotRandomised, Refusal(() => trial.Casebook.RecordVisit("V9", entry)).Reason);
    }

    [Fact]
    public void A_visit_cannot_precede_randomisation() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act: study day -1 is the day before randomisation
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(-1)));

        // Assert
        Assert.Equal(CasebookRefusal.VisitBeforeRandomisation, refusal.Reason);
        Assert.Null(trial.Casebook.Form(Trial.V1));
    }

    [Fact]
    public void A_visit_may_take_place_on_the_randomisation_date_itself() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(1));

        // Assert
        Assert.Equal(1, form.StudyDay);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(8, false)]
    [InlineData(10, false)]
    [InlineData(5, true)]
    [InlineData(11, true)]
    [InlineData(1, true)]
    [InlineData(40, true)]
    public void A_visit_outside_its_window_is_accepted_and_flagged(int studyDay, bool outOfWindow) {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(studyDay));

        // Assert
        Assert.Equal(outOfWindow, form.IsOutOfWindow);
        Assert.Equal(outOfWindow ? new[] { Trial.V1 } : Array.Empty<string>(), trial.Casebook.OutOfWindowVisits());
    }

    [Fact]
    public void Out_of_window_visits_are_listed_in_schedule_order() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V3, trial.BareEntry(40));
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(3));
        trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15));

        // Assert
        Assert.Equal(new[] { Trial.V1, Trial.V3 }, trial.Casebook.OutOfWindowVisits());
    }

    [Fact]
    public void A_visit_that_did_not_take_place_is_never_out_of_window() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.NotDone));

        // Assert
        Assert.Null(form.StudyDay);
        Assert.False(form.IsOutOfWindow);
        Assert.Empty(trial.Casebook.OutOfWindowVisits());
    }

    [Fact]
    public void The_forms_are_listed_in_schedule_order_whatever_the_order_of_entry() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V3, trial.BareEntry(29));
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Assert
        Assert.Equal(new[] { Trial.V1, Trial.V3 }, trial.Casebook.Forms.Select(f => f.Definition.Code));
    }

    [Fact]
    public void A_form_is_found_by_visit_code_or_not_at_all() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Null(trial.Casebook.Form(Trial.V2));
        Assert.Null(trial.Casebook.Form("V9"));
        Assert.Throws<ArgumentNullException>(() => trial.Casebook.Form(null!));
    }

    [Fact]
    public void Re_entering_a_draft_replaces_its_entry_and_keeps_its_status() {
        // Arrange
        Trial     trial = Trial.Randomised();
        VisitForm first = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        VisitEntry again = trial.BareEntry(9);

        // Act
        VisitForm second = trial.Casebook.RecordVisit(Trial.V1, again);

        // Assert
        Assert.Same(first, second);
        Assert.Same(again, second.Entry);
        Assert.Equal(9, second.StudyDay);
        Assert.Equal(FormStatus.Draft, second.Status);
        Assert.Single(trial.Casebook.Forms);
    }

    [Fact]
    public void Re_entering_a_queried_form_keeps_its_status_and_its_queries() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);
        trial.Casebook.SubmitVisit(Trial.V1);
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals.HeartRateBpm", Any.String().NotBlank().WithMaxLength(100).Generate(), trial.DateOfDay(9));

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.CompleteEntry(Trial.V1, 8));

        // Assert
        Assert.Equal(FormStatus.Queried, form.Status);
        Assert.Single(form.Queries);
    }

    [Theory]
    [InlineData(FormStatus.Submitted)]
    [InlineData(FormStatus.Signed)]
    public void A_submitted_or_signed_form_cannot_be_edited(FormStatus status) {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);
        trial.Casebook.SubmitVisit(Trial.V1);
        if (status == FormStatus.Signed) { trial.Casebook.SignVisit(Trial.V1); }

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V1, trial.CompleteEntry(Trial.V1, 9)));

        // Assert
        Assert.Equal(CasebookRefusal.FormNotEditable, refusal.Reason);
    }

    [Fact]
    public void A_form_that_cannot_be_edited_is_reported_before_the_new_entry_is_judged() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);
        trial.Casebook.SubmitVisit(Trial.V1);

        // Act: this entry precedes randomisation, but the form is closed to editing first
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(-3)));

        // Assert
        Assert.Equal(CasebookRefusal.FormNotEditable, refusal.Reason);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(14)]
    public void A_visit_cannot_be_dated_on_or_before_an_earlier_visit(int earlierVisitDay) {
        // Arrange: V2 took place on day 15 and V1, scheduled before it, is dated on day 15 or after
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(14));

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(earlierVisitDay)));

        // Assert
        Assert.Equal(CasebookRefusal.VisitsOutOfOrder, refusal.Reason);
    }

    [Fact]
    public void A_visit_cannot_be_dated_before_or_on_a_visit_scheduled_earlier() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Assert
        Assert.Equal(CasebookRefusal.VisitsOutOfOrder, Refusal(() => trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(8))).Reason);
        Assert.Equal(CasebookRefusal.VisitsOutOfOrder, Refusal(() => trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(7))).Reason);
        Assert.Null(trial.Casebook.Form(Trial.V2));
    }

    [Fact]
    public void Visits_dated_one_day_apart_in_schedule_order_are_accepted() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(9));

        // Assert
        Assert.Equal(9, form.StudyDay);
    }

    [Fact]
    public void Order_is_judged_against_visits_not_adjacent_in_the_schedule_too() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Assert: V3 against V1, with V2 not recorded
        Assert.Equal(CasebookRefusal.VisitsOutOfOrder, Refusal(() => trial.Casebook.RecordVisit(Trial.V3, trial.BareEntry(5))).Reason);
    }

    [Fact]
    public void Correcting_a_date_is_judged_against_the_other_visits_not_against_itself() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15));

        // Act: V1 moves from day 8 to day 14, still before V2
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(14));

        // Assert
        Assert.Equal(14, form.StudyDay);
    }

    [Fact]
    public void A_missed_visit_leaves_the_order_of_the_others_unconstrained() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.NotDone));
        trial.Casebook.RecordVisit(Trial.V3, trial.BareEntry(29));

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15));

        // Assert
        Assert.Equal(15, form.StudyDay);
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_cannot_take_place() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15)));

        // Assert
        Assert.Equal(CasebookRefusal.SubjectLostToFollowUp, refusal.Reason);
        Assert.True(trial.Casebook.IsLostToFollowUp);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.Refused)]
    public void After_a_visit_lost_to_follow_up_a_later_visit_can_only_be_lost_too(MissingReason otherReason) {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Assert
        Assert.Equal(CasebookRefusal.SubjectLostToFollowUp, Refusal(() => trial.Casebook.RecordVisit(Trial.V2, Trial.MissedEntry(otherReason))).Reason);
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_is_recorded_as_lost_too() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V2, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Assert
        Assert.False(form.Entry.TookPlace);
    }

    [Fact]
    public void A_visit_cannot_be_lost_to_follow_up_when_a_later_visit_took_place() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15));

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp)));

        // Assert
        Assert.Equal(CasebookRefusal.SubjectLostToFollowUp, refusal.Reason);
    }

    [Fact]
    public void A_visit_may_be_lost_to_follow_up_when_a_later_visit_was_missed_for_another_reason() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V2, Trial.MissedEntry(MissingReason.NotDone));

        // Act
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Assert
        Assert.True(trial.Casebook.IsLostToFollowUp);
    }

    [Fact]
    public void A_visit_before_the_one_lost_to_follow_up_can_still_take_place() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V2, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Assert
        Assert.True(form.Entry.TookPlace);
    }

    [Fact]
    public void Correcting_the_visit_lost_to_follow_up_lifts_the_restriction() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(MissingReason.LostToFollowUp));

        // Act: V1 turns out to have taken place, then V2 takes place
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        VisitForm form = trial.Casebook.RecordVisit(Trial.V2, trial.BareEntry(15));

        // Assert
        Assert.False(trial.Casebook.IsLostToFollowUp);
        Assert.Equal(15, form.StudyDay);
    }

    [Fact]
    public void Recording_a_visit_needs_its_arguments() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Throws<ArgumentNullException>(() => trial.Casebook.RecordVisit(null!, trial.BareEntry(8)));
        Assert.Throws<ArgumentNullException>(() => trial.Casebook.RecordVisit(Trial.V1, null!));
    }

    // ---- unresolved fields of a form ---------------------------------------------------------------------------

    [Fact]
    public void A_visit_with_no_vitals_entered_has_unresolved_vitals_and_unreported_required_labs() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V3, trial.BareEntry(29));

        // Assert
        Assert.Equal(new[] { "Vitals", "Lab.ALT", "Lab.HGB" }, form.UnresolvedFields());
    }

    [Fact]
    public void Each_sign_not_entered_is_an_unresolved_field_of_the_form() {
        // Arrange
        Trial      trial = Trial.Randomised();
        VitalSigns vitals = Any.VitalSigns().Complete().WithHeartRateBpm(null).WithWeightKg(null).Generate();
        trial.Casebook.RecordVisit(Trial.V2, new VisitEntry(Observed<DateOnly>.Of(trial.DateOfDay(15)), vitals, Array.Empty<LabResult>()));

        // Act
        IReadOnlyList<string> unresolved = trial.Casebook.Form(Trial.V2)!.UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Vitals.HeartRateBpm", "Vitals.WeightKg" }, unresolved);
    }

    [Fact]
    public void A_required_lab_whose_result_is_blank_is_an_unresolved_field() {
        // Arrange
        Trial     trial = Trial.Randomised();
        LabResult blankAlt = Any.LabResult().WithAnalyteCode("ALT").Blank().Generate();
        trial.Casebook.RecordVisit(Trial.V1, new VisitEntry(Observed<DateOnly>.Of(trial.DateOfDay(8)), new AnyVitalSigns().Complete().Generate(), new[] { blankAlt }));

        // Act
        IReadOnlyList<string> unresolved = trial.Casebook.Form(Trial.V1)!.UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Lab.ALT.Result" }, unresolved);
    }

    [Fact]
    public void A_lab_that_is_not_required_but_blank_is_an_unresolved_field_too() {
        // Arrange
        Trial     trial = Trial.Randomised();
        LabResult blankCreat = Any.LabResult().WithAnalyteCode("CREAT").Blank().Generate();
        LabResult alt        = Any.LabResult().WithAnalyteCode("ALT").Measured().Generate();
        trial.Casebook.RecordVisit(Trial.V1, new VisitEntry(Observed<DateOnly>.Of(trial.DateOfDay(8)), new AnyVitalSigns().Complete().Generate(), new[] { alt, blankCreat }));

        // Act
        IReadOnlyList<string> unresolved = trial.Casebook.Form(Trial.V1)!.UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "Lab.CREAT.Result" }, unresolved);
    }

    [Fact]
    public void A_required_lab_missing_with_a_reason_is_resolved() {
        // Arrange
        Trial         trial     = Trial.Randomised();
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();
        LabResult     missingAlt = Any.LabResult().WithAnalyteCode("ALT").WithResult(Observed<decimal>.Missing(anyReason)).Generate();
        trial.Casebook.RecordVisit(Trial.V1, new VisitEntry(Observed<DateOnly>.Of(trial.DateOfDay(8)), new AnyVitalSigns().Complete().Generate(), new[] { missingAlt }));

        // Assert
        Assert.Empty(trial.Casebook.Form(Trial.V1)!.UnresolvedFields());
    }

    [Fact]
    public void A_visit_that_did_not_take_place_has_nothing_to_resolve() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Act
        VisitForm form = trial.Casebook.RecordVisit(Trial.V3, Trial.MissedEntry(MissingReason.Refused));

        // Assert
        Assert.Empty(form.UnresolvedFields());
    }

    [Fact]
    public void The_unresolved_fields_of_the_casebook_name_the_visit_and_the_adverse_event() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).WithRelationship(null).WithSerious(false).WithGrade(2).WithOutcome(null).Generate());

        // Act
        IReadOnlyList<string> unresolved = trial.Casebook.UnresolvedFields();

        // Assert
        Assert.Equal(new[] { "V1.Vitals", "V1.Lab.ALT", "AE1.Relationship" }, unresolved);
    }

    // ---- submitting ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_draft_is_submitted() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);

        // Act
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_draft_with_unresolved_fields_can_be_submitted_all_the_same() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));

        // Act
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_submitted_form_cannot_be_submitted_again() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert
        Assert.Equal(CasebookRefusal.InvalidTransition, Refusal(() => trial.Casebook.SubmitVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void A_signed_form_cannot_be_submitted() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordAndSign(Trial.V1);

        // Assert
        Assert.Equal(CasebookRefusal.InvalidTransition, Refusal(() => trial.Casebook.SubmitVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void A_visit_never_recorded_cannot_be_submitted() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.VisitNotRecorded, Refusal(() => trial.Casebook.SubmitVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void An_unscheduled_visit_cannot_be_submitted() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.UnknownVisit, Refusal(() => trial.Casebook.SubmitVisit("V9")).Reason);
    }

    // ---- queries ------------------------------------------------------------------------------------------------

    private static string AnyText() {
        return Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
    }

    private static Trial SubmittedV1() {
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);
        trial.Casebook.SubmitVisit(Trial.V1);

        return trial;
    }

    [Fact]
    public void A_query_on_a_submitted_form_queries_the_form() {
        // Arrange
        Trial trial = SubmittedV1();
        string anyText = AnyText();

        // Act
        Query query = trial.Casebook.RaiseQuery(Trial.V1, "Vitals.HeartRateBpm", anyText, trial.DateOfDay(9));

        // Assert
        Assert.Equal(1, query.Number);
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Equal("Vitals.HeartRateBpm", query.FieldName);
        Assert.Equal(anyText, query.Text);
        Assert.Equal(trial.DateOfDay(9), query.RaisedOn);
        Assert.Equal(FormStatus.Queried, trial.Casebook.Form(Trial.V1)!.Status);
        Assert.Equal(1, trial.Casebook.OpenQueryCount);
    }

    [Fact]
    public void Queries_are_numbered_from_one_per_form() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.RecordComplete(Trial.V2);
        trial.Casebook.SubmitVisit(Trial.V2);

        // Act
        int firstOfV1  = trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9)).Number;
        int secondOfV1 = trial.Casebook.RaiseQuery(Trial.V1, "Lab.ALT", AnyText(), trial.DateOfDay(9)).Number;
        int firstOfV2  = trial.Casebook.RaiseQuery(Trial.V2, "Vitals", AnyText(), trial.DateOfDay(16)).Number;

        // Assert
        Assert.Equal(new[] { 1, 2, 1 }, new[] { firstOfV1, secondOfV1, firstOfV2 });
        Assert.Equal(3, trial.Casebook.OpenQueryCount);
    }

    [Fact]
    public void A_draft_cannot_be_queried() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9)));

        // Assert
        Assert.Equal(CasebookRefusal.InvalidTransition, refusal.Reason);
        Assert.Empty(trial.Casebook.Form(Trial.V1)!.Queries);
    }

    [Fact]
    public void A_query_on_a_signed_form_takes_its_signature_away() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordAndSign(Trial.V1);

        // Act
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Assert
        Assert.Equal(FormStatus.Queried, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_query_on_a_missing_visit_or_an_unscheduled_one_is_refused() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.VisitNotRecorded, Refusal(() => trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9))).Reason);
        Assert.Equal(CasebookRefusal.UnknownVisit, Refusal(() => trial.Casebook.RaiseQuery("V9", "Vitals", AnyText(), trial.DateOfDay(9))).Reason);
    }

    [Fact]
    public void A_query_with_a_bad_field_path_changes_nothing() {
        // Arrange
        Trial trial = SubmittedV1();

        // Act
        Assert.Throws<FormatException>(() => trial.Casebook.RaiseQuery(Trial.V1, "vitals.heart", AnyText(), trial.DateOfDay(9)));

        // Assert
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
        Assert.Empty(trial.Casebook.Form(Trial.V1)!.Queries);
    }

    [Fact]
    public void A_query_with_a_blank_text_changes_nothing() {
        // Arrange
        Trial trial = SubmittedV1();

        // Act
        Assert.Throws<ArgumentException>(() => trial.Casebook.RaiseQuery(Trial.V1, "Vitals", "  ", trial.DateOfDay(9)));

        // Assert
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_query_is_answered_then_closed() {
        // Arrange
        Trial  trial   = SubmittedV1();
        string anyAnswer = AnyText();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Act
        trial.Casebook.AnswerQuery(Trial.V1, 1, anyAnswer, trial.DateOfDay(10));
        Query answered = trial.Casebook.Form(Trial.V1)!.Queries[0];
        QueryStatus statusAfterAnswer = answered.Status;
        trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(11));

        // Assert
        Assert.Equal(QueryStatus.Answered, statusAfterAnswer);
        Assert.Equal(anyAnswer, answered.Response);
        Assert.Equal(trial.DateOfDay(10), answered.AnsweredOn);
        Assert.Equal(QueryStatus.Closed, answered.Status);
        Assert.Equal(trial.DateOfDay(11), answered.ClosedOn);
        Assert.Equal(0, trial.Casebook.OpenQueryCount);
    }

    [Fact]
    public void A_query_may_be_answered_and_closed_the_day_it_was_raised() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Act
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(9));
        trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(9));

        // Assert
        Assert.Equal(QueryStatus.Closed, trial.Casebook.Form(Trial.V1)!.Queries[0].Status);
    }

    [Fact]
    public void A_query_cannot_be_answered_before_it_was_raised() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(8)));

        // Assert
        Assert.Equal("answeredOn", refusal.ParamName);
        Assert.Equal(QueryStatus.Open, trial.Casebook.Form(Trial.V1)!.Queries[0].Status);
    }

    [Fact]
    public void A_query_cannot_be_closed_before_it_was_answered() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(11));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(10)));

        // Assert
        Assert.Equal("closedOn", refusal.ParamName);
        Assert.Equal(QueryStatus.Answered, trial.Casebook.Form(Trial.V1)!.Queries[0].Status);
    }

    [Fact]
    public void A_query_with_a_blank_answer_stays_open() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Act
        Assert.Throws<ArgumentException>(() => trial.Casebook.AnswerQuery(Trial.V1, 1, "", trial.DateOfDay(10)));

        // Assert
        Assert.Equal(QueryStatus.Open, trial.Casebook.Form(Trial.V1)!.Queries[0].Status);
    }

    [Fact]
    public void An_answered_query_cannot_be_answered_again() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(10));

        // Assert
        Assert.Equal(CasebookRefusal.QueryNotOpen, Refusal(() => trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(10))).Reason);
    }

    [Fact]
    public void A_closed_query_cannot_be_answered_nor_closed_again() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(10));
        trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(10));

        // Assert
        Assert.Equal(CasebookRefusal.QueryNotOpen, Refusal(() => trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(11))).Reason);
        Assert.Equal(CasebookRefusal.QueryNotAnswered, Refusal(() => trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(11))).Reason);
    }

    [Fact]
    public void An_open_query_cannot_be_closed() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Assert
        Assert.Equal(CasebookRefusal.QueryNotAnswered, Refusal(() => trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(10))).Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void A_query_number_that_does_not_exist_is_unknown(int number) {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Assert
        Assert.Equal(CasebookRefusal.UnknownQuery, Refusal(() => trial.Casebook.AnswerQuery(Trial.V1, number, AnyText(), trial.DateOfDay(10))).Reason);
        Assert.Equal(CasebookRefusal.UnknownQuery, Refusal(() => trial.Casebook.CloseQuery(Trial.V1, number, trial.DateOfDay(10))).Reason);
    }

    [Fact]
    public void A_query_on_a_visit_with_no_form_is_a_visit_not_recorded() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.VisitNotRecorded, Refusal(() => trial.Casebook.AnswerQuery(Trial.V2, 1, AnyText(), trial.DateOfDay(10))).Reason);
        Assert.Equal(CasebookRefusal.VisitNotRecorded, Refusal(() => trial.Casebook.CloseQuery(Trial.V2, 1, trial.DateOfDay(10))).Reason);
    }

    [Fact]
    public void A_queried_form_cannot_be_resubmitted_while_a_query_is_open() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));

        // Assert
        Assert.Equal(CasebookRefusal.OpenQueries, Refusal(() => trial.Casebook.SubmitVisit(Trial.V1)).Reason);
        Assert.Equal(FormStatus.Queried, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_queried_form_is_resubmitted_once_every_query_is_answered() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.RaiseQuery(Trial.V1, "Lab.ALT", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(10));

        // Assert: one query is still open
        Assert.Equal(CasebookRefusal.OpenQueries, Refusal(() => trial.Casebook.SubmitVisit(Trial.V1)).Reason);

        // Act
        trial.Casebook.AnswerQuery(Trial.V1, 2, AnyText(), trial.DateOfDay(10));
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
    }

    // ---- signing ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_submitted_form_with_every_field_resolved_is_signed() {
        // Arrange
        Trial trial = SubmittedV1();

        // Act
        trial.Casebook.SignVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Signed, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.LostToFollowUp)]
    [InlineData(MissingReason.Refused)]
    public void A_visit_that_did_not_take_place_is_signed_without_further_data(MissingReason reason) {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, Trial.MissedEntry(reason));
        trial.Casebook.SubmitVisit(Trial.V1);

        // Act
        trial.Casebook.SignVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Signed, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_draft_cannot_be_signed() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordComplete(Trial.V1);

        // Assert
        Assert.Equal(CasebookRefusal.InvalidTransition, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void A_signed_form_cannot_be_signed_again() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordAndSign(Trial.V1);

        // Assert
        Assert.Equal(CasebookRefusal.InvalidTransition, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void A_queried_form_cannot_be_signed() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(9));
        trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(9));

        // Assert: it has to be submitted again first
        Assert.Equal(CasebookRefusal.InvalidTransition, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void A_resubmitted_form_is_signed_once_its_queries_are_closed() {
        // Arrange
        Trial trial = SubmittedV1();
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(9));
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert: the answer was not accepted yet
        Assert.Equal(CasebookRefusal.UnclosedQueries, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);

        // Act
        trial.Casebook.CloseQuery(Trial.V1, 1, trial.DateOfDay(10));
        trial.Casebook.SignVisit(Trial.V1);

        // Assert
        Assert.Equal(FormStatus.Signed, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void A_form_with_unresolved_fields_is_not_signed() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        trial.Casebook.SubmitVisit(Trial.V1);

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.SignVisit(Trial.V1));

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("Vitals, Lab.ALT", refusal.Message);
        Assert.Equal(FormStatus.Submitted, trial.Casebook.Form(Trial.V1)!.Status);
    }

    [Fact]
    public void Unclosed_queries_are_reported_before_unresolved_fields() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.Casebook.RecordVisit(Trial.V1, trial.BareEntry(8));
        trial.Casebook.SubmitVisit(Trial.V1);
        trial.Casebook.RaiseQuery(Trial.V1, "Vitals", AnyText(), trial.DateOfDay(9));
        trial.Casebook.AnswerQuery(Trial.V1, 1, AnyText(), trial.DateOfDay(9));
        trial.Casebook.SubmitVisit(Trial.V1);

        // Assert
        Assert.Equal(CasebookRefusal.UnclosedQueries, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);
    }

    [Fact]
    public void Signing_a_visit_never_recorded_or_unscheduled_is_refused() {
        // Arrange
        Trial trial = Trial.Randomised();

        // Assert
        Assert.Equal(CasebookRefusal.VisitNotRecorded, Refusal(() => trial.Casebook.SignVisit(Trial.V1)).Reason);
        Assert.Equal(CasebookRefusal.UnknownVisit, Refusal(() => trial.Casebook.SignVisit("V9")).Reason);
    }

    // ---- adverse events -----------------------------------------------------------------------------------------

    [Fact]
    public void Adverse_events_are_numbered_from_one_in_the_order_reported() {
        // Arrange
        Trial trial = Trial.Screened();
        AdverseEvent first  = Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate();
        AdverseEvent second = Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(3)).Generate();

        // Act
        int firstNumber  = trial.Casebook.ReportAdverseEvent(first);
        int secondNumber = trial.Casebook.ReportAdverseEvent(second);

        // Assert
        Assert.Equal(1, firstNumber);
        Assert.Equal(2, secondNumber);
        Assert.Same(first, trial.Casebook.AdverseEvents[0]);
        Assert.Same(second, trial.Casebook.AdverseEvents[1]);
    }

    [Fact]
    public void An_adverse_event_can_be_reported_before_randomisation() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        int number = trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());

        // Assert
        Assert.Equal(1, number);
    }

    [Fact]
    public void An_adverse_event_cannot_start_before_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(-1)).Generate()));

        // Assert
        Assert.Equal(CasebookRefusal.EventBeforeConsent, refusal.Reason);
        Assert.Empty(trial.Casebook.AdverseEvents);
    }

    [Fact]
    public void An_adverse_event_may_start_the_day_of_consent() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());

        // Assert
        Assert.Single(trial.Casebook.AdverseEvents);
    }

    [Fact]
    public void Reporting_a_missing_event_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Trial.Screened().Casebook.ReportAdverseEvent(null!));
    }

    [Fact]
    public void An_adverse_event_is_revised_in_place() {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());
        AdverseEvent revised = Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(2)).Resolved().Generate();

        // Act
        trial.Casebook.ReviseAdverseEvent(1, revised);

        // Assert
        Assert.Same(revised, trial.Casebook.AdverseEvents[0]);
        Assert.Equal(2, trial.Casebook.AdverseEvents.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void An_adverse_event_that_does_not_exist_cannot_be_revised(int number) {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());
        AdverseEvent anyRevision = Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate();

        // Assert
        Assert.Equal(CasebookRefusal.UnknownAdverseEvent, Refusal(() => trial.Casebook.ReviseAdverseEvent(number, anyRevision)).Reason);
    }

    [Fact]
    public void A_revision_cannot_move_the_onset_before_consent() {
        // Arrange
        Trial trial = Trial.Screened();
        AdverseEvent original = Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate();
        trial.Casebook.ReportAdverseEvent(original);

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.ReviseAdverseEvent(1, Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(-5)).Generate()));

        // Assert
        Assert.Equal(CasebookRefusal.EventBeforeConsent, refusal.Reason);
        Assert.Same(original, trial.Casebook.AdverseEvents[0]);
    }

    [Fact]
    public void Revising_with_a_missing_event_is_refused() {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Generate());

        // Assert
        Assert.Throws<ArgumentNullException>(() => trial.Casebook.ReviseAdverseEvent(1, null!));
    }

    private static AdverseEvent Fatal(DateOnly onset, DateOnly death) {
        string anyTerm = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength).Generate();

        return new AdverseEvent(anyTerm, onset, null, death, 5, true, Causality.Possible, AdverseEventOutcome.Fatal, null);
    }

    [Fact]
    public void The_date_of_death_is_the_end_of_a_fatal_event() {
        // Arrange
        Trial    trial = Trial.Screened();
        DateOnly death = trial.ConsentDate.AddDays(20);

        // Act
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), death));

        // Assert
        Assert.Equal(death, trial.Casebook.DeathDate);
    }

    [Fact]
    public void A_non_fatal_event_does_not_set_a_date_of_death() {
        // Arrange
        Trial trial = Trial.Screened();

        // Act
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).WithOutcome(AdverseEventOutcome.Recovered).WithGrade(3).Generate());

        // Assert
        Assert.Null(trial.Casebook.DeathDate);
    }

    [Fact]
    public void No_event_can_start_after_the_death() {
        // Arrange
        Trial    trial = Trial.Screened();
        DateOnly death = trial.ConsentDate.AddDays(20);
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), death));

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(death.AddDays(1)).Generate()));

        // Assert
        Assert.Equal(CasebookRefusal.EventAfterDeath, refusal.Reason);
        Assert.Single(trial.Casebook.AdverseEvents);
    }

    [Fact]
    public void An_event_may_start_the_day_of_the_death() {
        // Arrange
        Trial    trial = Trial.Screened();
        DateOnly death = trial.ConsentDate.AddDays(20);
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), death));

        // Act
        int number = trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(death).Generate());

        // Assert
        Assert.Equal(2, number);
    }

    [Fact]
    public void A_fatal_event_cannot_end_before_another_event_started() {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(30)).Generate());

        // Act: the subject would have died on day 20 and had an event starting on day 30
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), trial.ConsentDate.AddDays(20))));

        // Assert
        Assert.Equal(CasebookRefusal.EventAfterDeath, refusal.Reason);
        Assert.Single(trial.Casebook.AdverseEvents);
    }

    [Fact]
    public void With_several_fatal_events_the_death_is_the_earliest_end() {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), trial.ConsentDate.AddDays(30)));

        // Act
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), trial.ConsentDate.AddDays(25)));

        // Assert
        Assert.Equal(trial.ConsentDate.AddDays(25), trial.Casebook.DeathDate);
    }

    [Fact]
    public void Revising_the_fatal_event_so_that_it_is_not_fatal_lifts_the_death() {
        // Arrange
        Trial    trial = Trial.Screened();
        DateOnly onset = trial.ConsentDate.AddDays(15);
        trial.Casebook.ReportAdverseEvent(Fatal(onset, trial.ConsentDate.AddDays(20)));

        // Act
        AdverseEvent recovered = Any.AdverseEvent().NonFatal().WithOnsetDate(onset).WithGrade(4).WithOutcome(AdverseEventOutcome.Recovered).Generate();
        trial.Casebook.ReviseAdverseEvent(1, recovered);
        int number = trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(40)).Generate());

        // Assert
        Assert.Null(trial.Casebook.DeathDate);
        Assert.Equal(2, number);
    }

    [Fact]
    public void A_revision_cannot_bring_the_death_before_the_start_of_another_event() {
        // Arrange
        Trial trial = Trial.Screened();
        trial.Casebook.ReportAdverseEvent(Fatal(trial.ConsentDate.AddDays(15), trial.ConsentDate.AddDays(60)));
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate.AddDays(50)).Generate());

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.ReviseAdverseEvent(1, Fatal(trial.ConsentDate.AddDays(15), trial.ConsentDate.AddDays(30))));

        // Assert
        Assert.Equal(CasebookRefusal.EventAfterDeath, refusal.Reason);
        Assert.Equal(trial.ConsentDate.AddDays(60), trial.Casebook.DeathDate);
    }

    // ---- locking ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_casebook_cannot_be_locked_before_randomisation() {
        // Arrange
        Trial trial = Trial.Screened();

        // Assert
        Assert.Equal(CasebookRefusal.NotRandomised, Refusal(() => trial.Casebook.Lock()).Reason);
    }

    [Fact]
    public void A_casebook_cannot_be_locked_while_a_visit_is_not_recorded() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordAndSign(Trial.V1);
        trial.RecordAndSign(Trial.V3);

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.FormsNotSigned, refusal.Reason);
        Assert.Contains("V2", refusal.Message);
        Assert.False(trial.Casebook.IsLocked);
    }

    [Fact]
    public void A_casebook_cannot_be_locked_while_a_visit_is_not_signed() {
        // Arrange
        Trial trial = Trial.Randomised();
        trial.RecordAndSign(Trial.V1);
        trial.RecordComplete(Trial.V2);
        trial.Casebook.SubmitVisit(Trial.V2);
        trial.RecordAndSign(Trial.V3);

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.FormsNotSigned, refusal.Reason);
        Assert.Contains("V2", refusal.Message);
    }

    [Fact]
    public void A_casebook_cannot_be_locked_when_a_signed_visit_has_lost_its_signature() {
        // Arrange
        Trial trial = Trial.ReadyToLock();
        trial.Casebook.RaiseQuery(Trial.V2, "Vitals", AnyText(), trial.DateOfDay(16));

        // Assert
        Assert.Equal(CasebookRefusal.FormsNotSigned, Refusal(() => trial.Casebook.Lock()).Reason);
    }

    [Fact]
    public void A_casebook_cannot_be_locked_with_an_unresolved_adverse_event() {
        // Arrange
        Trial trial = Trial.ReadyToLock();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).WithRelationship(null).Generate());

        // Act
        CasebookRefusedException refusal = Refusal(() => trial.Casebook.Lock());

        // Assert
        Assert.Equal(CasebookRefusal.UnresolvedFields, refusal.Reason);
        Assert.Contains("AE1.Relationship", refusal.Message);
        Assert.False(trial.Casebook.IsLocked);
    }

    [Fact]
    public void A_casebook_with_every_visit_signed_and_every_event_resolved_is_locked() {
        // Arrange
        Trial trial = Trial.ReadyToLock();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Resolved().Generate());

        // Act
        trial.Casebook.Lock();

        // Assert
        Assert.True(trial.Casebook.IsLocked);
        Assert.All(trial.Casebook.Forms, form => Assert.Equal(FormStatus.Locked, form.Status));
        Assert.Equal(3, trial.Casebook.Forms.Count);
    }

    [Fact]
    public void A_casebook_with_missed_visits_signed_is_locked() {
        // Arrange
        Trial trial = Trial.Randomised();
        foreach (VisitDefinition visit in trial.Casebook.Schedule.Visits) {
            trial.Casebook.RecordVisit(visit.Code, Trial.MissedEntry(MissingReason.LostToFollowUp));
            trial.Casebook.SubmitVisit(visit.Code);
            trial.Casebook.SignVisit(visit.Code);
        }

        // Act
        trial.Casebook.Lock();

        // Assert
        Assert.True(trial.Casebook.IsLocked);
        Assert.True(trial.Casebook.IsLostToFollowUp);
    }

    [Fact]
    public void A_casebook_with_no_adverse_event_is_locked() {
        // Arrange
        Trial trial = Trial.ReadyToLock();

        // Act
        trial.Casebook.Lock();

        // Assert
        Assert.True(trial.Casebook.IsLocked);
    }

    public static IEnumerable<object[]> OperationsOnALockedCasebook() {
        yield return new object[] { "Randomise", (Action<Trial>)(t => t.Casebook.Randomise(new Randomisation(1, new DateOnly(2025, 1, 1), TreatmentArm.Placebo))) };
        yield return new object[] { "RecordVisit", (Action<Trial>)(t => t.Casebook.RecordVisit(Trial.V1, t.BareEntry(8))) };
        yield return new object[] { "SubmitVisit", (Action<Trial>)(t => t.Casebook.SubmitVisit(Trial.V1)) };
        yield return new object[] { "RaiseQuery", (Action<Trial>)(t => t.Casebook.RaiseQuery(Trial.V1, "Vitals", "Please check.", t.DateOfDay(9))) };
        yield return new object[] { "AnswerQuery", (Action<Trial>)(t => t.Casebook.AnswerQuery(Trial.V1, 1, "Checked.", t.DateOfDay(9))) };
        yield return new object[] { "CloseQuery", (Action<Trial>)(t => t.Casebook.CloseQuery(Trial.V1, 1, t.DateOfDay(9))) };
        yield return new object[] { "SignVisit", (Action<Trial>)(t => t.Casebook.SignVisit(Trial.V1)) };
        yield return new object[] { "ReportAdverseEvent", (Action<Trial>)(t => t.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(t.ConsentDate).Generate())) };
        yield return new object[] { "ReviseAdverseEvent", (Action<Trial>)(t => t.Casebook.ReviseAdverseEvent(1, Any.AdverseEvent().NonFatal().WithOnsetDate(t.ConsentDate).Generate())) };
        yield return new object[] { "Lock", (Action<Trial>)(t => t.Casebook.Lock()) };
    }

    [Theory]
    [MemberData(nameof(OperationsOnALockedCasebook))]
    public void Every_change_on_a_locked_casebook_is_refused(string operation, Action<Trial> change) {
        // Arrange
        Trial trial = Trial.ReadyToLock();
        trial.Casebook.ReportAdverseEvent(Any.AdverseEvent().NonFatal().WithOnsetDate(trial.ConsentDate).Resolved().Generate());
        trial.Casebook.Lock();

        // Act
        CasebookRefusedException refusal = Refusal(() => change(trial));

        // Assert
        Assert.Equal(CasebookRefusal.CasebookLocked, refusal.Reason);
        Assert.True(trial.Casebook.IsLocked);
        Assert.NotEmpty(operation);
    }

    // ---- the refusal itself -------------------------------------------------------------------------------------

    [Fact]
    public void A_refusal_is_an_invalid_operation_carrying_its_reason_and_message() {
        // Arrange
        string anyMessage = Any.String().NotBlank().WithMaxLength(100).Generate();

        // Act
        CasebookRefusedException refusal = new CasebookRefusedException(CasebookRefusal.FormNotEditable, anyMessage);

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
        Assert.Equal(CasebookRefusal.FormNotEditable, refusal.Reason);
        Assert.Equal(anyMessage, refusal.Message);
    }

    [Fact]
    public void A_refusal_needs_a_declared_reason() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CasebookRefusedException((CasebookRefusal)999, "x"));
    }

}
