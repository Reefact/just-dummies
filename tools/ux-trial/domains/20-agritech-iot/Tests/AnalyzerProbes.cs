using JustDummies;

namespace AgriTech.Tests;

/// <summary>Chains written to see which JDxxx rules fire on domain-specific shapes. Never executed.</summary>
internal static class AnalyzerProbes {

    internal static void Shapes() {
        _ = Any.Double().Between(90.0, -90.0).Generate();                          // crossed constant bounds
        _ = Any.Double().Between(double.NaN, 90.0).Generate();                     // NaN bound
        _ = Any.Double().LessThan(double.PositiveInfinity).Generate();             // infinite bound
        _ = Any.Double().OneOf(0.0, -0.0).Generate();                              // equal values, two spellings
        _ = Any.Double().OneOf(90.0, 90.0).Generate();                             // literal duplicate
        _ = Any.Double().GreaterThan(90.0).LessThan(-90.0).Generate();             // empty double chain
        _ = Any.Double().GreaterThan(double.MaxValue).Generate();                  // no double above MaxValue
        _ = Any.Single().Between(-40f, 85f).Except(-40f).Except(-40f).Generate(); // repeated exclusion
        _ = Any.ArrayOf(Any.Double()).WithCount(1_000_001).Generate();             // above the cap
        _ = Any.ArrayOf(Any.Double()).WithMaxCount(2_000_000).Generate();          // maximum above the cap
        _ = Any.SetOf(Any.Half().Between((Half)0, (Half)0.001)).WithCount(10_000).Generate(); // more distinct than Half holds
        _ = Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct().Generate();     // 3 distinct of 2
        _ = Any.Double().Between(-90.0, 90.0).Between(-45.0, 45.0).Generate();     // bound declared twice
        _ = Any.Double().GreaterThanOrEqualTo(-180.0).LessThanOrEqualTo(180.0).Generate(); // range form
        _ = Any.Double().Between(1.0, 2.0).Zero().Generate();                      // Zero outside the range
        _ = Any.ArrayOf(Any.Int32().OneOf(1, 2)).WithCount(3).Distinct().Generate();          // 3 distinct of 2 (int)
        _ = Any.ArrayOf(Any.OneOf(1.0, 2.0)).WithCount(3).Distinct().Generate();             // 3 distinct of 2 (generic pool)
        _ = Any.Single().OneOf(1f, float.NaN).Generate();                                    // documented JD014 case
        _ = Any.Half().OneOf((Half)1, Half.NaN).Generate();                                  // same on Half
        _ = Any.Double().LessThanOrEqualTo(double.NaN).Generate();                           // NaN ceiling
    }

}
