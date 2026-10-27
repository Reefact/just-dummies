namespace Insurance.Domain;

/// <summary>What the policyholder pays on each claim before the insurer pays anything.</summary>
public sealed class Deductible {

    public const decimal Ceiling = 5_000m;

    public Deductible(decimal amount) {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, Ceiling);
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("A deductible is an amount in cents.", nameof(amount));
        }

        Amount = amount;
    }

    public decimal Amount { get; }

}
