using System;

using JustDummies;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>
///     The values of this domain span up to twenty-five decades. A range bounded on both sides is drawn
///     uniformly in value, so [1e-13, 1e-2] would put nine draws in ten above 1e-3 and never meet a
///     picofarad: the decade is drawn instead, uniformly, and the value follows from it.
/// </summary>
public static class LogScale {

    public static IAny<double> Between(double min, double max) {
        return Any.Double()
                  .Between(Math.Log10(min), Math.Log10(max))
                  .As(exponent => Math.Clamp(Math.Pow(10, exponent), min, max));
    }

}
