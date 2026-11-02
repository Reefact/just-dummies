using System.Numerics;
using System.Text.RegularExpressions;

namespace TokenLedger.Domain;

/// <summary>
/// The fixed description of a fungible token: its name, ticker symbol, number of decimals
/// (1 token = 10^decimals base units) and the supply cap that minting can never exceed.
/// Converts between human amounts (exact decimals) and base units, exactly or not at all.
/// </summary>
public sealed partial class TokenDefinition : IEquatable<TokenDefinition>
{
    public const int MaxNameLength = 50;
    public const int MaxDecimals = 36;

    public TokenDefinition(string name, string symbol, int decimals, TokenAmount supplyCap)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(supplyCap);

        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength || name.Trim().Length != name.Length)
        {
            throw new ArgumentException($"A token name has 1 to {MaxNameLength} characters, without leading or trailing spaces.", nameof(name));
        }

        if (!SymbolPattern().IsMatch(symbol))
        {
            throw new ArgumentException("A symbol is 2 to 11 upper-case letters or digits, starting with a letter.", nameof(symbol));
        }

        if (decimals < 0 || decimals > MaxDecimals)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), decimals, $"A token has 0 to {MaxDecimals} decimals.");
        }

        BigInteger oneToken = BigInteger.Pow(10, decimals);
        if (supplyCap.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(supplyCap), supplyCap, "The supply cap is positive.");
        }

        if (!(supplyCap.BaseUnits % oneToken).IsZero)
        {
            throw new ArgumentException($"The supply cap is a whole number of tokens: a multiple of 10^{decimals} base units.", nameof(supplyCap));
        }

        Name = name;
        Symbol = symbol;
        Decimals = decimals;
        SupplyCap = supplyCap;
        OneToken = new TokenAmount(oneToken);
    }

    public string Name { get; }

    public string Symbol { get; }

    public int Decimals { get; }

    public TokenAmount SupplyCap { get; }

    /// <summary>10^decimals base units.</summary>
    public TokenAmount OneToken { get; }

    /// <summary>
    /// The base units of a human amount. Refused when negative, when finer than the token's
    /// decimals (after normalisation: <c>1.50</c> is <c>1.5</c>) or beyond 2^256 − 1 base units.
    /// </summary>
    public TokenAmount ToBaseUnits(ExactDecimal humanAmount)
    {
        ArgumentNullException.ThrowIfNull(humanAmount);
        if (humanAmount.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(humanAmount), humanAmount, "A token amount is not negative.");
        }

        if (humanAmount.Scale > Decimals)
        {
            throw new ArgumentException($"{humanAmount} has {humanAmount.Scale} fractional digits; {Symbol} has {Decimals} decimals.", nameof(humanAmount));
        }

        BigInteger units = humanAmount.ToScaledInteger(Decimals);
        if (units > TokenAmount.MaxBaseUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(humanAmount), humanAmount, "The amount exceeds 2^256 - 1 base units.");
        }

        return new TokenAmount(units);
    }

    /// <summary>Parses a human amount such as <c>1234.5678</c> straight into base units.</summary>
    public TokenAmount ParseAmount(string text) => ToBaseUnits(ExactDecimal.Parse(text));

    /// <summary>The exact human value of an amount of base units.</summary>
    public ExactDecimal ToHuman(TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        return new ExactDecimal(amount.BaseUnits, Decimals);
    }

    public string Format(TokenAmount amount) => $"{ToHuman(amount)} {Symbol}";

    public bool Equals(TokenDefinition? other) =>
        other is not null
        && Name == other.Name
        && Symbol == other.Symbol
        && Decimals == other.Decimals
        && SupplyCap.Equals(other.SupplyCap);

    public override bool Equals(object? obj) => Equals(obj as TokenDefinition);

    public override int GetHashCode() => HashCode.Combine(Name, Symbol, Decimals, SupplyCap);

    public override string ToString() => $"{Name} ({Symbol}, {Decimals} decimals)";

    [GeneratedRegex("^[A-Z][A-Z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex SymbolPattern();
}
