namespace Payments.Domain;

/// <summary>An amount in a currency, with at most two decimal places.</summary>
public sealed class Money : IEquatable<Money> {

    public const decimal MaximumAmount = 999_999_999.99m;

    public Money(decimal amount, Currency currency) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaximumAmount);
        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException("An amount carries at most two decimal places.", nameof(amount));
        }
        if (!Enum.IsDefined(currency)) {
            throw new ArgumentOutOfRangeException(nameof(currency));
        }
        Amount   = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public Money Add(Money other) {
        if (other.Currency != Currency) {
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => Equals(obj as Money);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public override string ToString() => $"{Amount:0.00} {Currency}";

}
