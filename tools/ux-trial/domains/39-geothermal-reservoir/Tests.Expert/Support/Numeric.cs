using Xunit;

namespace GeothermalReservoir.Tests.Expert.Support;

internal static class Numeric {

    /// <summary>Asserts two doubles agree to a relative tolerance, with an absolute floor for values near zero.</summary>
    public static void AssertClose(double expected, double actual, double relativeTolerance = 1e-9, double absoluteFloor = 0.0) {
        double allowed = Math.Max(absoluteFloor, relativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual)));

        Assert.True(Math.Abs(expected - actual) <= allowed,
                    $"Expected {expected:R} but got {actual:R} (difference {Math.Abs(expected - actual):R}, allowed {allowed:R}).");
    }

}
