using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, never trips its type's constructor.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawsWithoutThrowing<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            _ = generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyNir_always_builds_a_valid_Nir() {
        DrawsWithoutThrowing(Any.Nir());
    }

    [Fact]
    public void Scaffolded_AnyMolecule_always_builds_a_valid_Molecule() {
        DrawsWithoutThrowing(Any.Molecule());
    }

    [Fact]
    public void Scaffolded_AnyDosage_always_builds_a_valid_Dosage() {
        DrawsWithoutThrowing(Any.Dosage());
    }

    [Fact]
    public void Scaffolded_AnyTreatmentDuration_always_builds_a_valid_TreatmentDuration() {
        DrawsWithoutThrowing(Any.TreatmentDuration());
    }

    [Fact]
    public void Scaffolded_AnyPrescriptionLine_always_builds_a_valid_PrescriptionLine() {
        DrawsWithoutThrowing(Any.PrescriptionLine());
    }

    [Fact]
    public void Scaffolded_AnyPatient_always_builds_a_valid_Patient() {
        DrawsWithoutThrowing(Any.Patient());
    }

    [Fact]
    public void Scaffolded_AnyPrescription_always_builds_a_valid_Prescription() {
        DrawsWithoutThrowing(Any.Prescription());
    }

}
