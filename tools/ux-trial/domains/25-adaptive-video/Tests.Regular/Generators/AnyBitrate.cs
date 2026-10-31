// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bitrate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Bitrate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBitrate : IAny<Bitrate> {

    private readonly IAny<int> _kbps;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrate()
        : this(kbps: AnyValidKbps()) { }

    private static AnyInt32 AnyValidKbps() {
        return Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps);
    }

    private AnyBitrate(IAny<int> kbps) {
        _kbps = kbps;
    }

    /// <summary>Pins <c>kbps</c> to a fixed value.</summary>
    public AnyBitrate WithKbps(int value) {
        return new AnyBitrate(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>kbps</c> from <c>AnyValidKbps()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBitrate WithKbps(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBitrate(configure(AnyValidKbps()));
    }

    /// <summary>Produces one arbitrary <see cref="Bitrate" />.</summary>
    public Bitrate Generate() {
        return new Bitrate(_kbps.Generate());
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
