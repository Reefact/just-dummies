// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LoggerReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using AgriTech.Domain;
using JustDummies;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LoggerReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLoggerReading : IAny<LoggerReading> {

    private IAny<SensorId>       _logger;
    private IAny<DateTimeOffset> _takenAt;
    private IAny<decimal>        _celsius;
    private IAny<BatteryVoltage> _battery;

    private AnySensorId       _loggerRecipe;
    private AnyDateTimeOffset _takenAtRecipe;
    private AnyDecimal        _celsiusRecipe;
    private AnyBatteryVoltage _batteryRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLoggerReading()
        : this(logger:  AnyValidLogger(),
               takenAt: AnyValidTakenAt(),
               celsius: AnyValidCelsius(),
               battery: AnyValidBattery()) { }

    private static AnySensorId AnyValidLogger() {
        return new AnySensorId();
    }

    private static AnyDateTimeOffset AnyValidTakenAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyDecimal AnyValidCelsius() {
        return Any.Decimal().Between(LoggerReading.MinCelsius, LoggerReading.MaxCelsius).WithScale(1);
    }

    private static AnyBatteryVoltage AnyValidBattery() {
        return new AnyBatteryVoltage();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLoggerReading(AnySensorId       logger,
                             AnyDateTimeOffset takenAt,
                             AnyDecimal        celsius,
                             AnyBatteryVoltage battery) {
        _logger        = logger;
        _loggerRecipe  = logger;
        _takenAt       = takenAt;
        _takenAtRecipe = takenAt;
        _celsius       = celsius;
        _celsiusRecipe = celsius;
        _battery       = battery;
        _batteryRecipe = battery;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLoggerReading(AnyLoggerReading from) {
        _logger        = from._logger;
        _loggerRecipe  = from._loggerRecipe;
        _takenAt       = from._takenAt;
        _takenAtRecipe = from._takenAtRecipe;
        _celsius       = from._celsius;
        _celsiusRecipe = from._celsiusRecipe;
        _battery       = from._battery;
        _batteryRecipe = from._batteryRecipe;
    }

    /// <summary>Pins <c>logger</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoggerReading WithLogger(SensorId value) {
        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._logger = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>logger</c> is drawn from — <c>AnyValidLogger()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySensorId, AnySensorId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoggerReading WithLogger(Func<AnySensorId, AnySensorId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._loggerRecipe = configure(_loggerRecipe);
        copy._logger       = copy._loggerRecipe;

        return copy;
    }

    /// <summary>Pins <c>takenAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoggerReading WithTakenAt(DateTimeOffset value) {
        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._takenAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>takenAt</c> is drawn from — <c>AnyValidTakenAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoggerReading WithTakenAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._takenAtRecipe = configure(_takenAtRecipe);
        copy._takenAt       = copy._takenAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>celsius</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoggerReading WithCelsius(decimal value) {
        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._celsius = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>celsius</c> is drawn from — <c>AnyValidCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoggerReading WithCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._celsiusRecipe = configure(_celsiusRecipe);
        copy._celsius       = copy._celsiusRecipe;

        return copy;
    }

    /// <summary>Pins <c>battery</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLoggerReading WithBattery(BatteryVoltage value) {
        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._battery = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>battery</c> is drawn from — <c>AnyValidBattery()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatteryVoltage, AnyBatteryVoltage&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLoggerReading WithBattery(Func<AnyBatteryVoltage, AnyBatteryVoltage> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLoggerReading copy = new AnyLoggerReading(this);

        copy._batteryRecipe = configure(_batteryRecipe);
        copy._battery       = copy._batteryRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LoggerReading" />.</summary>
    public LoggerReading Generate() {
        return new LoggerReading(_logger.Generate(),
                                 _takenAt.Generate(),
                                 _celsius.Generate(),
                                 _battery.Generate());
    }

}
