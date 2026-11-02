using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>
///     The insured's claim file under one supplementary policy: every claim submitted, its assessment against the
///     guarantee table, the deductible and the annual ceiling consumed by the other claims of the same year, and
///     its payment by SEPA credit transfer to the insured's account.
/// </summary>
public sealed class ClaimFile {

    private readonly List<ReimbursementClaim> _claims = [];

    public ClaimFile(SupplementaryPolicy policy) {
        ArgumentNullException.ThrowIfNull(policy);
        Policy = policy;
    }

    public SupplementaryPolicy Policy { get; }

    public IReadOnlyList<ReimbursementClaim> Claims => _claims;

    public ReimbursementClaim? Find(ClaimReference reference) {
        ArgumentNullException.ThrowIfNull(reference);
        return _claims.FirstOrDefault(claim => claim.Reference.Equals(reference));
    }

    /// <summary>What the approved and paid claims dispensed in that calendar year hold on the annual ceiling.</summary>
    public decimal ConsumedCeiling(int year) =>
        _claims.Where(claim => claim.ConsumesCeiling && claim.BenefitYear == year)
               .Sum(claim => claim.Assessment!.Payable);

    public decimal RemainingCeiling(int year) => Math.Max(0m, Policy.AnnualCeiling.Amount - ConsumedCeiling(year));

    /// <summary>Files a claim. Refused when the prescription is not the insured's, falls outside the coverage, or was already claimed.</summary>
    public void Submit(ReimbursementClaim claim) {
        ArgumentNullException.ThrowIfNull(claim);
        if (claim.Status != ClaimStatus.Submitted) {
            throw new InvalidOperationException($"Claim {claim.Reference} has already gone through assessment ({claim.Status}).");
        }
        if (Find(claim.Reference) is not null) {
            throw new InvalidOperationException($"Claim {claim.Reference} is already in the file.");
        }
        if (!Policy.Insures(claim.Prescription.Patient.Nir)) {
            throw new ClaimRefusedException(ClaimRefusalReason.PatientIsNotInsured,
                $"Prescription {claim.Prescription.Id} was written for {claim.Prescription.Patient.Nir}, not for the insured {Policy.Insured.Nir}.");
        }
        if (!Policy.Covers(claim.DispensedOn)) {
            throw new ClaimRefusedException(ClaimRefusalReason.OutsideCoveragePeriod,
                $"Policy {Policy.Number} covers {Policy.Period.Start} to {Policy.Period.End}, not {claim.DispensedOn}.");
        }
        if (_claims.Any(other => other.Prescription.Id == claim.Prescription.Id && other.Status != ClaimStatus.Rejected)) {
            throw new ClaimRefusedException(ClaimRefusalReason.PrescriptionAlreadyClaimed,
                $"Prescription {claim.Prescription.Id} is already claimed in this file.");
        }

        _claims.Add(claim);
    }

    /// <summary>
    ///     Computes what the policy owes: per line, the scheduled rate of the price paid, never more than what the
    ///     statutory scheme left at the insured's charge; then the deductible; then whatever exceeds the ceiling
    ///     left for the dispensing year.
    /// </summary>
    public ClaimAssessment Assess(ClaimReference reference) {
        ReimbursementClaim claim = Get(reference);
        if (claim.Status != ClaimStatus.Submitted) {
            throw new InvalidOperationException($"Claim {reference} is {claim.Status}; only a submitted claim is assessed.");
        }

        decimal covered = 0m;
        foreach (DispensedLine line in claim.Lines) {
            ReimbursementRate? rate = Policy.Schedule.RateFor(line.Category);
            if (rate is not null) {
                covered += Math.Min(rate.Of(line.PricePaid), line.RemainingCharge);
            }
        }

        decimal deductible       = Math.Min(Policy.Deductible.Amount, covered);
        decimal afterDeductible  = covered - deductible;
        decimal ceilingReduction = Math.Max(0m, afterDeductible - RemainingCeiling(claim.BenefitYear));

        ClaimAssessment assessment = new(covered, deductible, ceilingReduction);
        claim.MarkAssessed(assessment);

        return assessment;
    }

    /// <summary>Accepts an assessed claim. Refused when nothing is payable, or when claims approved since its assessment left too little ceiling.</summary>
    public void Approve(ClaimReference reference) {
        ReimbursementClaim claim = Get(reference);
        if (claim.Status != ClaimStatus.Assessed) {
            throw new InvalidOperationException($"Claim {reference} is {claim.Status}; only an assessed claim is approved.");
        }

        ClaimAssessment assessment = claim.Assessment!;
        if (assessment.Payable == 0m) {
            ClaimRefusalReason reason = assessment.IsCappedByCeiling ? ClaimRefusalReason.AnnualCeilingExhausted : ClaimRefusalReason.NothingToReimburse;
            throw new ClaimRefusedException(reason, $"Claim {reference} leaves nothing to reimburse.");
        }
        decimal remaining = RemainingCeiling(claim.BenefitYear);
        if (assessment.Payable > remaining) {
            throw new ClaimRefusedException(ClaimRefusalReason.AnnualCeilingExhausted,
                $"Claim {reference} would pay {assessment.Payable:0.00} EUR with only {remaining:0.00} EUR left under the {claim.BenefitYear} ceiling.");
        }

        claim.MarkApproved();
    }

    public void Reject(ClaimReference reference, ClaimRefusalReason reason) {
        if (!Enum.IsDefined(reason)) {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason.");
        }
        ReimbursementClaim claim = Get(reference);
        if (claim.Status != ClaimStatus.Assessed) {
            throw new InvalidOperationException($"Claim {reference} is {claim.Status}; only an assessed claim is rejected.");
        }

        claim.MarkRejected(reason);
    }

    /// <summary>Issues the SEPA credit transfer from the insurer's account to the insured's, for the approved amount.</summary>
    public CreditTransfer Pay(ClaimReference reference, Party insurer, DateOnly executionDate) {
        ArgumentNullException.ThrowIfNull(insurer);
        ReimbursementClaim claim = Get(reference);
        if (claim.Status != ClaimStatus.Approved) {
            throw new InvalidOperationException($"Claim {reference} is {claim.Status}; only an approved claim is paid.");
        }
        if (executionDate < claim.DispensedOn) {
            throw new ArgumentOutOfRangeException(nameof(executionDate), executionDate,
                $"A reimbursement cannot be executed before the dispensing date {claim.DispensedOn}.");
        }

        CreditTransfer transfer = new(
            reference.Value,
            insurer,
            Policy.Payee,
            new Money(claim.Assessment!.Payable, Currency.EUR),
            executionDate,
            TransferPriority.Normal,
            new RemittanceInformation($"Reimbursement {reference} policy {Policy.Number}"));
        claim.MarkPaid(transfer);

        return transfer;
    }

    private ReimbursementClaim Get(ClaimReference reference) {
        ArgumentNullException.ThrowIfNull(reference);
        return Find(reference) ?? throw new KeyNotFoundException($"No claim {reference} in the file of policy {Policy.Number}.");
    }

}
