// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Iban --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Iban" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIban : IAny<Iban> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIban()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return AnyIbanIn(Any.String().Alpha().InUpperCase().WithLength(2));
    }

    private AnyIban(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyIban WithValue(string value) {
        return new AnyIban(new FixedValue<string>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Iban" />.</summary>
    public Iban Generate() {
        return new Iban(_value.Generate());
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
