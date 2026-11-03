// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ScreeningNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ScreeningNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScreeningNumber : IAny<ScreeningNumber> {

    private IAny<string> _value;

    private AnyPattern _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScreeningNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // CCsss-nnnn: two upper-case letters, a site 001-999, a subject sequence 0001-9999 (000 and 0000 are never issued).
        return Any.StringMatching("[A-Z]{2}(00[1-9]|0[1-9][0-9]|[1-9][0-9]{2})-(000[1-9]|00[1-9][0-9]|0[1-9][0-9]{2}|[1-9][0-9]{3})");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyScreeningNumber(AnyPattern value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyScreeningNumber(AnyScreeningNumber from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScreeningNumber WithValue(string value) {
        AnyScreeningNumber copy = new AnyScreeningNumber(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScreeningNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScreeningNumber copy = new AnyScreeningNumber(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ScreeningNumber" />.</summary>
    public ScreeningNumber Generate() {
        return new ScreeningNumber(_value.Generate());
    }

}
