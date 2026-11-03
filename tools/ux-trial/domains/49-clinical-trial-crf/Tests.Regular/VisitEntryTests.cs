using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class VisitEntryTests {

    private static Observed<DateOnly> Took() {
        return Observed<DateOnly>.Of(TrialFields.Date().Generate());
    }

    private static LabResult Lab(string code) {
        return Any.LabResult().WithAnalyteCode(code).Generate();
    }

    [Fact]
    public void A_visit_that_took_place_carries_its_date_vitals_and_labs() {
        // Arrange
        Observed<DateOnly> anyDate   = Took();
        VitalSigns         anyVitals = Any.VitalSigns().Generate();
        LabResult          alt       = Lab("ALT");

        // Act
        VisitEntry entry = new VisitEntry(anyDate, anyVitals, new[] { alt });

        // Assert
        Assert.True(entry.TookPlace);
        Assert.Equal(anyDate, entry.VisitDate);
        Assert.Same(anyVitals, entry.Vitals);
        Assert.Same(alt, Assert.Single(entry.Labs));
    }

    [Fact]
    public void A_visit_that_took_place_may_not_have_vitals_nor_labs_yet() {
        // Act
        VisitEntry entry = new VisitEntry(Took(), null, Array.Empty<LabResult>());

        // Assert
        Assert.Null(entry.Vitals);
        Assert.Empty(entry.Labs);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.LostToFollowUp)]
    [InlineData(MissingReason.Refused)]
    public void A_visit_that_did_not_take_place_is_recorded_with_its_reason(MissingReason reason) {
        // Act
        VisitEntry entry = new VisitEntry(Observed<DateOnly>.Missing(reason), null, Array.Empty<LabResult>());

        // Assert
        Assert.False(entry.TookPlace);
        Assert.True(entry.VisitDate.IsMissing(reason));
    }

    [Fact]
    public void A_scheduled_visit_is_never_not_applicable() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(MissingReason.NotApplicable), null, Array.Empty<LabResult>()));

        // Assert
        Assert.Equal("visitDate", refusal.ParamName);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_carries_no_vital_signs() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().DifferentFrom(MissingReason.NotApplicable).Generate();
        VitalSigns    anyVitals = Any.VitalSigns().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(anyReason), anyVitals, Array.Empty<LabResult>()));

        // Assert
        Assert.Equal("visitDate", refusal.ParamName);
    }

    [Fact]
    public void A_visit_that_did_not_take_place_carries_no_laboratory_result() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().DifferentFrom(MissingReason.NotApplicable).Generate();
        LabResult     anyLab    = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Observed<DateOnly>.Missing(anyReason), null, new[] { anyLab }));
    }

    [Fact]
    public void Missing_arguments_are_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(null!, null, Array.Empty<LabResult>()));
        Assert.Throws<ArgumentNullException>(() => new VisitEntry(Took(), null, null!));
    }

    [Fact]
    public void A_visit_cannot_list_a_null_laboratory_result() {
        // Assert
        Assert.Throws<ArgumentException>(() => new VisitEntry(Took(), null, new LabResult[] { Lab("ALT"), null! }));
    }

    [Fact]
    public void Each_analyte_is_reported_once_per_visit() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new VisitEntry(Took(), null, new[] { Lab("ALT"), Lab("HGB"), Lab("ALT") }));

        // Assert
        Assert.Equal("labs", refusal.ParamName);
    }

    [Fact]
    public void A_laboratory_result_is_found_by_its_analyte_code() {
        // Arrange
        LabResult  alt   = Lab("ALT");
        VisitEntry entry = new VisitEntry(Took(), null, new[] { Lab("HGB"), alt });

        // Assert
        Assert.Same(alt, entry.Lab("ALT"));
        Assert.Null(entry.Lab("CREAT"));
        Assert.Null(entry.Lab("alt"));
    }

    [Fact]
    public void Looking_up_a_missing_analyte_code_is_refused() {
        // Arrange
        VisitEntry anyEntry = Any.VisitEntry().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyEntry.Lab(null!));
    }

    [Fact]
    public void Changing_the_list_given_to_the_constructor_does_not_change_the_entry() {
        // Arrange
        List<LabResult> labs = new List<LabResult> { Lab("ALT") };

        // Act
        VisitEntry entry = new VisitEntry(Took(), null, labs);
        labs.Add(Lab("HGB"));

        // Assert
        Assert.Single(entry.Labs);
    }

    [Fact]
    public void A_visit_that_took_place_keeps_the_order_of_its_labs() {
        // Arrange
        VisitEntry entry = new VisitEntry(Took(), null, new[] { Lab("HGB"), Lab("ALT"), Lab("CREAT") });

        // Assert
        Assert.Equal(new[] { "HGB", "ALT", "CREAT" }, entry.Labs.Select(l => l.AnalyteCode));
    }

}
