using System.Globalization;
using System.Numerics;

namespace TokenLedger.Domain;

/// <summary>
/// A quantity of base units, as an unsigned 256-bit integer: [0, 2^256 − 1]. Arithmetic never
/// wraps: a result outside that range throws <see cref="OverflowException"/>.
/// </summary>
public sealed class TokenAmount : IEquatable<TokenAmount>, IComparable<TokenAmount>
{
    /// <summary>2^256 − 1, the largest amount a 256-bit word holds.</summary>
    public static readonly BigInteger MaxBaseUnits = (BigInteger.One << 256) - 1;

    public TokenAmount(BigInteger baseUnits)
    {
        if (baseUnits.Sign < 0 || baseUnits > MaxBaseUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUnits), baseUnits, "An amount of base units lies in [0, 2^256 - 1].");
        }

        BaseUnits = baseUnits;
    }

    public static TokenAmount Zero { get; } = new(BigInteger.Zero);

    /// <summary>2^256 − 1 base units; as an allowance it means "unlimited".</summary>
    public static TokenAmount Max { get; } = new(MaxBaseUnits);

    public BigInteger BaseUnits { get; }

    public bool IsZero => BaseUnits.IsZero;

    public TokenAmount Add(TokenAmount other)
    {
        ArgumentNullException.ThrowIfNull(other);
        BigInteger sum = BaseUnits + other.BaseUnits;
        if (sum > MaxBaseUnits)
        {
            throw new OverflowException($"{this} + {other} exceeds 2^256 - 1 base units.");
        }

        return new TokenAmount(sum);
    }

    public TokenAmount Subtract(TokenAmount other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.BaseUnits > BaseUnits)
        {
            throw new OverflowException($"{this} - {other} would be negative.");
        }

        return new TokenAmount(BaseUnits - other.BaseUnits);
    }

    public TokenAmount MultiplyBy(ulong factor)
    {
        BigInteger product = BaseUnits * factor;
        if (product > MaxBaseUnits)
        {
            throw new OverflowException($"{this} x {factor} exceeds 2^256 - 1 base units.");
        }

        return new TokenAmount(product);
    }

    /// <summary>Whether <c>this + other</c> stays within 2^256 − 1.</summary>
    public bool CanAdd(TokenAmount other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return BaseUnits + other.BaseUnits <= MaxBaseUnits;
    }

    public int CompareTo(TokenAmount? other) => other is null ? 1 : BaseUnits.CompareTo(other.BaseUnits);

    public bool Equals(TokenAmount? other) => other is not null && BaseUnits == other.BaseUnits;

    public override bool Equals(object? obj) => Equals(obj as TokenAmount);

    public override int GetHashCode() => BaseUnits.GetHashCode();

    public override string ToString() => BaseUnits.ToString(CultureInfo.InvariantCulture);
}
