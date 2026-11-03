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
    private IAny<EncodingVariant>  _chosen;
    private AdaptationReason?     _reasonPin;
    private IAny<double>           _predictedDownloadSeconds;

    private AnySegment                _segmentRecipe;
    private AnyEncodingVariant        _previousRecipe;
    private AnyEncodingVariant        _chosenRecipe;
    private AnyDouble                 _predictedDownloadSecondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdaptationDecision()
        : this(segment:                  AnyValidSegment(),
               previous:                 AnyValidPrevious(),
               chosen:                   AnyValidChosen(),
               predictedDownloadSeconds: AnyValidPredictedDownloadSeconds()) { }

    private static AnySegment AnyValidSegment() {
        return new AnySegment();
    }

    private static AnyEncodingVariant AnyValidPrevious() {
        return new AnyEncodingVariant();
    }

    private static AnyEncodingVariant AnyValidChosen() {
        return new AnyEncodingVariant();
    }

    private static AnyDouble AnyValidPredictedDownloadSeconds() {
        return Any.Double().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAdaptationDecision(AnySegment                segment,
                                  AnyEncodingVariant        previous,
                                  AnyEncodingVariant        chosen,
                                  AnyDouble                 predictedDownloadSeconds) {
        _segment                        = segment;
        _segmentRecipe                  = segment;
        _previous                       = previous;
        _previousRecipe                 = previous;
        _chosen                         = chosen;
        _chosenRecipe                   = chosen;
        _predictedDownloadSeconds       = predictedDownloadSeconds;
        _predictedDownloadSecondsRecipe = predictedDownloadSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAdaptationDecision(AnyAdaptationDecision from) {
        _segment                        = from._segment;
        _segmentRecipe                  = from._segmentRecipe;
        _previous                       = from._previous;
        _previousRecipe                 = from._previousRecipe;
        _chosen                         = from._chosen;
        _chosenRecipe                   = from._chosenRecipe;
        _reasonPin                      = from._reasonPin;
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

    /// <summary>Pins <c>chosen</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationDecision WithChosen(EncodingVariant value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._chosen = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>chosen</c> is drawn from — <c>AnyValidChosen()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEncodingVariant, AnyEncodingVariant&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationDecision WithChosen(Func<AnyEncodingVariant, AnyEncodingVariant> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._chosenRecipe = configure(_chosenRecipe);
        copy._chosen       = copy._chosenRecipe;

        return copy;
    }

    /// <summary>
    ///     Pins <c>reason</c>. Left alone, the reason is drawn among those that agree with the move from <c>previous</c>
    ///     to <c>chosen</c> (the constructor refuses the others), so it has no refining overload.
    /// </summary>
    public AnyAdaptationDecision WithReason(AdaptationReason value) {
        AnyAdaptationDecision copy = new AnyAdaptationDecision(this);

        copy._reasonPin = value;

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
        EncodingVariant previous = _previous.Generate();
        EncodingVariant chosen   = _chosen.Generate();

        // The constructor ties the reason to the move, so the reason is drawn among those the move allows.
        int                direction = chosen.Bitrate.CompareTo(previous.Bitrate);
        AdaptationReason[] allowed   = direction == 0 ? [AdaptationReason.Startup, AdaptationReason.Hold, AdaptationReason.Panic]
                                     : direction > 0  ? [AdaptationReason.Startup, AdaptationReason.SwitchUp]
                                                      : [AdaptationReason.Startup, AdaptationReason.SwitchDown, AdaptationReason.Panic];
        AdaptationReason reason = _reasonPin ?? Any.ElementOf(allowed).Generate();

        return new AdaptationDecision(_segment.Generate(),
                                      previous,
                                      chosen,
                                      reason,
                                      _predictedDownloadSeconds.Generate());
    }

}
