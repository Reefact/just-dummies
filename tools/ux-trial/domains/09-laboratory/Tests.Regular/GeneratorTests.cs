using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

/// <summary>The hand-completed generators are code of this project: they get a test of their own.</summary>
public sealed class GeneratorTests {

    [Fact]
    public void A_pinned_reception_is_kept_and_the_collection_stays_within_the_window() {
        // Arrange
        DateTimeOffset anyReceivedAt = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        Specimen anySpecimen = Any.Specimen().WithReceivedAt(anyReceivedAt).Generate();

        // Assert
        Assert.Equal(anyReceivedAt, anySpecimen.ReceivedAt);
        Assert.InRange(anySpecimen.ReceivedAt - anySpecimen.CollectedAt, TimeSpan.Zero, Specimen.StabilityWindow);
    }

    [Fact]
    public void A_pinned_result_unit_is_followed_by_the_range() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Act
        LabResult anyResult = Any.LabResult().WithMeasurement(Any.Measurement().WithUnit(anyUnit).Generate()).Generate();

        // Assert
        Assert.Equal(anyUnit, anyResult.Range.Unit);
    }

    [Fact]
    public void A_pinned_report_time_is_kept_and_follows_the_reception() {
        // Arrange
        DateTimeOffset anyReportedAt = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        LabResult anyResult = Any.LabResult().WithReportedAt(anyReportedAt).Generate();

        // Assert
        Assert.Equal(anyReportedAt, anyResult.ReportedAt);
        Assert.True(anyResult.Specimen.ReceivedAt <= anyReportedAt);
    }

    [Fact]
    public void A_pinned_result_count_is_the_size_of_the_report() {
        // Arrange
        int anyCount = Any.Int32().Between(1, 6).Generate();

        // Act
        LabReport anyReport = Any.LabReport().WithResultCount(anyCount).Generate();

        // Assert
        Assert.Equal(anyCount, anyReport.Results.Count);
    }

    [Fact]
    public void A_pinned_sample_id_is_the_one_of_every_result() {
        // Arrange
        SampleId anyId = Any.SampleId().Generate();

        // Act
        LabReport anyReport = Any.LabReport().WithSampleId(anyId).Generate();

        // Assert
        Assert.Equal(anyId, anyReport.SampleId);
        Assert.All(anyReport.Results, result => Assert.Equal(anyId, result.Specimen.Id));
    }

    [Fact]
    public void A_pinned_range_without_critical_limits_is_never_given_any() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().WithoutCriticalLow().WithoutCriticalHigh().Generate();

        // Assert
        Assert.Null(anyRange.CriticalLow);
        Assert.Null(anyRange.CriticalHigh);
    }

    [Fact]
    public void Critical_limits_are_drawn_beyond_the_bounds_that_were_drawn() {
        // Arrange
        ReferenceRange anyRange = Any.ReferenceRange().Generate();

        // Assert
        Assert.True(anyRange.CriticalLow is null || anyRange.CriticalLow <= anyRange.Lower);
        Assert.True(anyRange.CriticalHigh is null || anyRange.CriticalHigh >= anyRange.Upper);
    }

}
