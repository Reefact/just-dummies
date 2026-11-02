using System.Text.RegularExpressions;

namespace TaxRules.Domain;

/// <summary>
/// A taxing jurisdiction in ISO 3166 style: a country (<c>FR</c>) or one of its subdivisions
/// (<c>US-CA</c>, <c>CH-ZH</c>): two upper-case letters, optionally a hyphen and one to three
/// upper-case letters or digits.
/// </summary>
public sealed class JurisdictionCode : IEquatable<JurisdictionCode>
{
    private static readonly Regex Pattern = new("^[A-Z]{2}(-[A-Z0-9]{1,3})?$", RegexOptions.CultureInvariant);

    public JurisdictionCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Pattern.IsMatch(value))
        {
            throw new FormatException($"'{value}' is not a jurisdiction code such as 'FR' or 'US-CA'.");
        }

        Value = value;
    }

    public string Value { get; }

    public string Country => Value[..2];

    public bool IsSubdivision => Value.Length > 2;

    /// <summary>True for the same jurisdiction, or when this is a country and <paramref name="other"/> one of its subdivisions.</summary>
    public bool Covers(JurisdictionCode other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Equals(other) || (!IsSubdivision && other.Country == Country);
    }

    public bool Equals(JurisdictionCode? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as JurisdictionCode);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
