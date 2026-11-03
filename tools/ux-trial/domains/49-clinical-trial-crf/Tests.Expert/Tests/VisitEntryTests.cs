using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class VisitEntryTests {

    public static IEnumerable<object[]> ReasonsAVisitCanBeMissedFor() {
        foreach (MissingReason reason in Enum.GetValues<MissingReason>()) {
            if (reason != MissingReason.NotApplicable) { yield return [reason]; }
        }
    }

    [Fact]
    public void A_visit_with_a_date_took_place() {
        // Arrange
        DateOnly anyDate = Scenarios.AnyDate();

        // Act
        VisitEntry entry = Any.VisitEntry().OnDate(anyDate).Generate();

        // Assert
        Assert.True(entry.TookPlace);
        Assert.Equal(anyDate, entry.VisitDate.Value);
    }

    [Theory]
    [MemberData(nameof(ReasonsAVisitCanBeMissedFor))]
    public void A_visit_missed_for_a_reason_took_not_place_and_carries_no_data(MissingReason reason) {
        // Act
        VisitEntry entry = Any.VisitEntry().Missing(reason).Generate();

        // Assert
        Assert.False(entry.TookPlace);
        Assert.True(entry.VisitDate.IsMissing(reason));
        Assert.Null(entry.Vitals);
        Assert.Empty(entry.Labs);
    }

    [Fact]
    public void A_scheduled_visit_is_never_not_applicable() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitEntry().Missing(MissingReason.NotApplicable).Generate());

        // Assert
        Assert.Equal("visitDate", refusal.ParamName);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_cannot_carry_vital_signs() {
        // Arrange
        Observed<DateOnly> anyMissedVisit = AnyObservedOf.MissingExcept<DateOnly>(MissingReason.NotApplicable).Generate();
        VitalSigns         anyVitals      = Any.VitalSigns().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitEntry(anyMissedVisit, anyVitals, []));

        // Assert
        Assert.Equal("visitDate", refusal.ParamName);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_cannot_carry_laboratory_results() {
        // Arrange
        Observed<DateOnly> anyMissedVisit = AnyObservedOf.MissingExcept<DateOnly>(MissingReason.NotApplicable).Generate();
        LabResult          anyLab         = Any.LabResult().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitEntry(anyMissedVisit, null, [anyLab]));

        // Assert
        Assert.Equal("visitDate", refusal.ParamName);
    }

    [Fact]
    public void A_visit_that_took_place_may_carry_no_vital_signs_and_no_labs() {
        // Arrange
        Observed<DateOnly> anyVisitDate = AnyObservedOf.OfValues(Any.DateOnly().Between(TrialCalendar.Earliest, TrialCalendar.Latest)).Generate();

        // Act
        VisitEntry entry = new VisitEntry(anyVisitDate, null, []);

        // Assert
        Assert.True(entry.TookPlace);
        Assert.Null(entry.Vitals);
        Assert.Empty(entry.Labs);
    }

    [Fact]
    public void A_missing_visit_date_or_list_of_labs_is_refused() {
        // Arrange
        Observed<DateOnly> anyVisitDate = AnyObservedOf.OfValues(Any.DateOnly().Between(TrialCalendar.Earliest, TrialCalendar.Latest)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(null!, null, []));
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(anyVisitDate, null, null!));
    }

    [Fact]
    public void A_null_lab_is_refused() {
        // Arrange
        LabResult anyLab = Any.LabResult().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitEntry().WithLabs(anyLab, null!).Generate());

        // Assert
        Assert.Equal("labs", refusal.ParamName);
    }

    [Fact]
    public void An_analyte_is_reported_once_per_visit() {
        // Arrange
        string    anyCode  = AnyAnalyteCodes.Code().Generate();
        LabResult first    = Any.LabResult().WithAnalyteCode(anyCode).Generate();
        LabResult repeated = Any.LabResult().WithAnalyteCode(anyCode).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VisitEntry().WithLabs(first, repeated).Generate());

        // Assert
        Assert.Equal("labs", refusal.ParamName);
    }

    [Fact]
    public void A_lab_is_found_by_its_analyte_code() {
        // Arrange
        LabResult alt = Any.LabResult().WithAnalyteCode("ALT").Generate();
        LabResult hgb = Any.LabResult().WithAnalyteCode("HGB").Generate();
        VisitEntry entry = Any.VisitEntry().WithLabs(alt, hgb).Generate();

        // Act / Assert
        Assert.Same(hgb, entry.Lab("HGB"));
        Assert.Null(entry.Lab("CREA"));
        Assert.Throws<ArgumentNullException>(() => entry.Lab(null!));
    }

    [Fact]
    public void An_entry_does_not_follow_the_list_of_labs_it_was_built_from() {
        // Arrange
        List<LabResult> labs  = [Any.LabResult().Generate()];
        VisitEntry      entry = Any.VisitEntry().WithLabs(labs.ToArray()).Generate();
        VisitEntry      copy  = new VisitEntry(entry.VisitDate, entry.Vitals, labs);

        // Act
        labs.Add(Any.LabResult().WithAnalyteCode("ZZ").Generate());

        // Assert
        Assert.Single(copy.Labs);
    }

}
