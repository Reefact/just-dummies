using System.Text.RegularExpressions;

namespace ManuscriptReconstruction.Domain;

/// <summary>The conventional short name of a manuscript witness, such as "A", "B2" or "Vat.3225".</summary>
public sealed partial class WitnessSiglum : IEquatable<WitnessSiglum>
{
    public const int MaxLength = 12;

    public WitnessSiglum(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length is 0 or > MaxLength)
        {
            throw new ArgumentException($"A siglum has 1 to {MaxLength} characters.", nameof(value));
        }

        if (!SiglumPattern().IsMatch(value))
        {
            throw new ArgumentException("A siglum starts with an uppercase letter followed by letters, digits or dots.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(WitnessSiglum? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as WitnessSiglum);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z][A-Za-z0-9.]*$")]
    private static partial Regex SiglumPattern();
}
