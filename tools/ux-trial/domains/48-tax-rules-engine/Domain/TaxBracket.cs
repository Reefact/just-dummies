namespace TaxRules.Domain;

/// <summary>A band of income [lower, upper) taxed at one rate; an open-ended band has no upper bound.</summary>
public sealed class TaxBracket : IEquatable<TaxBracket>
{
    public TaxBracket(Money lower, Money? upper, Rate rate)
    {
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(rate);
        if (upper is not null && upper.CompareTo(lower) <= 0)
        {
            throw new ArgumentException($"A bracket's upper bound ({upper}) is above its lower bound ({lower}).", nameof(upper));
        }

        Lower = lower;
        Upper = upper;
        Rate = rate;
    }

    public Money Lower { get; }

    public Money? Upper { get; }

    public Rate Rate { get; }

    public bool IsOpenEnded => Upper is null;

    public bool Contains(Money income)
    {
        ArgumentNullException.ThrowIfNull(income);
        return income.CompareTo(Lower) >= 0 && (Upper is null || income.CompareTo(Upper) < 0);
    }

    /// <summary>The unrounded tax due on the part of <paramref name="income"/> that falls in this band.</summary>
    public decimal TaxOn(Money income)
    {
        ArgumentNullException.ThrowIfNull(income);
        if (income.CompareTo(Lower) <= 0)
        {
            return 0m;
        }

        decimal top = Upper is null ? income.Amount : Math.Min(income.Amount, Upper.Amount);
        return (top - Lower.Amount) * Rate.Fraction;
    }

    public bool Equals(TaxBracket? other) =>
        other is not null && other.Lower.Equals(Lower) && Equals(other.Upper, Upper) && other.Rate.Equals(Rate);

    public override bool Equals(object? obj) => Equals(obj as TaxBracket);

    public override int GetHashCode() => HashCode.Combine(Lower, Upper, Rate);

    public override string ToString() => $"[{Lower}, {(Upper is null ? "∞" : Upper.ToString())}) at {Rate}";
}
