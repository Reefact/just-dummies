using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BillingPeriod" /> values: a start, then a length of 1 to 31 days,
///     so the end is after the start and within a month by construction.
/// </summary>
public sealed partial class AnyBillingPeriod : IAny<BillingPeriod> {

    private AnyDateOnly _startRecipe;
    private AnyInt32    _lengthInDaysRecipe;
    private DateOnly?   _startPin;

    public AnyBillingPeriod() {
        _startRecipe        = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
        _lengthInDaysRecipe = Any.Int32().Between(1, 31);
    }

    private AnyBillingPeriod(AnyBillingPeriod from) {
        _startRecipe        = from._startRecipe;
        _lengthInDaysRecipe = from._lengthInDaysRecipe;
        _startPin           = from._startPin;
    }

    public AnyBillingPeriod WithStart(DateOnly value) {
        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._startPin = value;

        return copy;
    }

    public AnyBillingPeriod WithLengthInDays(int days) {
        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._lengthInDaysRecipe = Any.Int32().Between(days, days);

        return copy;
    }

    public BillingPeriod Generate() {
        DateOnly start = _startPin ?? _startRecipe.Generate();
        int      days  = _lengthInDaysRecipe.Generate();

        return new BillingPeriod(start, start.AddDays(days));
    }

}
