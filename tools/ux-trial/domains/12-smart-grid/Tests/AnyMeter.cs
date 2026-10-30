// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Meter --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Edited by hand: the commissioning reading must be an actual reading of the meter's own delivery
// point, so its recipe is a function of the drawn delivery point.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Meter" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeter : IAny<Meter> {

    private readonly IAny<DeliveryPointId> _deliveryPoint;
    private readonly IAny<TariffOption>    _tariff;
    private readonly Func<DeliveryPointId, IAny<MeterReading>> _commissioningReading;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeter()
        : this(deliveryPoint:        AnyValidDeliveryPoint(),
               tariff:               AnyValidTariff(),
               commissioningReading: deliveryPoint => AnyValidCommissioningReading(deliveryPoint)) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyEnum<TariffOption> AnyValidTariff() {
        return Any.Enum<TariffOption>();
    }

    private static AnyMeterReading AnyValidCommissioningReading(DeliveryPointId deliveryPoint) {
        return new AnyMeterReading().WithDeliveryPoint(deliveryPoint).WithSource(ReadingSource.Actual);
    }

    private AnyMeter(IAny<DeliveryPointId> deliveryPoint,
                     IAny<TariffOption>    tariff,
                     Func<DeliveryPointId, IAny<MeterReading>> commissioningReading) {
        _deliveryPoint        = deliveryPoint;
        _tariff               = tariff;
        _commissioningReading = commissioningReading;
    }

    /// <summary>Pins <c>deliveryPoint</c> to a fixed value.</summary>
    public AnyMeter WithDeliveryPoint(DeliveryPointId value) {
        return new AnyMeter(new FixedValue<DeliveryPointId>(value), _tariff, _commissioningReading);
    }

    /// <summary>Draws <c>deliveryPoint</c> from <c>AnyValidDeliveryPoint()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeter WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        return new AnyMeter(configure(AnyValidDeliveryPoint()), _tariff, _commissioningReading);
    }

    /// <summary>Pins <c>tariff</c> to a fixed value.</summary>
    public AnyMeter WithTariff(TariffOption value) {
        return new AnyMeter(_deliveryPoint, new FixedValue<TariffOption>(value), _commissioningReading);
    }

    /// <summary>Draws <c>tariff</c> from <c>AnyValidTariff()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeter WithTariff(Func<AnyEnum<TariffOption>, AnyEnum<TariffOption>> configure) {
        return new AnyMeter(_deliveryPoint, configure(AnyValidTariff()), _commissioningReading);
    }

    /// <summary>Pins <c>commissioningReading</c> to a fixed value.</summary>
    public AnyMeter WithCommissioningReading(MeterReading value) {
        return new AnyMeter(_deliveryPoint, _tariff, _ => new FixedValue<MeterReading>(value));
    }

    /// <summary>Draws <c>commissioningReading</c> from <c>AnyValidCommissioningReading()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeter WithCommissioningReading(Func<AnyMeterReading, AnyMeterReading> configure) {
        return new AnyMeter(_deliveryPoint, _tariff, deliveryPoint => configure(AnyValidCommissioningReading(deliveryPoint)));
    }

    /// <summary>Produces one arbitrary <see cref="Meter" />.</summary>
    public Meter Generate() {
        DeliveryPointId deliveryPoint = _deliveryPoint.Generate();

        return new Meter(deliveryPoint,
                         _tariff.Generate(),
                         _commissioningReading(deliveryPoint).Generate());
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
