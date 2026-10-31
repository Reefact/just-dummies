// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlaybackSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PlaybackSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlaybackSession : IAny<PlaybackSession> {

    private readonly IAny<BitrateLadder>     _ladder;
    private readonly IAny<PlaybackBuffer>    _buffer;
    private readonly IAny<AdaptationPolicy>  _policy;
    private readonly IAny<BandwidthEstimate> _bandwidth;
    private readonly IAny<double>            _segmentDurationSeconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlaybackSession()
        : this(ladder:                 AnyValidLadder(),
               buffer:                 AnyValidBuffer(),
               policy:                 AnyValidPolicy(),
               bandwidth:              AnyValidBandwidth(),
               segmentDurationSeconds: AnyValidSegmentDurationSeconds()) { }

    private static AnyBitrateLadder AnyValidLadder() {
        return new AnyBitrateLadder();
    }

    private static AnyPlaybackBuffer AnyValidBuffer() {
        // The session needs room for the comfort threshold (at most 15 s) and a segment (at most 10 s).
        return new AnyPlaybackBuffer().WithCapacitySeconds(c => c.GreaterThanOrEqualTo(15));
    }

    private static AnyAdaptationPolicy AnyValidPolicy() {
        return new AnyAdaptationPolicy();
    }

    private static AnyBandwidthEstimate AnyValidBandwidth() {
        return new AnyBandwidthEstimate();
    }

    private static AnyDouble AnyValidSegmentDurationSeconds() {
        // At most 10 s, which the buffer base above always has room for.
        return Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds);
    }

    private AnyPlaybackSession(IAny<BitrateLadder>     ladder,
                               IAny<PlaybackBuffer>    buffer,
                               IAny<AdaptationPolicy>  policy,
                               IAny<BandwidthEstimate> bandwidth,
                               IAny<double>            segmentDurationSeconds) {
        _ladder                 = ladder;
        _buffer                 = buffer;
        _policy                 = policy;
        _bandwidth              = bandwidth;
        _segmentDurationSeconds = segmentDurationSeconds;
    }

    /// <summary>Pins <c>ladder</c> to a fixed value.</summary>
    public AnyPlaybackSession WithLadder(BitrateLadder value) {
        return new AnyPlaybackSession(new FixedValue<BitrateLadder>(value), _buffer, _policy, _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Draws <c>ladder</c> from <c>AnyValidLadder()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackSession WithLadder(Func<AnyBitrateLadder, AnyBitrateLadder> configure) {
        return new AnyPlaybackSession(configure(AnyValidLadder()), _buffer, _policy, _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Pins <c>buffer</c> to a fixed value.</summary>
    public AnyPlaybackSession WithBuffer(PlaybackBuffer value) {
        return new AnyPlaybackSession(_ladder, new FixedValue<PlaybackBuffer>(value), _policy, _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Draws <c>buffer</c> from <c>AnyValidBuffer()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackSession WithBuffer(Func<AnyPlaybackBuffer, AnyPlaybackBuffer> configure) {
        return new AnyPlaybackSession(_ladder, configure(AnyValidBuffer()), _policy, _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Pins <c>policy</c> to a fixed value.</summary>
    public AnyPlaybackSession WithPolicy(AdaptationPolicy value) {
        return new AnyPlaybackSession(_ladder, _buffer, new FixedValue<AdaptationPolicy>(value), _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Draws <c>policy</c> from <c>AnyValidPolicy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackSession WithPolicy(Func<AnyAdaptationPolicy, AnyAdaptationPolicy> configure) {
        return new AnyPlaybackSession(_ladder, _buffer, configure(AnyValidPolicy()), _bandwidth, _segmentDurationSeconds);
    }

    /// <summary>Pins <c>bandwidth</c> to a fixed value.</summary>
    public AnyPlaybackSession WithBandwidth(BandwidthEstimate value) {
        return new AnyPlaybackSession(_ladder, _buffer, _policy, new FixedValue<BandwidthEstimate>(value), _segmentDurationSeconds);
    }

    /// <summary>Draws <c>bandwidth</c> from <c>AnyValidBandwidth()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackSession WithBandwidth(Func<AnyBandwidthEstimate, AnyBandwidthEstimate> configure) {
        return new AnyPlaybackSession(_ladder, _buffer, _policy, configure(AnyValidBandwidth()), _segmentDurationSeconds);
    }

    /// <summary>Pins <c>segmentDurationSeconds</c> to a fixed value.</summary>
    public AnyPlaybackSession WithSegmentDurationSeconds(double value) {
        return new AnyPlaybackSession(_ladder, _buffer, _policy, _bandwidth, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>segmentDurationSeconds</c> from <c>AnyValidSegmentDurationSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackSession WithSegmentDurationSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPlaybackSession(_ladder, _buffer, _policy, _bandwidth, configure(AnyValidSegmentDurationSeconds()));
    }

    /// <summary>Produces one arbitrary <see cref="PlaybackSession" />.</summary>
    public PlaybackSession Generate() {
        return new PlaybackSession(_ladder.Generate(),
                                   _buffer.Generate(),
                                   _policy.Generate(),
                                   _bandwidth.Generate(),
                                   _segmentDurationSeconds.Generate());
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
