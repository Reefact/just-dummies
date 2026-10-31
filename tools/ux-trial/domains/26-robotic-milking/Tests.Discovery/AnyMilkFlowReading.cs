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

    private readonly IAny<UdderQuarter> _quarter;
    private readonly IAny<TimeSpan>     _elapsed;
    private readonly IAny<decimal>      _flowKgPerMinute;
    private readonly IAny<decimal>      _conductivityMsPerCm;
    private readonly IAny<decimal>      _temperatureCelsius;
    private readonly IAny<bool>         _bloodDetected;

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
        return Any.Decimal().Between(0m, 8m);
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

    private AnyMilkFlowReading(IAny<UdderQuarter> quarter,
                               IAny<TimeSpan>     elapsed,
                               IAny<decimal>      flowKgPerMinute,
                               IAny<decimal>      conductivityMsPerCm,
                               IAny<decimal>      temperatureCelsius,
                               IAny<bool>         bloodDetected) {
        _quarter             = quarter;
        _elapsed             = elapsed;
        _flowKgPerMinute     = flowKgPerMinute;
        _conductivityMsPerCm = conductivityMsPerCm;
        _temperatureCelsius  = temperatureCelsius;
        _bloodDetected       = bloodDetected;
    }

    /// <summary>Pins <c>quarter</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithQuarter(UdderQuarter value) {
        return new AnyMilkFlowReading(new FixedValue<UdderQuarter>(value), _elapsed, _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Draws <c>quarter</c> from <c>AnyValidQuarter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithQuarter(Func<AnyEnum<UdderQuarter>, AnyEnum<UdderQuarter>> configure) {
        return new AnyMilkFlowReading(configure(AnyValidQuarter()), _elapsed, _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Pins <c>elapsed</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithElapsed(TimeSpan value) {
        return new AnyMilkFlowReading(_quarter, new FixedValue<TimeSpan>(value), _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Draws <c>elapsed</c> from <c>AnyValidElapsed()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithElapsed(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyMilkFlowReading(_quarter, configure(AnyValidElapsed()), _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Pins <c>flowKgPerMinute</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithFlowKgPerMinute(decimal value) {
        return new AnyMilkFlowReading(_quarter, _elapsed, new FixedValue<decimal>(value), _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Draws <c>flowKgPerMinute</c> from <c>AnyValidFlowKgPerMinute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithFlowKgPerMinute(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMilkFlowReading(_quarter, _elapsed, configure(AnyValidFlowKgPerMinute()), _conductivityMsPerCm, _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Pins <c>conductivityMsPerCm</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithConductivityMsPerCm(decimal value) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, new FixedValue<decimal>(value), _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Draws <c>conductivityMsPerCm</c> from <c>AnyValidConductivityMsPerCm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithConductivityMsPerCm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, configure(AnyValidConductivityMsPerCm()), _temperatureCelsius, _bloodDetected);
    }

    /// <summary>Pins <c>temperatureCelsius</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithTemperatureCelsius(decimal value) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, _conductivityMsPerCm, new FixedValue<decimal>(value), _bloodDetected);
    }

    /// <summary>Draws <c>temperatureCelsius</c> from <c>AnyValidTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithTemperatureCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, _conductivityMsPerCm, configure(AnyValidTemperatureCelsius()), _bloodDetected);
    }

    /// <summary>Pins <c>bloodDetected</c> to a fixed value.</summary>
    public AnyMilkFlowReading WithBloodDetected(bool value) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, new FixedValue<bool>(value));
    }

    /// <summary>Draws <c>bloodDetected</c> from <c>AnyValidBloodDetected()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkFlowReading WithBloodDetected(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyMilkFlowReading(_quarter, _elapsed, _flowKgPerMinute, _conductivityMsPerCm, _temperatureCelsius, configure(AnyValidBloodDetected()));
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
