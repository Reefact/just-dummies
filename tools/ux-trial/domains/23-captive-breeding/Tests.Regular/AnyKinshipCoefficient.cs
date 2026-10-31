// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KinshipCoefficient --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="KinshipCoefficient" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKinshipCoefficient : IAny<KinshipCoefficient> {

    private readonly IAny<double> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKinshipCoefficient()
        : this(value: AnyValidValue()) { }

    private static AnyDouble AnyValidValue() {
        return Any.Double().Between(0.0, 1.0);
    }

    private AnyKinshipCoefficient(IAny<double> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyKinshipCoefficient WithValue(double value) {
        return new AnyKinshipCoefficient(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKinshipCoefficient WithValue(Func<AnyDouble, AnyDouble> configure) {
        return new AnyKinshipCoefficient(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="KinshipCoefficient" />.</summary>
    public KinshipCoefficient Generate() {
        return new KinshipCoefficient(_value.Generate());
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
