using System.Text.RegularExpressions;

namespace ColdChain.Domain;

/// <summary>
/// A party licensed for Good Distribution Practice: ISO country code, <c>-GDP-</c>, the 5-digit licence number
/// (e.g. <c>FR-GDP-04217</c>). Carriers, forwarders and warehouses all take custody under such a licence.
/// </summary>
public sealed class CustodianId : IEquatable<CustodianId>
{
    public CustodianId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Regex.IsMatch(value, "^[A-Z]{2}-GDP-[0-9]{5}$"))
        {
            throw new FormatException($"'{value}' is not a GDP licence like FR-GDP-04217.");
        }

        if (value.EndsWith("00000", StringComparison.Ordinal))
        {
            throw new ArgumentException("Licence number 00000 is never issued.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string Country => Value[..2];

    public bool Equals(CustodianId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as CustodianId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
