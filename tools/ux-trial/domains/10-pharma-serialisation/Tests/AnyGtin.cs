// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Gtin --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Gtin" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGtin : IAny<Gtin> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGtin()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // Hand-written: 14 digits whose last one is the GS1 mod-10 check digit of the first 13.
        // A check digit is not a regular-language property, so the 13-digit body is drawn and the
        // domain's own factory appends the digit.
        return Any.StringMatching(@"\d{13}").As(body => Gtin.FromBody(body).Value);
    }

    private AnyGtin(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyGtin WithValue(string value) {
        return new AnyGtin(new FixedValue<string>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Gtin" />.</summary>
    public Gtin Generate() {
        return new Gtin(_value.Generate());
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
