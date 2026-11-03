// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LiquidityProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LiquidityProfile" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLiquidityProfile : IAny<LiquidityProfile> {

    private IAny<string>                   _scenarioName;
    private IAny<Currency>                 _currency;
    private IAny<IReadOnlyList<BucketGap>> _gaps;

    private AnyString          _scenarioNameRecipe;
    private AnyCurrency        _currencyRecipe;
    private IAny<IReadOnlyList<BucketGap>> _gapsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLiquidityProfile()
        : this(scenarioName: AnyValidScenarioName(),
               currency:     AnyValidCurrency(),
               gaps:         AnyValidGaps()) { }

    private static AnyString AnyValidScenarioName() {
        return Any.String().NotBlank();
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static IAny<IReadOnlyList<BucketGap>> AnyValidGaps() {
        // Gaps form a ladder of buckets and each cumulative gap is the running sum of the gaps.
        return new AnyGapLadder();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLiquidityProfile(AnyString          scenarioName,
                                AnyCurrency        currency,
                                IAny<IReadOnlyList<BucketGap>> gaps) {
        _scenarioName       = scenarioName;
        _scenarioNameRecipe = scenarioName;
        _currency           = currency;
        _currencyRecipe     = currency;
        _gaps               = gaps;
        _gapsRecipe         = gaps;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLiquidityProfile(AnyLiquidityProfile from) {
        _scenarioName       = from._scenarioName;
        _scenarioNameRecipe = from._scenarioNameRecipe;
        _currency           = from._currency;
        _currencyRecipe     = from._currencyRecipe;
        _gaps               = from._gaps;
        _gapsRecipe         = from._gapsRecipe;
    }

    /// <summary>Pins <c>scenarioName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLiquidityProfile WithScenarioName(string value) {
        AnyLiquidityProfile copy = new AnyLiquidityProfile(this);

        copy._scenarioName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scenarioName</c> is drawn from — <c>AnyValidScenarioName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLiquidityProfile WithScenarioName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLiquidityProfile copy = new AnyLiquidityProfile(this);

        copy._scenarioNameRecipe = configure(_scenarioNameRecipe);
        copy._scenarioName       = copy._scenarioNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLiquidityProfile WithCurrency(Currency value) {
        AnyLiquidityProfile copy = new AnyLiquidityProfile(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLiquidityProfile WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLiquidityProfile copy = new AnyLiquidityProfile(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Pins <c>gaps</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLiquidityProfile WithGaps(IReadOnlyList<BucketGap> value) {
        AnyLiquidityProfile copy = new AnyLiquidityProfile(this);

        copy._gaps = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LiquidityProfile" />.</summary>
    public LiquidityProfile Generate() {
        return new LiquidityProfile(_scenarioName.Generate(),
                                    _currency.Generate(),
                                    _gaps.Generate());
    }

}
