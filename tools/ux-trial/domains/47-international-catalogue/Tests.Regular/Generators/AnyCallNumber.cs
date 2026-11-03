// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CallNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CallNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCallNumber : IAny<CallNumber> {

    private IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCallNumber()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[0-9]{3}(\\.[0-9]{1,8})? [A-Z][0-9]{1,4}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCallNumber(IAny<string> value) {
        _value       = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCallNumber(AnyCallNumber from) {
        _value       = from._value;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCallNumber WithValue(string value) {
        AnyCallNumber copy = new AnyCallNumber(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CallNumber" />.</summary>
    public CallNumber Generate() {
        return new CallNumber(_value.Generate());
    }

}
