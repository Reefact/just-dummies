namespace Serialisation.Domain;

/// <summary>The price a payer reimburses for one pack, in euros, to the cent.</summary>
public sealed class ReimbursementPrice {

    public ReimbursementPrice(decimal amount) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, 100_000m);
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("A reimbursement price is expressed to the cent.", nameof(amount));
        }

        Amount = amount;
    }

    public decimal Amount { get; }

    public override bool Equals(object? obj) => obj is ReimbursementPrice other && other.Amount == Amount;
    public override int GetHashCode() => Amount.GetHashCode();

}
