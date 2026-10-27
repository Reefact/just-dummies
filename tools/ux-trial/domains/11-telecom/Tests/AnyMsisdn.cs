// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Msisdn --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Msisdn" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMsisdn : IAny<Msisdn> {

    private readonly IAny<string> _digits;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMsisdn()
        : this(digits: AnyValidDigits()) { }

    private static AnyPattern AnyValidDigits() {
        // Msisdn's guard: ^[1-9][0-9]{7,14}$ (E.164 digits, no leading zero).
        return Any.StringMatching(@"[1-9][0-9]{7,14}");
    }

    private AnyMsisdn(IAny<string> digits) {
        _digits = digits;
    }

    /// <summary>Pins <c>digits</c> to a fixed value.</summary>
    public AnyMsisdn WithDigits(string value) {
        return new AnyMsisdn(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>digits</c> from <c>AnyValidDigits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMsisdn WithDigits(Func<AnyPattern, AnyPattern> configure) {
        return new AnyMsisdn(configure(AnyValidDigits()));
    }

    /// <summary>Produces one arbitrary <see cref="Msisdn" />.</summary>
    public Msisdn Generate() {
        return new Msisdn(_digits.Generate());
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
