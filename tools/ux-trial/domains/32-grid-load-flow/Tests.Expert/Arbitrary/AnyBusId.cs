// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BusId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BusId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBusId : IAny<BusId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBusId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // BusId.Format, copied as written: an upper-case letter, then up to 15 of [A-Z0-9_-].
        return Any.StringMatching("^[A-Z][A-Z0-9_-]{0,15}$");
    }

    private AnyBusId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyBusId WithValue(string value) {
        return new AnyBusId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBusId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyBusId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="BusId" />.</summary>
    public BusId Generate() {
        return new BusId(_value.Generate());
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
