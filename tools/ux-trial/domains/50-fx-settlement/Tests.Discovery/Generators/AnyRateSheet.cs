// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RateSheet --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RateSheet" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRateSheet : IAny<RateSheet> {

    private IAny<Currency>                  _pivot;
    private IAny<DateOnly>                  _asOf;
    private IAny<IEnumerable<ExchangeRate>> _rates;

    private AnyCurrency               _pivotRecipe;
    private IAny<DateOnly>            _asOfRecipe;
    private AnySequence<ExchangeRate> _ratesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRateSheet()
        : this(pivot: AnyValidPivot(),
               asOf:  AnyValidAsOf(),
               rates: AnyValidRates()) { }

    private static AnyCurrency AnyValidPivot() {
        return new AnyCurrency();
    }

    private static IAny<DateOnly> AnyValidAsOf() {
        return BusinessDays.Any();
    }

    private static AnySequence<ExchangeRate> AnyValidRates() {
        // Not refinable: the rates depend on the pivot, so Generate() draws them unless a test pinned them.
        return Any.SequenceOf(new AnyExchangeRate());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRateSheet(AnyCurrency               pivot,
                         IAny<DateOnly>            asOf,
                         AnySequence<ExchangeRate> rates) {
        _pivot       = pivot;
        _pivotRecipe = pivot;
        _asOf        = asOf;
        _asOfRecipe  = asOf;
        _rates       = rates;
        _ratesRecipe = rates;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRateSheet(AnyRateSheet from) {
        _pivot       = from._pivot;
        _pivotRecipe = from._pivotRecipe;
        _asOf        = from._asOf;
        _asOfRecipe  = from._asOfRecipe;
        _rates       = from._rates;
        _ratesRecipe = from._ratesRecipe;
    }

    /// <summary>Pins <c>pivot</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRateSheet WithPivot(Currency value) {
        AnyRateSheet copy = new AnyRateSheet(this);

        copy._pivot = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pivot</c> is drawn from — <c>AnyValidPivot()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRateSheet WithPivot(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRateSheet copy = new AnyRateSheet(this);

        copy._pivotRecipe = configure(_pivotRecipe);
        copy._pivot       = copy._pivotRecipe;

        return copy;
    }

    /// <summary>Pins <c>asOf</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRateSheet WithAsOf(DateOnly value) {
        AnyRateSheet copy = new AnyRateSheet(this);

        copy._asOf = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>rates</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRateSheet WithRates(IEnumerable<ExchangeRate> value) {
        AnyRateSheet copy = new AnyRateSheet(this);

        copy._rates = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RateSheet" />.</summary>
    public RateSheet Generate() {
        Currency pivot = _pivot.Generate();
        DateOnly asOf  = _asOf.Generate();

        IEnumerable<ExchangeRate> rates = ReferenceEquals(_rates, _ratesRecipe) ? DrawRates(pivot) : _rates.Generate();

        return new RateSheet(pivot, asOf, rates);
    }

    /// <summary>One to four currencies other than the pivot, each quoted against it, one way round or the other.</summary>
    private static List<ExchangeRate> DrawRates(Currency pivot) {
        List<Currency> quoted = Any.ListOf(new AnyCurrency().WithCode(c => c.DifferentFrom(pivot.Code)))
                                   .WithCountBetween(1, 4)
                                   .DistinctBy(currency => currency.Code)
                                   .Generate();

        List<ExchangeRate> rates = new List<ExchangeRate>();
        foreach (Currency other in quoted) {
            CurrencyPair pair = Any.Boolean().Generate() ? new CurrencyPair(pivot, other) : new CurrencyPair(other, pivot);

            rates.Add(new AnyExchangeRate().WithPair(pair).Generate());
        }

        return rates;
    }

}
