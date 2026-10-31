namespace CatastropheLosses.Domain;

/// <summary>Per-occurrence contract terms: the insured retains the deductible, the insurer pays up to the limit.</summary>
public sealed class PolicyTerms
{
    public PolicyTerms(decimal deductible, decimal limit)
    {
        if (deductible < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(deductible), deductible, "Deductible cannot be negative.");
        }

        if (limit <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be strictly positive.");
        }

        Deductible = deductible;
        Limit = limit;
    }

    public decimal Deductible { get; }

    public decimal Limit { get; }

    /// <summary>The insured loss: min(max(groundUp - deductible, 0), limit).</summary>
    public decimal Apply(decimal groundUpLoss)
    {
        if (groundUpLoss < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(groundUpLoss), groundUpLoss, "A ground-up loss cannot be negative.");
        }

        return Math.Min(Math.Max(groundUpLoss - Deductible, 0m), Limit);
    }
}
