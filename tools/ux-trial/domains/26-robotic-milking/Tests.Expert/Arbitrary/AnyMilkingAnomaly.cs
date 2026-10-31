// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingAnomaly --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingAnomaly" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingAnomaly : IAny<MilkingAnomaly> {

    private readonly IAny<AnomalyKind>   _kind;
    private readonly IAny<UdderQuarter?> _quarter;
    private readonly IAny<TimeSpan>      _elapsed;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingAnomaly()
        : this(kind:    AnyValidKind(),
               quarter: AnyValidQuarter().AsNullable(),
               elapsed: AnyValidElapsed()) { }

    private static AnyEnum<AnomalyKind> AnyValidKind() {
        // Kind and quarter are tied (quarter-level kinds name a quarter, session-level ones forbid it):
        // Generate() derives the quarter from the drawn kind.
        return Any.Enum<AnomalyKind>();
    }

    private static AnyEnum<UdderQuarter> AnyValidQuarter() {
        return Any.Enum<UdderQuarter>();
    }

    private static AnyTimeSpan AnyValidElapsed() {
        return Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    private AnyMilkingAnomaly(IAny<AnomalyKind>   kind,
                              IAny<UdderQuarter?> quarter,
                              IAny<TimeSpan>      elapsed) {
        _kind    = kind;
        _quarter = quarter;
        _elapsed = elapsed;
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyMilkingAnomaly WithKind(AnomalyKind value) {
        return new AnyMilkingAnomaly(new FixedValue<AnomalyKind>(value), _quarter, _elapsed);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingAnomaly WithKind(Func<AnyEnum<AnomalyKind>, AnyEnum<AnomalyKind>> configure) {
        return new AnyMilkingAnomaly(configure(AnyValidKind()), _quarter, _elapsed);
    }

    /// <summary>Pins <c>quarter</c> to a fixed value.</summary>
    public AnyMilkingAnomaly WithQuarter(UdderQuarter? value) {
        return new AnyMilkingAnomaly(_kind, new FixedValue<UdderQuarter?>(value), _elapsed);
    }

    /// <summary>Draws <c>quarter</c> from <c>AnyValidQuarter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingAnomaly WithQuarter(Func<AnyEnum<UdderQuarter>, AnyEnum<UdderQuarter>> configure) {
        return new AnyMilkingAnomaly(_kind, configure(AnyValidQuarter()).AsNullable(), _elapsed);
    }

    /// <summary>Pins <c>elapsed</c> to a fixed value.</summary>
    public AnyMilkingAnomaly WithElapsed(TimeSpan value) {
        return new AnyMilkingAnomaly(_kind, _quarter, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>elapsed</c> from <c>AnyValidElapsed()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingAnomaly WithElapsed(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyMilkingAnomaly(_kind, _quarter, configure(AnyValidElapsed()));
    }

    /// <summary>Produces one arbitrary <see cref="MilkingAnomaly" />.</summary>
    public MilkingAnomaly Generate() {
        AnomalyKind kind = _kind.Generate();
        // Hand-written: the quarter is drawn only for a kind that names one, so the cross-field rule holds by construction.
        UdderQuarter? quarter = MilkingAnomaly.IsQuarterLevel(kind) ? _quarter.Generate() : null;

        return new MilkingAnomaly(kind, quarter, _elapsed.Generate());
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
