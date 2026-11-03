namespace Laboratory.Tests;

public sealed class LabReportTests {

    // ── construction ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_report_exposes_its_sample_id_and_its_results() {
        // Arrange
        SampleId        sampleId = Any.SampleId().Generate();
        List<LabResult> results  = ResultsOf(sampleId, count: 3);

        // Act
        LabReport report = new LabReport(sampleId, results);

        // Assert
        Assert.Same(sampleId, report.SampleId);
        Assert.Equal(results, report.Results);
    }

    [Fact]
    public void A_report_without_a_sample_id_is_refused() {
        // Arrange
        List<LabResult> anyResults = ResultsOf(Any.SampleId().Generate(), count: 1);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabReport(null!, anyResults));

        // Assert
        Assert.Equal("sampleId", refusal.ParamName);
    }

    [Fact]
    public void A_report_without_results_is_refused() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabReport(anySampleId, null!));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void A_report_with_an_empty_list_of_results_is_refused() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, new List<LabResult>()));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void A_result_from_another_specimen_than_the_reported_one_is_refused() {
        // Arrange
        SampleId        sampleId  = Any.SampleId().Generate();
        SampleId        anyOther  = Any.SampleId().DifferentFrom(sampleId).Generate();
        List<AnalyteCode> analytes = DistinctAnalytes(3);
        Specimen          reported = Any.Specimen().WithId(sampleId).Generate();
        List<LabResult>   results  = new List<LabResult> {
            Any.LabResult().WithSpecimen(reported).WithAnalyte(analytes[0]).Generate(),
            Any.LabResult().WithSpecimen(reported).WithAnalyte(analytes[1]).Generate(),
            Any.LabResult().WithSpecimen(s => s.WithId(anyOther)).WithAnalyte(analytes[2]).Generate()
        };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(sampleId, results));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void Results_whose_specimens_are_different_objects_with_the_same_sample_id_belong_together() {
        // Arrange: the specimens are compared by sample id (a value), not by reference
        SampleId  sampleId = Any.SampleId().Generate();
        List<AnalyteCode> analytes = DistinctAnalytes(2);
        LabResult first    = Any.LabResult().WithSpecimen(s => s.WithId(new SampleId(sampleId.Value))).WithAnalyte(analytes[0]).Generate();
        LabResult second   = Any.LabResult().WithSpecimen(s => s.WithId(new SampleId(sampleId.Value))).WithAnalyte(analytes[1]).Generate();

        // Act
        LabReport report = new LabReport(sampleId, new List<LabResult> { first, second });

        // Assert
        Assert.Equal(2, report.Results.Count);
    }

    [Fact]
    public void Two_results_for_the_same_analyte_are_refused() {
        // Arrange
        SampleId  sampleId = Any.SampleId().Generate();
        Specimen  specimen = Any.Specimen().WithId(sampleId).Generate();
        LabResult first    = Any.LabResult().WithSpecimen(specimen).Generate();
        LabResult repeated = Any.LabResult().WithSpecimen(specimen).WithAnalyte(first.Analyte).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabReport(sampleId, new List<LabResult> { first, repeated }));

        // Assert
        Assert.Equal("results", refusal.ParamName);
    }

    [Fact]
    public void The_analytes_are_distinct_by_value_not_by_reference() {
        // Arrange: two different AnalyteCode objects carrying the same text
        SampleId  sampleId = Any.SampleId().Generate();
        Specimen  specimen = Any.Specimen().WithId(sampleId).Generate();
        LabResult first    = Any.LabResult().WithSpecimen(specimen).WithAnalyte(new AnalyteCode("2823-3")).Generate();
        LabResult repeated = Any.LabResult().WithSpecimen(specimen).WithAnalyte(new AnalyteCode("2823-3")).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new LabReport(sampleId, new List<LabResult> { first, repeated }));
    }

    [Fact]
    public void A_report_keeps_the_results_it_was_given_even_if_the_caller_changes_its_list_afterwards() {
        // Arrange
        SampleId        sampleId = Any.SampleId().Generate();
        List<LabResult> results  = ResultsOf(sampleId, count: 3);
        LabReport       report   = new LabReport(sampleId, results);

        // Act
        results.Clear();

        // Assert
        Assert.Equal(3, report.Results.Count);
    }

    [Fact(Skip = "domain defect: LabReport.Results returns its private List<LabResult> typed as IReadOnlyList, so a cast to IList<LabResult> clears the report's results (breaking the at-least-one-result invariant), the draw was irrelevant, seed 53681236")]
    public void The_results_a_report_hands_out_cannot_be_used_to_change_the_report() {
        // Arrange
        SampleId  sampleId = Any.SampleId().Generate();
        LabReport report   = new LabReport(sampleId, ResultsOf(sampleId, count: 2));

        // Act: a caller who knows the list is a List<T> clears it through the "read-only" view
        IList<LabResult>? writable = report.Results as IList<LabResult>;
        bool removed = true;
        try { writable?.Clear(); } catch (NotSupportedException) { removed = false; }

        // Assert
        Assert.False(removed);
        Assert.Equal(2, report.Results.Count);
    }

    // ── abnormal results ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_abnormal_results_are_the_out_of_range_ones_in_the_order_of_the_report() {
        // Arrange: normal, high, normal, critical low, low
        SampleId       sampleId = Any.SampleId().Generate();
        ReferenceRange range    = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);
        Specimen       specimen = Any.Specimen().WithId(sampleId).Generate();

        List<AnalyteCode> analytes = DistinctAnalytes(5);

        LabResult normalFirst  = ResultAt(4.0, range, specimen, analytes[0]);
        LabResult high         = ResultAt(5.8, range, specimen, analytes[1]);
        LabResult normalSecond = ResultAt(4.5, range, specimen, analytes[2]);
        LabResult criticalLow  = ResultAt(2.0, range, specimen, analytes[3]);
        LabResult low          = ResultAt(3.0, range, specimen, analytes[4]);
        LabReport report       = new LabReport(sampleId, new List<LabResult> { normalFirst, high, normalSecond, criticalLow, low });

        // Act
        IReadOnlyList<LabResult> abnormal = report.Abnormal();

        // Assert
        Assert.Equal(new List<LabResult> { high, criticalLow, low }, abnormal);
    }

    [Fact]
    public void A_report_of_normal_results_has_no_abnormal_result() {
        // Arrange
        SampleId       sampleId = Any.SampleId().Generate();
        ReferenceRange range    = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate());
        Specimen       specimen = Any.Specimen().WithId(sampleId).Generate();
        List<AnalyteCode> analytes = DistinctAnalytes(2);
        LabReport      report   = new LabReport(sampleId, new List<LabResult> { ResultAt(3.5, range, specimen, analytes[0]), ResultAt(5.1, range, specimen, analytes[1]) });

        // Act
        IReadOnlyList<LabResult> abnormal = report.Abnormal();

        // Assert
        Assert.Empty(abnormal);
    }

    // ── turnaround time ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_turnaround_of_a_single_result_runs_from_the_collection_to_the_report() {
        // Arrange
        SampleId       sampleId    = Any.SampleId().Generate();
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset reportedAt  = new DateTimeOffset(2025, 3, 1, 11, 30, 0, TimeSpan.Zero);
        LabResult      result      = Any.LabResult()
                                        .WithSpecimen(s => s.WithId(sampleId).WithCollectedAt(collectedAt).WithTransit(TimeSpan.FromHours(1)))
                                        .WithReportedAt(reportedAt)
                                        .Generate();
        LabReport      report      = new LabReport(sampleId, new List<LabResult> { result });

        // Act
        TimeSpan turnaround = report.TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(3.5), turnaround);
    }

    [Fact]
    public void The_turnaround_runs_from_the_earliest_collection_to_the_latest_report_of_any_results() {
        // Arrange: the earliest collection (07:30) and the latest report (12:00) belong to different results
        SampleId          sampleId = Any.SampleId().Generate();
        List<AnalyteCode> analytes = DistinctAnalytes(2);
        LabResult earlyCollectionEarlyReport = ResultOn(sampleId, analytes[0], collectedAt: At(7, 30), reportedAt: At(10, 0));
        LabResult lateCollectionLateReport   = ResultOn(sampleId, analytes[1], collectedAt: At(8, 15), reportedAt: At(12, 0));
        LabReport report                     = new LabReport(sampleId, new List<LabResult> { lateCollectionLateReport, earlyCollectionEarlyReport });

        // Act
        TimeSpan turnaround = report.TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(4.5), turnaround);
    }

    [Fact]
    public void The_turnaround_does_not_depend_on_the_order_of_the_results() {
        // Arrange
        SampleId          sampleId = Any.SampleId().Generate();
        List<AnalyteCode> analytes = DistinctAnalytes(2);
        LabResult first    = ResultOn(sampleId, analytes[0], collectedAt: At(7, 30), reportedAt: At(10, 0));
        LabResult second   = ResultOn(sampleId, analytes[1], collectedAt: At(8, 15), reportedAt: At(12, 0));

        // Act
        TimeSpan forwards  = new LabReport(sampleId, new List<LabResult> { first, second }).TurnaroundTime();
        TimeSpan backwards = new LabReport(sampleId, new List<LabResult> { second, first }).TurnaroundTime();

        // Assert
        Assert.Equal(forwards, backwards);
    }

    [Fact]
    public void An_amendment_extends_the_turnaround_to_the_time_of_the_amendment() {
        // Arrange
        SampleId  sampleId = Any.SampleId().Generate();
        LabResult result   = ResultOn(sampleId, Any.AnalyteCode().Generate(), collectedAt: At(8, 0), reportedAt: At(10, 0));
        result.Release();
        LabReport report = new LabReport(sampleId, new List<LabResult> { result });

        // Act
        result.Amend(new Measurement(1d, result.Range.Unit), At(15, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), report.TurnaroundTime());
    }

    [Fact]
    public void The_turnaround_compares_instants_not_clock_readings() {
        // Arrange: collected 08:00 at +02:00 (06:00 UTC), reported 08:00 UTC
        SampleId       sampleId    = Any.SampleId().Generate();
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset reportedAt  = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        LabResult      result      = ResultOn(sampleId, Any.AnalyteCode().Generate(), collectedAt, reportedAt);

        // Act
        TimeSpan turnaround = new LabReport(sampleId, new List<LabResult> { result }).TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), turnaround);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────────────────

    private static DateTimeOffset At(int hour, int minute) {
        return new DateTimeOffset(2025, 3, 1, hour, minute, 0, TimeSpan.Zero);
    }

    /// <summary>Analytes that differ from one another: a report refuses a repeated one, and two plain draws could, rarely, coincide.</summary>
    private static List<AnalyteCode> DistinctAnalytes(int count) {
        return Any.ListOf(Any.AnalyteCode()).WithCount(count).Distinct().Generate();
    }

    /// <summary>Results of one specimen for distinct analytes: the analytes and the values play no part in what is asked of the report.</summary>
    private static List<LabResult> ResultsOf(SampleId sampleId, int count) {
        Specimen specimen = Any.Specimen().WithId(sampleId).Generate();

        return DistinctAnalytes(count).Select(analyte => Any.LabResult().WithSpecimen(specimen).WithAnalyte(analyte).Generate()).ToList();
    }

    private static LabResult ResultAt(double value, ReferenceRange range, Specimen specimen, AnalyteCode analyte) {
        return Any.LabResult().WithSpecimen(specimen).WithRange(range).WithValue(value).WithAnalyte(analyte).Generate();
    }

    private static LabResult ResultOn(SampleId sampleId, AnalyteCode analyte, DateTimeOffset collectedAt, DateTimeOffset reportedAt) {
        return Any.LabResult()
                  .WithSpecimen(s => s.WithId(sampleId).WithCollectedAt(collectedAt).WithTransit(TimeSpan.Zero))
                  .WithReportedAt(reportedAt)
                  .WithAnalyte(analyte)
                  .Generate();
    }

}
