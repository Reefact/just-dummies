using JustDummies;

namespace Repro20Six;

internal static class Probe {
    internal static void Shapes() {
        _ = Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct().Generate(); // line 7: typed double pool
        _ = Any.ArrayOf(Any.Int32().OneOf(1, 2)).WithCount(3).Distinct().Generate();      // line 8: typed int pool
        _ = Any.SetOf(Any.Int32().OneOf(1, 2)).WithCount(3).Generate();                   // line 9: typed int pool, set
        _ = Any.ArrayOf(Any.OneOf(1.0, 2.0)).WithCount(3).Distinct().Generate();          // line 10: control, generic pool
        _ = Any.SetOf(Any.Char().OneOf('a', 'b')).WithCount(5).Generate();                // line 11: control, documented
    }
}
