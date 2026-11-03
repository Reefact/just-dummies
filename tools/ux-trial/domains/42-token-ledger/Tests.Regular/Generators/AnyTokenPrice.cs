// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenPrice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TokenPrice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenPrice : IAny<TokenPrice> {

    private IAny<ExactDecimal> _perToken;
    private IAny<string>       _quoteCurrency;

    private AnyExactDecimal _perTokenRecipe;
    private AnyPattern       _quoteCurrencyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTokenPrice()
        : this(perToken:      AnyValidPerToken(),
               quoteCurrency: AnyValidQuoteCurrency()) { }

    private static AnyExactDecimal AnyValidPerToken() {
        // A price is positive and carries at most 18 fractional digits.
        return new AnyExactDecimal().WithMantissa(m => m.GreaterThanOrEqualTo(1)).WithScale(s => s.LessThanOrEqualTo(TokenPrice.MaxPriceScale));
    }

    private static AnyPattern AnyValidQuoteCurrency() {
        return Any.StringMatching("[A-Z]{3}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTokenPrice(AnyExactDecimal perToken,
                          AnyPattern       quoteCurrency) {
        _perToken            = perToken;
        _perTokenRecipe      = perToken;
        _quoteCurrency       = quoteCurrency;
        _quoteCurrencyRecipe = quoteCurrency;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTokenPrice(AnyTokenPrice from) {
        _perToken            = from._perToken;
        _perTokenRecipe      = from._perTokenRecipe;
        _quoteCurrency       = from._quoteCurrency;
        _quoteCurrencyRecipe = from._quoteCurrencyRecipe;
    }

    /// <summary>Pins <c>perToken</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenPrice WithPerToken(ExactDecimal value) {
        AnyTokenPrice copy = new AnyTokenPrice(this);

        copy._perToken = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>perToken</c> is drawn from — <c>AnyValidPerToken()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyExactDecimal, AnyExactDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenPrice WithPerToken(Func<AnyExactDecimal, AnyExactDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenPrice copy = new AnyTokenPrice(this);

        copy._perTokenRecipe = configure(_perTokenRecipe);
        copy._perToken       = copy._perTokenRecipe;

        return copy;
    }

    /// <summary>Pins <c>quoteCurrency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenPrice WithQuoteCurrency(string value) {
        AnyTokenPrice copy = new AnyTokenPrice(this);

        copy._quoteCurrency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quoteCurrency</c> is drawn from — <c>AnyValidQuoteCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenPrice WithQuoteCurrency(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenPrice copy = new AnyTokenPrice(this);

        copy._quoteCurrencyRecipe = configure(_quoteCurrencyRecipe);
        copy._quoteCurrency       = copy._quoteCurrencyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TokenPrice" />.</summary>
    public TokenPrice Generate() {
        return new TokenPrice(_perToken.Generate(),
                              _quoteCurrency.Generate());
    }

}
