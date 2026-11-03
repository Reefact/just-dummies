// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Prescription --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Prescription" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrescription : IAny<Prescription> {

    private IAny<Guid>                            _id;
    private IAny<Patient>                         _patient;
    private IAny<string>                          _prescriberRpps;
    private IAny<DateOnly>                        _issuedOn;
    private IAny<IReadOnlyList<PrescriptionLine>> _lines;

    private AnyGuid                   _idRecipe;
    private AnyPatient                _patientRecipe;
    private AnyPattern                 _prescriberRppsRecipe;
    private AnyDateOnly               _issuedOnRecipe;
    private AnyList<PrescriptionLine> _linesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrescription()
        : this(id:             AnyValidId(),
               patient:        AnyValidPatient(),
               prescriberRpps: AnyValidPrescriberRpps(),
               issuedOn:       AnyValidIssuedOn(),
               lines:          AnyValidLines()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyPatient AnyValidPatient() {

        return new AnyPatient();
    }

    private static AnyPattern AnyValidPrescriberRpps() {

        return Any.StringMatching("[0-9]{11}");
    }

    private static AnyDateOnly AnyValidIssuedOn() {

        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static AnyList<PrescriptionLine> AnyValidLines() {
        return Any.ListOf(new AnyPrescriptionLine()).NonEmpty().WithMaxCount(Prescription.MaxLines);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPrescription(AnyGuid                   id,
                            AnyPatient                patient,
                            AnyPattern                 prescriberRpps,
                            AnyDateOnly               issuedOn,
                            AnyList<PrescriptionLine> lines) {
        _id                   = id;
        _idRecipe             = id;
        _patient              = patient;
        _patientRecipe        = patient;
        _prescriberRpps       = prescriberRpps;
        _prescriberRppsRecipe = prescriberRpps;
        _issuedOn             = issuedOn;
        _issuedOnRecipe       = issuedOn;
        _lines                = lines;
        _linesRecipe          = lines;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPrescription(AnyPrescription from) {
        _id                   = from._id;
        _idRecipe             = from._idRecipe;
        _patient              = from._patient;
        _patientRecipe        = from._patientRecipe;
        _prescriberRpps       = from._prescriberRpps;
        _prescriberRppsRecipe = from._prescriberRppsRecipe;
        _issuedOn             = from._issuedOn;
        _issuedOnRecipe       = from._issuedOnRecipe;
        _lines                = from._lines;
        _linesRecipe          = from._linesRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescription WithId(Guid value) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescription WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescription copy = new AnyPrescription(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>patient</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescription WithPatient(Patient value) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._patient = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>patient</c> is drawn from — <c>AnyValidPatient()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPatient, AnyPatient&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescription WithPatient(Func<AnyPatient, AnyPatient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescription copy = new AnyPrescription(this);

        copy._patientRecipe = configure(_patientRecipe);
        copy._patient       = copy._patientRecipe;

        return copy;
    }

    /// <summary>Pins <c>prescriberRpps</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescription WithPrescriberRpps(string value) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._prescriberRpps = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>prescriberRpps</c> is drawn from — <c>AnyValidPrescriberRpps()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescription WithPrescriberRpps(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescription copy = new AnyPrescription(this);

        copy._prescriberRppsRecipe = configure(_prescriberRppsRecipe);
        copy._prescriberRpps       = copy._prescriberRppsRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescription WithIssuedOn(DateOnly value) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._issuedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>issuedOn</c> is drawn from — <c>AnyValidIssuedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescription WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescription copy = new AnyPrescription(this);

        copy._issuedOnRecipe = configure(_issuedOnRecipe);
        copy._issuedOn       = copy._issuedOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>lines</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrescription WithLines(IReadOnlyList<PrescriptionLine> value) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._lines = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lines</c> is drawn from — <c>AnyValidLines()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;PrescriptionLine&gt;, AnyList&lt;PrescriptionLine&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrescription WithLines(Func<AnyList<PrescriptionLine>, AnyList<PrescriptionLine>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrescription copy = new AnyPrescription(this);

        copy._linesRecipe = configure(_linesRecipe);
        copy._lines       = copy._linesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Prescription" />.</summary>
    public Prescription Generate() {
        return new Prescription(_id.Generate(),
                                _patient.Generate(),
                                _prescriberRpps.Generate(),
                                _issuedOn.Generate(),
                                _lines.Generate());
    }

}
