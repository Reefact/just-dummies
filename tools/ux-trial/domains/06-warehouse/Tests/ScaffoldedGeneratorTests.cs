using JustDummies;
using JustDummies.Xunit;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

/// <summary>Harness checks: every scaffolded generator, as resolved, builds a valid value on every draw.</summary>
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AssertAlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            T value = generator.Generate();
            Assert.NotNull(value);
        }
    }

    [Fact, Reproducible]
    public void Scaffolded_AnySscc_always_builds_a_valid_Sscc() => AssertAlwaysBuilds(new AnySscc());

    [Fact, Reproducible]
    public void Scaffolded_AnyEan13_always_builds_a_valid_Ean13() => AssertAlwaysBuilds(new AnyEan13());

    [Fact, Reproducible]
    public void Scaffolded_AnyBinLocation_always_builds_a_valid_BinLocation() => AssertAlwaysBuilds(new AnyBinLocation());

    [Fact, Reproducible]
    public void Scaffolded_AnyQuantity_always_builds_a_valid_Quantity() => AssertAlwaysBuilds(new AnyQuantity());

    [Fact, Reproducible]
    public void Scaffolded_AnyLotNumber_always_builds_a_valid_LotNumber() => AssertAlwaysBuilds(new AnyLotNumber());

    [Fact, Reproducible]
    public void Scaffolded_AnyTemperatureRange_always_builds_a_valid_TemperatureRange() => AssertAlwaysBuilds(new AnyTemperatureRange());

    [Fact, Reproducible]
    public void Scaffolded_AnyReceiptLine_always_builds_a_valid_ReceiptLine() => AssertAlwaysBuilds(new AnyReceiptLine());

    [Fact, Reproducible]
    public void Scaffolded_AnyPalletReceipt_always_builds_a_valid_PalletReceipt() => AssertAlwaysBuilds(new AnyPalletReceipt());

}
