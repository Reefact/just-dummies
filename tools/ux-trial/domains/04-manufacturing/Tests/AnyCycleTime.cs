// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CycleTime --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CycleTime" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCycleTime : IAny<CycleTime> {

    private readonly IAny<TimeSpan> _duration;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCycleTime()
        : this(duration: AnyValidDuration()) { }

    private static AnyTimeSpan AnyValidDuration() {
        // CycleTime: 1 s to 1 h, whole seconds.
        return Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    private AnyCycleTime(IAny<TimeSpan> duration) {
        _duration = duration;
    }

    /// <summary>Pins <c>duration</c> to a fixed value.</summary>
    public AnyCycleTime WithDuration(TimeSpan value) {
        return new AnyCycleTime(new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>duration</c> from <c>AnyValidDuration()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCycleTime WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyCycleTime(configure(AnyValidDuration()));
    }

    /// <summary>Produces one arbitrary <see cref="CycleTime" />.</summary>
    public CycleTime Generate() {
        return new CycleTime(_duration.Generate());
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
