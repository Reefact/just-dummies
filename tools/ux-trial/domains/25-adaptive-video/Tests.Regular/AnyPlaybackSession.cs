// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlaybackSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PlaybackSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlaybackSession : IAny<PlaybackSession> {

    private IAny<BitrateLadder>     _ladder;
    private IAny<PlaybackBuffer>    _buffer;
    private IAny<AdaptationPolicy>  _policy;
    private IAny<BandwidthEstimate> _bandwidth;
    private IAny<double>            _segmentDurationSeconds;

    private AnyBitrateLadder     _ladderRecipe;
    private AnyPlaybackBuffer    _bufferRecipe;
    private AnyAdaptationPolicy  _policyRecipe;
    private AnyBandwidthEstimate _bandwidthRecipe;
    private AnyDouble            _segmentDurationSecondsRecipe;

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
        return new AnyPlaybackBuffer();
    }

    private static AnyAdaptationPolicy AnyValidPolicy() {
        return new AnyAdaptationPolicy();
    }

    private static AnyBandwidthEstimate AnyValidBandwidth() {
        return new AnyBandwidthEstimate();
    }

    private static AnyDouble AnyValidSegmentDurationSeconds() {
        return Any.Double().Between(0.5d, 10d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPlaybackSession(AnyBitrateLadder     ladder,
                               AnyPlaybackBuffer    buffer,
                               AnyAdaptationPolicy  policy,
                               AnyBandwidthEstimate bandwidth,
                               AnyDouble            segmentDurationSeconds) {
        _ladder                       = ladder;
        _ladderRecipe                 = ladder;
        _buffer                       = buffer;
        _bufferRecipe                 = buffer;
        _policy                       = policy;
        _policyRecipe                 = policy;
        _bandwidth                    = bandwidth;
        _bandwidthRecipe              = bandwidth;
        _segmentDurationSeconds       = segmentDurationSeconds;
        _segmentDurationSecondsRecipe = segmentDurationSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPlaybackSession(AnyPlaybackSession from) {
        _ladder                       = from._ladder;
        _ladderRecipe                 = from._ladderRecipe;
        _buffer                       = from._buffer;
        _bufferRecipe                 = from._bufferRecipe;
        _policy                       = from._policy;
        _policyRecipe                 = from._policyRecipe;
        _bandwidth                    = from._bandwidth;
        _bandwidthRecipe              = from._bandwidthRecipe;
        _segmentDurationSeconds       = from._segmentDurationSeconds;
        _segmentDurationSecondsRecipe = from._segmentDurationSecondsRecipe;
    }

    /// <summary>Pins <c>ladder</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackSession WithLadder(BitrateLadder value) {
        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._ladder = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ladder</c> is drawn from — <c>AnyValidLadder()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBitrateLadder, AnyBitrateLadder&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackSession WithLadder(Func<AnyBitrateLadder, AnyBitrateLadder> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._ladderRecipe = configure(_ladderRecipe);
        copy._ladder       = copy._ladderRecipe;

        return copy;
    }

    /// <summary>Pins <c>buffer</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackSession WithBuffer(PlaybackBuffer value) {
        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._buffer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>buffer</c> is drawn from — <c>AnyValidBuffer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPlaybackBuffer, AnyPlaybackBuffer&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackSession WithBuffer(Func<AnyPlaybackBuffer, AnyPlaybackBuffer> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._bufferRecipe = configure(_bufferRecipe);
        copy._buffer       = copy._bufferRecipe;

        return copy;
    }

    /// <summary>Pins <c>policy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackSession WithPolicy(AdaptationPolicy value) {
        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._policy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>policy</c> is drawn from — <c>AnyValidPolicy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAdaptationPolicy, AnyAdaptationPolicy&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackSession WithPolicy(Func<AnyAdaptationPolicy, AnyAdaptationPolicy> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._policyRecipe = configure(_policyRecipe);
        copy._policy       = copy._policyRecipe;

        return copy;
    }

    /// <summary>Pins <c>bandwidth</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackSession WithBandwidth(BandwidthEstimate value) {
        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._bandwidth = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bandwidth</c> is drawn from — <c>AnyValidBandwidth()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBandwidthEstimate, AnyBandwidthEstimate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackSession WithBandwidth(Func<AnyBandwidthEstimate, AnyBandwidthEstimate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._bandwidthRecipe = configure(_bandwidthRecipe);
        copy._bandwidth       = copy._bandwidthRecipe;

        return copy;
    }

    /// <summary>Pins <c>segmentDurationSeconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackSession WithSegmentDurationSeconds(double value) {
        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._segmentDurationSeconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>segmentDurationSeconds</c> is drawn from — <c>AnyValidSegmentDurationSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackSession WithSegmentDurationSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackSession copy = new AnyPlaybackSession(this);

        copy._segmentDurationSecondsRecipe = configure(_segmentDurationSecondsRecipe);
        copy._segmentDurationSeconds       = copy._segmentDurationSecondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PlaybackSession" />.</summary>
    public PlaybackSession Generate() {
        PlaybackBuffer buffer   = _buffer.Generate();
        double         capacity = buffer.CapacitySeconds;

        // The two ties of the constructor, both through the capacity: comfort <= capacity, segment <= capacity.
        // They are written onto the recipes; a pinned policy or segment duration is handed over as it is.
        IAny<AdaptationPolicy> policy = ReferenceEquals(_policy, _policyRecipe)
            ? _policyRecipe.WithPanicBufferSeconds(p => p.LessThan(capacity)).WithComfortBufferSeconds(c => c.LessThanOrEqualTo(capacity))
            : _policy;
        IAny<double> segmentDuration = ReferenceEquals(_segmentDurationSeconds, _segmentDurationSecondsRecipe)
            ? _segmentDurationSecondsRecipe.LessThanOrEqualTo(capacity)
            : _segmentDurationSeconds;

        return new PlaybackSession(_ladder.Generate(),
                                   buffer,
                                   policy.Generate(),
                                   _bandwidth.Generate(),
                                   segmentDuration.Generate());
    }

}
