namespace HealthcareClaims.Domain;

/// <summary>
///     The statutory reimbursement category of a dispensed medicine, which the supplementary insurer's
///     guarantee table is keyed on. <see cref="Unlisted"/> medicines are not reimbursed by the statutory scheme at all.
/// </summary>
public enum ReimbursementCategory {
    Irreplaceable,
    Major,
    Moderate,
    Low,
    Unlisted
}

public enum ClaimStatus {
    Submitted,
    Assessed,
    Approved,
    Rejected,
    Paid
}

/// <summary>Why a claim is refused at submission, at approval, or rejected by the assessor.</summary>
public enum ClaimRefusalReason {
    PatientIsNotInsured,
    OutsideCoveragePeriod,
    PrescriptionAlreadyClaimed,
    NothingToReimburse,
    AnnualCeilingExhausted,
    MissingDocuments,
    SuspectedFraud
}
