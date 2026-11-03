// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContainerCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Freight.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ContainerCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainerCode : IAny<ContainerCode> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainerCode()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.Combine(Any.String().Alpha().InUpperCase().WithLength(3), Any.OneOf('U', 'J', 'Z'), Any.Int32().Between(0, 999_999), (owner, category, serial) => ContainerCode.Create(owner, category, serial).Value);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyContainerCode(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyContainerCode(AnyContainerCode from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainerCode WithValue(string value) {
        AnyContainerCode copy = new AnyContainerCode(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;string&gt;, IAny&lt;string&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainerCode WithValue(Func<IAny<string>, IAny<string>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainerCode copy = new AnyContainerCode(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ContainerCode" />.</summary>
    public ContainerCode Generate() {
        return new ContainerCode(_value.Generate());
    }

}
