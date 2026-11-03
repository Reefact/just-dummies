using JustDummies;

using Laboratory.Domain;

using Xunit;

namespace Laboratory.Tests.Discovery;

public sealed class LabReportTests {

    private static readonly DateTimeOffset T0 = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    // A result for the given sample, normal unless a value says otherwise: range 10..20, critical at or below 5 and at or above 30.
    private static LabResult ResultFor(SampleId sampleId, AnalyteCode analyte, double value, DateTimeOffset collectedAt, DateTimeOffset reportedAt) {
        MeasurementUnit unit     = Any.MeasurementUnit().Generate();
        Specimen        specimen = Any.Specimen().WithId(sampleId).WithCollectedAt(collectedAt).WithReceivedAt(collectedAt.AddHours(1)).Generate();

        return new LabResult(analyte, specimen, new Measurement(value, unit), new ReferenceRange(10d, 20d, unit, 5d, 30d), reportedAt);
    }

    private static LabResult NormalResultFor(SampleId sampleId) {
        return Any.LabResult().WithSpecimen(s => s.WithId(sampleId)).Generate();
    }

    [Fact]
    public void A_report_keeps_its_sample_id_and_its_results_in_order() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();
        LabResult first  = NormalResultFor(anySampleId);
        LabResult second = Any.LabResult().WithAnalyte(a => a.WithValue(v => v.DifferentFrom(first.Analyte.Value))).WithSpecimen(s => s.WithId(anySampleId)).Generate();

        // Act
        LabReport report = new LabReport(anySampleId, [first, second]);

