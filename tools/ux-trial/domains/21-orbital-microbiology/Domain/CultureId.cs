using System.Text.RegularExpressions;

namespace OrbitalMicrobiology.Domain;

/// <summary>Identifies a culture: the chamber it grows in and its sequence number, e.g. <c>CH03-C0042</c>.</summary>
public sealed partial class CultureId : IEquatable<CultureId>
{
    public CultureId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = Format().Match(value);
        if (!match.Success)
        {
            throw new ArgumentException($"A culture id reads 'CHnn-Cnnnn', got '{value}'.", nameof(value));
        }

        int chamber = int.Parse(match.Groups["chamber"].Value, System.Globalization.CultureInfo.InvariantCulture);
        if (chamber is < 1 or > MaxChamber)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"The station has chambers 01 to {MaxChamber:00}.");
        }

        Value = value;
        Chamber = chamber;
    }

    public const int MaxChamber = 12;

    public string Value { get; }

    public int Chamber { get; }

    public bool Equals(CultureId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as CultureId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex(@"^CH(?<chamber>\d{2})-C\d{4}$")]
    private static partial Regex Format();
}
