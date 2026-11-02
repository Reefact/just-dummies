# Healthcare claims — model

Namespace `HealthcareClaims.Domain` (`Domain/`). A supplementary health insurer reimburses dispensed prescriptions.
The domain is **transverse**: it references three other trial models as projects and reuses their types unchanged —
`Insurance.Domain` (03-insurance: `PolicyNumber`, `CoveragePeriod`, `Deductible`), `Clinical.Domain` (08-patient-record:
`Nir`, `Patient`, `Prescription`, `PrescriptionLine`) and `Payments.Domain` (01-payments: `Money`, `Currency`, `Iban`, `Party`,
`CreditTransfer`, `RemittanceInformation`). Their own guards apply as they stand (e.g. a `CoveragePeriod` ends strictly after it
starts and lasts at most one year; a `Deductible` is 0..5 000 in cents; a `Money` is > 0 with two decimals; a `Prescription`
is dispensable from its issue date to three months later, and only once `Sign()`ed). No type name clashes across the three.
All amounts here are euros.

## Value objects
- `ReimbursementRate(percent)` — (0, 100], at most one decimal. `Of(amount)`: amount ≥ 0, rate applied and rounded **down** to the cent.
- `ReimbursementSchedule(rates)` — the guarantee table, `IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate>`: at least one
  entry, defined categories, no null rate. A category absent from the table is not covered. `Covers`, `RateFor` (null when absent).
  Equality by entries.
- `ClaimReference(string)` — `CLM-20yy-nnnnnn` (`FormatException` otherwise); sequence `000000` refused (`ArgumentException`). Exposes `Year`.
- `ClaimAssessment(coveredAmount, deductibleApplied, ceilingReduction)` — each in [0, 1 000 000] and in cents;
  **deductibleApplied + ceilingReduction ≤ coveredAmount**. `Payable` = covered − deductible − reduction; `IsCappedByCeiling`.
- Enums `ReimbursementCategory` (Irreplaceable, Major, Moderate, Low, Unlisted — the statutory category), `ClaimStatus`
  (Submitted, Assessed, Approved, Rejected, Paid), `ClaimRefusalReason` (PatientIsNotInsured, OutsideCoveragePeriod,
  PrescriptionAlreadyClaimed, NothingToReimburse, AnnualCeilingExhausted, MissingDocuments, SuspectedFraud).

## Entities
- `DispensedLine(prescriptionLine, category, packs, unitPrice, statutoryReimbursement?)` — defined category; packs 1..12;
  unit price in EUR, at most 5 000; `PricePaid` = unit price × packs. The statutory reimbursement is optional, and when present:
  **not for an `Unlisted` medicine**, in EUR, and **at most the price paid** (equal allowed). `RemainingCharge` = paid − statutory.
- `SupplementaryPolicy(number, insured, period, deductible, annualCeiling, schedule, payee)` — `insured` is a Clinical `Patient`,
  `payee` a Payments `Party` (the insured's IBAN). **The period cannot start before the year carried by the policy number**
  (`IssueYear`) **nor before the insured's birth date**; ceiling in EUR, at most 100 000; **deductible strictly below the ceiling**.
  The deductible applies to each claim, the ceiling to each **calendar year** (a period may straddle two). `Covers(day)`, `Insures(nir)`.
- `ReimbursementClaim(reference, prescription, dispensedOn, lines)` — **the prescription must be `Dispensed`**; **dispensedOn within
  [IssuedOn, LastDispensingDay]** of the prescription (`ArgumentOutOfRangeException`); 1 to `prescription.Lines.Count` lines, **each
  dispensing a line of that very prescription (same instance), none twice**. Born `Submitted`; `Assessment`, `RejectionReason`,
  `Payment` are filled as the file moves it on. `BenefitYear` = year of dispensedOn; `TotalPaid`, `TotalRemainingCharge`;
  `ConsumesCeiling` when Approved or Paid.

## Aggregate: `ClaimFile(policy)` — the insured's claim file
- `Submit(claim)` — the claim must still be Submitted and its reference new to the file (`InvalidOperationException`); then refused
  with `ClaimRefusedException` (carrying a `ClaimRefusalReason`), in order: the prescription's patient NIR is not the insured's
  (`PatientIsNotInsured`); dispensedOn outside the coverage period (`OutsideCoveragePeriod`); the same prescription id is already in
  a claim that is not Rejected (`PrescriptionAlreadyClaimed`).
- `Assess(reference)` — Submitted → Assessed. Per line: the scheduled rate of the price paid (0 when the category is not covered),
  **never more than the line's remaining charge**; summed; minus the deductible (at most the covered amount); minus whatever exceeds
  the ceiling remaining for the benefit year. Returns the `ClaimAssessment`.
- `Approve(reference)` — Assessed → Approved. Refused (`ClaimRefusedException`) when nothing is payable (`AnnualCeilingExhausted`
  if the ceiling cut it to zero, else `NothingToReimburse`), or when claims approved since the assessment leave less ceiling than
  the payable amount (`AnnualCeilingExhausted`).
- `Reject(reference, reason)` — Assessed → Rejected, reason recorded; an undefined reason is `ArgumentOutOfRangeException`.
- `Pay(reference, insurer, executionDate)` — Approved → Paid; executionDate not before dispensedOn. Builds a Payments `CreditTransfer`:
  end-to-end id = the reference, insurer → policy payee, payable amount in EUR, `Normal` priority, remittance
  `Reimbursement <reference> policy <number>`. The transfer's own guards apply (business day, distinct accounts) and leave the claim
  Approved when they throw.
- `ConsumedCeiling(year)` — sum of payable amounts of Approved and Paid claims of that benefit year; `RemainingCeiling(year)` ≥ 0.
- `Find(reference)` (null if absent); every other lookup of an unknown reference throws `KeyNotFoundException`. A transition from
  the wrong status throws `InvalidOperationException`.
