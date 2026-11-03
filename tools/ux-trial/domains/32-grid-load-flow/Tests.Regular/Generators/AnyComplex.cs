using System;
using System.Numerics;

using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>A generator of finite <see cref="Complex" /> values, written by hand: <c>any</c> has no scaffold for a type of the framework.</summary>
public sealed class AnyComplex : IAny<Complex> {

    private readonly AnyDouble _real;
    private readonly AnyDouble _imaginary;

    public AnyComplex()
        : this(Any.Double().Between(-10_000d, 10_000d), Any.Double().Between(-10_000d, 10_000d)) { }

    private AnyComplex(AnyDouble real, AnyDouble imaginary) {
        _real      = real;
        _imaginary = imaginary;
    }

    public AnyComplex WithReal(Func<AnyDouble, AnyDouble> configure) {
        return new AnyComplex(configure(_real), _imaginary);
    }

    public AnyComplex WithImaginary(Func<AnyDouble, AnyDouble> configure) {
        return new AnyComplex(_real, configure(_imaginary));
    }

    public Complex Generate() {
        return new Complex(_real.Generate(), _imaginary.Generate());
    }

}
