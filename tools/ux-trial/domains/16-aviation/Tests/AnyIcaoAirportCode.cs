// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IcaoAirportCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IcaoAirportCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIcaoAirportCode : IAny<IcaoAirportCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIcaoAirportCode()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // Four upper-case ASCII letters: the constructor checks char.IsAsciiLetterUpper on each one.
        return Any.String().Alpha().InUpperCase().WithLength(4);
    }

    private AnyIcaoAirportCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyIcaoAirportCode WithValue(string value) {
        return new AnyIcaoAirportCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIcaoAirportCode WithValue(Func<AnyString, AnyString> configure) {
        return new AnyIcaoAirportCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="IcaoAirportCode" />.</summary>
    public IcaoAirportCode Generate() {
        return new IcaoAirportCode(_value.Generate());
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
