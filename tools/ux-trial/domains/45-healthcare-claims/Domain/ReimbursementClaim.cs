using Clinical.Domain;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>
///     A request to reimburse one dispensed prescription: which lines were dispensed, on which day, at which price.
///     A claim is born <see cref="ClaimStatus.Submitted"/>; only the insured's <see cref="ClaimFile"/> moves it on.
/// </summary>
public sealed class ReimbursementClaim {

    private readonly List<DispensedLine> _lines;

    public ReimbursementClaim(ClaimReference reference, Prescription prescription, DateOnly dispensedOn, IReadOnlyList<DispensedLine> lines) {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(prescription);
        ArgumentNullException.ThrowIfNull(lines);
        if (prescription.Status != PrescriptionStatus.Dispensed) {
            throw new ArgumentException($"Only a dispensed prescription can be claimed; this one is {prescription.Status}.", nameof(prescription));
        }
        if (dispensedOn < prescription.IssuedOn || dispensedOn > prescription.LastDispensingDay) {
            throw new ArgumentOutOfRangeException(nameof(dispensedOn), dispensedOn,
                $"A prescription issued on {prescription.IssuedOn} is dispensed between that day and {prescription.LastDispensingDay}.");
        }
        if (lines.Count == 0 || lines.Count > prescription.Lines.Count) {
            throw new ArgumentException($"A claim has between 1 and {prescription.Lines.Count} lines, one per prescription line.", nameof(lines));
        }
        HashSet<PrescriptionLine> seen = new(ReferenceEqualityComparer.Instance);
        foreach (DispensedLine line in lines) {
            ArgumentNullException.ThrowIfNull(line, nameof(lines));
            if (!prescription.Lines.Contains(line.PrescriptionLine)) {
                throw new ArgumentException($"{line.PrescriptionLine.Molecule} is not a line of prescription {prescription.Id}.", nameof(lines));
            }
            if (!seen.Add(line.PrescriptionLine)) {
                throw new ArgumentException($"{line.PrescriptionLine.Molecule} is dispensed twice in the same claim.", nameof(lines));
            }
        }

        Reference    = reference;
        Prescription = prescription;
        DispensedOn  = dispensedOn;
        _lines       = [.. lines];
        Status       = ClaimStatus.Submitted;
    }

    public ClaimReference Reference    { get; }
    public Prescription   Prescription { get; }
    public DateOnly       DispensedOn  { get; }

    public IReadOnlyList<DispensedLine> Lines => _lines;

    public ClaimStatus Status { get; private set; }

    /// <summary>Set once the claim is assessed.</summary>
    public ClaimAssessment? Assessment { get; private set; }

    /// <summary>Set once the claim is rejected.</summary>
    public ClaimRefusalReason? RejectionReason { get; private set; }

    /// <summary>Set once the claim is paid.</summary>
    public CreditTransfer? Payment { get; private set; }

    /// <summary>The calendar year whose annual ceiling the claim consumes: the year of the dispensing date.</summary>
    public int BenefitYear => DispensedOn.Year;

    /// <summary>What the insured paid at the pharmacy for the whole claim.</summary>
    public decimal TotalPaid => _lines.Sum(line => line.PricePaid);

    /// <summary>What remained at the insured's charge after the statutory scheme.</summary>
    public decimal TotalRemainingCharge => _lines.Sum(line => line.RemainingCharge);

    /// <summary>True when the claim holds on the annual ceiling: approved or already paid.</summary>
    public bool ConsumesCeiling => Status is ClaimStatus.Approved or ClaimStatus.Paid;

    internal void MarkAssessed(ClaimAssessment assessment) {
        EnsureStatus(ClaimStatus.Submitted, "assessed");
        Assessment = assessment;
        Status     = ClaimStatus.Assessed;
    }

    internal void MarkApproved() {
        EnsureStatus(ClaimStatus.Assessed, "approved");
        Status = ClaimStatus.Approved;
    }

    internal void MarkRejected(ClaimRefusalReason reason) {
        EnsureStatus(ClaimStatus.Assessed, "rejected");
        RejectionReason = reason;
        Status          = ClaimStatus.Rejected;
    }

    internal void MarkPaid(CreditTransfer payment) {
        EnsureStatus(ClaimStatus.Approved, "paid");
        Payment = payment;
        Status  = ClaimStatus.Paid;
    }

    private void EnsureStatus(ClaimStatus expected, string transition) {
        if (Status != expected) {
            throw new InvalidOperationException($"Only a {expected} claim can be {transition}; claim {Reference} is {Status}.");
        }
    }

}
