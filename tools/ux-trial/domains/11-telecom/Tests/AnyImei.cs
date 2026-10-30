// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Imei --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Imei" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyImei : IAny<Imei> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyImei()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // Imei's guard: 15 digits, the last a Luhn check digit over the first 14.
        return Any.StringMatching(@"[0-9]{14}").As(body => body + Telecom.Domain.Imei.LuhnCheckDigit(body));
    }

    private AnyImei(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyImei WithValue(string value) {
        return new AnyImei(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyImei WithValue(Func<IAny<string>, IAny<string>> configure) {
        return new AnyImei(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Imei" />.</summary>
    public Imei Generate() {
        return new Imei(_value.Generate());
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
