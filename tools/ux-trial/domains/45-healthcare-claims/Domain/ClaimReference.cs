using System.Globalization;
using System.Text.RegularExpressions;

namespace HealthcareClaims.Domain;

/// <summary>A claim reference such as <c>CLM-2026-004217</c>: the year it was opened and a six-digit sequence that never starts at zero.</summary>
public sealed class ClaimReference : IEquatable<ClaimReference> {

    private static readonly Regex Format = new(@"^CLM-(?<year>20\d{2})-(?<sequence>\d{6})$", RegexOptions.Compiled);

    public ClaimReference(string value) {
        ArgumentNullException.ThrowIfNull(value);
        Match match = Format.Match(value);
        if (!match.Success) {
            throw new FormatException($"'{value}' is not a claim reference (CLM-20yy-999999).");
        }
        if (match.Groups["sequence"].Value == "000000") {
            throw new ArgumentException("A claim sequence starts at 000001.", nameof(value));
        }

        Value = value;
        Year  = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
    }

    public string Value { get; }

    public int Year { get; }

    public bool Equals(ClaimReference? other) => other is not null && other.Value == Value;
    public override bool Equals(object? obj) => Equals(obj as ClaimReference);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
