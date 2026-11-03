using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class VisitEntryTests {

    private static Observed<DateOnly> Took(DateOnly date) => Observed<DateOnly>.Of(date);

    [Fact]
    public void A_visit_that_took_place_carries_its_date_vitals_and_labs() {
        // Arrange
        DateOnly        visitDate = new DateOnly(2024, 3, 18);
        VitalSigns      anyVitals = Any.VitalSigns().Generate();
        List<LabResult> anyLabs   = Any.ListOf(Any.LabResult()).WithCountBetween(1, 4).DistinctBy(lab => lab.AnalyteCode).Generate();

        // Act
        VisitEntry entry = new VisitEntry(Took(visitDate), anyVitals, anyLabs);

        // Assert
        Assert.True(entry.TookPlace);
        Assert.Equal(visitDate, entry.VisitDate.Value);
        Assert.Same(anyVitals, entry.Vitals);
        Assert.Equal(anyLabs, entry.Labs);
    }

    [Fact]
    public void A_visit_may_take_place_with_neither_vitals_nor_labs() {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act
        VisitEntry entry = new VisitEntry(Took(anyDate), null, Array.Empty<LabResult>());

        // Assert
        Assert.Null(entry.Vitals);
        Assert.Empty(entry.Labs);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.LostToFollowUp)]
    [InlineData(MissingReason.Refused)]
    public void A_visit_that_did_not_take_place_carries_a_reason_and_nothing_else(MissingReason reason) {
        // Act
        VisitEntry entry = new VisitEntry(Observed<DateOnly>.Missing(reason), null, Array.Empty<LabResult>());

        // Assert
        Assert.False(entry.TookPlace);
        Assert.True(entry.VisitDate.IsMissing(reason));
    }

    [Fact]
    public void A_scheduled_visit_is_never_not_applicable() {
        Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(MissingReason.NotApplicable), null, Array.Empty<LabResult>()));
    }

    [Fact]
    public void A_visit_that_did_not_take_place_carries_no_vitals() {
        // Arrange
        MissingReason anyReason = Observations.ReasonVisitDidNotTakePlace().Generate();
        VitalSigns    anyVitals = Any.VitalSigns().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(anyReason), anyVitals, Array.Empty<LabResult>()));
    }

    [Fact]
    public void A_visit_that_did_not_take_place_carries_no_labs() {
        // Arrange
        MissingReason anyReason = Observations.ReasonVisitDidNotTakePlace().Generate();
        LabResult     anyLab    = Any.LabResult().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(anyReason), null, new[] { anyLab }));
    }

    [Fact]
    public void Each_analyte_is_reported_once() {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();
        LabResult first  = Any.LabResult().WithAnalyteCode("ALT").Generate();
        LabResult second = Any.LabResult().WithAnalyteCode("ALT").Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Took(anyDate), null, new[] { first, second }));
    }

    [Fact]
    public void A_lab_result_cannot_be_null() {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Took(anyDate), null, new LabResult[] { null! }));
    }

    [Fact]
    public void A_visit_date_and_a_list_of_labs_are_required() {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(null!, null, Array.Empty<LabResult>()));
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(Took(anyDate), null, null!));
    }

    [Fact]
    public void A_lab_is_found_by_its_analyte_code() {
        // Arrange
        DateOnly  anyDate = TrialCalendar.Date().Generate();
        LabResult alt     = Any.LabResult().WithAnalyteCode("ALT").Generate();
        LabResult hgb     = Any.LabResult().WithAnalyteCode("HGB").Generate();

        // Act
        VisitEntry entry = new VisitEntry(Took(anyDate), null, new[] { alt, hgb });

        // Assert
        Assert.Same(hgb, entry.Lab("HGB"));
        Assert.Null(entry.Lab("CREAT"));
    }

    [Fact]
    public void Looking_up_a_lab_requires_an_analyte_code() {
        // Arrange
        VisitEntry anyEntry = Any.VisitEntry().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyEntry.Lab(null!));
    }

}
