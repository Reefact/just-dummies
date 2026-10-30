namespace Shop.Domain;

/// <summary>A non-negative amount in one of the shop's currencies, in cents (two decimal places at most).</summary>
public sealed class Money {

    public Money(decimal amount, string currency) {
        if (amount < 0m) {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "An amount cannot be negative.");
        }
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("An amount carries at most two decimal places.", nameof(amount));
        }

        Currency = currency switch {
            "EUR" or "USD" or "GBP" => currency,
            _                       => throw new ArgumentException($"Unsupported currency '{currency}'.", nameof(currency))
        };
        Amount = amount;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency) => new Money(0m, currency);

    public Money Plus(Money other) {
        EnsureSameCurrency(other);

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Times(int factor) => new Money(Amount * factor, Currency);

    /// <summary>Takes a percentage off, rounding the discount half away from zero to the cent.</summary>
    public Money Less(DiscountPercentage discount) {
        decimal reduction = decimal.Round(Amount * discount.Value / 100m, 2, MidpointRounding.AwayFromZero);

        return new Money(Amount - reduction, Currency);
    }

    private void EnsureSameCurrency(Money other) {
        if (other.Currency != Currency) {
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
        }
    }

    public override bool Equals(object? obj) => obj is Money other && other.Amount == Amount && other.Currency == Currency;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";

}
