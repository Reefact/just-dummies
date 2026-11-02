using System.Globalization;
using System.Text.RegularExpressions;

namespace ClinicalTrial.Domain;

/// <summary>
/// The subject's screening number: ISO country code, three-digit site number, four-digit subject sequence
/// (e.g. <c>FR012-0042</c>). Site 000 and subject 0000 are never issued.
/// </summary>
public sealed partial class ScreeningNumber : IEquatable<ScreeningNumber>
{
    public ScreeningNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = Pattern().Match(value);
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a screening number (expected CCsss-nnnn, e.g. FR012-0042).");
        }

        int site = int.Parse(match.Groups["site"].Value, CultureInfo.InvariantCulture);
        int sequence = int.Parse(match.Groups["seq"].Value, CultureInfo.InvariantCulture);
        if (site == 0)
        {
            throw new ArgumentException("Site 000 is never issued.", nameof(value));
        }

        if (sequence == 0)
        {
            throw new ArgumentException("Subject sequence 0000 is never issued.", nameof(value));
        }

        Value = value;
        Country = match.Groups["country"].Value;
        Site = site;
        Sequence = sequence;
    }

    public string Value { get; }

    public string Country { get; }

    public int Site { get; }

    public int Sequence { get; }

    [GeneratedRegex(@"^(?<country>[A-Z]{2})(?<site>\d{3})-(?<seq>\d{4})$")]
    private static partial Regex Pattern();

    public bool Equals(ScreeningNumber? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as ScreeningNumber);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
