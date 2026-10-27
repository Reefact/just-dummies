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

    [Fact, Reproducible]
    public void Scaffolded_AnyReceiptStatus_always_builds_a_declared_ReceiptStatus() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.True(Enum.IsDefined(new AnyReceiptStatus().Generate()));
        }
    }

    // Bug: `any generate PalletType` emits `return new PalletType();`, i.e. (PalletType)0, which is not a
    // declared member of an enum numbered from 1. The generator never varies and never draws a real member.
    [Fact, Reproducible]
    public void Bug_Scaffolded_AnyPalletType_draws_an_undeclared_member() {
        PalletType drawn = new AnyPalletType().Generate();

        Assert.True(Enum.IsDefined(drawn), $"AnyPalletType drew {(int)drawn}, which PalletType does not declare.");
    }

    // Bug: the same emission for an enum whose zero is declared compiles, passes the check above, and yet
    // draws one member forever: the "generator of arbitrary ReceiptStatus values" is a constant.
    [Fact, Reproducible]
    public void Bug_Scaffolded_AnyReceiptStatus_never_varies() {
        HashSet<ReceiptStatus> seen = [];
        for (int draw = 0; draw < Draws; draw++) {
            seen.Add(new AnyReceiptStatus().Generate());
        }

        Assert.True(seen.Count > 1, $"500 draws produced only {string.Join(", ", seen)}.");
    }

    // Bug: the recipe the scaffolder emitted for BinLocation's level, verbatim (`Any.Int32()`, recap column
    // empty): the guard is a switch expression after the first assignment to state, and is skipped silently.
    // AnyBinLocation.cs has been fixed by hand; this replays the emitted recipe against the real constructor.
    [Fact, Reproducible]
    public void Bug_Scaffolded_level_recipe_for_BinLocation_ignores_its_guard() {
        AnyInt32 emittedLevelRecipe = Any.Int32();

        for (int draw = 0; draw < Draws; draw++) {
            int anyLevel = emittedLevelRecipe.Generate();
            BinLocation bin = new BinLocation('A', 1, anyLevel);
            Assert.NotNull(bin);
        }
    }

}
