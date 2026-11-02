using System.Globalization;
using Clinical.Domain;
using Insurance.Domain;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>
///     A supplementary health policy: who is insured, over which period, with which deductible per claim, which
///     annual ceiling per calendar year, which guarantee table, and the bank account reimbursements are paid to.
/// </summary>
public sealed class SupplementaryPolicy {

    public const decimal MaximumAnnualCeiling = 100_000m;

    public SupplementaryPolicy(PolicyNumber          number,
                               Patient               insured,
                               CoveragePeriod        period,
                               Deductible            deductible,
                               Money                 annualCeiling,
                               ReimbursementSchedule schedule,
                               Party                 payee) {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(insured);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(deductible);
        ArgumentNullException.ThrowIfNull(annualCeiling);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(payee);

        int issueYear = int.Parse(number.Value.AsSpan(4, 4), CultureInfo.InvariantCulture);
        if (period.Start.Year < issueYear) {
            throw new ArgumentException($"Policy {number} was issued in {issueYear}; it cannot cover from {period.Start}.", nameof(period));
        }
        if (period.Start < insured.BirthDate) {
            throw new ArgumentException($"Coverage cannot start on {period.Start}, before the insured was born on {insured.BirthDate}.", nameof(period));
        }
        if (annualCeiling.Currency != Currency.EUR) {
            throw new ArgumentException("An annual ceiling is set in euros.", nameof(annualCeiling));
        }
        if (annualCeiling.Amount > MaximumAnnualCeiling) {
            throw new ArgumentOutOfRangeException(nameof(annualCeiling), annualCeiling.Amount, $"An annual ceiling is at most {MaximumAnnualCeiling} EUR.");
        }
        if (deductible.Amount >= annualCeiling.Amount) {
            throw new ArgumentException($"A deductible of {deductible.Amount} EUR would swallow an annual ceiling of {annualCeiling}.", nameof(deductible));
        }

        Number        = number;
        Insured       = insured;
        Period        = period;
        Deductible    = deductible;
        AnnualCeiling = annualCeiling;
        Schedule      = schedule;
        Payee         = payee;
        IssueYear     = issueYear;
    }

    public PolicyNumber          Number        { get; }
    public Patient               Insured       { get; }
    public CoveragePeriod        Period        { get; }
    public Deductible            Deductible    { get; }
    public Money                 AnnualCeiling { get; }
    public ReimbursementSchedule Schedule      { get; }
    public Party                 Payee         { get; }

    /// <summary>The year carried by the policy number.</summary>
    public int IssueYear { get; }

    public bool Covers(DateOnly day) => Period.Contains(day);

    public bool Insures(Nir nir) => Insured.Nir.Equals(nir);

}
