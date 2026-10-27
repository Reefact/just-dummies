// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReadingBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReadingBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReadingBatch : IAny<ReadingBatch> {

    private readonly IAny<SensorId>                     _gateway;
    private readonly IAny<IReadOnlyList<SensorReading>> _readings;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReadingBatch()
        : this(gateway:  AnyValidGateway(),
               // Hand fix: the guard's ceiling (500 000) steers the count draw (ADR-0076), so the scaffolded
               // default drew ~250 000 readings per batch (about 2 s each). The default dummy stays small;
               // WithReadings(r => ...) still refines the full valid base, up to the domain's maximum.
               readings: AnyValidReadings().WithMaxCount(20)) { }

    private static AnySensorId AnyValidGateway() {
        return new AnySensorId();
    }

    private static AnyList<SensorReading> AnyValidReadings() {
return Any.ListOf(new AnySensorReading()).NonEmpty().WithMaxCount(500000);
    }

    private AnyReadingBatch(IAny<SensorId>                     gateway,
                            IAny<IReadOnlyList<SensorReading>> readings) {
        _gateway  = gateway;
        _readings = readings;
    }

    /// <summary>Pins <c>gateway</c> to a fixed value.</summary>
    public AnyReadingBatch WithGateway(SensorId value) {
        return new AnyReadingBatch(new FixedValue<SensorId>(value), _readings);
    }

    /// <summary>Draws <c>gateway</c> from <c>AnyValidGateway()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReadingBatch WithGateway(Func<AnySensorId, AnySensorId> configure) {
        return new AnyReadingBatch(configure(AnyValidGateway()), _readings);
    }

    /// <summary>Pins <c>readings</c> to a fixed value.</summary>
    public AnyReadingBatch WithReadings(IReadOnlyList<SensorReading> value) {
        return new AnyReadingBatch(_gateway, new FixedValue<IReadOnlyList<SensorReading>>(value));
    }

    /// <summary>Draws <c>readings</c> from <c>AnyValidReadings()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReadingBatch WithReadings(Func<AnyList<SensorReading>, AnyList<SensorReading>> configure) {
        return new AnyReadingBatch(_gateway, configure(AnyValidReadings()));
    }

    /// <summary>Produces one arbitrary <see cref="ReadingBatch" />.</summary>
    public ReadingBatch Generate() {
        return new ReadingBatch(_gateway.Generate(),
                                _readings.Generate());
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
