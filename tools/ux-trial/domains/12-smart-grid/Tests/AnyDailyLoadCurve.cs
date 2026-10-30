// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DailyLoadCurve --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DailyLoadCurve" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDailyLoadCurve : IAny<DailyLoadCurve> {

    private readonly IAny<DeliveryPointId>               _deliveryPoint;
    private readonly IAny<DateOnly>                      _day;
    private readonly IAny<IReadOnlyList<LoadCurvePoint>> _points;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDailyLoadCurve()
        : this(deliveryPoint: AnyValidDeliveryPoint(),
               day:           AnyValidDay(),
               points:        AnyValidPoints()) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyDateOnly AnyValidDay() {
        return Any.DateOnly();
    }

    private static IAny<IReadOnlyList<LoadCurvePoint>> AnyValidPoints() {
        return AnyValidPoints(AnyValidPower());
    }

    // The 48 slots are contiguous and in order, so only the power of each slot is drawn: the slot
    // grid itself is the invariant, not a dummy.
    private static AnyDecimal AnyValidPower() {
        return Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).WithScale(3);
    }

    private static IAny<IReadOnlyList<LoadCurvePoint>> AnyValidPoints(IAny<decimal> power) {
        return Any.ListOf(power)
                  .WithCount(DailyLoadCurve.SlotsPerDay)
                  .As(IReadOnlyList<LoadCurvePoint> (powers) => powers.Select((kw, slot) => new LoadCurvePoint(TimeOnly.MinValue.AddMinutes(30 * slot), kw)).ToList());
    }

    private AnyDailyLoadCurve(IAny<DeliveryPointId>               deliveryPoint,
                              IAny<DateOnly>                      day,
                              IAny<IReadOnlyList<LoadCurvePoint>> points) {
        _deliveryPoint = deliveryPoint;
        _day           = day;
        _points        = points;
    }

    /// <summary>Pins <c>deliveryPoint</c> to a fixed value.</summary>
    public AnyDailyLoadCurve WithDeliveryPoint(DeliveryPointId value) {
        return new AnyDailyLoadCurve(new FixedValue<DeliveryPointId>(value), _day, _points);
    }

    /// <summary>Draws <c>deliveryPoint</c> from <c>AnyValidDeliveryPoint()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDailyLoadCurve WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        return new AnyDailyLoadCurve(configure(AnyValidDeliveryPoint()), _day, _points);
    }

    /// <summary>Pins <c>day</c> to a fixed value.</summary>
    public AnyDailyLoadCurve WithDay(DateOnly value) {
        return new AnyDailyLoadCurve(_deliveryPoint, new FixedValue<DateOnly>(value), _points);
    }

    /// <summary>Draws <c>day</c> from <c>AnyValidDay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDailyLoadCurve WithDay(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyDailyLoadCurve(_deliveryPoint, configure(AnyValidDay()), _points);
    }

    /// <summary>Pins <c>points</c> to a fixed value.</summary>
    public AnyDailyLoadCurve WithPoints(IReadOnlyList<LoadCurvePoint> value) {
        return new AnyDailyLoadCurve(_deliveryPoint, _day, new FixedValue<IReadOnlyList<LoadCurvePoint>>(value));
    }

    /// <summary>Draws the power of every slot from <c>AnyValidPower()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDailyLoadCurve WithSlotPowers(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDailyLoadCurve(_deliveryPoint, _day, AnyValidPoints(configure(AnyValidPower())));
    }

    /// <summary>Produces one arbitrary <see cref="DailyLoadCurve" />.</summary>
    public DailyLoadCurve Generate() {
        return new DailyLoadCurve(_deliveryPoint.Generate(),
                                  _day.Generate(),
                                  _points.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
