// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Epoch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Epoch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEpoch : IAny<Epoch> {

    private readonly IAny<DateTimeOffset> _utc;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEpoch()
        : this(utc: AnyValidUtc()) { }

    private static AnyDateTimeOffset AnyValidUtc() {
        // An epoch is UTC (Any.DateTimeOffset() keeps +00:00 unless an offset is declared) and not before Sputnik.
        // The far bound is ours: an ephemeris adds minutes to an epoch, so stay clear of DateTimeOffset.MaxValue.
        return Any.DateTimeOffset().Between(Epoch.SpaceAgeStart, new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private AnyEpoch(IAny<DateTimeOffset> utc) {
        _utc = utc;
    }

    /// <summary>Pins <c>utc</c> to a fixed value.</summary>
    public AnyEpoch WithUtc(DateTimeOffset value) {
        return new AnyEpoch(new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>utc</c> from <c>AnyValidUtc()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEpoch WithUtc(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyEpoch(configure(AnyValidUtc()));
    }

    /// <summary>Produces one arbitrary <see cref="Epoch" />.</summary>
    public Epoch Generate() {
        return new Epoch(_utc.Generate());
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
