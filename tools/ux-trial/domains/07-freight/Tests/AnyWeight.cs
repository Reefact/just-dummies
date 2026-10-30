// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Weight --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Weight" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWeight : IAny<Weight> {

    private readonly IAny<double> _kilograms;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWeight()
        : this(kilograms: AnyValidKilograms()) { }

    private static AnyDouble AnyValidKilograms() {
        return Any.Double().Positive().LessThanOrEqualTo(32500d);
    }

    private AnyWeight(IAny<double> kilograms) {
        _kilograms = kilograms;
    }

    /// <summary>Pins <c>kilograms</c> to a fixed value.</summary>
    public AnyWeight WithKilograms(double value) {
        return new AnyWeight(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>kilograms</c> from <c>AnyValidKilograms()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWeight WithKilograms(Func<AnyDouble, AnyDouble> configure) {
        return new AnyWeight(configure(AnyValidKilograms()));
    }

    /// <summary>Produces one arbitrary <see cref="Weight" />.</summary>
    public Weight Generate() {
        return new Weight(_kilograms.Generate());
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
