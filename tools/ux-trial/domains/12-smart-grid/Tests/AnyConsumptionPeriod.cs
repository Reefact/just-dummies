// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ConsumptionPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Edited by hand: the closing reading must belong to the opening's delivery point, come later and not
// go below its index. Those rules relate two parameters, so the closing reading's recipe is a function
// of the drawn opening reading.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ConsumptionPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConsumptionPeriod : IAny<ConsumptionPeriod> {

    private readonly IAny<MeterReading>                     _opening;
    private readonly Func<MeterReading, IAny<MeterReading>> _closing;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConsumptionPeriod()
        : this(opening: AnyValidOpening(),
               closing: opening => AnyValidClosing(opening)) { }

    private static AnyMeterReading AnyValidOpening() {
        // A closing reading has to fit after it.
        return new AnyMeterReading().WithReadingDate(date => date.Before(DateOnly.MaxValue));
    }

    private static AnyMeterReading AnyValidClosing(MeterReading opening) {
        return new AnyMeterReading().WithDeliveryPoint(opening.DeliveryPoint)
                                    .WithReadingDate(date => date.After(opening.ReadingDate))
                                    .WithIndex(index => index.WithKwh(kwh => kwh.GreaterThanOrEqualTo(opening.Index.Kwh)));
    }

    private AnyConsumptionPeriod(IAny<MeterReading>                     opening,
                                 Func<MeterReading, IAny<MeterReading>> closing) {
        _opening = opening;
        _closing = closing;
    }

    /// <summary>Pins <c>opening</c> to a fixed value.</summary>
    public AnyConsumptionPeriod WithOpening(MeterReading value) {
        return new AnyConsumptionPeriod(new FixedValue<MeterReading>(value), _closing);
    }

    /// <summary>Draws <c>opening</c> from <c>AnyValidOpening()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConsumptionPeriod WithOpening(Func<AnyMeterReading, AnyMeterReading> configure) {
        return new AnyConsumptionPeriod(configure(AnyValidOpening()), _closing);
    }

    /// <summary>Pins <c>closing</c> to a fixed value.</summary>
    public AnyConsumptionPeriod WithClosing(MeterReading value) {
        return new AnyConsumptionPeriod(_opening, _ => new FixedValue<MeterReading>(value));
    }

    /// <summary>Draws <c>closing</c> from <c>AnyValidClosing(opening)</c>, refined by <paramref name="configure" />.</summary>
    public AnyConsumptionPeriod WithClosing(Func<AnyMeterReading, AnyMeterReading> configure) {
        return new AnyConsumptionPeriod(_opening, opening => configure(AnyValidClosing(opening)));
    }

    /// <summary>Produces one arbitrary <see cref="ConsumptionPeriod" />.</summary>
    public ConsumptionPeriod Generate() {
        MeterReading opening = _opening.Generate();

        return new ConsumptionPeriod(opening, _closing(opening).Generate());
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
