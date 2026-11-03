// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Observation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Observation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyObservation : IAny<Observation> {

    private IAny<CultureId>         _cultureId;
    private IAny<Species>           _species;
    private IAny<double>            _elapsedHours;
    private IAny<Concentration>     _measured;
    private IAny<double>            _relativeUncertainty;
    private IAny<ObservationMethod> _method;

    private AnyCultureId               _cultureIdRecipe;
    private AnySpecies                 _speciesRecipe;
    private AnyDouble                  _elapsedHoursRecipe;
    private AnyConcentration           _measuredRecipe;
    private AnyDouble                  _relativeUncertaintyRecipe;
    private AnyEnum<ObservationMethod> _methodRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyObservation()
        : this(cultureId:           AnyValidCultureId(),
               species:             AnyValidSpecies(),
               elapsedHours:        AnyValidElapsedHours(),
               measured:            AnyValidMeasured(),
               relativeUncertainty: AnyValidRelativeUncertainty(),
               method:              AnyValidMethod()) { }

    private static AnyCultureId AnyValidCultureId() {
        return new AnyCultureId();
    }

    private static AnySpecies AnyValidSpecies() {
        return new AnySpecies();
    }

    private static AnyDouble AnyValidElapsedHours() {
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    private static AnyConcentration AnyValidMeasured() {
        return new AnyConcentration();
    }

    private static AnyDouble AnyValidRelativeUncertainty() {
        // An optical-density reading cannot claim less than 10 %: 0.1 to 1 is valid whichever method is drawn.
        // A test about a finer uncertainty pins both the uncertainty and a direct method.
        return Any.Double().Between(Observation.MinIndirectRelativeUncertainty, 1d);
    }

    private static AnyEnum<ObservationMethod> AnyValidMethod() {
        return Any.Enum<ObservationMethod>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyObservation(AnyCultureId               cultureId,
                           AnySpecies                 species,
                           AnyDouble                  elapsedHours,
                           AnyConcentration           measured,
                           AnyDouble                  relativeUncertainty,
                           AnyEnum<ObservationMethod> method) {
        _cultureId                 = cultureId;
        _cultureIdRecipe           = cultureId;
        _species                   = species;
        _speciesRecipe             = species;
        _elapsedHours              = elapsedHours;
        _elapsedHoursRecipe        = elapsedHours;
        _measured                  = measured;
        _measuredRecipe            = measured;
        _relativeUncertainty       = relativeUncertainty;
        _relativeUncertaintyRecipe = relativeUncertainty;
        _method                    = method;
        _methodRecipe              = method;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyObservation(AnyObservation from) {
        _cultureId                 = from._cultureId;
        _cultureIdRecipe           = from._cultureIdRecipe;
        _species                   = from._species;
        _speciesRecipe             = from._speciesRecipe;
        _elapsedHours              = from._elapsedHours;
        _elapsedHoursRecipe        = from._elapsedHoursRecipe;
        _measured                  = from._measured;
        _measuredRecipe            = from._measuredRecipe;
        _relativeUncertainty       = from._relativeUncertainty;
        _relativeUncertaintyRecipe = from._relativeUncertaintyRecipe;
        _method                    = from._method;
        _methodRecipe              = from._methodRecipe;
    }

    /// <summary>Pins <c>cultureId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithCultureId(CultureId value) {
        AnyObservation copy = new AnyObservation(this);

        copy._cultureId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cultureId</c> is drawn from — <c>AnyValidCultureId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCultureId, AnyCultureId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithCultureId(Func<AnyCultureId, AnyCultureId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._cultureIdRecipe = configure(_cultureIdRecipe);
        copy._cultureId       = copy._cultureIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>species</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithSpecies(Species value) {
        AnyObservation copy = new AnyObservation(this);

        copy._species = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>species</c> is drawn from — <c>AnyValidSpecies()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpecies, AnySpecies&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithSpecies(Func<AnySpecies, AnySpecies> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._speciesRecipe = configure(_speciesRecipe);
        copy._species       = copy._speciesRecipe;

        return copy;
    }

    /// <summary>Pins <c>elapsedHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithElapsedHours(double value) {
        AnyObservation copy = new AnyObservation(this);

        copy._elapsedHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>elapsedHours</c> is drawn from — <c>AnyValidElapsedHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithElapsedHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._elapsedHoursRecipe = configure(_elapsedHoursRecipe);
        copy._elapsedHours       = copy._elapsedHoursRecipe;

        return copy;
    }

    /// <summary>Pins <c>measured</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithMeasured(Concentration value) {
        AnyObservation copy = new AnyObservation(this);

        copy._measured = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>measured</c> is drawn from — <c>AnyValidMeasured()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyConcentration, AnyConcentration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithMeasured(Func<AnyConcentration, AnyConcentration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._measuredRecipe = configure(_measuredRecipe);
        copy._measured       = copy._measuredRecipe;

        return copy;
    }

    /// <summary>Pins <c>relativeUncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithRelativeUncertainty(double value) {
        AnyObservation copy = new AnyObservation(this);

        copy._relativeUncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>relativeUncertainty</c> is drawn from — <c>AnyValidRelativeUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithRelativeUncertainty(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._relativeUncertaintyRecipe = configure(_relativeUncertaintyRecipe);
        copy._relativeUncertainty       = copy._relativeUncertaintyRecipe;

        return copy;
    }

    /// <summary>Pins <c>method</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyObservation WithMethod(ObservationMethod value) {
        AnyObservation copy = new AnyObservation(this);

        copy._method = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>method</c> is drawn from — <c>AnyValidMethod()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ObservationMethod&gt;, AnyEnum&lt;ObservationMethod&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyObservation WithMethod(Func<AnyEnum<ObservationMethod>, AnyEnum<ObservationMethod>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyObservation copy = new AnyObservation(this);

        copy._methodRecipe = configure(_methodRecipe);
        copy._method       = copy._methodRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Observation" />.</summary>
    public Observation Generate() {
        return new Observation(_cultureId.Generate(),
                               _species.Generate(),
                               _elapsedHours.Generate(),
                               _measured.Generate(),
                               _relativeUncertainty.Generate(),
                               _method.Generate());
    }

}
