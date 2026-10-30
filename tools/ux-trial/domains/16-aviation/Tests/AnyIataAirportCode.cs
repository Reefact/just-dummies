// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IataAirportCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IataAirportCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIataAirportCode : IAny<IataAirportCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIataAirportCode()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // Three upper-case ASCII letters, as the constructor's ^[A-Z]{3}$ demands.
        return Any.String().Alpha().InUpperCase().WithLength(3);
    }

    private AnyIataAirportCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyIataAirportCode WithValue(string value) {
        return new AnyIataAirportCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIataAirportCode WithValue(Func<AnyString, AnyString> configure) {
        return new AnyIataAirportCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="IataAirportCode" />.</summary>
    public IataAirportCode Generate() {
        return new IataAirportCode(_value.Generate());
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
