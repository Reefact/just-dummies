using JustDummies;
using JustDummies.Xunit;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

/// <summary>
/// Harness checks of the scaffolded (and hand-resolved) generators: every draw must clear the real
/// constructor. Each test draws many times on purpose; these are checks of the generators, not dummies.
/// </summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawsWithoutThrowing<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            _ = generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyCadastralReference_always_builds_a_valid_CadastralReference() {
        DrawsWithoutThrowing(Arbitrary.CadastralReference());
    }

    [Fact]
    public void Scaffolded_AnySurface_always_builds_a_valid_Surface() {
        DrawsWithoutThrowing(Arbitrary.Surface());
    }

    [Fact]
    public void Scaffolded_AnyEnergyPerformanceDiagnosis_always_builds_a_valid_EnergyPerformanceDiagnosis() {
        DrawsWithoutThrowing(Arbitrary.EnergyPerformanceDiagnosis());
    }

    [Fact]
    public void Scaffolded_AnyDocumentLink_always_builds_a_valid_DocumentLink() {
        DrawsWithoutThrowing(Arbitrary.DocumentLink());
    }

    [Fact]
    public void Scaffolded_AnyProperty_always_builds_a_valid_Property() {
        DrawsWithoutThrowing(Any.Property());
    }

    [Fact]
    public void Scaffolded_AnyRentalListing_always_builds_a_valid_RentalListing() {
        DrawsWithoutThrowing(Arbitrary.RentalListing());
    }

    [Fact]
    public void Scaffolded_AnyTransfer_always_builds_a_valid_Transfer() {
        DrawsWithoutThrowing(Arbitrary.Transfer());
    }
}
