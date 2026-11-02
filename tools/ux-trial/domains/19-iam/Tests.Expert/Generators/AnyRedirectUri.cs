// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RedirectUri --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Identity.Domain;
using JustDummies;

namespace Identity.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="RedirectUri" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRedirectUri : IAny<RedirectUri> {

    private IAny<Uri> _value;

    private AnyWebUri _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRedirectUri()
        : this(value: AnyValidValue()) { }

    private static AnyWebUri AnyValidValue() {
        return Any.Uri().Web().UsingHttps();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRedirectUri(AnyWebUri value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRedirectUri(AnyRedirectUri from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRedirectUri WithValue(Uri value) {
        AnyRedirectUri copy = new AnyRedirectUri(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWebUri, AnyWebUri&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRedirectUri WithValue(Func<AnyWebUri, AnyWebUri> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRedirectUri copy = new AnyRedirectUri(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RedirectUri" />.</summary>
    public RedirectUri Generate() {
        return new RedirectUri(_value.Generate());
    }

}
