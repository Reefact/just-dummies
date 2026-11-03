// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EstimatedState --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="EstimatedState" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEstimatedState : IAny<EstimatedState> {

    private IAny<Species>       _species;
    private IAny<double>        _elapsedHours;
    private IAny<Concentration> _concentration;
    private IAny<double>        _relativeUncertainty;
    private IAny<GrowthPhase>   _phase;
    private IAny<int>           _observationCount;

    private AnySpecies           _speciesRecipe;
    private AnyDouble            _elapsedHoursRecipe;
    private AnyConcentration     _concentrationRecipe;
    private AnyDouble            _relativeUncertaintyRecipe;
    private AnyEnum<GrowthPhase> _phaseRecipe;
    private AnyInt32             _observationCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEstimatedState()
        : this(species:             AnyValidSpecies(),
               elapsedHours:        AnyValidElapsedHours(),
               concentration:       AnyValidConcentration(),
               relativeUncertainty: AnyValidRelativeUncertainty(),
               phase:               AnyValidPhase(),
               observationCount:    AnyValidObservationCount()) { }

    private static AnySpecies AnyValidSpecies() {
        return new AnySpecies();
    }

    private static AnyDouble AnyValidElapsedHours() {
        return Any.Double().Between(0d, 10_000d);
    }

    private static AnyConcentration AnyValidConcentration() {
        return new AnyConcentration();
    }

    private static AnyDouble AnyValidRelativeUncertainty() {
        return Any.Double().Positive().LessThanOrEqualTo(1d);
    }

    private static AnyEnum<GrowthPhase> AnyValidPhase() {
        return Any.Enum<GrowthPhase>();
    }

    private static AnyInt32 AnyValidObservationCount() {
        return Any.Int32().Between(0, 1_000);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEstimatedState(AnySpecies           species,
                              AnyDouble            elapsedHours,
                              AnyConcentration     concentration,
                              AnyDouble            relativeUncertainty,
                              AnyEnum<GrowthPhase> phase,
                              AnyInt32             observationCount) {
        _species                   = species;
        _speciesRecipe             = species;
        _elapsedHours              = elapsedHours;
        _elapsedHoursRecipe        = elapsedHours;
        _concentration             = concentration;
        _concentrationRecipe       = concentration;
        _relativeUncertainty       = relativeUncertainty;
        _relativeUncertaintyRecipe = relativeUncertainty;
        _phase                     = phase;
        _phaseRecipe               = phase;
        _observationCount          = observationCount;
        _observationCountRecipe    = observationCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEstimatedState(AnyEstimatedState from) {
        _species                   = from._species;
        _speciesRecipe             = from._speciesRecipe;
        _elapsedHours              = from._elapsedHours;
        _elapsedHoursRecipe        = from._elapsedHoursRecipe;
        _concentration             = from._concentration;
        _concentrationRecipe       = from._concentrationRecipe;
        _relativeUncertainty       = from._relativeUncertainty;
        _relativeUncertaintyRecipe = from._relativeUncertaintyRecipe;
        _phase                     = from._phase;
        _phaseRecipe               = from._phaseRecipe;
        _observationCount          = from._observationCount;
        _observationCountRecipe    = from._observationCountRecipe;
    }

    /// <summary>Pins <c>species</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithSpecies(Species value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._species = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>species</c> is drawn from — <c>AnyValidSpecies()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpecies, AnySpecies&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithSpecies(Func<AnySpecies, AnySpecies> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._speciesRecipe = configure(_speciesRecipe);
        copy._species       = copy._speciesRecipe;

        return copy;
    }

    /// <summary>Pins <c>elapsedHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithElapsedHours(double value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._elapsedHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>elapsedHours</c> is drawn from — <c>AnyValidElapsedHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithElapsedHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._elapsedHoursRecipe = configure(_elapsedHoursRecipe);
        copy._elapsedHours       = copy._elapsedHoursRecipe;

        return copy;
    }

    /// <summary>Pins <c>concentration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithConcentration(Concentration value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._concentration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>concentration</c> is drawn from — <c>AnyValidConcentration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyConcentration, AnyConcentration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithConcentration(Func<AnyConcentration, AnyConcentration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._concentrationRecipe = configure(_concentrationRecipe);
        copy._concentration       = copy._concentrationRecipe;

        return copy;
    }

    /// <summary>Pins <c>relativeUncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithRelativeUncertainty(double value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._relativeUncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>relativeUncertainty</c> is drawn from — <c>AnyValidRelativeUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithRelativeUncertainty(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._relativeUncertaintyRecipe = configure(_relativeUncertaintyRecipe);
        copy._relativeUncertainty       = copy._relativeUncertaintyRecipe;

        return copy;
    }

    /// <summary>Pins <c>phase</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithPhase(GrowthPhase value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._phase = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>phase</c> is drawn from — <c>AnyValidPhase()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;GrowthPhase&gt;, AnyEnum&lt;GrowthPhase&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithPhase(Func<AnyEnum<GrowthPhase>, AnyEnum<GrowthPhase>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._phaseRecipe = configure(_phaseRecipe);
        copy._phase       = copy._phaseRecipe;

        return copy;
    }

    /// <summary>Pins <c>observationCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEstimatedState WithObservationCount(int value) {
        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._observationCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>observationCount</c> is drawn from — <c>AnyValidObservationCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEstimatedState WithObservationCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEstimatedState copy = new AnyEstimatedState(this);

        copy._observationCountRecipe = configure(_observationCountRecipe);
        copy._observationCount       = copy._observationCountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="EstimatedState" />.</summary>
    public EstimatedState Generate() {
        return new EstimatedState(_species.Generate(),
                                  _elapsedHours.Generate(),
                                  _concentration.Generate(),
                                  _relativeUncertainty.Generate(),
                                  _phase.Generate(),
                                  _observationCount.Generate());
    }

}
