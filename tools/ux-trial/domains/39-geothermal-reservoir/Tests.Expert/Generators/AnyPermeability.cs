// Scaffolded by `any`, then edited by hand: the scaffold drew the value uniformly in [1e-20, 1e-9] m2,
// which spans eleven decades yet lands on 1e-10..1e-9 in 90 % of the draws. Rock permeability is a
// magnitude, so the exponent is what is drawn, uniformly. It is ours; do not re-scaffold it with --force.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>A generator of arbitrary <see cref="Permeability" /> values, uniform in the decade, not in the value.</summary>
public sealed partial class AnyPermeability : IAny<Permeability> {

    private IAny<double> _squareMetres;
    private AnyDouble    _exponentRecipe;

    public AnyPermeability()
        : this(Any.Double().Between(Math.Log10(Permeability.MinimumSquareMetres), Math.Log10(Permeability.MaximumSquareMetres))) { }

    private AnyPermeability(AnyDouble exponent) {
        _exponentRecipe = exponent;
        _squareMetres   = FromExponent(exponent);
    }

    private AnyPermeability(AnyPermeability from) {
        _squareMetres   = from._squareMetres;
        _exponentRecipe = from._exponentRecipe;
    }

    private static IAny<double> FromExponent(IAny<double> exponent) {
        return exponent.As(e => Math.Min(Permeability.MaximumSquareMetres, Math.Max(Permeability.MinimumSquareMetres, Math.Pow(10.0, e))));
    }

    public AnyPermeability WithSquareMetres(double value) {
        AnyPermeability copy = new AnyPermeability(this);
        copy._squareMetres = Any.Fixed(value);
        return copy;
    }

    /// <summary>Refines the decade the permeability is drawn from: the exponent of ten, -20 to -9 in the base.</summary>
    public AnyPermeability WithDecade(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPermeability copy = new AnyPermeability(this);
        copy._exponentRecipe = configure(_exponentRecipe);
        copy._squareMetres   = FromExponent(copy._exponentRecipe);
        return copy;
    }

    public Permeability Generate() {
        return new Permeability(_squareMetres.Generate());
    }

}
