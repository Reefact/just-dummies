using System.Text.RegularExpressions;

namespace FxSettlement.Domain;

/// <summary>
/// An ISO 4217 currency and its minor units: the number of decimal places an amount in that
/// currency may carry (JPY 0, USD 2, KWD 3, CLF 4).
/// </summary>
public sealed partial class Currency : IEquatable<Currency>
{
    public const int MaxMinorUnits = 4;

    private static readonly Dictionary<string, int> IsoMinorUnits = new(StringComparer.Ordinal)
    {
        ["AUD"] = 2,
        ["BHD"] = 3,
        ["CAD"] = 2,
        ["CHF"] = 2,
        ["CLF"] = 4,
        ["CLP"] = 0,
        ["EUR"] = 2,
        ["GBP"] = 2,
        ["JOD"] = 3,
        ["JPY"] = 0,
        ["KRW"] = 0,
        ["KWD"] = 3,
        ["NOK"] = 2,
        ["OMR"] = 3,
        ["SEK"] = 2,
        ["TND"] = 3,
        ["USD"] = 2,
    };

    public Currency(string code, int minorUnits)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!CodePattern().IsMatch(code))
        {
            throw new FormatException($"'{code}' is not an ISO 4217 alphabetic code (three upper-case letters).");
        }

        if (code[0] == 'X')
        {
            throw new ArgumentException($"{code}: X-codes (metals, funds, testing) are not settlement currencies.", nameof(code));
        }

        if (minorUnits < 0 || minorUnits > MaxMinorUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(minorUnits), minorUnits, $"Minor units lie in [0, {MaxMinorUnits}].");
        }

        if (IsoMinorUnits.TryGetValue(code, out int iso) && iso != minorUnits)
        {
            throw new ArgumentException($"ISO 4217 gives {code} {iso} minor units, not {minorUnits}.", nameof(minorUnits));
        }

        Code = code;
        MinorUnits = minorUnits;
    }

    public static Currency Usd { get; } = new("USD", 2);

    public static Currency Eur { get; } = new("EUR", 2);

    public static Currency Gbp { get; } = new("GBP", 2);

    public static Currency Chf { get; } = new("CHF", 2);

    public static Currency Cad { get; } = new("CAD", 2);

    public static Currency Jpy { get; } = new("JPY", 0);

    public static Currency Krw { get; } = new("KRW", 0);

    public static Currency Kwd { get; } = new("KWD", 3);

    public static Currency Bhd { get; } = new("BHD", 3);

    public static Currency Tnd { get; } = new("TND", 3);

    public static Currency Clf { get; } = new("CLF", 4);

    public string Code { get; }

    public int MinorUnits { get; }

    /// <summary>The smallest amount the currency can express: 1 for JPY, 0.01 for USD, 0.001 for KWD.</summary>
    public decimal SmallestUnit => 1m / Pow10(MinorUnits);

    /// <summary>A currency from the ISO table this desk knows; unknown codes are refused.</summary>
    public static Currency Iso(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!IsoMinorUnits.TryGetValue(code, out int minorUnits))
        {
            throw new ArgumentException($"{code} is not in the desk's currency table.", nameof(code));
        }

        return new Currency(code, minorUnits);
    }

    /// <summary>Whether the amount has no more fractional digits than the currency allows (trailing zeros do not count).</summary>
    public bool Allows(decimal amount) => decimal.Round(amount, MinorUnits) == amount;

    /// <summary>Rounds an amount to the currency's minor units with the given midpoint mode.</summary>
    public decimal Round(decimal amount, MidpointRounding mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown midpoint rounding mode.");
        }

        return decimal.Round(amount, MinorUnits, mode);
    }

    public bool Equals(Currency? other) => other is not null && other.Code == Code && other.MinorUnits == MinorUnits;

    public override bool Equals(object? obj) => Equals(obj as Currency);

    public override int GetHashCode() => HashCode.Combine(Code, MinorUnits);

    public override string ToString() => Code;

    internal static decimal Pow10(int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
        {
            result *= 10m;
        }

        return result;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CodePattern();
}
