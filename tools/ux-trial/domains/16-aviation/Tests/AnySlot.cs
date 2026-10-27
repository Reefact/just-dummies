// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Slot --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Slot" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySlot : IAny<Slot> {

    private readonly IAny<IcaoAirportCode> _airport;
    private readonly IAny<DateTime>        _start;
    private readonly IAny<TimeSpan>        _duration;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySlot()
        : this(airport:  AnyValidAirport(),
               start:    AnyValidStart(),
               duration: AnyValidDuration()) { }

    private static AnyIcaoAirportCode AnyValidAirport() {
        return new AnyIcaoAirportCode();
    }

    private static AnyDateTime AnyValidStart() {
        // Coordinated in UTC, on a five-minute boundary.
        return Any.DateTime()
                  .Between(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2035, 12, 31, 0, 0, 0, DateTimeKind.Utc))
                  .WithGranularity(TimeSpan.FromMinutes(5));
    }

    private static AnyTimeSpan AnyValidDuration() {
        // From five minutes to one hour.
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
    }

    private AnySlot(IAny<IcaoAirportCode> airport,
                    IAny<DateTime>        start,
                    IAny<TimeSpan>        duration) {
        _airport  = airport;
        _start    = start;
        _duration = duration;
    }

    /// <summary>Pins <c>airport</c> to a fixed value.</summary>
    public AnySlot WithAirport(IcaoAirportCode value) {
        return new AnySlot(new FixedValue<IcaoAirportCode>(value), _start, _duration);
    }

    /// <summary>Draws <c>airport</c> from <c>AnyValidAirport()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlot WithAirport(Func<AnyIcaoAirportCode, AnyIcaoAirportCode> configure) {
        return new AnySlot(configure(AnyValidAirport()), _start, _duration);
    }

    /// <summary>Pins <c>start</c> to a fixed value.</summary>
    public AnySlot WithStart(DateTime value) {
        return new AnySlot(_airport, new FixedValue<DateTime>(value), _duration);
    }

    /// <summary>Draws <c>start</c> from <c>AnyValidStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlot WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnySlot(_airport, configure(AnyValidStart()), _duration);
    }

    /// <summary>Pins <c>duration</c> to a fixed value.</summary>
    public AnySlot WithDuration(TimeSpan value) {
        return new AnySlot(_airport, _start, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>duration</c> from <c>AnyValidDuration()</c>, refined by <paramref name="configure" />.</summary>
    public AnySlot WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnySlot(_airport, _start, configure(AnyValidDuration()));
    }

    /// <summary>Produces one arbitrary <see cref="Slot" />.</summary>
    public Slot Generate() {
        return new Slot(_airport.Generate(),
                        _start.Generate(),
                        _duration.Generate());
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
