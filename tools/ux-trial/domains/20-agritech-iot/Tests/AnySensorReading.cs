// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SensorReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SensorReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySensorReading : IAny<SensorReading> {

    private readonly IAny<SensorId>         _sensor;
    private readonly IAny<GeoCoordinate>    _position;
    private readonly IAny<RelativeHumidity> _humidity;
    private readonly IAny<float>            _soilTemperatureCelsius;
    private readonly IAny<BatteryVoltage>   _battery;
    private readonly IAny<DateTimeOffset>   _takenAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySensorReading()
        : this(sensor:                 AnyValidSensor(),
               position:               AnyValidPosition(),
               humidity:               AnyValidHumidity(),
               soilTemperatureCelsius: AnyValidSoilTemperatureCelsius(),
               battery:                AnyValidBattery(),
               takenAt:                AnyValidTakenAt()) { }

    private static AnySensorId AnyValidSensor() {
        return new AnySensorId();
    }

    private static AnyGeoCoordinate AnyValidPosition() {
        return new AnyGeoCoordinate();
    }

    private static AnyRelativeHumidity AnyValidHumidity() {
        return new AnyRelativeHumidity();
    }

    private static AnySingle AnyValidSoilTemperatureCelsius() {
        return Any.Single().Between(-40f, 85f);
    }

    private static AnyBatteryVoltage AnyValidBattery() {
        return new AnyBatteryVoltage();
    }

    private static AnyDateTimeOffset AnyValidTakenAt() {
        return Any.DateTimeOffset().WithOffset(TimeSpan.Zero);
    }

    private AnySensorReading(IAny<SensorId>         sensor,
                             IAny<GeoCoordinate>    position,
                             IAny<RelativeHumidity> humidity,
                             IAny<float>            soilTemperatureCelsius,
                             IAny<BatteryVoltage>   battery,
                             IAny<DateTimeOffset>   takenAt) {
        _sensor                 = sensor;
        _position               = position;
        _humidity               = humidity;
        _soilTemperatureCelsius = soilTemperatureCelsius;
        _battery                = battery;
        _takenAt                = takenAt;
    }

    /// <summary>Pins <c>sensor</c> to a fixed value.</summary>
    public AnySensorReading WithSensor(SensorId value) {
        return new AnySensorReading(new FixedValue<SensorId>(value), _position, _humidity, _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Draws <c>sensor</c> from <c>AnyValidSensor()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithSensor(Func<AnySensorId, AnySensorId> configure) {
        return new AnySensorReading(configure(AnyValidSensor()), _position, _humidity, _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Pins <c>position</c> to a fixed value.</summary>
    public AnySensorReading WithPosition(GeoCoordinate value) {
        return new AnySensorReading(_sensor, new FixedValue<GeoCoordinate>(value), _humidity, _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Draws <c>position</c> from <c>AnyValidPosition()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithPosition(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        return new AnySensorReading(_sensor, configure(AnyValidPosition()), _humidity, _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Pins <c>humidity</c> to a fixed value.</summary>
    public AnySensorReading WithHumidity(RelativeHumidity value) {
        return new AnySensorReading(_sensor, _position, new FixedValue<RelativeHumidity>(value), _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Draws <c>humidity</c> from <c>AnyValidHumidity()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithHumidity(Func<AnyRelativeHumidity, AnyRelativeHumidity> configure) {
        return new AnySensorReading(_sensor, _position, configure(AnyValidHumidity()), _soilTemperatureCelsius, _battery, _takenAt);
    }

    /// <summary>Pins <c>soilTemperatureCelsius</c> to a fixed value.</summary>
    public AnySensorReading WithSoilTemperatureCelsius(float value) {
        return new AnySensorReading(_sensor, _position, _humidity, new FixedValue<float>(value), _battery, _takenAt);
    }

    /// <summary>Draws <c>soilTemperatureCelsius</c> from <c>AnyValidSoilTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithSoilTemperatureCelsius(Func<AnySingle, AnySingle> configure) {
        return new AnySensorReading(_sensor, _position, _humidity, configure(AnyValidSoilTemperatureCelsius()), _battery, _takenAt);
    }

    /// <summary>Pins <c>battery</c> to a fixed value.</summary>
    public AnySensorReading WithBattery(BatteryVoltage value) {
        return new AnySensorReading(_sensor, _position, _humidity, _soilTemperatureCelsius, new FixedValue<BatteryVoltage>(value), _takenAt);
    }

    /// <summary>Draws <c>battery</c> from <c>AnyValidBattery()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithBattery(Func<AnyBatteryVoltage, AnyBatteryVoltage> configure) {
        return new AnySensorReading(_sensor, _position, _humidity, _soilTemperatureCelsius, configure(AnyValidBattery()), _takenAt);
    }

    /// <summary>Pins <c>takenAt</c> to a fixed value.</summary>
    public AnySensorReading WithTakenAt(DateTimeOffset value) {
        return new AnySensorReading(_sensor, _position, _humidity, _soilTemperatureCelsius, _battery, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>takenAt</c> from <c>AnyValidTakenAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorReading WithTakenAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnySensorReading(_sensor, _position, _humidity, _soilTemperatureCelsius, _battery, configure(AnyValidTakenAt()));
    }

    /// <summary>Produces one arbitrary <see cref="SensorReading" />.</summary>
    public SensorReading Generate() {
        return new SensorReading(_sensor.Generate(),
                                 _position.Generate(),
                                 _humidity.Generate(),
                                 _soilTemperatureCelsius.Generate(),
                                 _battery.Generate(),
                                 _takenAt.Generate());
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
