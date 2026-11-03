// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TaxpayerId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TaxpayerId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTaxpayerId : IAny<TaxpayerId> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTaxpayerId()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // Eight digits, the first non-zero, then the Luhn check digit the domain computes itself.

        return Any.StringMatching("[1-9][0-9]{7}").As(firstEight => firstEight + TaxpayerId.CheckDigitFor(firstEight));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTaxpayerId(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTaxpayerId(AnyTaxpayerId from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxpayerId WithValue(string value) {
        AnyTaxpayerId copy = new AnyTaxpayerId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TaxpayerId" />.</summary>
    public TaxpayerId Generate() {
        return new TaxpayerId(_value.Generate());
    }

}
