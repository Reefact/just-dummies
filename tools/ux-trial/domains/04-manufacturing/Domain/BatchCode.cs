using System.Text.RegularExpressions;

namespace Manufacturing.Domain;

/// <summary>A production lot: plant code, two-digit year, ISO week (01-53) and a four-digit sequence, e.g. "LYN-2614-0042".</summary>
public sealed class BatchCode : IEquatable<BatchCode> {

    private static readonly Regex Format = new Regex(@"^(?<plant>[A-Z]{3})-(?<year>\d{2})(?<week>0[1-9]|[1-4]\d|5[0-3])-(?<sequence>\d{4})$");

    public BatchCode(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Match match = Format.Match(value);
        if (!match.Success) {
            throw new FormatException($"'{value}' is not a batch code (PPP-YYWW-NNNN).");
        }
        Value = value;
        Plant = match.Groups["plant"].Value;
        Week  = int.Parse(match.Groups["week"].Value);
    }

    public string Value { get; }
    public string Plant { get; }
    public int    Week  { get; }

    public bool Equals(BatchCode? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as BatchCode);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

}
