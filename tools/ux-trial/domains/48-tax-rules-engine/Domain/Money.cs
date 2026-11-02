using System.Globalization;

namespace TaxRules.Domain;

/// <summary>A non-negative amount in the schedule's currency, expressed to the cent.</summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public const decimal MaxAmount = 999_999_999_999.99m;

    public Money(decimal amount)
    {
        if (amount < 0m || amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, $"An amount lies in [0, {MaxAmount}].");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException($"An amount is expressed to the cent; got {amount}.", nameof(amount));
        }

        Amount = amount;
    }

    public static Money Zero { get; } = new(0m);

    public decimal Amount { get; }

    public bool IsZero => Amount == 0m;

    /// <summary>Rounds a computed amount to the cent, half away from zero.</summary>
    public static Money RoundedFrom(decimal amount) => new(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

    public static Money Min(Money left, Money right) => left.CompareTo(right) <= 0 ? left : right;

    public static Money Max(Money left, Money right) => left.CompareTo(right) >= 0 ? left : right;

    public Money Plus(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Money(Amount + other.Amount);
    }

    /// <summary>The difference; refused when it would be negative.</summary>
    public Money Minus(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Amount > Amount)
        {
            throw new InvalidOperationException($"Cannot take {other} from {this}: an amount is never negative.");
        }

        return new Money(Amount - other.Amount);
    }

    /// <summary>The difference, or zero when <paramref name="other"/> is larger.</summary>
    public Money MinusOrZero(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Amount >= Amount ? Zero : new Money(Amount - other.Amount);
    }

    public Money Times(Rate rate)
    {
        ArgumentNullException.ThrowIfNull(rate);
        return RoundedFrom(Amount * rate.Fraction);
    }

    public Money Times(int factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);
        return new Money(Amount * factor);
    }

    public int CompareTo(Money? other) => other is null ? 1 : Amount.CompareTo(other.Amount);

    public bool Equals(Money? other) => other is not null && other.Amount == Amount;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => Amount.GetHashCode();

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}
