// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Porosity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Porosity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPorosity : IAny<Porosity> {

    private readonly IAny<double> _fraction;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPorosity()
        : this(fraction: AnyValidFraction()) { }

    private static AnyDouble AnyValidFraction() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Porosity.Maximum);
    }

    private AnyPorosity(IAny<double> fraction) {
        _fraction = fraction;
    }

    /// <summary>Pins <c>fraction</c> to a fixed value.</summary>
    public AnyPorosity WithFraction(double value) {
        return new AnyPorosity(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>fraction</c> from <c>AnyValidFraction()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPorosity WithFraction(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPorosity(configure(AnyValidFraction()));
    }

    /// <summary>Produces one arbitrary <see cref="Porosity" />.</summary>
    public Porosity Generate() {
        return new Porosity(_fraction.Generate());
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
