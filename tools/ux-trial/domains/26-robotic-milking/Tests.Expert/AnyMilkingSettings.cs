// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingSettings --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingSettings" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingSettings : IAny<MilkingSettings> {

    private IAny<TimeSpan> _cleaningTime;
    private IAny<TimeSpan> _letDownDelay;
    private IAny<decimal>  _vacuumKpa;
    private IAny<decimal>  _detachFlowThreshold;
    private IAny<int>      _maxAttachAttempts;
    private IAny<TimeSpan> _minimumInterval;

    private AnyTimeSpan _cleaningTimeRecipe;
    private AnyTimeSpan _letDownDelayRecipe;
    private AnyDecimal  _vacuumKpaRecipe;
    private AnyDecimal  _detachFlowThresholdRecipe;
    private AnyInt32    _maxAttachAttemptsRecipe;
    private AnyTimeSpan _minimumIntervalRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingSettings()
        : this(cleaningTime:        AnyValidCleaningTime(),
               letDownDelay:        AnyValidLetDownDelay(),
               vacuumKpa:           AnyValidVacuumKpa(),
               detachFlowThreshold: AnyValidDetachFlowThreshold(),
               maxAttachAttempts:   AnyValidMaxAttachAttempts(),
               minimumInterval:     AnyValidMinimumInterval()) { }

    private static AnyTimeSpan AnyValidCleaningTime() {
        return Any.TimeSpan().Between(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
    }

    private static AnyTimeSpan AnyValidLetDownDelay() {
        return Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromSeconds(90));
    }

    private static AnyDecimal AnyValidVacuumKpa() {
        return Any.Decimal().Between(38m, 50m);
    }

    private static AnyDecimal AnyValidDetachFlowThreshold() {
        return Any.Decimal().Between(0.1m, 0.5m);
    }

    private static AnyInt32 AnyValidMaxAttachAttempts() {
        return Any.Int32().Positive().LessThanOrEqualTo(5);
    }

    private static AnyTimeSpan AnyValidMinimumInterval() {
        return Any.TimeSpan().Between(TimeSpan.FromHours(4), TimeSpan.FromHours(12));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMilkingSettings(AnyTimeSpan cleaningTime,
                               AnyTimeSpan letDownDelay,
                               AnyDecimal  vacuumKpa,
                               AnyDecimal  detachFlowThreshold,
                               AnyInt32    maxAttachAttempts,
                               AnyTimeSpan minimumInterval) {
        _cleaningTime              = cleaningTime;
        _cleaningTimeRecipe        = cleaningTime;
        _letDownDelay              = letDownDelay;
        _letDownDelayRecipe        = letDownDelay;
        _vacuumKpa                 = vacuumKpa;
        _vacuumKpaRecipe           = vacuumKpa;
        _detachFlowThreshold       = detachFlowThreshold;
        _detachFlowThresholdRecipe = detachFlowThreshold;
        _maxAttachAttempts         = maxAttachAttempts;
        _maxAttachAttemptsRecipe   = maxAttachAttempts;
        _minimumInterval           = minimumInterval;
        _minimumIntervalRecipe     = minimumInterval;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMilkingSettings(AnyMilkingSettings from) {
        _cleaningTime              = from._cleaningTime;
        _cleaningTimeRecipe        = from._cleaningTimeRecipe;
        _letDownDelay              = from._letDownDelay;
        _letDownDelayRecipe        = from._letDownDelayRecipe;
        _vacuumKpa                 = from._vacuumKpa;
        _vacuumKpaRecipe           = from._vacuumKpaRecipe;
        _detachFlowThreshold       = from._detachFlowThreshold;
        _detachFlowThresholdRecipe = from._detachFlowThresholdRecipe;
        _maxAttachAttempts         = from._maxAttachAttempts;
        _maxAttachAttemptsRecipe   = from._maxAttachAttemptsRecipe;
        _minimumInterval           = from._minimumInterval;
        _minimumIntervalRecipe     = from._minimumIntervalRecipe;
    }

    /// <summary>Pins <c>cleaningTime</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithCleaningTime(TimeSpan value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._cleaningTime       = Any.Fixed(value);
        copy._cleaningTimeRecipe = Any.TimeSpan().Between(value, value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cleaningTime</c> is drawn from — <c>AnyValidCleaningTime()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithCleaningTime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._cleaningTimeRecipe = configure(_cleaningTimeRecipe);
        copy._cleaningTime       = copy._cleaningTimeRecipe;

        return copy;
    }

    /// <summary>Pins <c>letDownDelay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithLetDownDelay(TimeSpan value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._letDownDelay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>letDownDelay</c> is drawn from — <c>AnyValidLetDownDelay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithLetDownDelay(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._letDownDelayRecipe = configure(_letDownDelayRecipe);
        copy._letDownDelay       = copy._letDownDelayRecipe;

        return copy;
    }

    /// <summary>Pins <c>vacuumKpa</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithVacuumKpa(decimal value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._vacuumKpa = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>vacuumKpa</c> is drawn from — <c>AnyValidVacuumKpa()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithVacuumKpa(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._vacuumKpaRecipe = configure(_vacuumKpaRecipe);
        copy._vacuumKpa       = copy._vacuumKpaRecipe;

        return copy;
    }

    /// <summary>Pins <c>detachFlowThreshold</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithDetachFlowThreshold(decimal value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._detachFlowThreshold = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>detachFlowThreshold</c> is drawn from — <c>AnyValidDetachFlowThreshold()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithDetachFlowThreshold(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._detachFlowThresholdRecipe = configure(_detachFlowThresholdRecipe);
        copy._detachFlowThreshold       = copy._detachFlowThresholdRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxAttachAttempts</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithMaxAttachAttempts(int value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._maxAttachAttempts = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxAttachAttempts</c> is drawn from — <c>AnyValidMaxAttachAttempts()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithMaxAttachAttempts(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._maxAttachAttemptsRecipe = configure(_maxAttachAttemptsRecipe);
        copy._maxAttachAttempts       = copy._maxAttachAttemptsRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumInterval</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSettings WithMinimumInterval(TimeSpan value) {
        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._minimumInterval = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumInterval</c> is drawn from — <c>AnyValidMinimumInterval()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSettings WithMinimumInterval(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSettings copy = new AnyMilkingSettings(this);

        copy._minimumIntervalRecipe = configure(_minimumIntervalRecipe);
        copy._minimumInterval       = copy._minimumIntervalRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MilkingSettings" />.</summary>
    /// <remarks>
    ///     Four cleanings plus the let-down delay must stimulate the cow for the minimum duration: the delay is drawn
    ///     first, then the cleaning time from its own recipe raised to the shortest brushing the delay allows.
    /// </remarks>
    public MilkingSettings Generate() {
        TimeSpan letDownDelay = _letDownDelay.Generate();

        // Rounded up: a floor division by four could land one tick short of the minimum stimulation.
        long      shortfallTicks  = (MilkingSettings.MinimumLetDownStimulation - letDownDelay).Ticks;
        TimeSpan  shortestCleaning = TimeSpan.FromTicks(Math.Max(shortfallTicks, 0) / 4 + (shortfallTicks > 0 && shortfallTicks % 4 != 0 ? 1 : 0));
        TimeSpan  cleaningTime     = _cleaningTimeRecipe.GreaterThanOrEqualTo(shortestCleaning).Generate();

        return new MilkingSettings(cleaningTime,
                                   letDownDelay,
                                   _vacuumKpa.Generate(),
                                   _detachFlowThreshold.Generate(),
                                   _maxAttachAttempts.Generate(),
                                   _minimumInterval.Generate());
    }

}
