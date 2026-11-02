namespace FxSettlement.Domain;

/// <summary>
/// A signed amount in one currency, carrying no more fractional digits than the currency's minor units
/// and at most 15 integer digits.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    /// <summary>Settlement systems carry at most 15 integer digits.</summary>
    public const decimal MaxMagnitude = 999_999_999_999_999m;

    public Money(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (amount < -MaxMagnitude || amount > MaxMagnitude)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, $"An amount lies within ±{MaxMagnitude}.");
        }

        if (!currency.Allows(amount))
        {
            throw new ArgumentException(
                $"{amount} has more fractional digits than {currency.Code} allows ({currency.MinorUnits}).",
                nameof(amount));
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Plus(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Minus(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Negated() => new(-Amount, Currency);

    public Money Abs() => new(Math.Abs(Amount), Currency);

    public bool Equals(Money? other) => other is not null && other.Amount == Amount && other.Currency.Equals(Currency);

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount.ToString("F" + Currency.MinorUnits, System.Globalization.CultureInfo.InvariantCulture)} {Currency.Code}";

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!other.Currency.Equals(Currency))
        {
            throw new InvalidOperationException($"Cannot combine {Currency.Code} with {other.Currency.Code}.");
        }
    }
}
