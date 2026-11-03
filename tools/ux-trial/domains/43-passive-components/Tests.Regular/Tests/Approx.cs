namespace PassiveComponents.Tests.Regular;

internal static class Approx {

    /// <summary>Equal within a relative tolerance: the domain's own magnitudes span twenty-five decades.</summary>
    public static void Equal(double expected, double actual, double relativeTolerance = 1e-9) {
        Assert.True(
            Math.Abs(expected - actual) <= relativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual)),
            $"expected {expected:R}, got {actual:R}");
    }

}
