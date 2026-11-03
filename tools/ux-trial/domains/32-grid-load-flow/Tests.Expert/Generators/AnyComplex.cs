using System.Numerics;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>A complex power or voltage whose two parts are finite and of an ordinary magnitude.</summary>
public sealed class AnyComplex : IAny<Complex> {

    private readonly IAny<Complex> _complex = Any.Combine(Any.Double().Between(-1_000d, 1_000d),
                                                          Any.Double().Between(-1_000d, 1_000d),
                                                          (real, imaginary) => new Complex(real, imaginary));

    public Complex Generate() {
        return _complex.Generate();
    }

}
