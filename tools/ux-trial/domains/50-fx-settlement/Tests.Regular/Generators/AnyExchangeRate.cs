// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExchangeRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using FxSettlement.Domain;
using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="ExchangeRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExchangeRate : IAny<ExchangeRate> {

    private IAny<CurrencyPair> _pair;
    private IAny<decimal>      _value;

    private AnyCurrencyPair _pairRecipe;
    private AnyDecimal      _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExchangeRate()
        : this(pair:  AnyValidPair(),
               value: AnyValidValue()) { }

    private static AnyCurrencyPair AnyValidPair() {
        return new AnyCurrencyPair();
    }

    private static AnyDecimal AnyValidValue() {
        return Any.Decimal().Positive().LessThanOrEqualTo(ExchangeRate.MaxValue);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExchangeRate(AnyCurrencyPair pair,
                            AnyDecimal      value) {
        _pair        = pair;
        _pairRecipe  = pair;
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExchangeRate(AnyExchangeRate from) {
        _pair        = from._pair;
        _pairRecipe  = from._pairRecipe;
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>pair</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExchangeRate WithPair(CurrencyPair value) {
        AnyExchangeRate copy = new AnyExchangeRate(this);

        copy._pair = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pair</c> is drawn from — <c>AnyValidPair()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrencyPair, AnyCurrencyPair&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExchangeRate WithPair(Func<AnyCurrencyPair, AnyCurrencyPair> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExchangeRate copy = new AnyExchangeRate(this);

        copy._pairRecipe = configure(_pairRecipe);
        copy._pair       = copy._pairRecipe;

        return copy;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExchangeRate WithValue(decimal value) {
        AnyExchangeRate copy = new AnyExchangeRate(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExchangeRate WithValue(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExchangeRate copy = new AnyExchangeRate(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ExchangeRate" />.</summary>
    public ExchangeRate Generate() {
        CurrencyPair pair = _pair.Generate();

        // A rate carries no more decimal places than its pair: the scale is applied once the pair is drawn.
        // A pinned value is the caller's, and is handed over as it is.
        decimal value = ReferenceEquals(_value, _valueRecipe)
            ? _valueRecipe.WithScale(pair.RateDecimalPlaces).Generate()
            : _value.Generate();

        return new ExchangeRate(pair, value);
    }

}
