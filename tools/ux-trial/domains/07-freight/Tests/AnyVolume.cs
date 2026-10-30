// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Volume --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Volume" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVolume : IAny<Volume> {

    private readonly IAny<double> _cubicMetres;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVolume()
        : this(cubicMetres: AnyValidCubicMetres()) { }

    private static AnyDouble AnyValidCubicMetres() {
        return Any.Double().Positive().LessThanOrEqualTo(Volume.MaxCubicMetres);
    }

    private AnyVolume(IAny<double> cubicMetres) {
        _cubicMetres = cubicMetres;
    }

    /// <summary>Pins <c>cubicMetres</c> to a fixed value.</summary>
    public AnyVolume WithCubicMetres(double value) {
        return new AnyVolume(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>cubicMetres</c> from <c>AnyValidCubicMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVolume WithCubicMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVolume(configure(AnyValidCubicMetres()));
    }

    /// <summary>Produces one arbitrary <see cref="Volume" />.</summary>
    public Volume Generate() {
        return new Volume(_cubicMetres.Generate());
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
