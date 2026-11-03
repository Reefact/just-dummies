// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Money --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Money" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMoney : IAny<Money> {

    private IAny<decimal>  _amount;
    private IAny<Currency> _currency;

    private AnyDecimal  _amountRecipe;
    private AnyCurrency _currencyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMoney()
        : this(amount:   AnyValidAmount(),
               currency: AnyValidCurrency()) { }

    private static AnyDecimal AnyValidAmount() {
        // An ordinary magnitude: the scale follows the currency and is applied in Generate().
        return Any.Decimal().Between(-1_000_000_000m, 1_000_000_000m);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMoney(AnyDecimal  amount,
                     AnyCurrency currency) {
        _amount         = amount;
        _amountRecipe   = amount;
        _currency       = currency;
        _currencyRecipe = currency;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMoney(AnyMoney from) {
        _amount         = from._amount;
        _amountRecipe   = from._amountRecipe;
        _currency       = from._currency;
        _currencyRecipe = from._currencyRecipe;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMoney WithAmount(decimal value) {
        AnyMoney copy = new AnyMoney(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMoney WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMoney copy = new AnyMoney(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMoney WithCurrency(Currency value) {
        AnyMoney copy = new AnyMoney(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMoney WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMoney copy = new AnyMoney(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Money" />.</summary>
    public Money Generate() {
        Currency currency = _currency.Generate();

        // An amount never carries more fractional digits than its currency: the recipe is put on the
        // currency's scale, while a pinned amount is handed over as it is (and refused if it breaks the rule).
        IAny<decimal> amount = ReferenceEquals(_amount, _amountRecipe) ? _amountRecipe.WithScale(currency.MinorUnits) : _amount;

        return new Money(amount.Generate(), currency);
    }

}
