namespace HealthcareClaims.Domain;

/// <summary>
///     The outcome of assessing a claim, in euros: what the guarantee table covers, less the policy's deductible,
///     less what no longer fits under the annual ceiling.
/// </summary>
public sealed class ClaimAssessment : IEquatable<ClaimAssessment> {

    public const decimal MaximumAmount = 1_000_000m;

    public ClaimAssessment(decimal coveredAmount, decimal deductibleApplied, decimal ceilingReduction) {
        ValidateAmount(coveredAmount, nameof(coveredAmount));
        ValidateAmount(deductibleApplied, nameof(deductibleApplied));
        ValidateAmount(ceilingReduction, nameof(ceilingReduction));
        if (deductibleApplied + ceilingReduction > coveredAmount) {
            throw new ArgumentException(
                $"The deductible ({deductibleApplied}) and the ceiling reduction ({ceilingReduction}) cannot exceed the covered amount ({coveredAmount}).",
                nameof(ceilingReduction));
        }

        CoveredAmount     = coveredAmount;
        DeductibleApplied = deductibleApplied;
        CeilingReduction  = ceilingReduction;
    }

    public decimal CoveredAmount     { get; }
    public decimal DeductibleApplied { get; }
    public decimal CeilingReduction  { get; }

    public decimal Payable => CoveredAmount - DeductibleApplied - CeilingReduction;

    public bool IsCappedByCeiling => CeilingReduction > 0m;

    private static void ValidateAmount(decimal amount, string parameterName) {
        if (amount < 0m || amount > MaximumAmount) {
            throw new ArgumentOutOfRangeException(parameterName, amount, $"An assessed amount lies between 0 and {MaximumAmount}.");
        }
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("An assessed amount is in cents.", parameterName);
        }
    }

    public bool Equals(ClaimAssessment? other) =>
        other is not null
        && other.CoveredAmount == CoveredAmount
        && other.DeductibleApplied == DeductibleApplied
        && other.CeilingReduction == CeilingReduction;

    public override bool Equals(object? obj) => Equals(obj as ClaimAssessment);
    public override int GetHashCode() => HashCode.Combine(CoveredAmount, DeductibleApplied, CeilingReduction);
    public override string ToString() => $"{Payable:0.00} EUR payable";

}
