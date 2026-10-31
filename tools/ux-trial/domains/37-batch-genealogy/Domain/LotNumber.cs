using System.Text.RegularExpressions;

namespace BatchGenealogy.Domain;

/// <summary>
/// A lot number such as <c>API-240117</c>, or a sub-lot number such as <c>API-240117.03</c>:
/// a 2-4 letter site/material prefix, a 6 digit sequence and an optional 2 digit sub-lot index (01-99).
/// </summary>
public sealed partial class LotNumber : IEquatable<LotNumber>
{
    public LotNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = Pattern().Match(value);
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a lot number: expected e.g. API-240117 or API-240117.03.");
        }

        if (match.Groups["sub"].Success && match.Groups["sub"].Value == "00")
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A sub-lot index runs from 01 to 99.");
        }

        Value = value;
        BaseLot = match.Groups["base"].Value;
        SubLotIndex = match.Groups["sub"].Success ? int.Parse(match.Groups["sub"].Value, System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    public string Value { get; }

    /// <summary>The lot number without its sub-lot suffix.</summary>
    public string BaseLot { get; }

    public int? SubLotIndex { get; }

    public bool IsSubLot => SubLotIndex is not null;

    /// <summary>Builds the number of sub-lot <paramref name="index"/> of this lot.</summary>
    public LotNumber SubLot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 99);
        return new LotNumber($"{BaseLot}.{index:00}");
    }

    public bool Equals(LotNumber? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as LotNumber);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^(?<base>[A-Z]{2,4}-[0-9]{6})(\.(?<sub>[0-9]{2}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
