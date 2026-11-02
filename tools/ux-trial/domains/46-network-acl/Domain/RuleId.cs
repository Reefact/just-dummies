using System.Text.RegularExpressions;

namespace NetworkAcl.Domain;

/// <summary>A rule's name within its policy: 1-32 characters, lower-case letters, digits and single hyphens, starting with a letter and not ending with a hyphen (e.g. <c>allow-dns-out</c>).</summary>
public sealed partial class RuleId : IEquatable<RuleId>
{
    public const int MaxLength = 32;

    public RuleId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0 || value.Length > MaxLength)
        {
            throw new ArgumentException($"A rule id has 1 to {MaxLength} characters, got {value.Length}.", nameof(value));
        }

        if (!Format().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a rule id: lower-case letters, digits and single hyphens, starting with a letter, not ending with a hyphen.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(RuleId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as RuleId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z](?:-?[a-z0-9])*$")]
    private static partial Regex Format();
}
