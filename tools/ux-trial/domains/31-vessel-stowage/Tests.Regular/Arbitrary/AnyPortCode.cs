// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PortCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PortCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortCode : IAny<PortCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPortCode()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}");
    }

    private AnyPortCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyPortCode WithValue(string value) {
        return new AnyPortCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortCode WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyPortCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="PortCode" />.</summary>
    public PortCode Generate() {
        return new PortCode(_value.Generate());
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
