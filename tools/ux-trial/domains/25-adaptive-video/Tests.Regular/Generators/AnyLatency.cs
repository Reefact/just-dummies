// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Latency --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Latency" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLatency : IAny<Latency> {

    private readonly IAny<int> _roundTripMilliseconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLatency()
        : this(roundTripMilliseconds: AnyValidRoundTripMilliseconds()) { }

    private static AnyInt32 AnyValidRoundTripMilliseconds() {
        return Any.Int32().Between(0, Latency.MaxMilliseconds);
    }

    private AnyLatency(IAny<int> roundTripMilliseconds) {
        _roundTripMilliseconds = roundTripMilliseconds;
    }

    /// <summary>Pins <c>roundTripMilliseconds</c> to a fixed value.</summary>
    public AnyLatency WithRoundTripMilliseconds(int value) {
        return new AnyLatency(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>roundTripMilliseconds</c> from <c>AnyValidRoundTripMilliseconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLatency WithRoundTripMilliseconds(Func<AnyInt32, AnyInt32> configure) {
        return new AnyLatency(configure(AnyValidRoundTripMilliseconds()));
    }

    /// <summary>Produces one arbitrary <see cref="Latency" />.</summary>
    public Latency Generate() {
        return new Latency(_roundTripMilliseconds.Generate());
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
