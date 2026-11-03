// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PrescriptionLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PrescriptionLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrescriptionLine : IAny<PrescriptionLine> {

    private IAny<Molecule>              _molecule;
    private IAny<Dosage>                _dosage;
    private IAny<RouteOfAdministration> _route;
    private IAny<TreatmentDuration>     _duration;

    private Dosage?                        _fixedDosage;
    private RouteOfAdministration?         _fixedRoute;

    private AnyMolecule                    _moleculeRecipe;
    private AnyDosage                      _dosageRecipe;
    private AnyEnum<RouteOfAdministration> _routeRecipe;
    private AnyTreatmentDuration           _durationRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrescriptionLine()
        : this(molecule: AnyValidMolecule(),
               dosage:   AnyValidDosage(),
               route:    AnyValidRoute(),
               duration: AnyValidDuration()) { }

    private static AnyMolecule AnyValidMolecule() {
        return new AnyMolecule();
    }

    private static AnyDosage AnyValidDosage() {
        return new AnyDosage();
    }

    private static AnyEnum<RouteOfAdministration> AnyValidRoute() {
        return Any.Enum<RouteOfAdministration>();
    }

    private static AnyTreatmentDuration AnyValidDuration() {
        return new AnyTreatmentDuration();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPrescriptionLine(AnyMolecule                    molecule,
                                AnyDosage                      dosage,
                                AnyEnum<RouteOfAdministration> route,
                                AnyTreatmentDuration           duration) {
        _molecule       = molecule;
        _moleculeRecipe = molecule;
        _dosage         = dosage;
        _dosageRecipe   = dosage;
        _route          = route;
        _routeRecipe    = route;
        _duration       = duration;
        _durationRecipe = duration;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPrescriptionLine(AnyPrescriptionLine from) {
        _molecule       = from._molecule;
        _moleculeRecipe = from._moleculeRecipe;
        _dosage         = from._dosage;
        _dosageRecipe   = from._dosageRecipe;
        _route          = from._route;
        _routeRecipe    = from._routeRecipe;
        _duration       = from._duration;
        _durationRecipe = from._durationRecipe;
        _fixedDosage    = from._fixedDosage;
        _fixedRoute     = from._fixedRoute;
    }

    /// <summary>Pins <c>molecule</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescriptionLine WithMolecule(Molecule value) {
        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._molecule = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>molecule</c> is drawn from — <c>AnyValidMolecule()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMolecule, AnyMolecule&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescriptionLine WithMolecule(Func<AnyMolecule, AnyMolecule> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._moleculeRecipe = configure(_moleculeRecipe);
        copy._molecule       = copy._moleculeRecipe;

        return copy;
    }

    /// <summary>Pins <c>dosage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescriptionLine WithDosage(Dosage value) {
        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._dosage      = Any.Fixed(value);
        copy._fixedDosage = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>dosage</c> is drawn from — <c>AnyValidDosage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDosage, AnyDosage&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescriptionLine WithDosage(Func<AnyDosage, AnyDosage> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._dosageRecipe = configure(_dosageRecipe);
        copy._dosage       = copy._dosageRecipe;

        return copy;
    }

    /// <summary>Pins <c>route</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescriptionLine WithRoute(RouteOfAdministration value) {
        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._route      = Any.Fixed(value);
        copy._fixedRoute = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>route</c> is drawn from — <c>AnyValidRoute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RouteOfAdministration&gt;, AnyEnum&lt;RouteOfAdministration&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescriptionLine WithRoute(Func<AnyEnum<RouteOfAdministration>, AnyEnum<RouteOfAdministration>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._routeRecipe = configure(_routeRecipe);
        copy._route       = copy._routeRecipe;

        return copy;
    }

    /// <summary>Pins <c>duration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescriptionLine WithDuration(TreatmentDuration value) {
        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>duration</c> is drawn from — <c>AnyValidDuration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTreatmentDuration, AnyTreatmentDuration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescriptionLine WithDuration(Func<AnyTreatmentDuration, AnyTreatmentDuration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescriptionLine copy = new AnyPrescriptionLine(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PrescriptionLine" />. The unit of the dose and the route are drawn together, since the line only accepts some pairs.</summary>
    public PrescriptionLine Generate() {
        Dosage                dosage;
        RouteOfAdministration route;

        if (_fixedDosage is not null) {
            dosage = _fixedDosage;
            route  = _fixedRoute ?? _routeRecipe.Except(RoutesRefusedFor(dosage.Unit)).Generate();
        } else if (_fixedRoute is not null) {
            route  = _fixedRoute.Value;
            dosage = _dosageRecipe.WithUnit(u => u.Except(UnitsRefusedFor(route))).Generate();
        } else {
            dosage = _dosage.Generate();
            route  = _routeRecipe.Except(RoutesRefusedFor(dosage.Unit)).Generate();
        }

        return new PrescriptionLine(_molecule.Generate(), dosage, route, _duration.Generate());
    }

    private static RouteOfAdministration[] RoutesRefusedFor(DoseUnit unit) {
        return unit switch {
            DoseUnit.Tablet => new[] { RouteOfAdministration.Intravenous, RouteOfAdministration.Intramuscular, RouteOfAdministration.Subcutaneous, RouteOfAdministration.Topical, RouteOfAdministration.Inhaled, RouteOfAdministration.Rectal },
            DoseUnit.Puff   => new[] { RouteOfAdministration.Oral, RouteOfAdministration.Sublingual, RouteOfAdministration.Intravenous, RouteOfAdministration.Intramuscular, RouteOfAdministration.Subcutaneous, RouteOfAdministration.Topical, RouteOfAdministration.Rectal },
            _               => Array.Empty<RouteOfAdministration>()
        };
    }

    private static DoseUnit[] UnitsRefusedFor(RouteOfAdministration route) {
        return route switch {
            RouteOfAdministration.Oral or RouteOfAdministration.Sublingual => new[] { DoseUnit.Puff },
            RouteOfAdministration.Inhaled                                  => new[] { DoseUnit.Tablet },
            _                                                              => new[] { DoseUnit.Tablet, DoseUnit.Puff }
        };
    }

}
