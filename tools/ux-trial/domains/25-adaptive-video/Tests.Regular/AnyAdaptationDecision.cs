// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AdaptationDecision --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AdaptationDecision" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAdaptationDecision : IAny<AdaptationDecision> {

    private IAny<Segment>          _segment;
    private IAny<EncodingVariant>  _previous;
    private IAny<AdaptationReason> _reason;
    private IAny<double>           _predictedDownloadSeconds;

    private AnySegment                _segmentRecipe;
    private AnyEncodingVariant        _previousRecipe;
    private AnyEnum<AdaptationReason> _reasonRecipe;
    private AnyDouble                 _predictedDownloadSecondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdaptationDecision()
        : this(segment:                  AnyValidSegment(),
               previous:                 AnyValidPrevious(),
               reason:                   AnyValidReason(),
               predictedDownloadSeconds: AnyValidPredictedDownloadSeconds()) { }

    private static AnySegment AnyValidSegment() {
        return new AnySegment();
    }

    private static AnyEncodingVariant AnyValidPrevious() {
        // Interior bitrates only, so that a move up and a move down both exist from the drawn variant.
        return new AnyEncodingVariant().WithBitrate(b => b.WithKbps(k => k.Between(100, 99_000)));
    }

    private static AnyEnum<AdaptationReason> AnyValidReason() {
        return Any.Enum<AdaptationReason>();
    }

    private static AnyDouble AnyValidPredictedDownloadSeconds() {
        return Any.Double().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAdaptationDecision(AnySegment                segment,
                                  AnyEncodingVariant        previous,
                                  AnyEnum<AdaptationReason> reason,
                                  AnyDouble                 predictedDownloadSeconds) {
        _segment                        = segment;
        _segmentRecipe                  = segment;
        _previous                       = previous;
        _previousRecipe                 = previous;
        _reason                         = reason;
        _reasonRecipe                   = reason;
        _predictedDownloadSeconds       = predictedDownloadSeconds;
        _predictedDownloadSecondsRecipe = predictedDownloadSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAdaptationDecision(AnyAdaptationDecision from) {
        _segment                        = from._segment;
        _segmentRecipe                  = from._segmentRecipe;
        _previous                       = from._previous;
        _previousRecipe                 = from._previousRecipe;
        _reason                         = from._reason;
        _reasonRecipe                   = from._reasonRecipe;
        _predictedDownloadSeconds       = from._predictedDownloadSeconds;
        _predictedDownloadSecondsRecipe = from._predictedDownloadSecondsRecipe;
    }

    /// <summary>Pins <c>segment</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationDecision WithSegment(Segment value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._segment = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>segment</c> is drawn from — <c>AnyValidSegment()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySegment, AnySegment&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationDecision WithSegment(Func<AnySegment, AnySegment> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._segmentRecipe = configure(_segmentRecipe);
        copy._segment       = copy._segmentRecipe;

        return copy;
    }

    /// <summary>Pins <c>previous</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationDecision WithPrevious(EncodingVariant value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._previous = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>previous</c> is drawn from — <c>AnyValidPrevious()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEncodingVariant, AnyEncodingVariant&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationDecision WithPrevious(Func<AnyEncodingVariant, AnyEncodingVariant> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._previousRecipe = configure(_previousRecipe);
        copy._previous       = copy._previousRecipe;

        return copy;
    }

    /// <summary>Pins <c>reason</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationDecision WithReason(AdaptationReason value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._reason = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reason</c> is drawn from — <c>AnyValidReason()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;AdaptationReason&gt;, AnyEnum&lt;AdaptationReason&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationDecision WithReason(Func<AnyEnum<AdaptationReason>, AnyEnum<AdaptationReason>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._reasonRecipe = configure(_reasonRecipe);
        copy._reason       = copy._reasonRecipe;

        return copy;
    }

    /// <summary>Pins <c>predictedDownloadSeconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationDecision WithPredictedDownloadSeconds(double value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._predictedDownloadSeconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>predictedDownloadSeconds</c> is drawn from — <c>AnyValidPredictedDownloadSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationDecision WithPredictedDownloadSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._predictedDownloadSecondsRecipe = configure(_predictedDownloadSecondsRecipe);
        copy._predictedDownloadSeconds       = copy._predictedDownloadSecondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AdaptationDecision" />.</summary>
    public AdaptationDecision Generate() {
        EncodingVariant   previous = _previous.Generate();
        AdaptationReason  reason   = _reason.Generate();

        // The reason must agree with the move: chosen is derived from previous, not drawn on its own.
        EncodingVariant chosen = reason switch {
            AdaptationReason.Hold       => previous,
            AdaptationReason.SwitchUp   => AnyChosen().WithBitrate(b => b.WithKbps(k => k.GreaterThan(previous.Bitrate.Kbps))).Generate(),
            AdaptationReason.SwitchDown => AnyChosen().WithBitrate(b => b.WithKbps(k => k.LessThan(previous.Bitrate.Kbps))).Generate(),
            AdaptationReason.Panic      => AnyChosen().WithBitrate(b => b.WithKbps(k => k.LessThanOrEqualTo(previous.Bitrate.Kbps))).Generate(),
            _                           => AnyChosen().Generate()
        };

        return new AdaptationDecision(_segment.Generate(),
                                      previous,
                                      chosen,
                                      reason,
                                      _predictedDownloadSeconds.Generate());
    }

    private static AnyEncodingVariant AnyChosen() {
        return new AnyEncodingVariant();
    }

}
