// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Imei --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Imei" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyImei : IAny<Imei> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyImei()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // 14 digits, then the Luhn check digit computed here, independently of Imei.LuhnCheckDigit,
        // so a defect in the domain's own computation cannot hide behind the generator.
        return Any.StringMatching("[0-9]{14}").As(body => body + CheckDigit(body));
    }

    private static char CheckDigit(string body) {
        int sum = 0;
        for (int i = 0; i < body.Length; i++) {
            int digit = body[body.Length - 1 - i] - '0';
            if (i % 2 == 0) {
                digit *= 2;
                if (digit > 9) { digit -= 9; }
            }
            sum += digit;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyImei(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyImei(AnyImei from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyImei WithValue(string value) {
        AnyImei copy = new AnyImei(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Imei" />.</summary>
    public Imei Generate() {
        return new Imei(_value.Generate());
    }

}
