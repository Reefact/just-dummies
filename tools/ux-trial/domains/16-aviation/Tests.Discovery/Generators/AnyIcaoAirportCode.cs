// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IcaoAirportCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IcaoAirportCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIcaoAirportCode : IAny<IcaoAirportCode> {

    private IAny<string> _value;

    private AnyString _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIcaoAirportCode()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        return Any.String().Alpha().InUpperCase().WithLength(4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIcaoAirportCode(AnyString value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIcaoAirportCode(AnyIcaoAirportCode from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIcaoAirportCode WithValue(string value) {
        AnyIcaoAirportCode copy = new AnyIcaoAirportCode(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIcaoAirportCode WithValue(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIcaoAirportCode copy = new AnyIcaoAirportCode(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="IcaoAirportCode" />.</summary>
    public IcaoAirportCode Generate() {
        return new IcaoAirportCode(_value.Generate());
    }

}
