using System.Text.RegularExpressions;

namespace Serialisation.Domain;

/// <summary>A manufacturer's lot number (GS1 AI 10), restricted to upper-case letters, digits and hyphens.</summary>
public sealed class BatchNumber {

    public BatchNumber(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 20) {
            throw new ArgumentOutOfRangeException(nameof(value), "A batch number holds at most 20 characters.");
        }
        if (!Regex.IsMatch(value, "^[A-Z0-9-]+$")) {
            throw new ArgumentException($"Batch number '{value}' holds a character outside A-Z, 0-9 and '-'.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override bool Equals(object? obj) => obj is BatchNumber other && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
