// Scaffolded by `any` (JustDummies), then rewritten by hand: the sheet's as-of date may not follow the
// value date, a rule between two parameters that the scaffold marked `to verify`.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>
///     A generator of arbitrary, open <see cref="SettlementBatch" /> values: a business value date, a rate sheet
///     fixed no later than that date, a tolerance in (0, 0.05].
/// </summary>
public sealed class AnySettlementBatch : IAny<SettlementBatch> {

    private readonly DateOnly?                          _valueDate;
    private readonly RateSheet?                         _sheet;
    private readonly Func<AnyRateSheet, AnyRateSheet>   _refineSheet;
    private readonly decimal?                           _tolerance;

    public AnySettlementBatch()
        : this(null, null, sheet => sheet, null) { }

    private AnySettlementBatch(DateOnly? valueDate, RateSheet? sheet, Func<AnyRateSheet, AnyRateSheet> refineSheet, decimal? tolerance) {
        _valueDate   = valueDate;
        _sheet       = sheet;
        _refineSheet = refineSheet;
        _tolerance   = tolerance;
    }

    public AnySettlementBatch WithValueDate(DateOnly value) {
        return new AnySettlementBatch(value, _sheet, _refineSheet, _tolerance);
    }

    /// <summary>Pins the sheet; without a pinned value date, the batch settles within two weeks after its as-of date.</summary>
    public AnySettlementBatch WithReferenceRates(RateSheet value) {
        return new AnySettlementBatch(_valueDate, value, _refineSheet, _tolerance);
    }

    /// <summary>Refines the sheet it draws, which is fixed within two weeks before the value date.</summary>
    public AnySettlementBatch WithReferenceRates(Func<AnyRateSheet, AnyRateSheet> configure) {
        return new AnySettlementBatch(_valueDate, _sheet, configure, _tolerance);
    }

    public AnySettlementBatch WithTolerance(decimal value) {
        return new AnySettlementBatch(_valueDate, _sheet, _refineSheet, value);
    }

    public SettlementBatch Generate() {
        decimal tolerance = _tolerance ?? Any.Decimal().Between(0.0001m, SettlementBatch.MaxOffMarketTolerance).WithScale(4).Generate();

        if (_sheet is not null) {
            DateOnly valueDate = _valueDate ?? Days.BusinessBetween(_sheet.AsOf, _sheet.AsOf.AddDays(14)).Generate();

            return new SettlementBatch(valueDate, _sheet, tolerance);
        }

        DateOnly date = _valueDate ?? Days.Business().Generate();
        RateSheet sheet = _refineSheet(new AnyRateSheet().WithAsOf(Days.BusinessBetween(date.AddDays(-14), date))).Generate();

        return new SettlementBatch(date, sheet, tolerance);
    }

}