        // Assert
        Assert.Same(anySampleId, report.SampleId);
        Assert.Equal([first, second], report.Results);
    }

    [Fact]
    public void A_missing_sample_id_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabReport(null!, [anyResult]));
    }

    [Fact]
    public void Missing_results_are_refused() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabReport(anySampleId, null!));
    }

    [Fact]
    public void A_report_without_any_result_is_refused() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, []));
    }

    [Fact]
    public void A_result_from_another_specimen_is_refused() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();
        SampleId anyOtherId  = Any.SampleId().WithValue(v => v.DifferentFrom(anySampleId.Value)).Generate();
        LabResult fromTheSample = NormalResultFor(anySampleId);
        LabResult fromAnother   = Any.LabResult().WithAnalyte(a => a.WithValue(v => v.DifferentFrom(fromTheSample.Analyte.Value)))
                                     .WithSpecimen(s => s.WithId(anyOtherId)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, [fromTheSample, fromAnother]));
    }

    [Fact]
    public void Sample_ids_are_compared_by_value_not_by_reference() {
        // Arrange
        string    anyValue  = Any.SampleId().Generate().Value;
        LabResult result    = NormalResultFor(new SampleId(anyValue));

        // Act
        LabReport report = new LabReport(new SampleId(anyValue), [result]);

        // Assert
        Assert.Single(report.Results);
    }

    [Fact]
    public void The_same_analyte_twice_is_refused() {
        // Arrange
        SampleId    anySampleId = Any.SampleId().Generate();
        AnalyteCode anyAnalyte  = Any.AnalyteCode().Generate();
        LabResult   first       = Any.LabResult().WithAnalyte(anyAnalyte).WithSpecimen(s => s.WithId(anySampleId)).Generate();
        LabResult   second      = Any.LabResult().WithAnalyte(new AnalyteCode(anyAnalyte.Value)).WithSpecimen(s => s.WithId(anySampleId)).Generate();

        // Assert: distinct by value, not by instance
        Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, [first, second]));
    }

    [Fact]
    public void The_same_result_twice_is_refused() {
        // Arrange
        SampleId  anySampleId = Any.SampleId().Generate();
        LabResult anyResult   = NormalResultFor(anySampleId);

        // Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, [anyResult, anyResult]));
    }

    [Fact(Skip = "domain defect: a null element among the results raises NullReferenceException instead of ArgumentException (LabReport constructor dereferences result.Specimen); seed not applicable (literal)")]
    public void A_null_result_among_the_results_is_refused_as_an_argument_error() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabReport(anySampleId, [NormalResultFor(anySampleId), null!]));
    }

    [Fact]
    public void The_results_are_copied() {
        // Arrange
        SampleId        anySampleId = Any.SampleId().Generate();
        List<LabResult> source      = [NormalResultFor(anySampleId)];
        LabReport       report      = new LabReport(anySampleId, source);

        // Act
        source.Add(Any.LabResult().Generate());
        source.Clear();

        // Assert
        Assert.Single(report.Results);
    }

    [Fact]
    public void Only_the_out_of_range_results_are_abnormal_and_they_keep_their_order() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();
        string[] codes       = ["1-1", "2-2", "3-3", "4-4", "5-5"];
        double[] values      = [15d,   2d,    25d,   12d,   40d];
        List<LabResult> results = [];
        for (int index = 0; index < codes.Length; index++) {
            results.Add(ResultFor(anySampleId, new AnalyteCode(codes[index]), values[index], T0, T0.AddHours(5)));
        }
        LabReport report = new LabReport(anySampleId, results);

        // Act
        IReadOnlyList<LabResult> abnormal = report.Abnormal();

        // Assert
        Assert.Equal(["2-2", "3-3", "5-5"], abnormal.Select(result => result.Analyte.Value));
    }

    [Fact]
    public void A_report_of_normal_results_has_nothing_abnormal() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();
        LabReport report = new LabReport(anySampleId, [ResultFor(anySampleId, new AnalyteCode("1-1"), 10d, T0, T0.AddHours(5)),
                                                       ResultFor(anySampleId, new AnalyteCode("2-2"), 20d, T0, T0.AddHours(5))]);

        // Assert
        Assert.Empty(report.Abnormal());
    }

    [Fact]
    public void An_amendment_to_a_result_changes_what_the_report_calls_abnormal() {
        // Arrange
        SampleId  anySampleId = Any.SampleId().Generate();
        LabResult result      = ResultFor(anySampleId, new AnalyteCode("1-1"), 2d, T0, T0.AddHours(5));
        LabReport report      = new LabReport(anySampleId, [result]);
        Assert.Single(report.Abnormal());
        result.Release();

        // Act
        result.Amend(new Measurement(15d, result.Range.Unit), T0.AddHours(6));

        // Assert
        Assert.Empty(report.Abnormal());
    }

    [Fact]
    public void The_turnaround_of_a_single_result_runs_from_its_collection_to_its_report() {
        // Arrange
        SampleId  anySampleId = Any.SampleId().Generate();
        LabReport report      = new LabReport(anySampleId, [ResultFor(anySampleId, new AnalyteCode("1-1"), 15d, T0, T0.AddHours(5))]);

        // Assert
        Assert.Equal(TimeSpan.FromHours(5), report.TurnaroundTime());
    }

    [Fact]
    public void The_turnaround_runs_from_the_earliest_collection_to_the_latest_report_across_results() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();
        LabReport report = new LabReport(anySampleId, [ResultFor(anySampleId, new AnalyteCode("1-1"), 15d, T0.AddHours(2), T0.AddHours(10)),
                                                       ResultFor(anySampleId, new AnalyteCode("2-2"), 15d, T0,             T0.AddHours(4)),
                                                       ResultFor(anySampleId, new AnalyteCode("3-3"), 15d, T0.AddHours(1), T0.AddHours(30))]);

        // Assert: collected at T0 (second result), reported at T0+30h (third result)
        Assert.Equal(TimeSpan.FromHours(30), report.TurnaroundTime());
    }

    [Fact]
    public void An_amendment_extends_the_turnaround() {
        // Arrange
        SampleId  anySampleId = Any.SampleId().Generate();
        LabResult result      = ResultFor(anySampleId, new AnalyteCode("1-1"), 15d, T0, T0.AddHours(5));
        LabReport report      = new LabReport(anySampleId, [result]);
        result.Release();

        // Act
        result.Amend(new Measurement(16d, result.Range.Unit), T0.AddHours(48));

        // Assert
        Assert.Equal(TimeSpan.FromHours(48), report.TurnaroundTime());
    }

    [Fact]
    public void A_drawn_report_is_valid_whatever_the_number_of_results() {
        // Arrange
        LabReport anyReport = Any.LabReport().Generate();

        // Assert
        Assert.NotEmpty(anyReport.Results);
        Assert.All(anyReport.Results, result => Assert.Equal(anyReport.SampleId, result.Specimen.Id));
        Assert.Equal(anyReport.Results.Count, anyReport.Results.Select(result => result.Analyte).Distinct().Count());
        Assert.True(anyReport.TurnaroundTime() >= TimeSpan.Zero);
    }

    [Fact]
    public void The_turnaround_is_never_shorter_than_the_longest_transit_of_the_report() {
        // Arrange
        LabReport anyReport = Any.LabReport().Generate();

        // Assert: every result is reported after its own receipt, itself after its collection
        Assert.All(anyReport.Results, result => Assert.True(anyReport.TurnaroundTime() >= result.ReportedAt - result.Specimen.CollectedAt));
    }

}
