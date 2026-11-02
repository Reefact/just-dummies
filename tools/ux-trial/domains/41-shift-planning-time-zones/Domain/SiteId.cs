using System.Text.RegularExpressions;

namespace ShiftPlanning.Domain;

/// <summary>A site code: three upper-case letters, a dash, a two-digit number from 01 (e.g. <c>PAR-01</c>).</summary>
public sealed partial class SiteId : IEquatable<SiteId>
{
    public SiteId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Pattern().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not a site code (AAA-nn).");
        }

        if (value.EndsWith("-00", StringComparison.Ordinal))
        {
            throw new ArgumentException("Site numbers start at 01.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex("^[A-Z]{3}-[0-9]{2}$")]
    private static partial Regex Pattern();

    public bool Equals(SiteId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as SiteId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
