using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

/// <summary>Code the analyzers should catch. Not run: these methods are not tests.</summary>
internal static class AnalyzerProbes {

    // Expected JD023: an integer chain narrowed to nothing, every argument constant.
    public static int ContradictoryConstantLevel() {
        return Any.Int32().Between(1, 20).GreaterThan(30).Generate();
    }

    // Expected JD028: LotNumber has no value equality, so Distinct() constrains nothing.
    public static List<LotNumber> InertDistinctLots() {
        return Any.ListOf(new AnyLotNumber()).WithCount(5).Distinct().Generate();
    }

    // Expected JD013: a held list handed to Any.OneOf is a pool of one list.
    public static List<string> HeldListInOneOf() {
        List<string> suppliers = ["SUP-00001", "SUP-00002"];

        return Any.OneOf(suppliers).Generate();
    }

    // Expected JD025: a duplicated pool value.
    public static string DuplicatedSupplier() {
        return Any.String().OneOf("SUP-00001", "SUP-00001", "SUP-00002").Generate();
    }

    // Expected JD027: an operand drawn and thrown away.
    public static Quantity UnusedOperand() {
        return Any.Combine(Any.Int32().Between(0, 10), new AnySscc(), (units, sscc) => new Quantity(units)).Generate();
    }

    // Expected JD028, the documented shape: the element generator is an .As(...) over a constrained primitive.
    public static List<LotNumber> InertDistinctLotsThroughAs() {
        return Any.ListOf(Any.String().Alpha().WithLength(4).As(value => new LotNumber(value))).WithCount(5).Distinct().Generate();
    }

    // Expected JD004 (an error by default): a seed scope whose handle is discarded. Fired; disabled so the project builds.
#pragma warning disable JD004
    public static void DiscardedSeedScope() {
        Any.UseSeed(42);
    }
#pragma warning restore JD004

    // Expected JD016: more distinct members than the enum declares, constant count.
    public static HashSet<PalletType> TooManyPalletTypes() {
        return Any.SetOf(Any.Enum<PalletType>()).WithCount(4).Generate();
    }

    // Expected JD017: an exclusion that empties the enum.
    public static PalletType NoPalletTypeLeft() {
        return Any.Enum<PalletType>().Except(PalletType.Euro, PalletType.Industrial, PalletType.HalfEuro).Generate();
    }

    // Expected JD015: a fixed length that cannot hold the prefix.
    public static string SupplierTooShort() {
        return Any.String().StartingWith("SUP-").WithLength(3).Generate();
    }

    // No rule exists for this (the fundamental the library is built on): a drawn value reaching the assertion.
    public static void DrawnValueInTheAssertion() {
        int anyUnits = Any.Int32().Between(0, Quantity.Maximum).Generate();

        Quantity quantity = new(anyUnits);

        Assert.Equal(anyUnits, quantity.Units);
    }

    // Expected JD024 or JD032? A bound declared twice / a constraint with no effect.
    public static int RedundantBounds() {
        return Any.Int32().Between(1, 20).GreaterThanOrEqualTo(1).Generate();
    }

    // Expected JD015 for a string pool whose every value a constraint rejects.
    public static string PoolEmptiedByLength() {
        return Any.String().OneOf("SUP-1", "SUP-2").WithLength(9).Generate();
    }

}
