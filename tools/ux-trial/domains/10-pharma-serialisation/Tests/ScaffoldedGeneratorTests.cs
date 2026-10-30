using JustDummies;
using JustDummies.Xunit;
using Serialisation.Domain;
using Xunit;

namespace Serialisation.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, must never trip its constructor.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawMany<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyGtin_always_builds_a_valid_Gtin() => DrawMany(new AnyGtin());

    [Fact]
    public void Scaffolded_AnyBatchNumber_always_builds_a_valid_BatchNumber() => DrawMany(new AnyBatchNumber());

    [Fact]
    public void Scaffolded_AnySerialNumber_always_builds_a_valid_SerialNumber() => DrawMany(new AnySerialNumber());

    [Fact]
    public void Scaffolded_AnyReimbursementPrice_always_builds_a_valid_ReimbursementPrice() => DrawMany(new AnyReimbursementPrice());

    [Fact]
    public void Scaffolded_AnySerialisedPack_always_builds_a_valid_SerialisedPack() => DrawMany(new AnySerialisedPack());

    [Fact]
    public void Scaffolded_AnyBatch_always_builds_a_valid_Batch() => DrawMany(new AnyBatch());

    [Fact]
    public void Scaffolded_AnyShipment_always_builds_a_valid_Shipment() => DrawMany(new AnyShipment());

    [Fact]
    public void Scaffolded_AnyShelfLife_always_builds_a_valid_ShelfLife() => DrawMany(new AnyShelfLife());

    [Fact]
    public void Scaffolded_AnyRecallNotice_always_builds_a_valid_RecallNotice() => DrawMany(new AnyRecallNotice());

    [Fact]
    public void Handwritten_ExpiryMonth_always_builds_a_valid_ExpiryMonth() => DrawMany(Arbitrary.ExpiryMonth());

}
