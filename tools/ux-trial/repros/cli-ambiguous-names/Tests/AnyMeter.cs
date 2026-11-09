// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Meter --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Ambig.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Meter" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeter : IAny<Meter> {

    private readonly IAny<string> _serial;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeter()
        : this(serial: AnyValidSerial()) { }

    private static AnyString AnyValidSerial() {
        return Any.String().NonEmpty();
    }

    private AnyMeter(IAny<string> serial) {
        _serial = serial;
    }

    /// <summary>Pins <c>serial</c> to a fixed value.</summary>
    public AnyMeter WithSerial(string value) {
        return new AnyMeter(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>serial</c> from <c>AnyValidSerial()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeter WithSerial(Func<AnyString, AnyString> configure) {
        return new AnyMeter(configure(AnyValidSerial()));
    }

    /// <summary>Produces one arbitrary <see cref="Meter" />.</summary>
    public Meter Generate() {
        return new Meter(_serial.Generate());
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
