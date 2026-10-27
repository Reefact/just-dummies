namespace Trading.Domain;

/// <summary>A positive price, at most four decimal places, below an exchange-wide ceiling.</summary>
public sealed class Price : IComparable<Price> {

    public const decimal Ceiling = 1_000_000m;

    public Price(decimal amount) {
        Validate(amount);
        Amount = amount;
    }

    public decimal Amount { get; }

    private static void Validate(decimal amount) {
        if (amount <= 0m) {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "A price is strictly positive.");
        }
        if (amount >= Ceiling) {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "A price is below the exchange ceiling.");
        }
        if (decimal.Round(amount, 4) != amount) {
            throw new ArgumentException("A price has at most four decimal places.", nameof(amount));
        }
    }

    public int CompareTo(Price? other) {
        return other is null ? 1 : Amount.CompareTo(other.Amount);
    }

    public override bool Equals(object? obj) {
        return obj is Price other && other.Amount == Amount;
    }

    public override int GetHashCode() {
        return Amount.GetHashCode();
    }

    public override string ToString() {
        return Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

}
