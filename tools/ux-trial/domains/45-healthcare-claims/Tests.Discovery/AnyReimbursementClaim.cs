// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReimbursementClaim --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using Clinical.Domain;
using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementClaim" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReimbursementClaim : IAny<ReimbursementClaim> {

    private IAny<ClaimReference>               _reference;
    private IAny<Prescription>                 _prescription;
    private IAny<DateOnly>                     _dispensedOn;
    private IAny<IReadOnlyList<DispensedLine>> _lines;

    private AnyClaimReference      _referenceRecipe;
    private AnyPrescription        _prescriptionRecipe;
    private AnyDateOnly            _dispensedOnRecipe;
    private AnyList<DispensedLine> _linesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReimbursementClaim()
        : this(reference:    AnyValidReference(),
               prescription: AnyValidPrescription(),
               dispensedOn:  AnyValidDispensedOn(),
               lines:        AnyValidLines()) { }

    private static AnyClaimReference AnyValidReference() {
        return new AnyClaimReference();
    }

    private static AnyPrescription AnyValidPrescription() {
        return new AnyPrescription();
    }

    private static AnyDateOnly AnyValidDispensedOn() {
        return Any.DateOnly();
    }

    private static AnyList<DispensedLine> AnyValidLines() {
        return Any.ListOf(new AnyDispensedLine()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReimbursementClaim(AnyClaimReference      reference,
                                  AnyPrescription        prescription,
                                  AnyDateOnly            dispensedOn,
                                  AnyList<DispensedLine> lines) {
        _reference          = reference;
        _referenceRecipe    = reference;
        _prescription       = prescription;
        _prescriptionRecipe = prescription;
        _dispensedOn        = dispensedOn;
        _dispensedOnRecipe  = dispensedOn;
        _lines              = lines;
        _linesRecipe        = lines;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReimbursementClaim(AnyReimbursementClaim from) {
        _reference          = from._reference;
        _referenceRecipe    = from._referenceRecipe;
        _prescription       = from._prescription;
        _prescriptionRecipe = from._prescriptionRecipe;
        _dispensedOn        = from._dispensedOn;
        _dispensedOnRecipe  = from._dispensedOnRecipe;
        _lines              = from._lines;
        _linesRecipe        = from._linesRecipe;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementClaim WithReference(ClaimReference value) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyClaimReference, AnyClaimReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementClaim WithReference(Func<AnyClaimReference, AnyClaimReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>prescription</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementClaim WithPrescription(Prescription value) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._prescription = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>prescription</c> is drawn from — <c>AnyValidPrescription()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPrescription, AnyPrescription&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementClaim WithPrescription(Func<AnyPrescription, AnyPrescription> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._prescriptionRecipe = configure(_prescriptionRecipe);
        copy._prescription       = copy._prescriptionRecipe;

        return copy;
    }

    /// <summary>Pins <c>dispensedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementClaim WithDispensedOn(DateOnly value) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._dispensedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dispensedOn</c> is drawn from — <c>AnyValidDispensedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementClaim WithDispensedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._dispensedOnRecipe = configure(_dispensedOnRecipe);
        copy._dispensedOn       = copy._dispensedOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>lines</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementClaim WithLines(IReadOnlyList<DispensedLine> value) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._lines = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lines</c> is drawn from — <c>AnyValidLines()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;DispensedLine&gt;, AnyList&lt;DispensedLine&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementClaim WithLines(Func<AnyList<DispensedLine>, AnyList<DispensedLine>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._linesRecipe = configure(_linesRecipe);
        copy._lines       = copy._linesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReimbursementClaim" />.</summary>
    public ReimbursementClaim Generate() {
        Prescription prescription = _prescription.Generate();

        // Dispensed within the prescription's own window, unless a test pinned the day.
        DateOnly dispensedOn = ReferenceEquals(_dispensedOn, _dispensedOnRecipe)
            ? _dispensedOnRecipe.Between(prescription.IssuedOn, prescription.LastDispensingDay).Generate()
            : _dispensedOn.Generate();

        // Only a dispensed prescription can be claimed: a drawn (Draft) one is signed and dispensed here.
        if (prescription.Status == PrescriptionStatus.Draft) { prescription.Sign(); }
        if (prescription.Status == PrescriptionStatus.Signed) { prescription.Dispense(dispensedOn); }

        // One dispensed line per distinct line of that very prescription, unless a test pinned the lines.
        IReadOnlyList<DispensedLine> lines;
        if (ReferenceEquals(_lines, _linesRecipe)) {
            int count = Any.Int32().Between(1, prescription.Lines.Count).Generate();
            lines = Any.SetOf(Any.ElementOf(prescription.Lines)).WithCount(count).Generate()
                       .Select(line => new AnyDispensedLine().WithPrescriptionLine(line).Generate())
                       .ToList();
        } else {
            lines = _lines.Generate();
        }

        return new ReimbursementClaim(_reference.Generate(), prescription, dispensedOn, lines);
    }

}
