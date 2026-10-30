// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeterReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeterReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeterReading : IAny<MeterReading> {

    private readonly IAny<DeliveryPointId> _deliveryPoint;
    private readonly IAny<MeterIndex>      _index;
    private readonly IAny<DateOnly>        _readingDate;
    private readonly IAny<ReadingSource>   _source;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeterReading()
        : this(deliveryPoint: AnyValidDeliveryPoint(),
               index:         AnyValidIndex(),
               readingDate:   AnyValidReadingDate(),
               source:        AnyValidSource()) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyMeterIndex AnyValidIndex() {
        return new AnyMeterIndex();
    }

    private static AnyDateOnly AnyValidReadingDate() {
        return Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch);
    }

    private static AnyEnum<ReadingSource> AnyValidSource() {
        return Any.Enum<ReadingSource>();
    }

    private AnyMeterReading(IAny<DeliveryPointId> deliveryPoint,
                            IAny<MeterIndex>      index,
                            IAny<DateOnly>        readingDate,
                            IAny<ReadingSource>   source) {
        _deliveryPoint = deliveryPoint;
        _index         = index;
        _readingDate   = readingDate;
        _source        = source;
    }

    /// <summary>Pins <c>deliveryPoint</c> to a fixed value.</summary>
    public AnyMeterReading WithDeliveryPoint(DeliveryPointId value) {
        return new AnyMeterReading(new FixedValue<DeliveryPointId>(value), _index, _readingDate, _source);
    }

    /// <summary>Draws <c>deliveryPoint</c> from <c>AnyValidDeliveryPoint()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeterReading WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        return new AnyMeterReading(configure(AnyValidDeliveryPoint()), _index, _readingDate, _source);
    }

    /// <summary>Pins <c>index</c> to a fixed value.</summary>
    public AnyMeterReading WithIndex(MeterIndex value) {
        return new AnyMeterReading(_deliveryPoint, new FixedValue<MeterIndex>(value), _readingDate, _source);
    }

    /// <summary>Draws <c>index</c> from <c>AnyValidIndex()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeterReading WithIndex(Func<AnyMeterIndex, AnyMeterIndex> configure) {
        return new AnyMeterReading(_deliveryPoint, configure(AnyValidIndex()), _readingDate, _source);
    }

    /// <summary>Pins <c>readingDate</c> to a fixed value.</summary>
    public AnyMeterReading WithReadingDate(DateOnly value) {
        return new AnyMeterReading(_deliveryPoint, _index, new FixedValue<DateOnly>(value), _source);
    }

    /// <summary>Draws <c>readingDate</c> from <c>AnyValidReadingDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeterReading WithReadingDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyMeterReading(_deliveryPoint, _index, configure(AnyValidReadingDate()), _source);
    }

    /// <summary>Pins <c>source</c> to a fixed value.</summary>
    public AnyMeterReading WithSource(ReadingSource value) {
        return new AnyMeterReading(_deliveryPoint, _index, _readingDate, new FixedValue<ReadingSource>(value));
    }

    /// <summary>Draws <c>source</c> from <c>AnyValidSource()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeterReading WithSource(Func<AnyEnum<ReadingSource>, AnyEnum<ReadingSource>> configure) {
        return new AnyMeterReading(_deliveryPoint, _index, _readingDate, configure(AnyValidSource()));
    }

    /// <summary>Produces one arbitrary <see cref="MeterReading" />.</summary>
    public MeterReading Generate() {
        return new MeterReading(_deliveryPoint.Generate(),
                                _index.Generate(),
                                _readingDate.Generate(),
                                _source.Generate());
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
