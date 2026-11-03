// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Iban --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Iban" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIban : IAny<Iban> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIban()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return AnyValidValueIn(Any.StringMatching("[A-Z]{2}"));
    }

    // The check digits are derived from the drawn country and BBAN, never drawn.
    internal static IAny<string> AnyValidValueIn(IAny<string> country) {
        return Any.Combine(country,
                           Any.StringMatching("[A-Z0-9]{11,30}"),
                           (c, bban) => c + Iban.ComputeCheckDigits(c, bban) + bban);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIban(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIban(AnyIban from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIban WithValue(string value) {
        AnyIban copy = new AnyIban(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Iban" />.</summary>
    public Iban Generate() {
        return new Iban(_value.Generate());
    }

}
