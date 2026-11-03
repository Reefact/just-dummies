// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ticker --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ticker" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTicker : IAny<Ticker> {

    private IAny<string> _symbol;

    private AnyPattern _symbolRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTicker()
        : this(symbol: AnyValidSymbol()) { }

    private static AnyPattern AnyValidSymbol() {
        // One to five capitals, then an optional share-class suffix: "A", "BRK.B".
        return Any.StringMatching(@"[A-Z]{1,5}(\.[A-Z])?");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTicker(AnyPattern symbol) {
        _symbol       = symbol;
        _symbolRecipe = symbol;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTicker(AnyTicker from) {
        _symbol       = from._symbol;
        _symbolRecipe = from._symbolRecipe;
    }

    /// <summary>Pins <c>symbol</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTicker WithSymbol(string value) {
        AnyTicker copy = new AnyTicker(this);

        copy._symbol = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>symbol</c> is drawn from — <c>AnyValidSymbol()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTicker WithSymbol(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTicker copy = new AnyTicker(this);

        copy._symbolRecipe = configure(_symbolRecipe);
        copy._symbol       = copy._symbolRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Ticker" />.</summary>
    public Ticker Generate() {
        return new Ticker(_symbol.Generate());
    }

}
