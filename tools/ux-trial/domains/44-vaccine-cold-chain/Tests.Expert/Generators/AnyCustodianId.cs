// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CustodianId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using ColdChain.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CustodianId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCustodianId : IAny<CustodianId> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCustodianId()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.Combine(Any.String().Alpha().InUpperCase().WithLength(2), Any.Int32().Between(1, 99_999), (country, licence) => $"{country}-GDP-{licence:D5}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCustodianId(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCustodianId(AnyCustodianId from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCustodianId WithValue(string value) {
        AnyCustodianId copy = new AnyCustodianId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;string&gt;, IAny&lt;string&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCustodianId WithValue(Func<IAny<string>, IAny<string>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCustodianId copy = new AnyCustodianId(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CustodianId" />.</summary>
    public CustodianId Generate() {
        return new CustodianId(_value.Generate());
    }

}
