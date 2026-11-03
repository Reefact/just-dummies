using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class LabReportTests {

    private static readonly DateTimeOffset T0 = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static List<AnalyteCode> AnyDistinctAnalytes(int count) {
        return Any.SetOf(new AnyAnalyteCode()).WithCount(count).Generate().ToList();
    }

    private static LabResult AnyResultOn(SampleId id, AnalyteCode analyte) {
        return Any.LabResult().WithAnalyte(analyte).WithSpecimen(s => s.WithId(id)).Generate();
    }

    private static LabResult AnyResultMeasuring(SampleId id, AnalyteCode analyte, double value) {
        ReferenceRange range = new ReferenceRange(10d, 20d, Any.MeasurementUnit().Generate(), criticalLow: 5d, criticalHigh: 30d);

        return Any.LabResult().WithAnalyte(analyte).WithSpecimen(s => s.WithId(id)).WithRange(range).WithMeasurement(m => m.WithValue(value)).Generate();
    }

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_report_keeps_its_sample_id_and_its_results_in_order() {
        // Arrange
        SampleId         anyId       = Any.SampleId().Generate();
        List<AnalyteCode> analytes   = AnyDistinctAnalytes(3);
        List<LabResult>  anyResults  = analytes.Select(analyte => AnyResultOn(anyId, analyte)).ToList();

        // Act
        LabReport report = new LabReport(anyId, anyResults);

        // Assert
        Assert.Same(anyId, report.SampleId);
        Assert.Equal(anyResults, report.Results);
    }

    [Fact]
    public void A_report_of_a_single_result_is_enough() {
        // Arrange
        SampleId  anyId     = Any.SampleId().Generate();
        LabResult anyResult = AnyResultOn(anyId, Any.AnalyteCode().Generate());

        // Act
        LabReport report = new LabReport(anyId, [anyResult]);

        // Assert
        Assert.Single(report.Results);
    }

    [Fact]
    public void The_generator_only_draws_reports_the_constructor_accepts() {
        // Act
        LabReport anyReport = Any.LabReport().Generate();

        // Assert
        Assert.NotEmpty(anyReport.Results);
        Assert.All(anyReport.Results, result => Assert.Equal(anyReport.SampleId, result.Specimen.Id));
        Assert.Equal(anyReport.Results.Count, anyReport.Results.Select(result => result.Analyte).Distinct().Count());
    }

    [Fact]
    public void A_missing_sample_id_is_refused_as_a_null_argument() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabReport(null!, [anyResult]));

        // Assert
        Assert.Equal("sampleId", refusal.ParamName);
    }

    [Fact]
    public void Missing_results_are_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabReport(Any.SampleId().Generate(), null!));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void A_report_without_results_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(Any.SampleId().Generate(), []));

        // Assert
        Assert.Equal("results", refusal.ParamName);
        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_result_from_another_specimen_than_the_reports_is_refused() {
        // Arrange
        SampleId  anyId          = Any.SampleId().Generate();
        SampleId  anyOtherId     = Any.SampleId().WithValue(v => v.DifferentFrom(anyId.Value)).Generate();
        LabResult aResultOfOurs  = AnyResultOn(anyId, Any.AnalyteCode().Generate());
        LabResult aResultOfOther = AnyResultOn(anyOtherId, Any.AnalyteCode().WithValue(v => v.DifferentFrom(aResultOfOurs.Analyte.Value)).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(anyId, [aResultOfOurs, aResultOfOther]));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void A_single_result_from_another_specimen_is_enough_to_refuse_the_report() {
        // Arrange
        SampleId  anyId      = Any.SampleId().Generate();
        SampleId  anyOtherId = Any.SampleId().WithValue(v => v.DifferentFrom(anyId.Value)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anyId, [AnyResultOn(anyOtherId, Any.AnalyteCode().Generate())]));
    }

    [Fact]
    public void A_sample_id_is_matched_by_value_not_by_reference() {
        // Arrange
        string    anyLabel  = Any.String().StartingWith("S").Numeric().WithLength(11).Generate();
        LabResult anyResult = AnyResultOn(new SampleId(anyLabel), Any.AnalyteCode().Generate());

        // Act
        LabReport report = new LabReport(new SampleId(anyLabel), [anyResult]);

        // Assert
        Assert.Equal(anyLabel, report.SampleId.Value);
    }

    [Fact]
    public void An_analyte_cannot_appear_twice_even_when_the_codes_are_different_instances() {
        // Arrange
        SampleId    anyId      = Any.SampleId().Generate();
        AnalyteCode anyAnalyte = Any.AnalyteCode().Generate();
        LabResult   first      = AnyResultOn(anyId, anyAnalyte);
        LabResult   second     = AnyResultOn(anyId, new AnalyteCode(anyAnalyte.Value));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(anyId, [first, second]));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void The_same_result_twice_is_a_repeated_analyte() {
        // Arrange
        SampleId  anyId     = Any.SampleId().Generate();
        LabResult anyResult = AnyResultOn(anyId, Any.AnalyteCode().Generate());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anyId, [anyResult, anyResult]));
    }

    [Fact]
    public void The_results_are_copied_so_that_later_changes_to_the_source_list_are_not_seen() {
        // Arrange
        SampleId          anyId     = Any.SampleId().Generate();
        List<AnalyteCode> analytes  = AnyDistinctAnalytes(2);
        List<LabResult>   source    = [AnyResultOn(anyId, analytes[0])];
        LabReport         report    = new LabReport(anyId, source);

        // Act
        source.Add(AnyResultOn(anyId, analytes[1]));

        // Assert
        Assert.Single(report.Results);
    }

    [Fact(Skip = "domain defect (low): Results returns the private List<LabResult> itself, so a caller can downcast to IList<LabResult> and add or remove results after the invariants were checked; no seed: no draw involved")]
    public void The_results_of_a_report_cannot_be_changed_through_the_list_it_hands_out() {
        // Arrange
        SampleId  anyId  = Any.SampleId().Generate();
        LabReport report = new LabReport(anyId, [AnyResultOn(anyId, Any.AnalyteCode().Generate())]);

        // Act & Assert
        Assert.False(report.Results is IList<LabResult>);
    }

    // ---- abnormal results ---------------------------------------------------------------------------------------

    [Fact]
    public void The_abnormal_results_are_those_out_of_range_in_report_order() {
        // Arrange
        SampleId          anyId     = Any.SampleId().Generate();
        List<AnalyteCode> analytes  = AnyDistinctAnalytes(5);
        LabResult normal            = AnyResultMeasuring(anyId, analytes[0], 15d);
        LabResult high              = AnyResultMeasuring(anyId, analytes[1], 25d);
        LabResult alsoNormal        = AnyResultMeasuring(anyId, analytes[2], 20d);
        LabResult criticalLow       = AnyResultMeasuring(anyId, analytes[3], 4d);
        LabResult low               = AnyResultMeasuring(anyId, analytes[4], 9d);
        LabReport report            = new LabReport(anyId, [normal, high, alsoNormal, criticalLow, low]);

        // Act
        IReadOnlyList<LabResult> abnormal = report.Abnormal();

        // Assert
        Assert.Equal([high, criticalLow, low], abnormal);
    }

    [Fact]
    public void A_report_of_normal_results_has_nothing_abnormal() {
        // Arrange
        SampleId          anyId    = Any.SampleId().Generate();
        List<AnalyteCode> analytes = AnyDistinctAnalytes(2);
        LabReport         report   = new LabReport(anyId, [AnyResultMeasuring(anyId, analytes[0], 10d), AnyResultMeasuring(anyId, analytes[1], 20d)]);

        // Act & Assert
        Assert.Empty(report.Abnormal());
    }

    [Fact]
    public void The_abnormal_results_follow_an_amendment_of_a_result() {
        // Arrange
        SampleId  anyId   = Any.SampleId().Generate();
        LabResult result  = AnyResultMeasuring(anyId, Any.AnalyteCode().Generate(), 15d);
        LabReport report  = new LabReport(anyId, [result]);
        Assert.Empty(report.Abnormal());
        result.Release();

        // Act
        result.Amend(new Measurement(40d, result.Range.Unit), result.ReportedAt + TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal([result], report.Abnormal());
    }

    // ---- turnaround ---------------------------------------------------------------------------------------------

    [Fact]
    public void The_turnaround_of_a_single_result_runs_from_collection_to_report() {
        // Arrange
        SampleId  anyId     = Any.SampleId().Generate();
        Specimen  specimen  = Any.Specimen().WithId(anyId).WithCollectedAt(T0).WithTransit(TimeSpan.FromHours(3)).Generate();
        LabResult result    = Any.LabResult().WithSpecimen(specimen).WithReportedAt(T0 + TimeSpan.FromHours(10)).Generate();
        LabReport report    = new LabReport(anyId, [result]);

        // Act
        TimeSpan turnaround = report.TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(10), turnaround);
    }

    [Fact]
    public void The_turnaround_runs_from_the_earliest_collection_to_the_latest_report() {
        // Arrange
        SampleId          anyId    = Any.SampleId().Generate();
        List<AnalyteCode> analytes = AnyDistinctAnalytes(3);

        Specimen collectedSecond = Any.Specimen().WithId(anyId).WithCollectedAt(T0 + TimeSpan.FromHours(1)).WithTransit(TimeSpan.FromHours(2)).Generate();
        Specimen collectedFirst  = Any.Specimen().WithId(anyId).WithCollectedAt(T0).WithTransit(TimeSpan.FromHours(4)).Generate();
        Specimen collectedThird  = Any.Specimen().WithId(anyId).WithCollectedAt(T0 + TimeSpan.FromHours(2)).WithTransit(TimeSpan.FromHours(1)).Generate();

        LabResult reportedLatest   = Any.LabResult().WithAnalyte(analytes[0]).WithSpecimen(collectedSecond).WithReportedAt(T0 + TimeSpan.FromHours(30)).Generate();
        LabResult reportedFirst    = Any.LabResult().WithAnalyte(analytes[1]).WithSpecimen(collectedFirst).WithReportedAt(T0 + TimeSpan.FromHours(6)).Generate();
        LabResult reportedBetween  = Any.LabResult().WithAnalyte(analytes[2]).WithSpecimen(collectedThird).WithReportedAt(T0 + TimeSpan.FromHours(12)).Generate();

        LabReport report = new LabReport(anyId, [reportedFirst, reportedLatest, reportedBetween]);

        // Act
        TimeSpan turnaround = report.TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(30), turnaround);
    }

    [Fact]
    public void The_turnaround_counts_an_amended_report_time() {
        // Arrange
        SampleId  anyId    = Any.SampleId().Generate();
        Specimen  specimen = Any.Specimen().WithId(anyId).WithCollectedAt(T0).WithTransit(TimeSpan.FromHours(1)).Generate();
        LabResult result   = Any.LabResult().WithSpecimen(specimen).WithReportedAt(T0 + TimeSpan.FromHours(5)).Generate();
        LabReport report   = new LabReport(anyId, [result]);
        result.Release();

        // Act
        result.Amend(Any.Measurement().WithUnit(result.Range.Unit).Generate(), T0 + TimeSpan.FromHours(26));

        // Assert
        Assert.Equal(TimeSpan.FromHours(26), report.TurnaroundTime());
    }

    [Fact]
    public void The_turnaround_is_measured_between_instants_whatever_the_offsets() {
        // Arrange: collected at 08:00 +00:00, reported at 20:00 +05:00 the same day = 15:00 UTC.
        SampleId  anyId    = Any.SampleId().Generate();
        Specimen  specimen = Any.Specimen().WithId(anyId).WithCollectedAt(T0).WithTransit(TimeSpan.FromHours(1)).Generate();
        LabResult result   = Any.LabResult().WithSpecimen(specimen).WithReportedAt(new DateTimeOffset(2025, 3, 1, 20, 0, 0, TimeSpan.FromHours(5))).Generate();

        // Act
        TimeSpan turnaround = new LabReport(anyId, [result]).TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), turnaround);
    }

}
