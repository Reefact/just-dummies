// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RiskAssessment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RiskAssessment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRiskAssessment : IAny<RiskAssessment> {

    private IAny<Epoch>                _timeOfClosestApproach;
    private IAny<double>               _missDistanceKm;
    private IAny<double>               _relativeSpeedKmPerSecond;
    private IAny<double>               _combinedSigmaKm;
    private IAny<CollisionProbability> _probability;

    private AnyEpoch                _timeOfClosestApproachRecipe;
    private AnyDouble               _missDistanceKmRecipe;
    private AnyDouble               _relativeSpeedKmPerSecondRecipe;
    private AnyDouble               _combinedSigmaKmRecipe;
    private AnyCollisionProbability _probabilityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRiskAssessment()
        : this(timeOfClosestApproach:    AnyValidTimeOfClosestApproach(),
               missDistanceKm:           AnyValidMissDistanceKm(),
               relativeSpeedKmPerSecond: AnyValidRelativeSpeedKmPerSecond(),
               combinedSigmaKm:          AnyValidCombinedSigmaKm(),
               probability:              AnyValidProbability()) { }

    private static AnyEpoch AnyValidTimeOfClosestApproach() {
        return new AnyEpoch();
    }

    private static AnyDouble AnyValidMissDistanceKm() {
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    private static AnyDouble AnyValidRelativeSpeedKmPerSecond() {
        return Any.Double().Positive().LessThanOrEqualTo(24d);
    }

    private static AnyDouble AnyValidCombinedSigmaKm() {
        return Any.Double().Positive();
    }

    private static AnyCollisionProbability AnyValidProbability() {
        return new AnyCollisionProbability();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRiskAssessment(AnyEpoch                timeOfClosestApproach,
                              AnyDouble               missDistanceKm,
                              AnyDouble               relativeSpeedKmPerSecond,
                              AnyDouble               combinedSigmaKm,
                              AnyCollisionProbability probability) {
        _timeOfClosestApproach          = timeOfClosestApproach;
        _timeOfClosestApproachRecipe    = timeOfClosestApproach;
        _missDistanceKm                 = missDistanceKm;
        _missDistanceKmRecipe           = missDistanceKm;
        _relativeSpeedKmPerSecond       = relativeSpeedKmPerSecond;
        _relativeSpeedKmPerSecondRecipe = relativeSpeedKmPerSecond;
        _combinedSigmaKm                = combinedSigmaKm;
        _combinedSigmaKmRecipe          = combinedSigmaKm;
        _probability                    = probability;
        _probabilityRecipe              = probability;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRiskAssessment(AnyRiskAssessment from) {
        _timeOfClosestApproach          = from._timeOfClosestApproach;
        _timeOfClosestApproachRecipe    = from._timeOfClosestApproachRecipe;
        _missDistanceKm                 = from._missDistanceKm;
        _missDistanceKmRecipe           = from._missDistanceKmRecipe;
        _relativeSpeedKmPerSecond       = from._relativeSpeedKmPerSecond;
        _relativeSpeedKmPerSecondRecipe = from._relativeSpeedKmPerSecondRecipe;
        _combinedSigmaKm                = from._combinedSigmaKm;
        _combinedSigmaKmRecipe          = from._combinedSigmaKmRecipe;
        _probability                    = from._probability;
        _probabilityRecipe              = from._probabilityRecipe;
    }

    /// <summary>Pins <c>timeOfClosestApproach</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRiskAssessment WithTimeOfClosestApproach(Epoch value) {
        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._timeOfClosestApproach = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>timeOfClosestApproach</c> is drawn from — <c>AnyValidTimeOfClosestApproach()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEpoch, AnyEpoch&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRiskAssessment WithTimeOfClosestApproach(Func<AnyEpoch, AnyEpoch> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._timeOfClosestApproachRecipe = configure(_timeOfClosestApproachRecipe);
        copy._timeOfClosestApproach       = copy._timeOfClosestApproachRecipe;

        return copy;
    }

    /// <summary>Pins <c>missDistanceKm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRiskAssessment WithMissDistanceKm(double value) {
        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._missDistanceKm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>missDistanceKm</c> is drawn from — <c>AnyValidMissDistanceKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRiskAssessment WithMissDistanceKm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._missDistanceKmRecipe = configure(_missDistanceKmRecipe);
        copy._missDistanceKm       = copy._missDistanceKmRecipe;

        return copy;
    }

    /// <summary>Pins <c>relativeSpeedKmPerSecond</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRiskAssessment WithRelativeSpeedKmPerSecond(double value) {
        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._relativeSpeedKmPerSecond = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>relativeSpeedKmPerSecond</c> is drawn from — <c>AnyValidRelativeSpeedKmPerSecond()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRiskAssessment WithRelativeSpeedKmPerSecond(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._relativeSpeedKmPerSecondRecipe = configure(_relativeSpeedKmPerSecondRecipe);
        copy._relativeSpeedKmPerSecond       = copy._relativeSpeedKmPerSecondRecipe;

        return copy;
    }

    /// <summary>Pins <c>combinedSigmaKm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRiskAssessment WithCombinedSigmaKm(double value) {
        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._combinedSigmaKm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>combinedSigmaKm</c> is drawn from — <c>AnyValidCombinedSigmaKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRiskAssessment WithCombinedSigmaKm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._combinedSigmaKmRecipe = configure(_combinedSigmaKmRecipe);
        copy._combinedSigmaKm       = copy._combinedSigmaKmRecipe;

        return copy;
    }

    /// <summary>Pins <c>probability</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRiskAssessment WithProbability(CollisionProbability value) {
        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._probability = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>probability</c> is drawn from — <c>AnyValidProbability()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCollisionProbability, AnyCollisionProbability&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRiskAssessment WithProbability(Func<AnyCollisionProbability, AnyCollisionProbability> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRiskAssessment copy = new AnyRiskAssessment(this);

        copy._probabilityRecipe = configure(_probabilityRecipe);
        copy._probability       = copy._probabilityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RiskAssessment" />.</summary>
    public RiskAssessment Generate() {
        return new RiskAssessment(_timeOfClosestApproach.Generate(),
                                  _missDistanceKm.Generate(),
                                  _relativeSpeedKmPerSecond.Generate(),
                                  _combinedSigmaKm.Generate(),
                                  _probability.Generate());
    }

}
