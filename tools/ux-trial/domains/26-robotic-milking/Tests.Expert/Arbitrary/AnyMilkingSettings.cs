// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingSettings --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingSettings" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingSettings : IAny<MilkingSettings> {

    private readonly IAny<TimeSpan> _cleaningTime;
    private readonly IAny<TimeSpan> _letDownDelay;
    private readonly IAny<decimal>  _vacuumKpa;
    private readonly IAny<decimal>  _detachFlowThreshold;
    private readonly IAny<int>      _maxAttachAttempts;
    private readonly IAny<TimeSpan> _minimumInterval;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingSettings()
        : this(cleaningTime:        AnyValidCleaningTime(),
               letDownDelay:        AnyValidLetDownDelay(),
               vacuumKpa:           AnyValidVacuumKpa(),
               detachFlowThreshold: AnyValidDetachFlowThreshold(),
               maxAttachAttempts:   AnyValidMaxAttachAttempts(),
               minimumInterval:     AnyValidMinimumInterval()) { }

    private static AnyTimeSpan AnyValidCleaningTime() {
        return Any.TimeSpan().Between(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    private static AnyTimeSpan AnyValidLetDownDelay() {
        // Cross-field rule: 4 x cleaningTime + letDownDelay >= 30 s. Cleaning is at least 5 s, so a delay of at
        // least 10 s honours it by construction whatever cleaning time is drawn. A test needing a shorter delay
        // pins both.
        return Any.TimeSpan().Between(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(90)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    private static AnyDecimal AnyValidVacuumKpa() {
        return Any.Decimal().Between(38m, 50m).WithScale(1);
    }

    private static AnyDecimal AnyValidDetachFlowThreshold() {
        return Any.Decimal().Between(0.1m, 0.5m).WithScale(2);
    }

    private static AnyInt32 AnyValidMaxAttachAttempts() {
        return Any.Int32().Positive().LessThanOrEqualTo(5);
    }

    private static AnyTimeSpan AnyValidMinimumInterval() {
        return Any.TimeSpan().Between(TimeSpan.FromHours(4), TimeSpan.FromHours(12)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyMilkingSettings(IAny<TimeSpan> cleaningTime,
                               IAny<TimeSpan> letDownDelay,
                               IAny<decimal>  vacuumKpa,
                               IAny<decimal>  detachFlowThreshold,
                               IAny<int>      maxAttachAttempts,
                               IAny<TimeSpan> minimumInterval) {
        _cleaningTime        = cleaningTime;
        _letDownDelay        = letDownDelay;
        _vacuumKpa           = vacuumKpa;
        _detachFlowThreshold = detachFlowThreshold;
        _maxAttachAttempts   = maxAttachAttempts;
        _minimumInterval     = minimumInterval;
    }

    /// <summary>Pins <c>cleaningTime</c> to a fixed value.</summary>
    public AnyMilkingSettings WithCleaningTime(TimeSpan value) {
        return new AnyMilkingSettings(new FixedValue<TimeSpan>(value), _letDownDelay, _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Draws <c>cleaningTime</c> from <c>AnyValidCleaningTime()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithCleaningTime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyMilkingSettings(configure(AnyValidCleaningTime()), _letDownDelay, _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Pins <c>letDownDelay</c> to a fixed value.</summary>
    public AnyMilkingSettings WithLetDownDelay(TimeSpan value) {
        return new AnyMilkingSettings(_cleaningTime, new FixedValue<TimeSpan>(value), _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Draws <c>letDownDelay</c> from <c>AnyValidLetDownDelay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithLetDownDelay(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyMilkingSettings(_cleaningTime, configure(AnyValidLetDownDelay()), _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Pins <c>vacuumKpa</c> to a fixed value.</summary>
    public AnyMilkingSettings WithVacuumKpa(decimal value) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, new FixedValue<decimal>(value), _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Draws <c>vacuumKpa</c> from <c>AnyValidVacuumKpa()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithVacuumKpa(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, configure(AnyValidVacuumKpa()), _detachFlowThreshold, _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Pins <c>detachFlowThreshold</c> to a fixed value.</summary>
    public AnyMilkingSettings WithDetachFlowThreshold(decimal value) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, new FixedValue<decimal>(value), _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Draws <c>detachFlowThreshold</c> from <c>AnyValidDetachFlowThreshold()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithDetachFlowThreshold(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, configure(AnyValidDetachFlowThreshold()), _maxAttachAttempts, _minimumInterval);
    }

    /// <summary>Pins <c>maxAttachAttempts</c> to a fixed value.</summary>
    public AnyMilkingSettings WithMaxAttachAttempts(int value) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, _detachFlowThreshold, new FixedValue<int>(value), _minimumInterval);
    }

    /// <summary>Draws <c>maxAttachAttempts</c> from <c>AnyValidMaxAttachAttempts()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithMaxAttachAttempts(Func<AnyInt32, AnyInt32> configure) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, _detachFlowThreshold, configure(AnyValidMaxAttachAttempts()), _minimumInterval);
    }

    /// <summary>Pins <c>minimumInterval</c> to a fixed value.</summary>
    public AnyMilkingSettings WithMinimumInterval(TimeSpan value) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>minimumInterval</c> from <c>AnyValidMinimumInterval()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSettings WithMinimumInterval(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyMilkingSettings(_cleaningTime, _letDownDelay, _vacuumKpa, _detachFlowThreshold, _maxAttachAttempts, configure(AnyValidMinimumInterval()));
    }

    /// <summary>Produces one arbitrary <see cref="MilkingSettings" />.</summary>
    public MilkingSettings Generate() {
        return new MilkingSettings(_cleaningTime.Generate(),
                                   _letDownDelay.Generate(),
                                   _vacuumKpa.Generate(),
                                   _detachFlowThreshold.Generate(),
                                   _maxAttachAttempts.Generate(),
                                   _minimumInterval.Generate());
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
