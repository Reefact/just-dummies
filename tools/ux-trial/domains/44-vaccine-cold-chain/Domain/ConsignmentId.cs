using System.Globalization;
using System.Text.RegularExpressions;

namespace ColdChain.Domain;

/// <summary>A cold-chain consignment number: <c>VCC-</c>, the year of booking, a 6-digit sequence (e.g. <c>VCC-2026-000417</c>).</summary>
public sealed class ConsignmentId : IEquatable<ConsignmentId>
{
    private static readonly Regex Format = new(@"^VCC-(\d{4})-(\d{6})$", RegexOptions.CultureInvariant);

    public ConsignmentId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = Format.Match(value);
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a consignment number like VCC-2026-000417.");
        }

        int year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        if (year < 2000 || year > 2099)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The booking year lies between 2000 and 2099.");
        }

        if (match.Groups[2].Value == "000000")
        {
            throw new ArgumentException("The sequence number starts at 000001.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public int Year => int.Parse(Value.Substring(4, 4), CultureInfo.InvariantCulture);

    public bool Equals(ConsignmentId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as ConsignmentId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
