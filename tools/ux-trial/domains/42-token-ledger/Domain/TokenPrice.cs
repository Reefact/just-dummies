using System.Numerics;
using System.Text.RegularExpressions;

namespace TokenLedger.Domain;

/// <summary>
/// The price of one whole token in a quote currency (an ISO 4217-style code), carried exactly.
/// A quote never has more than <see cref="MaxPriceScale"/> fractional digits.
/// </summary>
public sealed partial class TokenPrice : IEquatable<TokenPrice>
{
    public const int MaxPriceScale = 18;

    public TokenPrice(ExactDecimal perToken, string quoteCurrency)
    {
        ArgumentNullException.ThrowIfNull(perToken);
        ArgumentNullException.ThrowIfNull(quoteCurrency);
        if (perToken.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(perToken), perToken, "A token price is positive.");
        }

        if (perToken.Scale > MaxPriceScale)
        {
            throw new ArgumentException($"A price has at most {MaxPriceScale} fractional digits; got {perToken}.", nameof(perToken));
        }

        if (!CurrencyPattern().IsMatch(quoteCurrency))
        {
            throw new ArgumentException("A quote currency is three upper-case letters.", nameof(quoteCurrency));
        }

        PerToken = perToken;
        QuoteCurrency = quoteCurrency;
    }

    public ExactDecimal PerToken { get; }

    public string QuoteCurrency { get; }

    /// <summary>The exact value of <paramref name="amount"/> of <paramref name="token"/> at this price.</summary>
    public ExactDecimal ValueOf(TokenAmount amount, TokenDefinition token)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(token);
        return token.ToHuman(amount).Multiply(PerToken);
    }

    /// <summary>
    /// The base units <paramref name="quoteAmount"/> buys at this price, rounded down to a whole
    /// base unit (the buyer never receives more than paid for).
    /// </summary>
    public TokenAmount TokensFor(ExactDecimal quoteAmount, TokenDefinition token)
    {
        ArgumentNullException.ThrowIfNull(quoteAmount);
        ArgumentNullException.ThrowIfNull(token);
        if (quoteAmount.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quoteAmount), quoteAmount, "A quote amount is not negative.");
        }

        // base units = quote / price × 10^decimals, with both sides brought to integers.
        BigInteger numerator = quoteAmount.Mantissa * BigInteger.Pow(10, PerToken.Scale + token.Decimals);
        BigInteger denominator = PerToken.Mantissa * BigInteger.Pow(10, quoteAmount.Scale);
        BigInteger units = BigInteger.Divide(numerator, denominator);
        if (units > TokenAmount.MaxBaseUnits)
        {
            throw new OverflowException($"{quoteAmount} {QuoteCurrency} buys more than 2^256 - 1 base units.");
        }

        return new TokenAmount(units);
    }

    public bool Equals(TokenPrice? other) => other is not null && PerToken.Equals(other.PerToken) && QuoteCurrency == other.QuoteCurrency;

    public override bool Equals(object? obj) => Equals(obj as TokenPrice);

    public override int GetHashCode() => HashCode.Combine(PerToken, QuoteCurrency);

    public override string ToString() => $"{PerToken} {QuoteCurrency}";

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();
}
