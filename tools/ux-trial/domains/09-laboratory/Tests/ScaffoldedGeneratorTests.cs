using JustDummies;
using JustDummies.Xunit;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

/// <summary>Harness: each scaffolded generator, drawn 500 times, must never make its constructor throw.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawMany<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyAnalyteCode_always_builds_a_valid_AnalyteCode() => DrawMany(new AnyAnalyteCode());

    [Fact]
    public void Scaffolded_AnyMeasurementUnit_always_builds_a_valid_MeasurementUnit() => DrawMany(new AnyMeasurementUnit());

    [Fact]
    public void Scaffolded_AnySampleId_always_builds_a_valid_SampleId() => DrawMany(new AnySampleId());

    [Fact]
    public void Scaffolded_AnyMeasurement_always_builds_a_valid_Measurement() => DrawMany(new AnyMeasurement());

    [Fact]
    public void Scaffolded_AnyDilutionFactor_always_builds_a_valid_DilutionFactor() => DrawMany(new AnyDilutionFactor());

    [Fact]
    public void Scaffolded_AnyReferenceRange_always_builds_a_valid_ReferenceRange() => DrawMany(new AnyReferenceRange());

    [Fact]
    public void Scaffolded_AnySpecimenQuality_always_builds_a_valid_SpecimenQuality() => DrawMany(new AnySpecimenQuality());

    [Fact]
    public void Scaffolded_AnySpecimen_always_builds_a_valid_Specimen() => DrawMany(new AnySpecimen());

    [Fact]
    public void Scaffolded_AnyLabResult_always_builds_a_valid_LabResult() => DrawMany(new AnyLabResult());

    [Fact]
    public void Scaffolded_AnyLabReport_always_builds_a_valid_LabReport() => DrawMany(new AnyLabReport());

}
