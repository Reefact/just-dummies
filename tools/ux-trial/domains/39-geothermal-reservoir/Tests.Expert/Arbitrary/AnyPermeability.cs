// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Permeability --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Permeability" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPermeability : IAny<Permeability> {

    private readonly IAny<double> _squareMetres;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPermeability()
        : this(squareMetres: AnyValidSquareMetres()) { }

    private static AnyDouble AnyValidSquareMetres() {
        // From tight rock to gravel, as Permeability declares it.
        return Any.Double().Between(Permeability.MinimumSquareMetres, Permeability.MaximumSquareMetres);
    }

    private AnyPermeability(IAny<double> squareMetres) {
        _squareMetres = squareMetres;
    }

    /// <summary>Pins <c>squareMetres</c> to a fixed value.</summary>
    public AnyPermeability WithSquareMetres(double value) {
        return new AnyPermeability(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>squareMetres</c> from <c>AnyValidSquareMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPermeability WithSquareMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPermeability(configure(AnyValidSquareMetres()));
    }

    /// <summary>Produces one arbitrary <see cref="Permeability" />.</summary>
    public Permeability Generate() {
        return new Permeability(_squareMetres.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
