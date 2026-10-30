// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PrescriptionLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PrescriptionLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrescriptionLine : IAny<PrescriptionLine> {

    private readonly IAny<Molecule>              _molecule;
    private readonly IAny<Dosage>                _dosage;
    private readonly IAny<RouteOfAdministration> _route;
    private readonly IAny<TreatmentDuration>     _duration;

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
        // A tablet is swallowed or melted and a puff is inhaled: the route depends on the unit, so the
        // default keeps the units every route accepts. A test wanting a tablet pins unit and route together.
        return new AnyDosage().WithUnit(u => u.Except(DoseUnit.Tablet, DoseUnit.Puff));
    }

    private static AnyEnum<RouteOfAdministration> AnyValidRoute() {
        return Any.Enum<RouteOfAdministration>();
    }

    private static AnyTreatmentDuration AnyValidDuration() {
        return new AnyTreatmentDuration();
    }

    private AnyPrescriptionLine(IAny<Molecule>              molecule,
                                IAny<Dosage>                dosage,
                                IAny<RouteOfAdministration> route,
                                IAny<TreatmentDuration>     duration) {
        _molecule = molecule;
        _dosage   = dosage;
        _route    = route;
        _duration = duration;
    }

    /// <summary>Pins <c>molecule</c> to a fixed value.</summary>
    public AnyPrescriptionLine WithMolecule(Molecule value) {
        return new AnyPrescriptionLine(new FixedValue<Molecule>(value), _dosage, _route, _duration);
    }

    /// <summary>Draws <c>molecule</c> from <c>AnyValidMolecule()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescriptionLine WithMolecule(Func<AnyMolecule, AnyMolecule> configure) {
        return new AnyPrescriptionLine(configure(AnyValidMolecule()), _dosage, _route, _duration);
    }

    /// <summary>Pins <c>dosage</c> to a fixed value.</summary>
    public AnyPrescriptionLine WithDosage(Dosage value) {
        return new AnyPrescriptionLine(_molecule, new FixedValue<Dosage>(value), _route, _duration);
    }

    /// <summary>Draws <c>dosage</c> from <c>AnyValidDosage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescriptionLine WithDosage(Func<AnyDosage, AnyDosage> configure) {
        return new AnyPrescriptionLine(_molecule, configure(AnyValidDosage()), _route, _duration);
    }

    /// <summary>Pins <c>route</c> to a fixed value.</summary>
    public AnyPrescriptionLine WithRoute(RouteOfAdministration value) {
        return new AnyPrescriptionLine(_molecule, _dosage, new FixedValue<RouteOfAdministration>(value), _duration);
    }

    /// <summary>Draws <c>route</c> from <c>AnyValidRoute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescriptionLine WithRoute(Func<AnyEnum<RouteOfAdministration>, AnyEnum<RouteOfAdministration>> configure) {
        return new AnyPrescriptionLine(_molecule, _dosage, configure(AnyValidRoute()), _duration);
    }

    /// <summary>Pins <c>duration</c> to a fixed value.</summary>
    public AnyPrescriptionLine WithDuration(TreatmentDuration value) {
        return new AnyPrescriptionLine(_molecule, _dosage, _route, new FixedValue<TreatmentDuration>(value));
    }

    /// <summary>Draws <c>duration</c> from <c>AnyValidDuration()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPrescriptionLine WithDuration(Func<AnyTreatmentDuration, AnyTreatmentDuration> configure) {
        return new AnyPrescriptionLine(_molecule, _dosage, _route, configure(AnyValidDuration()));
    }

    /// <summary>Produces one arbitrary <see cref="PrescriptionLine" />.</summary>
    public PrescriptionLine Generate() {
        return new PrescriptionLine(_molecule.Generate(),
                                    _dosage.Generate(),
                                    _route.Generate(),
                                    _duration.Generate());
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
