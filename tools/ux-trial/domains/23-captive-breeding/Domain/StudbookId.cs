using System.Text.RegularExpressions;

namespace CaptiveBreeding.Domain;

/// <summary>
/// The identifier a studbook keeper assigns to an individual: a two-to-four letter
/// institution prefix, a dash and a four-to-six digit number, e.g. <c>ZSL-01234</c>.
/// </summary>
public sealed partial class StudbookId : IEquatable<StudbookId>
{
    public StudbookId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format().IsMatch(value))
        {
            throw new FormatException(
                $"Studbook id '{value}' must be 2-4 uppercase letters, a dash, then 4-6 digits (e.g. ZSL-01234).");
        }

        Value = value;
    }

    public string Value { get; }

    public string Institution => Value[..Value.IndexOf('-')];

    public bool Equals(StudbookId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as StudbookId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    public static bool operator ==(StudbookId? left, StudbookId? right) => Equals(left, right);

    public static bool operator !=(StudbookId? left, StudbookId? right) => !Equals(left, right);

    [GeneratedRegex("^[A-Z]{2,4}-[0-9]{4,6}$")]
    private static partial Regex Format();
}
