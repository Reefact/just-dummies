// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkFlowReading --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkFlowReading" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkFlowReading : IAny<MilkFlowReading> {

    private IAny<UdderQuarter> _quarter;
    private IAny<TimeSpan>     _elapsed;
    private IAny<decimal>      _flowKgPerMinute;
    private IAny<decimal>      _conductivityMsPerCm;
    private IAny<decimal>      _temperatureCelsius;
    private IAny<bool>         _bloodDetected;

    private AnyEnum<UdderQuarter> _quarterRecipe;
    private AnyTimeSpan           _elapsedRecipe;
    private AnyDecimal            _flowKgPerMinuteRecipe;
    private AnyDecimal            _conductivityMsPerCmRecipe;
    private AnyDecimal            _temperatureCelsiusRecipe;
    private AnyBoolean            _bloodDetectedRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkFlowReading()
        : this(quarter:             AnyValidQuarter(),
               elapsed:             AnyValidElapsed(),
               flowKgPerMinute:     AnyValidFlowKgPerMinute(),
               conductivityMsPerCm: AnyValidConductivityMsPerCm(),
               temperatureCelsius:  AnyValidTemperatureCelsius(),
               bloodDetected:       AnyValidBloodDetected()) { }

    private static AnyEnum<UdderQuarter> AnyValidQuarter() {
        return Any.Enum<UdderQuarter>();
    }

    private static AnyTimeSpan AnyValidElapsed() {
        return Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(1));
    }

    private static AnyDecimal AnyValidFlowKgPerMinute() {
        return Any.Decimal().Between(0m, MilkFlowReading.MaxFlowKgPerMinute);
    }

    private static AnyDecimal AnyValidConductivityMsPerCm() {
        return Any.Decimal().Between(2m, 15m);
    }

    private static AnyDecimal AnyValidTemperatureCelsius() {
        return Any.Decimal().Between(30m, 42m);
    }

    private static AnyBoolean AnyValidBloodDetected() {
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMilkFlowReading(AnyEnum<UdderQuarter> quarter,
                               AnyTimeSpan           elapsed,
                               AnyDecimal            flowKgPerMinute,
                               AnyDecimal            conductivityMsPerCm,
                               AnyDecimal            temperatureCelsius,
                               AnyBoolean            bloodDetected) {
        _quarter                   = quarter;
        _quarterRecipe             = quarter;
        _elapsed                   = elapsed;
        _elapsedRecipe             = elapsed;
        _flowKgPerMinute           = flowKgPerMinute;
        _flowKgPerMinuteRecipe     = flowKgPerMinute;
        _conductivityMsPerCm       = conductivityMsPerCm;
        _conductivityMsPerCmRecipe = conductivityMsPerCm;
        _temperatureCelsius        = temperatureCelsius;
        _temperatureCelsiusRecipe  = temperatureCelsius;
        _bloodDetected             = bloodDetected;
        _bloodDetectedRecipe       = bloodDetected;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMilkFlowReading(AnyMilkFlowReading from) {
        _quarter                   = from._quarter;
        _quarterRecipe             = from._quarterRecipe;
        _elapsed                   = from._elapsed;
        _elapsedRecipe             = from._elapsedRecipe;
        _flowKgPerMinute           = from._flowKgPerMinute;
        _flowKgPerMinuteRecipe     = from._flowKgPerMinuteRecipe;
        _conductivityMsPerCm       = from._conductivityMsPerCm;
        _conductivityMsPerCmRecipe = from._conductivityMsPerCmRecipe;
        _temperatureCelsius        = from._temperatureCelsius;
        _temperatureCelsiusRecipe  = from._temperatureCelsiusRecipe;
        _bloodDetected             = from._bloodDetected;
        _bloodDetectedRecipe       = from._bloodDetectedRecipe;
    }

    /// <summary>Pins <c>quarter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithQuarter(UdderQuarter value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._quarter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quarter</c> is drawn from — <c>AnyValidQuarter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;UdderQuarter&gt;, AnyEnum&lt;UdderQuarter&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithQuarter(Func<AnyEnum<UdderQuarter>, AnyEnum<UdderQuarter>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._quarterRecipe = configure(_quarterRecipe);
        copy._quarter       = copy._quarterRecipe;

        return copy;
    }

    /// <summary>Pins <c>elapsed</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithElapsed(TimeSpan value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._elapsed = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>elapsed</c> is drawn from — <c>AnyValidElapsed()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithElapsed(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._elapsedRecipe = configure(_elapsedRecipe);
        copy._elapsed       = copy._elapsedRecipe;

        return copy;
    }

    /// <summary>Pins <c>flowKgPerMinute</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithFlowKgPerMinute(decimal value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._flowKgPerMinute = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>flowKgPerMinute</c> is drawn from — <c>AnyValidFlowKgPerMinute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithFlowKgPerMinute(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._flowKgPerMinuteRecipe = configure(_flowKgPerMinuteRecipe);
        copy._flowKgPerMinute       = copy._flowKgPerMinuteRecipe;

        return copy;
    }

    /// <summary>Pins <c>conductivityMsPerCm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithConductivityMsPerCm(decimal value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._conductivityMsPerCm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>conductivityMsPerCm</c> is drawn from — <c>AnyValidConductivityMsPerCm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithConductivityMsPerCm(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._conductivityMsPerCmRecipe = configure(_conductivityMsPerCmRecipe);
        copy._conductivityMsPerCm       = copy._conductivityMsPerCmRecipe;

        return copy;
    }

    /// <summary>Pins <c>temperatureCelsius</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithTemperatureCelsius(decimal value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._temperatureCelsius = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>temperatureCelsius</c> is drawn from — <c>AnyValidTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithTemperatureCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._temperatureCelsiusRecipe = configure(_temperatureCelsiusRecipe);
        copy._temperatureCelsius       = copy._temperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Pins <c>bloodDetected</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkFlowReading WithBloodDetected(bool value) {
        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._bloodDetected = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bloodDetected</c> is drawn from — <c>AnyValidBloodDetected()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkFlowReading WithBloodDetected(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkFlowReading copy = new AnyMilkFlowReading(this);

        copy._bloodDetectedRecipe = configure(_bloodDetectedRecipe);
        copy._bloodDetected       = copy._bloodDetectedRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MilkFlowReading" />.</summary>
    public MilkFlowReading Generate() {
        return new MilkFlowReading(_quarter.Generate(),
                                   _elapsed.Generate(),
                                   _flowKgPerMinute.Generate(),
                                   _conductivityMsPerCm.Generate(),
                                   _temperatureCelsius.Generate(),
                                   _bloodDetected.Generate());
    }

}
