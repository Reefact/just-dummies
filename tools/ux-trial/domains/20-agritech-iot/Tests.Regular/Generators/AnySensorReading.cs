// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SensorReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SensorReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySensorReading : IAny<SensorReading> {

    private IAny<SensorId>         _sensor;
    private IAny<GeoCoordinate>    _position;
    private IAny<RelativeHumidity> _humidity;
    private IAny<float>            _soilTemperatureCelsius;
    private IAny<BatteryVoltage>   _battery;
    private IAny<DateTimeOffset>   _takenAt;

    private AnySensorId         _sensorRecipe;
    private AnyGeoCoordinate    _positionRecipe;
    private AnyRelativeHumidity _humidityRecipe;
    private AnySingle           _soilTemperatureCelsiusRecipe;
    private AnyBatteryVoltage   _batteryRecipe;
    private AnyDateTimeOffset   _takenAtRecipe;

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
        return Any.DateTimeOffset();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySensorReading(AnySensorId         sensor,
                             AnyGeoCoordinate    position,
                             AnyRelativeHumidity humidity,
                             AnySingle           soilTemperatureCelsius,
                             AnyBatteryVoltage   battery,
                             AnyDateTimeOffset   takenAt) {
        _sensor                       = sensor;
        _sensorRecipe                 = sensor;
        _position                     = position;
        _positionRecipe               = position;
        _humidity                     = humidity;
        _humidityRecipe               = humidity;
        _soilTemperatureCelsius       = soilTemperatureCelsius;
        _soilTemperatureCelsiusRecipe = soilTemperatureCelsius;
        _battery                      = battery;
        _batteryRecipe                = battery;
        _takenAt                      = takenAt;
        _takenAtRecipe                = takenAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySensorReading(AnySensorReading from) {
        _sensor                       = from._sensor;
        _sensorRecipe                 = from._sensorRecipe;
        _position                     = from._position;
        _positionRecipe               = from._positionRecipe;
        _humidity                     = from._humidity;
        _humidityRecipe               = from._humidityRecipe;
        _soilTemperatureCelsius       = from._soilTemperatureCelsius;
        _soilTemperatureCelsiusRecipe = from._soilTemperatureCelsiusRecipe;
        _battery                      = from._battery;
        _batteryRecipe                = from._batteryRecipe;
        _takenAt                      = from._takenAt;
        _takenAtRecipe                = from._takenAtRecipe;
    }

    /// <summary>Pins <c>sensor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithSensor(SensorId value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._sensor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sensor</c> is drawn from — <c>AnyValidSensor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySensorId, AnySensorId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithSensor(Func<AnySensorId, AnySensorId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._sensorRecipe = configure(_sensorRecipe);
        copy._sensor       = copy._sensorRecipe;

        return copy;
    }

    /// <summary>Pins <c>position</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithPosition(GeoCoordinate value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._position = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>position</c> is drawn from — <c>AnyValidPosition()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGeoCoordinate, AnyGeoCoordinate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithPosition(Func<AnyGeoCoordinate, AnyGeoCoordinate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._positionRecipe = configure(_positionRecipe);
        copy._position       = copy._positionRecipe;

        return copy;
    }

    /// <summary>Pins <c>humidity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithHumidity(RelativeHumidity value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._humidity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>humidity</c> is drawn from — <c>AnyValidHumidity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRelativeHumidity, AnyRelativeHumidity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithHumidity(Func<AnyRelativeHumidity, AnyRelativeHumidity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._humidityRecipe = configure(_humidityRecipe);
        copy._humidity       = copy._humidityRecipe;

        return copy;
    }

    /// <summary>Pins <c>soilTemperatureCelsius</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithSoilTemperatureCelsius(float value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._soilTemperatureCelsius = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>soilTemperatureCelsius</c> is drawn from — <c>AnyValidSoilTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySingle, AnySingle&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithSoilTemperatureCelsius(Func<AnySingle, AnySingle> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._soilTemperatureCelsiusRecipe = configure(_soilTemperatureCelsiusRecipe);
        copy._soilTemperatureCelsius       = copy._soilTemperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Pins <c>battery</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithBattery(BatteryVoltage value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._battery = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>battery</c> is drawn from — <c>AnyValidBattery()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatteryVoltage, AnyBatteryVoltage&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithBattery(Func<AnyBatteryVoltage, AnyBatteryVoltage> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._batteryRecipe = configure(_batteryRecipe);
        copy._battery       = copy._batteryRecipe;

        return copy;
    }

    /// <summary>Pins <c>takenAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorReading WithTakenAt(DateTimeOffset value) {
        AnySensorReading copy = new AnySensorReading(this);

        copy._takenAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>takenAt</c> is drawn from — <c>AnyValidTakenAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorReading WithTakenAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorReading copy = new AnySensorReading(this);

        copy._takenAtRecipe = configure(_takenAtRecipe);
        copy._takenAt       = copy._takenAtRecipe;

        return copy;
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

}
