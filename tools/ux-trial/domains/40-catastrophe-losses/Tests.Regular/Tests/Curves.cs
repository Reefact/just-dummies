namespace CatastropheLosses.Tests.Regular.Tests;

/// <summary>Hand-written curves: when a loss is asserted, the curve it went through is part of the assertion.</summary>
internal static class Curves {

    /// <summary>Damage ratio equals intensity / 100, from 0 at intensity 0 to total loss at 100.</summary>
    public static VulnerabilityCurve Linear() {
        return new VulnerabilityCurve("linear", [0.0, 100.0], [0.0, 1.0]);
    }

}
