using JustDummies;

namespace Repro20Five;

internal static class Probe {
    internal static void Shapes() {
        _ = Any.Double().Between(double.NaN, 90.0).Generate();             // line 7: NaN bound
        _ = Any.Double().LessThan(double.PositiveInfinity).Generate();     // line 8: infinite bound
        _ = Any.Double().LessThanOrEqualTo(double.NaN).Generate();         // line 9: NaN ceiling
        _ = Any.Half().OneOf((Half)1, Half.NaN).Generate();                // line 10: Half non-finite pool value
        _ = Any.Single().OneOf(1f, float.NaN).Generate();                  // line 11: control, documented JD014
        _ = Any.Double().Between(90.0, -90.0).Generate();                  // line 12: control, transposed
    }
}
