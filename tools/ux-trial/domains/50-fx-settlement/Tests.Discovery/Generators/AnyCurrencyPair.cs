// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CurrencyPair --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CurrencyPair" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCurrencyPair : IAny<CurrencyPair> {

    private IAny<Currency> _base;
    private IAny<Currency> _quote;

    private AnyCurrency _baseRecipe;
    private AnyCurrency _quoteRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCurrencyPair()
        : this(@base: AnyValidBase(),
               quote: AnyValidQuote()) { }

    private static AnyCurrency AnyValidBase() {
        return new AnyCurrency();
    }

    private static AnyCurrency AnyValidQuote() {
        return new AnyCurrency();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCurrencyPair(AnyCurrency @base,
                            AnyCurrency quote) {
        _base        = @base;
        _baseRecipe  = @base;
        _quote       = quote;
        _quoteRecipe = quote;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCurrencyPair(AnyCurrencyPair from) {
        _base        = from._base;
        _baseRecipe  = from._baseRecipe;
        _quote       = from._quote;
        _quoteRecipe = from._quoteRecipe;
    }

    /// <summary>Pins <c>base</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCurrencyPair WithBase(Currency value) {
        AnyCurrencyPair copy = new AnyCurrencyPair(this);

        copy._base = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>base</c> is drawn from — <c>AnyValidBase()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCurrencyPair WithBase(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCurrencyPair copy = new AnyCurrencyPair(this);

        copy._baseRecipe = configure(_baseRecipe);
        copy._base       = copy._baseRecipe;

        return copy;
    }

    /// <summary>Pins <c>quote</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCurrencyPair WithQuote(Currency value) {
        AnyCurrencyPair copy = new AnyCurrencyPair(this);

        copy._quote = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quote</c> is drawn from — <c>AnyValidQuote()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCurrencyPair WithQuote(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCurrencyPair copy = new AnyCurrencyPair(this);

        copy._quoteRecipe = configure(_quoteRecipe);
        copy._quote       = copy._quoteRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CurrencyPair" />.</summary>
    public CurrencyPair Generate() {
        bool basePinned  = !ReferenceEquals(_base, _baseRecipe);
        bool quotePinned = !ReferenceEquals(_quote, _quoteRecipe);

        // The two codes differ: whichever side a test did not pin is drawn with a code other than the other side's.
        Currency @base;
        Currency quote;
        if (quotePinned) {
            quote  = _quote.Generate();
            @base  = basePinned ? _base.Generate() : _baseRecipe.WithCode(c => c.DifferentFrom(quote.Code)).Generate();
        } else {
            @base = _base.Generate();
            quote = _quoteRecipe.WithCode(c => c.DifferentFrom(@base.Code)).Generate();
        }

        return new CurrencyPair(@base, quote);
    }

}
