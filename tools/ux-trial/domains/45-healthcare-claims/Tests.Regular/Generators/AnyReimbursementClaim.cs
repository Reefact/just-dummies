using Clinical.Domain;

using HealthcareClaims.Domain;

using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementClaim" /> values, always on a dispensed prescription.
///     Written by hand after <c>any generate ReimbursementClaim</c>: three of its four parameters are tied to the prescription
///     (its status, its dispensing window, its own lines), which no per-parameter recipe can honour.
/// </summary>
public sealed class AnyReimbursementClaim : IAny<ReimbursementClaim> {

    private readonly IAny<ClaimReference>   _reference;
    private readonly SupplementaryPolicy?   _policy;
    private readonly Prescription?          _prescription;
    private readonly DateOnly?              _dispensedOn;
    private readonly AnyDispensedLine       _line;
    private readonly int?                   _lineCount;

    public AnyReimbursementClaim()
        : this(new AnyClaimReference(), null, null, null, new AnyDispensedLine(), null) { }

    private AnyReimbursementClaim(IAny<ClaimReference> reference,
                                  SupplementaryPolicy? policy,
                                  Prescription?        prescription,
                                  DateOnly?            dispensedOn,
                                  AnyDispensedLine     line,
                                  int?                 lineCount) {
        _reference    = reference;
        _policy       = policy;
        _prescription = prescription;
        _dispensedOn  = dispensedOn;
        _line         = line;
        _lineCount    = lineCount;
    }

    public AnyReimbursementClaim WithReference(ClaimReference reference) => new(Any.Fixed(reference), _policy, _prescription, _dispensedOn, _line, _lineCount);

    /// <summary>A claim by the policy's insured, dispensed within the policy period (unless <see cref="DispensedOn" /> says otherwise).</summary>
    public AnyReimbursementClaim ForPolicy(SupplementaryPolicy policy) => new(_reference, policy, _prescription, _dispensedOn, _line, _lineCount);

    /// <summary>A claim on this very prescription, dispensed within its dispensing window.</summary>
    public AnyReimbursementClaim ForPrescription(Prescription prescription) => new(_reference, _policy, prescription, _dispensedOn, _line, _lineCount);

    /// <summary>A claim dispensed on this day, on a prescription issued up to sixty days before.</summary>
    public AnyReimbursementClaim DispensedOn(DateOnly day) => new(_reference, _policy, _prescription, day, _line, _lineCount);

    /// <summary>Refines every dispensed line of the claim.</summary>
    public AnyReimbursementClaim WithLines(Func<AnyDispensedLine, AnyDispensedLine> configure) => new(_reference, _policy, _prescription, _dispensedOn, configure(_line), _lineCount);

    /// <summary>A claim dispensing exactly this many lines of its prescription.</summary>
    public AnyReimbursementClaim WithLineCount(int count) => new(_reference, _policy, _prescription, _dispensedOn, _line, count);

    public ReimbursementClaim Generate() {
        Prescription prescription;
        DateOnly     dispensedOn;
        if (_prescription is not null) {
            prescription = _prescription;
            dispensedOn  = _dispensedOn ?? Any.DateOnly().Between(prescription.IssuedOn, prescription.LastDispensingDay).Generate();
        } else {
            dispensedOn = _dispensedOn ?? (_policy is null
                              ? Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2029, 12, 31)).Generate()
                              : Any.DateOnly().Between(_policy.Period.Start, _policy.Period.End).Generate());
            DateOnly day = dispensedOn;
            AnyPrescription anyPrescription = new AnyPrescription().Dispensed().WithIssuedOn(issuedOn => issuedOn.Between(day.AddDays(-60), day));
            if (_policy is not null) { anyPrescription = anyPrescription.WithPatient(_policy.Insured); }
            prescription = anyPrescription.Generate();
        }

        int count = _lineCount ?? Any.Int32().Between(1, prescription.Lines.Count).Generate();
        List<DispensedLine> lines = Any.PermutationOf(prescription.Lines).Generate()
                                       .Take(count)
                                       .Select(line => _line.WithPrescriptionLine(line).Generate())
                                       .ToList();

        return new ReimbursementClaim(_reference.Generate(), prescription, dispensedOn, lines);
    }

}
