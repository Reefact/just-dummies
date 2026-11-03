// Scaffolded by `any`, then rewritten by hand: the sheet's as-of date may not postdate the value date, a tie
// between two parameters. The generator draws the value date first, then a sheet fixed on or before it.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

public sealed partial class AnySettlementBatch : IAny<SettlementBatch> {

    private IAny<DateOnly>  _valueDate;
    private AnyRateSheet    _referenceRatesRecipe;
    private RateSheet?      _referenceRates;
    private IAny<decimal>   _offMarketTolerance;

    public AnySettlementBatch() {
        _valueDate            = BusinessDays.Any();
        _referenceRatesRecipe = new AnyRateSheet();
        _offMarketTolerance   = Any.Decimal().Positive().LessThanOrEqualTo(SettlementBatch.MaxOffMarketTolerance);
    }

    private AnySettlementBatch(AnySettlementBatch from) {
        _valueDate            = from._valueDate;
        _referenceRatesRecipe = from._referenceRatesRecipe;
        _referenceRates       = from._referenceRates;
        _offMarketTolerance   = from._offMarketTolerance;
    }

    public AnySettlementBatch WithValueDate(DateOnly value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);
        copy._valueDate = Any.Fixed(value);
        return copy;
    }

    /// <summary>Pins the sheet; its as-of date is then the caller's to keep on or before the value date.</summary>
    public AnySettlementBatch WithReferenceRates(RateSheet value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);
        copy._referenceRates = value;
        return copy;
    }

    /// <summary>Refines the sheet drawn for the batch; its as-of date stays the batch's to choose.</summary>
    public AnySettlementBatch WithReferenceRates(Func<AnyRateSheet, AnyRateSheet> configure) {
        AnySettlementBatch copy = new AnySettlementBatch(this);
        copy._referenceRatesRecipe = configure(_referenceRatesRecipe);
        return copy;
    }

    public AnySettlementBatch WithOffMarketTolerance(decimal value) {
        AnySettlementBatch copy = new AnySettlementBatch(this);
        copy._offMarketTolerance = Any.Fixed(value);
        return copy;
    }

    public SettlementBatch Generate() {
        DateOnly valueDate = _valueDate.Generate();
        RateSheet sheet = _referenceRates
                          ?? _referenceRatesRecipe.WithAsOf(BusinessDays.Minus(valueDate, Any.Int32().Between(0, 5).Generate()))
                                                  .Generate();

        return new SettlementBatch(valueDate, sheet, _offMarketTolerance.Generate());
    }

}
