// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Portfolio --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Portfolio" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortfolio : IAny<Portfolio> {

    private IAny<string> _name;
    private IAny<string> _currency;

    private AnyString _nameRecipe;
    private AnyPattern _currencyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPortfolio()
        : this(name:     AnyValidName(),
               currency: AnyValidCurrency()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(100);
    }

    private static AnyPattern AnyValidCurrency() {
        return Any.StringMatching("[A-Z]{3}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPortfolio(AnyString name,
                         AnyPattern currency) {
        _name           = name;
        _nameRecipe     = name;
        _currency       = currency;
        _currencyRecipe = currency;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPortfolio(AnyPortfolio from) {
        _name           = from._name;
        _nameRecipe     = from._nameRecipe;
        _currency       = from._currency;
        _currencyRecipe = from._currencyRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPortfolio WithName(string value) {
        AnyPortfolio copy = new AnyPortfolio(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortfolio WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortfolio copy = new AnyPortfolio(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPortfolio WithCurrency(string value) {
        AnyPortfolio copy = new AnyPortfolio(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortfolio WithCurrency(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortfolio copy = new AnyPortfolio(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Portfolio" />.</summary>
    public Portfolio Generate() {
        return new Portfolio(_name.Generate(),
                             _currency.Generate());
    }

}
