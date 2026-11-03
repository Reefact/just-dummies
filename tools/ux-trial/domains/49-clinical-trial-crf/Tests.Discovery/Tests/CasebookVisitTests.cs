using JustDummies;

using Xunit;

using static ClinicalTrial.Domain.Tests.Protocol;

namespace ClinicalTrial.Domain.Tests;

public sealed class CasebookVisitTests {

    [Fact]
    public void Recording_a_visit_creates_a_draft_form_with_its_study_day() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VisitEntry      entry    = Entry(DateOfDay(8));

        // Act
        VisitForm form = casebook.RecordVisit("D8", entry);

        // Assert
        Assert.Equal(FormStatus.Draft, form.Status);
        Assert.Equal(8, form.StudyDay);
        Assert.Equal("D8", form.Definition.Code);
        Assert.Same(entry, form.Entry);
        Assert.Same(form, casebook.Form("D8"));
        Assert.False(form.IsOutOfWindow);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_has_no_study_day() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("D8", DidNotTakePlace(MissingReason.NotDone));

        // Assert
        Assert.Null(form.StudyDay);
        Assert.False(form.IsOutOfWindow);
    }

    [Fact]
    public void A_visit_cannot_be_recorded_before_randomisation() {
        // Arrange
        SubjectCasebook casebook = Casebook();
        VisitEntry      anyEntry = Entry(DateOfDay(8));

        // Act + Assert
        AssertRefused(CasebookRefusal.NotRandomised, () => casebook.RecordVisit("D8", anyEntry));
    }

    [Fact]
    public void Only_a_scheduled_visit_can_be_recorded() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VisitEntry      anyEntry = Entry(DateOfDay(8));

        // Act + Assert
        AssertRefused(CasebookRefusal.UnknownVisit, () => casebook.RecordVisit("D9", anyEntry));
    }

    [Fact]
    public void A_visit_cannot_precede_randomisation() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VisitEntry      onTheEve = Entry(RandomisationDay.AddDays(-1));

        // Act + Assert
        AssertRefused(CasebookRefusal.VisitBeforeRandomisation, () => casebook.RecordVisit("D8", onTheEve));
        Assert.Null(casebook.Form("D8"));
    }

    [Fact]
    public void A_visit_may_take_place_on_the_randomisation_day_though_it_is_out_of_window() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("D8", Entry(RandomisationDay));

        // Assert
        Assert.Equal(1, form.StudyDay);
        Assert.True(form.IsOutOfWindow);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void A_visit_outside_its_window_is_accepted_and_flagged(int studyDay, bool outOfWindow) {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        VisitForm form = casebook.RecordVisit("D8", Entry(DateOfDay(studyDay)));

        // Assert
        Assert.Equal(outOfWindow, form.IsOutOfWindow);
        Assert.Equal(outOfWindow ? new[] { "D8" } : Array.Empty<string>(), casebook.OutOfWindowVisits());
    }

    [Fact]
    public void Out_of_window_visits_are_listed_in_schedule_order() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        casebook.RecordVisit("D85", Entry(DateOfDay(100)));
        casebook.RecordVisit("D8", Entry(DateOfDay(12)));
        casebook.RecordVisit("D29", Entry(DateOfDay(29)));

        // Assert
        Assert.Equal(new[] { "D8", "D85" }, casebook.OutOfWindowVisits());
    }

    [Fact]
    public void Forms_are_listed_in_schedule_order_whatever_the_order_they_were_recorded_in() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        casebook.RecordVisit("D85", Entry(DateOfDay(85)));
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));

        // Assert
        Assert.Equal(new[] { "D8", "D85" }, casebook.Forms.Select(form => form.Definition.Code));
    }

    [Fact]
    public void A_draft_visit_can_be_entered_again_and_replaces_the_entry() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VisitForm       first    = casebook.RecordVisit("D8", Entry(DateOfDay(8)));
        VisitEntry      revised  = Entry(DateOfDay(9));

        // Act
        VisitForm second = casebook.RecordVisit("D8", revised);

        // Assert
        Assert.Same(first, second);
        Assert.Same(revised, second.Entry);
        Assert.Equal(9, second.StudyDay);
        Assert.Equal(FormStatus.Draft, second.Status);
    }

    [Fact]
    public void A_submitted_form_can_no_longer_be_edited() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));
        casebook.SubmitVisit("D8");
        VisitEntry anyEntry = Entry(DateOfDay(8));

        // Act + Assert
        AssertRefused(CasebookRefusal.FormNotEditable, () => casebook.RecordVisit("D8", anyEntry));
    }

    [Fact]
    public void A_queried_form_can_be_corrected_and_keeps_its_queries() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));
        casebook.SubmitVisit("D8");
        casebook.RaiseQuery("D8", "Vitals.HeartRateBpm", "Please confirm the heart rate", DateOfDay(12));
        VisitEntry corrected = Entry(DateOfDay(8));

        // Act
        VisitForm form = casebook.RecordVisit("D8", corrected);

        // Assert
        Assert.Equal(FormStatus.Queried, form.Status);
        Assert.Same(corrected, form.Entry);
        Assert.Single(form.Queries);
    }

    [Fact]
    public void A_visit_must_come_after_the_earlier_ones() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D29", Entry(DateOfDay(29)));

        // Act + Assert
        AssertRefused(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("D8", Entry(DateOfDay(30))));
        AssertRefused(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("D8", Entry(DateOfDay(29))));
        Assert.Equal(28, casebook.RecordVisit("D8", Entry(DateOfDay(28))).StudyDay);
    }

    [Fact]
    public void A_visit_must_come_before_the_later_ones() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", Entry(DateOfDay(8)));

        // Act + Assert
        AssertRefused(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("D29", Entry(DateOfDay(8))));
        AssertRefused(CasebookRefusal.VisitsOutOfOrder, () => casebook.RecordVisit("D29", Entry(DateOfDay(7))));
    }

    [Fact]
    public void A_visit_that_did_not_take_place_does_not_take_part_in_the_ordering() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D8", DidNotTakePlace(MissingReason.NotDone));

        // Act
        VisitForm form = casebook.RecordVisit("D29", Entry(DateOfDay(5)));

        // Assert
        Assert.Equal(5, form.StudyDay);
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_cannot_take_place() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.LostToFollowUp));
        VisitEntry anyEntry = Entry(DateOfDay(85));

        // Act + Assert
        AssertRefused(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("D85", anyEntry));
        Assert.True(casebook.IsLostToFollowUp);
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_cannot_be_missing_for_another_reason() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.LostToFollowUp));

        // Act + Assert
        AssertRefused(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("D85", DidNotTakePlace(MissingReason.NotDone)));
    }

    [Fact]
    public void After_a_visit_lost_to_follow_up_a_later_visit_is_lost_too() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.LostToFollowUp));

        // Act
        VisitForm form = casebook.RecordVisit("D85", DidNotTakePlace(MissingReason.LostToFollowUp));

        // Assert
        Assert.True(form.Entry.VisitDate.IsMissing(MissingReason.LostToFollowUp));
    }

    [Fact]
    public void A_visit_cannot_be_lost_to_follow_up_when_a_later_one_took_place() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D85", Entry(DateOfDay(85)));

        // Act + Assert
        AssertRefused(CasebookRefusal.SubjectLostToFollowUp, () => casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.LostToFollowUp)));
    }

    [Fact]
    public void An_earlier_visit_may_still_take_place_after_a_later_one_was_lost() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.LostToFollowUp));

        // Act
        VisitForm form = casebook.RecordVisit("D8", Entry(DateOfDay(8)));

        // Assert
        Assert.True(form.Entry.TookPlace);
    }

    [Fact]
    public void A_visit_missing_for_another_reason_is_not_a_loss_to_follow_up() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.Refused));
        casebook.RecordVisit("D85", Entry(DateOfDay(85)));

        // Assert
        Assert.False(casebook.IsLostToFollowUp);
    }

    [Fact]
    public void Recording_a_visit_requires_a_code_and_an_entry() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        VisitEntry      anyEntry = Entry(DateOfDay(8));

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => casebook.RecordVisit(null!, anyEntry));
        Assert.Throws<ArgumentNullException>(() => casebook.RecordVisit("D8", null!));
    }

    [Fact]
    public void A_form_that_was_not_recorded_is_absent() {
        Assert.Null(RandomisedCasebook().Form("D8"));
    }

    [Fact]
    public void Unresolved_fields_are_listed_per_visit_and_path() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();

        // Act
        casebook.RecordVisit("D8", new VisitEntry(Observed<DateOnly>.Of(DateOfDay(8)), null, Array.Empty<LabResult>()));
        casebook.RecordVisit("D29", DidNotTakePlace(MissingReason.Unknown));

        // Assert
        Assert.Equal(new[] { "D8.Vitals", "D8.Lab.ALT" }, casebook.UnresolvedFields());
    }

    [Fact]
    public void A_blank_lab_result_and_a_missing_required_analyte_are_unresolved() {
        // Arrange
        SubjectCasebook casebook = RandomisedCasebook();
        LabResult       blankAlt = Any.LabResult().WithAnalyteCode("ALT").WithResult(null).Generate();
        VisitEntry      entry    = Any.VisitEntry()
                                      .WithVisitDate(Observed<DateOnly>.Of(DateOfDay(29)))
                                      .WithVitals(Any.VitalSigns().FullyResolved().Generate())
                                      .WithLabs(new[] { blankAlt })
                                      .Generate();

        // Act
        casebook.RecordVisit("D29", entry);

        // Assert
        Assert.Equal(new[] { "D29.Lab.HGB", "D29.Lab.ALT.Result" }, casebook.UnresolvedFields());
    }

}
