// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Prediction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="Prediction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrediction : IAny<Prediction> {

    private IAny<Species>       _species;
    private IAny<double>        _madeAtHours;
    private IAny<double>        _horizonHours;
    private IAny<Concentration> _expected;
    private IAny<double>        _relativeUncertainty;

    private AnySpecies       _speciesRecipe;
    private AnyDouble        _madeAtHoursRecipe;
    private AnyDouble        _horizonHoursRecipe;
    private AnyConcentration _expectedRecipe;
    private AnyDouble        _relativeUncertaintyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrediction()
        : this(species:             AnyValidSpecies(),
               madeAtHours:         AnyValidMadeAtHours(),
               horizonHours:        AnyValidHorizonHours(),
               expected:            AnyValidExpected(),
               relativeUncertainty: AnyValidRelativeUncertainty()) { }

    private static AnySpecies AnyValidSpecies() {
        return new AnySpecies();
    }

    private static AnyDouble AnyValidMadeAtHours() {
        return Any.Double().Between(0d, 10_000d);
    }

    private static AnyDouble AnyValidHorizonHours() {
        return Any.Double().Positive().LessThanOrEqualTo(Prediction.MaxHorizonHours);
    }

    private static AnyConcentration AnyValidExpected() {
        return new AnyConcentration();
    }

    private static AnyDouble AnyValidRelativeUncertainty() {
        return Any.Double().Positive().LessThanOrEqualTo(1d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPrediction(AnySpecies       species,
                          AnyDouble        madeAtHours,
                          AnyDouble        horizonHours,
                          AnyConcentration expected,
                          AnyDouble        relativeUncertainty) {
        _species                   = species;
        _speciesRecipe             = species;
        _madeAtHours               = madeAtHours;
        _madeAtHoursRecipe         = madeAtHours;
        _horizonHours              = horizonHours;
        _horizonHoursRecipe        = horizonHours;
        _expected                  = expected;
        _expectedRecipe            = expected;
        _relativeUncertainty       = relativeUncertainty;
        _relativeUncertaintyRecipe = relativeUncertainty;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPrediction(AnyPrediction from) {
        _species                   = from._species;
        _speciesRecipe             = from._speciesRecipe;
        _madeAtHours               = from._madeAtHours;
        _madeAtHoursRecipe         = from._madeAtHoursRecipe;
        _horizonHours              = from._horizonHours;
        _horizonHoursRecipe        = from._horizonHoursRecipe;
        _expected                  = from._expected;
        _expectedRecipe            = from._expectedRecipe;
        _relativeUncertainty       = from._relativeUncertainty;
        _relativeUncertaintyRecipe = from._relativeUncertaintyRecipe;
    }

    /// <summary>Pins <c>species</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrediction WithSpecies(Species value) {
        AnyPrediction copy = new AnyPrediction(this);

        copy._species = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>species</c> is drawn from — <c>AnyValidSpecies()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpecies, AnySpecies&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrediction WithSpecies(Func<AnySpecies, AnySpecies> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrediction copy = new AnyPrediction(this);

        copy._speciesRecipe = configure(_speciesRecipe);
        copy._species       = copy._speciesRecipe;

        return copy;
    }

    /// <summary>Pins <c>madeAtHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrediction WithMadeAtHours(double value) {
        AnyPrediction copy = new AnyPrediction(this);

        copy._madeAtHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>madeAtHours</c> is drawn from — <c>AnyValidMadeAtHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrediction WithMadeAtHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrediction copy = new AnyPrediction(this);

        copy._madeAtHoursRecipe = configure(_madeAtHoursRecipe);
        copy._madeAtHours       = copy._madeAtHoursRecipe;

        return copy;
    }

    /// <summary>Pins <c>horizonHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrediction WithHorizonHours(double value) {
        AnyPrediction copy = new AnyPrediction(this);

        copy._horizonHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>horizonHours</c> is drawn from — <c>AnyValidHorizonHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrediction WithHorizonHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrediction copy = new AnyPrediction(this);

        copy._horizonHoursRecipe = configure(_horizonHoursRecipe);
        copy._horizonHours       = copy._horizonHoursRecipe;

        return copy;
    }

    /// <summary>Pins <c>expected</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrediction WithExpected(Concentration value) {
        AnyPrediction copy = new AnyPrediction(this);

        copy._expected = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>expected</c> is drawn from — <c>AnyValidExpected()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyConcentration, AnyConcentration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrediction WithExpected(Func<AnyConcentration, AnyConcentration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrediction copy = new AnyPrediction(this);

        copy._expectedRecipe = configure(_expectedRecipe);
        copy._expected       = copy._expectedRecipe;

        return copy;
    }

    /// <summary>Pins <c>relativeUncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrediction WithRelativeUncertainty(double value) {
        AnyPrediction copy = new AnyPrediction(this);

        copy._relativeUncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>relativeUncertainty</c> is drawn from — <c>AnyValidRelativeUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrediction WithRelativeUncertainty(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrediction copy = new AnyPrediction(this);

        copy._relativeUncertaintyRecipe = configure(_relativeUncertaintyRecipe);
        copy._relativeUncertainty       = copy._relativeUncertaintyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Prediction" />.</summary>
    public Prediction Generate() {
        return new Prediction(_species.Generate(),
                              _madeAtHours.Generate(),
                              _horizonHours.Generate(),
                              _expected.Generate(),
                              _relativeUncertainty.Generate());
    }

}
