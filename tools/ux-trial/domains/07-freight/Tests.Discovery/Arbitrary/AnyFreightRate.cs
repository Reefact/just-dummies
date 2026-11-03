// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FreightRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FreightRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFreightRate : IAny<FreightRate> {

    private IAny<decimal> _perRevenueTon;
    private IAny<string>  _currency;

    private AnyDecimal _perRevenueTonRecipe;
    private AnyPattern  _currencyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFreightRate()
        : this(perRevenueTon: AnyValidPerRevenueTon(),
               currency:      AnyValidCurrency()) { }

    private static AnyDecimal AnyValidPerRevenueTon() {
        return Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(2);
    }

    private static AnyPattern AnyValidCurrency() {
        return Any.StringMatching("[A-Z]{3}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFreightRate(AnyDecimal perRevenueTon,
                           AnyPattern  currency) {
        _perRevenueTon       = perRevenueTon;
        _perRevenueTonRecipe = perRevenueTon;
        _currency            = currency;
        _currencyRecipe      = currency;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFreightRate(AnyFreightRate from) {
        _perRevenueTon       = from._perRevenueTon;
        _perRevenueTonRecipe = from._perRevenueTonRecipe;
        _currency            = from._currency;
        _currencyRecipe      = from._currencyRecipe;
    }

    /// <summary>Pins <c>perRevenueTon</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFreightRate WithPerRevenueTon(decimal value) {
        AnyFreightRate copy = new AnyFreightRate(this);

        copy._perRevenueTon = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>perRevenueTon</c> is drawn from — <c>AnyValidPerRevenueTon()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFreightRate WithPerRevenueTon(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFreightRate copy = new AnyFreightRate(this);

        copy._perRevenueTonRecipe = configure(_perRevenueTonRecipe);
        copy._perRevenueTon       = copy._perRevenueTonRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFreightRate WithCurrency(string value) {
        AnyFreightRate copy = new AnyFreightRate(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFreightRate WithCurrency(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFreightRate copy = new AnyFreightRate(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="FreightRate" />.</summary>
    public FreightRate Generate() {
        return new FreightRate(_perRevenueTon.Generate(),
                               _currency.Generate());
    }

}
