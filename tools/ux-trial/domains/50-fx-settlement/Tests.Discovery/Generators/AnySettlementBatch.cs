// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SettlementBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SettlementBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySettlementBatch : IAny<SettlementBatch> {

    private IAny<DateOnly>  _valueDate;
    private IAny<RateSheet> _referenceRates;
    private IAny<decimal>   _offMarketTolerance;

    private IAny<DateOnly> _valueDateRecipe;
    private AnyRateSheet _referenceRatesRecipe;
    private AnyDecimal   _offMarketToleranceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySettlementBatch()
        : this(valueDate:          AnyValidValueDate(),
               referenceRates:     AnyValidReferenceRates(),
               offMarketTolerance: AnyValidOffMarketTolerance()) { }

    private static IAny<DateOnly> AnyValidValueDate() {
        // A business day; Generate() draws it on or after the sheet's as-of date unless a test pinned it.
        return BusinessDays.Any();
    }

    private static AnyRateSheet AnyValidReferenceRates() {
        return new AnyRateSheet();
    }

    private static AnyDecimal AnyValidOffMarketTolerance() {
        return Any.Decimal().Between(0.005m, SettlementBatch.MaxOffMarketTolerance);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySettlementBatch(IAny<DateOnly> valueDate,
                               AnyRateSheet referenceRates,
                               AnyDecimal   offMarketTolerance) {
        _valueDate                = valueDate;
        _valueDateRecipe          = valueDate;
        _referenceRates           = referenceRates;
        _referenceRatesRecipe     = referenceRates;
        _offMarketTolerance       = offMarketTolerance;
        _offMarketToleranceRecipe = offMarketTolerance;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySettlementBatch(AnySettlementBatch from) {
        _valueDate                = from._valueDate;
        _valueDateRecipe          = from._valueDateRecipe;
        _referenceRates           = from._referenceRates;
        _referenceRatesRecipe     = from._referenceRatesRecipe;
        _offMarketTolerance       = from._offMarketTolerance;
        _offMarketToleranceRecipe = from._offMarketToleranceRecipe;
    }

    /// <summary>Pins <c>valueDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementBatch WithValueDate(DateOnly value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);

        copy._valueDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>referenceRates</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementBatch WithReferenceRates(RateSheet value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);

        copy._referenceRates = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>referenceRates</c> is drawn from — <c>AnyValidReferenceRates()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRateSheet, AnyRateSheet&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlementBatch WithReferenceRates(Func<AnyRateSheet, AnyRateSheet> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlementBatch copy = new AnySettlementBatch(this);

        copy._referenceRatesRecipe = configure(_referenceRatesRecipe);
        copy._referenceRates       = copy._referenceRatesRecipe;

        return copy;
    }

    /// <summary>Pins <c>offMarketTolerance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementBatch WithOffMarketTolerance(decimal value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);

        copy._offMarketTolerance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>offMarketTolerance</c> is drawn from — <c>AnyValidOffMarketTolerance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlementBatch WithOffMarketTolerance(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlementBatch copy = new AnySettlementBatch(this);

        copy._offMarketToleranceRecipe = configure(_offMarketToleranceRecipe);
        copy._offMarketTolerance       = copy._offMarketToleranceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SettlementBatch" />.</summary>
    public SettlementBatch Generate() {
        DateOnly  valueDate;
        RateSheet referenceRates;

        // The sheet's as-of date is never after the value date: whichever of the two a test did not pin follows the other.
        if (!ReferenceEquals(_valueDate, _valueDateRecipe)) {
            valueDate      = _valueDate.Generate();
            referenceRates = ReferenceEquals(_referenceRates, _referenceRatesRecipe)
                ? _referenceRatesRecipe.WithAsOf(valueDate).Generate()
                : _referenceRates.Generate();
        } else {
            referenceRates = _referenceRates.Generate();
            valueDate      = BusinessDays.OnOrAfter(referenceRates.AsOf).Generate();
        }

        return new SettlementBatch(valueDate,
                                   referenceRates,
                                   _offMarketTolerance.Generate());
    }

}
