// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MicrobialPopulation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="MicrobialPopulation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMicrobialPopulation : IAny<MicrobialPopulation> {

    private IAny<Species>       _species;
    private IAny<Concentration> _concentration;
    private IAny<double>        _relativeUncertainty;
    private IAny<double>        _ageHours;

    private AnySpecies       _speciesRecipe;
    private AnyConcentration _concentrationRecipe;
    private AnyDouble        _relativeUncertaintyRecipe;
    private AnyDouble        _ageHoursRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMicrobialPopulation()
        : this(species:             AnyValidSpecies(),
               concentration:       AnyValidConcentration(),
               relativeUncertainty: AnyValidRelativeUncertainty(),
               ageHours:            AnyValidAgeHours()) { }

    private static AnySpecies AnyValidSpecies() {
        return new AnySpecies();
    }

    private static AnyConcentration AnyValidConcentration() {
        return new AnyConcentration();
    }

    private static AnyDouble AnyValidRelativeUncertainty() {
        return Any.Double().Positive().LessThanOrEqualTo(1d);
    }

    private static AnyDouble AnyValidAgeHours() {
        return Any.Double().Between(0d, 10_000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMicrobialPopulation(AnySpecies       species,
                                   AnyConcentration concentration,
                                   AnyDouble        relativeUncertainty,
                                   AnyDouble        ageHours) {
        _species                   = species;
        _speciesRecipe             = species;
        _concentration             = concentration;
        _concentrationRecipe       = concentration;
        _relativeUncertainty       = relativeUncertainty;
        _relativeUncertaintyRecipe = relativeUncertainty;
        _ageHours                  = ageHours;
        _ageHoursRecipe            = ageHours;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMicrobialPopulation(AnyMicrobialPopulation from) {
        _species                   = from._species;
        _speciesRecipe             = from._speciesRecipe;
        _concentration             = from._concentration;
        _concentrationRecipe       = from._concentrationRecipe;
        _relativeUncertainty       = from._relativeUncertainty;
        _relativeUncertaintyRecipe = from._relativeUncertaintyRecipe;
        _ageHours                  = from._ageHours;
        _ageHoursRecipe            = from._ageHoursRecipe;
    }

    /// <summary>Pins <c>species</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMicrobialPopulation WithSpecies(Species value) {
        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._species = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>species</c> is drawn from — <c>AnyValidSpecies()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpecies, AnySpecies&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMicrobialPopulation WithSpecies(Func<AnySpecies, AnySpecies> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._speciesRecipe = configure(_speciesRecipe);
        copy._species       = copy._speciesRecipe;

        return copy;
    }

    /// <summary>Pins <c>concentration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMicrobialPopulation WithConcentration(Concentration value) {
        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._concentration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>concentration</c> is drawn from — <c>AnyValidConcentration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyConcentration, AnyConcentration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMicrobialPopulation WithConcentration(Func<AnyConcentration, AnyConcentration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._concentrationRecipe = configure(_concentrationRecipe);
        copy._concentration       = copy._concentrationRecipe;

        return copy;
    }

    /// <summary>Pins <c>relativeUncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMicrobialPopulation WithRelativeUncertainty(double value) {
        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._relativeUncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>relativeUncertainty</c> is drawn from — <c>AnyValidRelativeUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMicrobialPopulation WithRelativeUncertainty(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._relativeUncertaintyRecipe = configure(_relativeUncertaintyRecipe);
        copy._relativeUncertainty       = copy._relativeUncertaintyRecipe;

        return copy;
    }

    /// <summary>Pins <c>ageHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMicrobialPopulation WithAgeHours(double value) {
        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._ageHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ageHours</c> is drawn from — <c>AnyValidAgeHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMicrobialPopulation WithAgeHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMicrobialPopulation copy = new AnyMicrobialPopulation(this);

        copy._ageHoursRecipe = configure(_ageHoursRecipe);
        copy._ageHours       = copy._ageHoursRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MicrobialPopulation" />.</summary>
    public MicrobialPopulation Generate() {
        return new MicrobialPopulation(_species.Generate(),
                                       _concentration.Generate(),
                                       _relativeUncertainty.Generate(),
                                       _ageHours.Generate());
    }

}
