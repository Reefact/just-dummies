// Scaffolded by `any`, then rewritten by hand: five of the claim's constructor guards tie its parameters to the
// prescription (its status, its dispensing window, its lines), which no per-parameter recipe can honour.
// `any generate ReimbursementClaim --force` would overwrite it.

using System;
using System.Collections.Generic;
using System.Linq;

using Clinical.Domain;
using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>What the pharmacy dispensed on one line of the prescription, in the terms a claims test cares about.</summary>
public sealed record Dispensing(ReimbursementCategory Category, decimal UnitPrice, int Packs = 1, decimal? StatutoryReimbursement = null);

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementClaim" /> values: the claim of a dispensed prescription,
///     dispensed on a day inside its window, for lines of that very prescription.
/// </summary>
public sealed partial class AnyReimbursementClaim : IAny<ReimbursementClaim> {

    private IAny<ClaimReference> _reference;
    private IAny<Patient>        _insured;
    private Prescription?        _prescription;
    private DateOnly?            _dispensedOn;
    private Dispensing[]?        _dispensings;

    public AnyReimbursementClaim() {
        _reference = new AnyClaimReference();
        _insured   = new AnyPatient();
    }

    private AnyReimbursementClaim(AnyReimbursementClaim from) {
        _reference    = from._reference;
        _insured      = from._insured;
        _prescription = from._prescription;
        _dispensedOn  = from._dispensedOn;
        _dispensings  = from._dispensings;
    }

    public AnyReimbursementClaim WithReference(ClaimReference value) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>The prescription was written for this patient (the insured of the file the claim goes to).</summary>
    public AnyReimbursementClaim ForInsured(Patient insured) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._insured = Any.Fixed(insured);

        return copy;
    }

    /// <summary>The claim is about this prescription, which must be a dispensed one.</summary>
    public AnyReimbursementClaim ForPrescription(Prescription prescription) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._prescription = prescription;

        return copy;
    }

    /// <summary>Pins the dispensing day; without a pinned prescription the prescription is issued shortly before it.</summary>
    public AnyReimbursementClaim DispensedOn(DateOnly day) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._dispensedOn = day;

        return copy;
    }

    /// <summary>Pins the claim's lines, one per prescription line in order; a drawn prescription gets exactly that many.</summary>
    public AnyReimbursementClaim Dispensing(params Dispensing[] dispensings) {
        AnyReimbursementClaim copy = new AnyReimbursementClaim(this);

        copy._dispensings = dispensings;

        return copy;
    }

    public ReimbursementClaim Generate() {
        // Draw the prescription first, then everything that must sit inside it: the day, then the lines.
        DateOnly     dispensedOn;
        Prescription prescription;
        if (_prescription is not null) {
            prescription = _prescription;
            dispensedOn  = _dispensedOn ?? Any.DateOnly().Between(prescription.IssuedOn, prescription.LastDispensingDay).Generate();
        } else {
            dispensedOn = _dispensedOn ?? Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Generate();
            DateOnly issuedOn  = dispensedOn.AddDays(-Any.Int32().Between(0, 85).Generate());
            int      lineCount = _dispensings?.Length ?? Any.Int32().Between(1, 4).Generate();

            prescription = Any.Prescription()
                              .WithPatient(_insured.Generate())
                              .WithIssuedOn(issuedOn)
                              .WithLines(lines => lines.WithCount(lineCount))
                              .Dispensed()
                              .Generate();
        }

        IReadOnlyList<DispensedLine> dispensedLines = _dispensings is null
                                                          ? ArbitraryLinesOf(prescription)
                                                          : _dispensings.Select((dispensing, index) => Dispense(prescription.Lines[index], dispensing)).ToList();

        return new ReimbursementClaim(_reference.Generate(), prescription, dispensedOn, dispensedLines);
    }

    private static IReadOnlyList<DispensedLine> ArbitraryLinesOf(Prescription prescription) {
        // Some of the prescription's lines, each once.
        int count = Any.Int32().Between(1, prescription.Lines.Count).Generate();

        return Any.PermutationOf(prescription.Lines).Generate()
                  .Take(count)
                  .Select(line => Any.DispensedLine().WithPrescriptionLine(line).Generate())
                  .ToList();
    }

    private static DispensedLine Dispense(PrescriptionLine line, Dispensing dispensing) {
        return Any.DispensedLine()
                  .WithPrescriptionLine(line)
                  .WithCategory(dispensing.Category)
                  .WithPacks(dispensing.Packs)
                  .WithUnitPrice(new Money(dispensing.UnitPrice, Currency.EUR))
                  .WithStatutoryReimbursement(dispensing.StatutoryReimbursement is { } statutory ? new Money(statutory, Currency.EUR) : null)
                  .Generate();
    }

}
