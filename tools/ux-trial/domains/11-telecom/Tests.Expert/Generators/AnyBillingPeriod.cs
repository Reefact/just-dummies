using System;

using JustDummies;
using Telecom.Domain;

namespace Telecom.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BillingPeriod" /> values: a start, then a length of 1 to 31
///     days. Written by hand — the constructor ties <c>end</c> to <c>start</c> through a day count the
///     scaffolder does not read — so the rule holds by construction, not by retrying.
/// </summary>
public sealed class AnyBillingPeriod : IAny<BillingPeriod> {

    private readonly AnyDateOnly _start;
    private readonly AnyInt32    _lengthInDays;
    private readonly DateOnly?   _endPin;

    public AnyBillingPeriod()
        : this(Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)),
               Any.Int32().Between(1, 31),
               endPin: null) { }

    private AnyBillingPeriod(AnyDateOnly start, AnyInt32 lengthInDays, DateOnly? endPin) {
        _start        = start;
        _lengthInDays = lengthInDays;
        _endPin       = endPin;
    }

    /// <summary>Pins the first day of the period.</summary>
    public AnyBillingPeriod StartingOn(DateOnly start) {
        return new AnyBillingPeriod(Any.DateOnly().OneOf(start), _lengthInDays, _endPin);
    }

    /// <summary>Pins the day the period ends on (exclusive); the start follows from it.</summary>
    public AnyBillingPeriod EndingOn(DateOnly end) {
        return new AnyBillingPeriod(_start, _lengthInDays, end);
    }

    /// <summary>Pins the number of days between start and end, 1 to 31.</summary>
    public AnyBillingPeriod Lasting(int days) {
        return new AnyBillingPeriod(_start, Any.Int32().OneOf(days), _endPin);
    }

    public BillingPeriod Generate() {
        int length = _lengthInDays.Generate();

        if (_endPin is { } end) {
            return new BillingPeriod(end.AddDays(-length), end);
        }

        DateOnly start = _start.Generate();

        return new BillingPeriod(start, start.AddDays(length));
    }

}
