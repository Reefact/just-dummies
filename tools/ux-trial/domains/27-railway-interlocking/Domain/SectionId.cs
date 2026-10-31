using System.Text.RegularExpressions;

namespace RailwayInterlocking.Domain;

/// <summary>Identifier of a track circuit, e.g. "T101": one or two capital letters then one to four digits.</summary>
public sealed class SectionId : IEquatable<SectionId>
{
    private static readonly Regex Format = new("^[A-Z]{1,2}[0-9]{1,4}$", RegexOptions.CultureInvariant);

    public SectionId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value))
        {
            throw new ArgumentException($"Section id '{value}' must be 1-2 capital letters followed by 1-4 digits.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(SectionId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as SectionId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
