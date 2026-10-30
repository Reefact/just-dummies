using JustDummies;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

public sealed class SpecimenAndReportTests {

    private static readonly MeasurementUnit MilligramsPerDecilitre = new("mg/dL");
    private static readonly ReferenceRange  Glucose               = new(70, 99, MilligramsPerDecilitre);

    [Fact]
    public void A_specimen_received_exactly_at_the_end_of_its_stability_window_is_accepted() {
        // Arrange
        SampleId       anyId        = new AnySampleId().Generate();
        SpecimenType   anyType      = Any.Enum<SpecimenType>().Generate();
        DateTimeOffset anyCollected = Any.DateTimeOffset().Between(LaboratoryCalendar.CollectionStart, LaboratoryCalendar.ReportingEnd).Generate();

        // Act
        Specimen specimen = new(anyId, anyType, anyCollected, anyCollected + TimeSpan.FromHours(72));

        // Assert
        Assert.Equal(TimeSpan.FromHours(72), specimen.ReceivedAt - specimen.CollectedAt);
    }

    [Fact]
    public void A_specimen_received_one_second_past_its_stability_window_is_refused() {
        // Arrange
        SampleId       anyId        = new AnySampleId().Generate();
        SpecimenType   anyType      = Any.Enum<SpecimenType>().Generate();
        DateTimeOffset anyCollected = Any.DateTimeOffset().Between(LaboratoryCalendar.CollectionStart, LaboratoryCalendar.ReportingEnd).Generate();

        // Act
        Action receiving = () => new Specimen(anyId, anyType, anyCollected, anyCollected + TimeSpan.FromHours(72) + TimeSpan.FromSeconds(1));

        // Assert
        Assert.Throws<ArgumentException>(receiving);
    }

    [Fact]
    public void A_report_lists_as_abnormal_only_the_results_outside_their_range() {
        // Arrange
        Specimen            anySpecimen  = Arbitrary.Specimen().Generate();
        List<AnalyteCode>   anyAnalytes  = Any.ListOf(new AnyAnalyteCode()).WithCount(3).Distinct().Generate();
        AnyLabResult        anyGlucose   = Any.LabResult().WithSpecimen(anySpecimen).WithRange(Glucose);
        LabResult           low          = anyGlucose.WithAnalyte(anyAnalytes[0]).WithMeasurement(new Measurement(55, MilligramsPerDecilitre)).Generate();
        LabResult           normal       = anyGlucose.WithAnalyte(anyAnalytes[1]).WithMeasurement(new Measurement(85, MilligramsPerDecilitre)).Generate();
        LabResult           high         = anyGlucose.WithAnalyte(anyAnalytes[2]).WithMeasurement(new Measurement(140, MilligramsPerDecilitre)).Generate();
        LabReport           report       = new(anySpecimen.Id, [low, normal, high]);

        // Act
        IReadOnlyList<LabResult> abnormal = report.Abnormal();

        // Assert
        Assert.Equal([low, high], abnormal);
    }

    [Fact]
    public void A_report_refuses_the_same_analyte_twice() {
        // Arrange
        Specimen     anySpecimen = Arbitrary.Specimen().Generate();
        AnalyteCode  anyAnalyte  = new AnyAnalyteCode().Generate();
        AnyLabResult anyResult   = Any.LabResult().WithSpecimen(anySpecimen).WithAnalyte(anyAnalyte);

        // Act
        Action reporting = () => new LabReport(anySpecimen.Id, [anyResult.Generate(), anyResult.Generate()]);

        // Assert
        Assert.Throws<ArgumentException>(reporting);
    }

    [Fact]
    public void The_turnaround_runs_from_collection_to_the_latest_report() {
        // Arrange
        DateTimeOffset collectedAt = new(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        Specimen       specimen    = Arbitrary.Specimen().WithCollectedAt(collectedAt).WithReceivedAt(collectedAt.AddHours(2)).Generate();
        AnyLabResult   anyResult   = Any.LabResult().WithSpecimen(specimen);
        List<AnalyteCode> anyAnalytes = Any.ListOf(new AnyAnalyteCode()).WithCount(2).Distinct().Generate();
        LabResult      first       = anyResult.WithAnalyte(anyAnalytes[0]).WithReportedAt(collectedAt.AddHours(5)).Generate();
        LabResult      last        = anyResult.WithAnalyte(anyAnalytes[1]).WithReportedAt(collectedAt.AddHours(26)).Generate();
        LabReport      report      = new(specimen.Id, [first, last]);

        // Act
        TimeSpan turnaround = report.TurnaroundTime();

        // Assert
        Assert.Equal(TimeSpan.FromHours(26), turnaround);
    }

    [Fact]
    public void A_hemolysed_specimen_is_not_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality anyHemolysed = Arbitrary.SpecimenQuality().WithHemolysisIndex(101).Generate();

        // Act
        bool acceptable = anyHemolysed.AcceptableForPotassium;

        // Assert
        Assert.False(acceptable);
    }

    [Fact]
    public void A_specimen_at_the_hemolysis_limit_with_enough_volume_is_acceptable_for_potassium() {
        // Arrange
        // The volume threshold is a prerequisite of this scenario, not what it verifies.
        SpecimenQuality anyBorderline = Arbitrary.SpecimenQuality()
                                                 .WithHemolysisIndex(100)
                                                 .WithVolumeMillilitres(v => v.GreaterThanOrEqualTo(SpecimenQuality.MinimumVolumeForChemistry))
                                                 .Generate();

        // Act
        bool acceptable = anyBorderline.AcceptableForPotassium;

        // Assert
        Assert.True(acceptable);
    }

}
